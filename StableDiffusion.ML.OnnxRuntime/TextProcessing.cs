using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;

namespace StableDiffusion.ML.OnnxRuntime
{
    public class TextProcessing : IDisposable
    {
        private InferenceSession _tokenizeSession;
        private InferenceSession _encodeSession;
        private bool _disposedValue;

        public TextProcessing(StableDiffusionConfig config)
        {
            // Tokenize session
            {
                // Create session options for custom op of extensions
                var sessionOptions = new SessionOptions();
                sessionOptions.RegisterOrtExtensions();

                // Create an InferenceSession from the onnx clip tokenizer.
                _tokenizeSession = new InferenceSession(config.TokenizerOnnxPath, sessionOptions);
            }

            // Encoder session
            {
                var sessionOptions = config.GetSessionOptionsForEp();
                _encodeSession = new InferenceSession(config.TextEncoderOnnxPath, sessionOptions);
            }
        }

        public DenseTensor<T> PreprocessText<T>(String prompt)
        {
            // Load the tokenizer and text encoder to tokenize and encode the text.
            var textTokenized = TokenizeText(prompt);
            var textPromptEmbeddings = TextEncoder<T>(textTokenized).ToArray();

            // Create uncond_input of blank tokens
            var uncondInputTokens = CreateUncondInput();
            var uncondEmbedding = TextEncoder<T>(uncondInputTokens).ToArray();

            // Concant textEmeddings and uncondEmbedding
            var textEmbeddings = new DenseTensor<T>(new[] { 2, 77, 768 });

            for (var i = 0; i < textPromptEmbeddings.Length; i++)
            {
                textEmbeddings[0, i / 768, i % 768] = uncondEmbedding[i];
                textEmbeddings[1, i / 768, i % 768] = textPromptEmbeddings[i];
            }
            return textEmbeddings;
        }
        public int[] TokenizeText(string text)
        {
            // Create an InferenceSession from the onnx clip tokenizer.
            var inputTensor = new DenseTensor<string>(new string[] { text }, new int[] { 1 });
            var inputString = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor<string>("string_input", inputTensor) };
            // Run session and send the input data in to get inference output. 
            var tokens = _tokenizeSession.Run(inputString);


            var inputIds = (tokens.ToList().First().Value as IEnumerable<long>).ToArray();
            Console.WriteLine(String.Join(" ", inputIds));

            // Cast inputIds to Int32
            var InputIdsInt = inputIds.Select(x => (int)x).ToArray();

            var modelMaxLength = 77;
            // Pad array with 49407 until length is modelMaxLength
            if (InputIdsInt.Length < modelMaxLength)
            {
                var pad = Enumerable.Repeat(49407, 77 - InputIdsInt.Length).ToArray();
                InputIdsInt = InputIdsInt.Concat(pad).ToArray();
            }

            return InputIdsInt;

        }

        public static int[] CreateUncondInput()
        {
            // Create an array of empty tokens for the unconditional input.
            var blankTokenValue = 49407;
            var modelMaxLength = 77;
            var inputIds = new List<Int32>();
            inputIds.Add(49406);
            var pad = Enumerable.Repeat(blankTokenValue, modelMaxLength - inputIds.Count()).ToArray();
            inputIds.AddRange(pad);

            return inputIds.ToArray();
        }

        public DenseTensor<T> TextEncoder<T>(int[] tokenizedInput)
        {
            // Create input tensor.
            var input_ids = TensorHelper.CreateTensor(tokenizedInput, new[] { 1, tokenizedInput.Count() });

            var input = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor<int>("input_ids", input_ids) };

            // Run inference.
            var encoded = _encodeSession.Run(input);

            if (typeof(T) == typeof(Half))
            {
                // When T is Half, convert from Float16
                var float16Data = (encoded.ToList().First().Value as IEnumerable<Float16>).ToArray();
                var halfArray = new Half[float16Data.Length];
                for (int i = 0; i < float16Data.Length; i++)
                {
                    halfArray[i] = (Half)(float)float16Data[i];
                }
                return TensorHelper.CreateTensor<T>((T[])(object)halfArray, new[] { 1, 77, 768 });
            }
            else
            {
                var lastHiddenState = (encoded.ToList().First().Value as IEnumerable<T>).ToArray();
                return TensorHelper.CreateTensor(lastHiddenState.ToArray(), new[] { 1, 77, 768 });
            }
        }

        public virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _tokenizeSession?.Dispose();
                    _encodeSession?.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
