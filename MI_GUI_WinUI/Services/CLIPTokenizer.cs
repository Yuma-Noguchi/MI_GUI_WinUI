using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;

namespace MI_GUI_WinUI.Services
{
    public class CLIPTokenizer
    {
        private readonly Dictionary<string, int> _vocab;
        private readonly Dictionary<int, string> _vocabReverse;
        private readonly Dictionary<string, string> _merges;
        private const string DefaultVocabFile = "vocab.json";
        private const string DefaultMergesFile = "merges.txt";
        private const int UnkToken = 49407;  // CLIP's unknown token ID
        private readonly Regex _pattern = new(@"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+");

        public CLIPTokenizer(string modelPath)
        {
            if (string.IsNullOrEmpty(modelPath))
                throw new ArgumentException("Model path cannot be empty", nameof(modelPath));

            var vocabPath = Path.Combine(modelPath, DefaultVocabFile);
            var mergesPath = Path.Combine(modelPath, DefaultMergesFile);

            if (!File.Exists(vocabPath))
                throw new FileNotFoundException("Vocabulary file not found", vocabPath);
            if (!File.Exists(mergesPath))
                throw new FileNotFoundException("Merges file not found", mergesPath);

            try
            {
                // Load vocabulary
                var vocabJson = File.ReadAllText(vocabPath);
                _vocab = JsonConvert.DeserializeObject<Dictionary<string, int>>(vocabJson)
                    ?? throw new InvalidOperationException("Failed to parse vocabulary file");

                _vocabReverse = _vocab.ToDictionary(x => x.Value, x => x.Key);

                // Load merges
                var mergeLines = File.ReadAllLines(mergesPath).Skip(1); // Skip header
                _merges = mergeLines.ToDictionary(
                    x => x,
                    x => x,
                    StringComparer.Ordinal);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse tokenizer files", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize tokenizer", ex);
            }
        }

        public long[] Tokenize(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Text cannot be empty", nameof(text));
            if (maxLength <= 0)
                throw new ArgumentException("Max length must be positive", nameof(maxLength));

            try
            {
                var tokens = new List<long>();
                var matches = _pattern.Matches(text.ToLowerInvariant());

                foreach (Match match in matches)
                {
                    var token = TokenizePiece(match.Value);
                    tokens.AddRange(token);

                    if (tokens.Count >= maxLength)
                        break;
                }

                // Pad or truncate to maxLength
                if (tokens.Count < maxLength)
                {
                    tokens.AddRange(Enumerable.Repeat((long)UnkToken, maxLength - tokens.Count));
                }
                else if (tokens.Count > maxLength)
                {
                    tokens = tokens.Take(maxLength).ToList();
                }

                return tokens.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to tokenize text: {text}", ex);
            }
        }

        private List<long> TokenizePiece(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<long>();

            try
            {
                var words = Tokenize(text);
                var tokens = new List<long>();

                foreach (var word in words)
                {
                    if (_vocab.TryGetValue(word, out int token))
                    {
                        tokens.Add(token);
                    }
                    else
                    {
                        // Handle unknown tokens by splitting into characters
                        foreach (var ch in word)
                        {
                            var charToken = _vocab.GetValueOrDefault(ch.ToString(), UnkToken);
                            tokens.Add(charToken);
                        }
                    }
                }

                return tokens;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to tokenize piece: {text}", ex);
            }
        }

        private List<string> Tokenize(string text)
        {
            var words = new List<string>();
            var buffer = new StringBuilder();

            foreach (var ch in text)
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (buffer.Length > 0)
                    {
                        words.Add(buffer.ToString());
                        buffer.Clear();
                    }
                    words.Add(" ");
                }
                else
                {
                    buffer.Append(ch);
                }
            }

            if (buffer.Length > 0)
                words.Add(buffer.ToString());

            return words;
        }

        private bool TryGetMergeRank(string piece1, string piece2, out int rank)
        {
            var key = $"{piece1} {piece2}";
            if (_merges.TryGetValue(key, out var rankStr))
            {
                rank = _merges.Keys.ToList().IndexOf(key);
                return true;
            }
            rank = -1;
            return false;
        }
    }
}
