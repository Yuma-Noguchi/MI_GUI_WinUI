using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MI_GUI_WinUI.Services
{
    public class CLIPTokenizer
    {
        private Dictionary<string, int> _vocab;
        private Dictionary<string, string> _merges;
        private List<string> _mergesList; // Added to store merges in order
        private readonly Regex _patSpaces;
        private readonly Regex _patToken;
        private readonly int _padTokenId = 49407; // [PAD] token ID in CLIP
        private readonly int _maxLength = 77;

        public CLIPTokenizer(string modelPath)
        {
            var vocabPath = Path.Combine(modelPath, "vocab.json");
            var mergesPath = Path.Combine(modelPath, "merges.txt");

            if (!File.Exists(vocabPath) || !File.Exists(mergesPath))
                throw new FileNotFoundException("CLIP tokenizer files not found");

            // Load vocabulary
            using var vocabStream = File.OpenRead(vocabPath);
            _vocab = JsonSerializer.Deserialize<Dictionary<string, int>>(vocabStream) ??
                throw new InvalidOperationException("Failed to load vocabulary");

            // Load BPE merges
            var mergeLines = File.ReadAllLines(mergesPath).Skip(1).ToArray();
            _merges = new Dictionary<string, string>();
            _mergesList = new List<string>();

            foreach (var line in mergeLines)
            {
                var parts = line.Split(' ');
                if (parts.Length == 2)
                {
                    var merge = parts[0] + " " + parts[1];
                    _merges[merge] = parts[0] + parts[1];
                    _mergesList.Add(merge);
                }
            }

            // Initialize regex patterns
            _patSpaces = new Regex(@"\s+");
            _patToken = new Regex(@"<\|startoftext\|>|<\|endoftext\|>|'s|'t|'re|'ve|'m|'ll|'d|[\p{L}]+|[\p{N}]|[^\s\p{L}\p{N}]+", RegexOptions.Compiled);
        }

        public long[] Tokenize(string text)
        {
            var result = new List<long>();

            // Add start token
            result.Add(_vocab["<|startoftext|>"]);

            // Normalize whitespace
            text = _patSpaces.Replace(text, " ").Trim();

            // Extract tokens
            var matches = _patToken.Matches(text);
            foreach (Match match in matches)
            {
                var token = match.Value.ToLowerInvariant();
                var bpeToken = BPEEncode(token);

                foreach (var bpe in bpeToken.Split(' '))
                {
                    if (_vocab.TryGetValue(bpe, out var id))
                    {
                        result.Add(id);
                    }
                }

                // Check if we're approaching the limit
                if (result.Count >= _maxLength - 1)
                    break;
            }

            // Add end token
            result.Add(_vocab["<|endoftext|>"]);

            // Pad to max length
            while (result.Count < _maxLength)
            {
                result.Add(_padTokenId);
            }

            // Truncate if too long
            if (result.Count > _maxLength)
            {
                result = result.Take(_maxLength - 1).ToList();
                result.Add(_vocab["<|endoftext|>"]);
            }

            return result.ToArray();
        }

        private string BPEEncode(string token)
        {
            if (string.IsNullOrEmpty(token))
                return "";

            // Convert to bytes for UTF-8 handling
            var bytes = Encoding.UTF8.GetBytes(token);

            // Convert bytes to string tokens
            var word = string.Join(" ", bytes.Select(b => b.ToString()));

            // Iteratively merge BPE pairs
            var pairs = GetPairs(word);

            if (!pairs.Any())
                return word;

            while (true)
            {
                var bigram = pairs.OrderBy(p => {
                    if (_merges.TryGetValue(p, out _))
                        return _mergesList.IndexOf(p);
                    return int.MaxValue;
                }).FirstOrDefault();

                if (string.IsNullOrEmpty(bigram) || !_merges.ContainsKey(bigram))
                    break;

                var parts = bigram.Split(' ');
                var newWord = "";
                var i = 0;

                while (i < word.Length)
                {
                    var j = word.IndexOf(bigram, i);
                    if (j == -1)
                    {
                        newWord += word.Substring(i);
                        break;
                    }
                    newWord += word.Substring(i, j - i);
                    newWord += _merges[bigram];
                    i = j + bigram.Length;
                }

                word = newWord;
                if (word.IndexOf(' ') == -1)
                    break;

                pairs = GetPairs(word);
            }

            return word;
        }

        private List<string> GetPairs(string word)
        {
            var pairs = new List<string>();
            var tokens = word.Split(' ');

            for (int i = 0; i < tokens.Length - 1; i++)
            {
                pairs.Add($"{tokens[i]} {tokens[i + 1]}");
            }

            return pairs;
        }
    }
}