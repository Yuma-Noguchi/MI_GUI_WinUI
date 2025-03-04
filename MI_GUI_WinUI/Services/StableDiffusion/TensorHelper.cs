using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Linq;

namespace MI_GUI_WinUI.Services.StableDiffusion
{
    internal static class TensorHelper
    {
        public static DenseTensor<T> CreateTensor<T>(T[] data, int[] dimensions) where T : struct
        {
            return new DenseTensor<T>(data, dimensions);
        }

        public static DenseTensor<float> Duplicate(float[] data, int[] dimensions)
        {
            data = data.Concat(data).ToArray();
            return CreateTensor(data, dimensions);
        }

        //public static DenseTensor<T> Duplicate<T>(T[] sourceArray, int[] targetDimensions) where T : struct
        //{
        //    // Simplify by just concatenating the array with itself (Microsoft's approach)
        //    var duplicatedArray = sourceArray.Concat(sourceArray).ToArray();
            
        //    // Validate dimensions
        //    int expectedSize = duplicatedArray.Length;
        //    int actualSize = 1;
        //    foreach (var dim in targetDimensions)
        //    {
        //        actualSize *= dim;
        //    }
            
        //    if (expectedSize != actualSize)
        //    {
        //        throw new ArgumentException(
        //            $"Invalid target dimensions: expected size {expectedSize}, got {actualSize} from dimensions {string.Join(",", targetDimensions)}");
        //    }
            
        //    return new DenseTensor<T>(duplicatedArray, targetDimensions);
        //}

        public static DenseTensor<float> SumTensors(Tensor<float>[] tensors, int[] dimensions)
        {
            if (tensors == null || tensors.Length == 0)
                throw new ArgumentException("No tensors provided for summation");
            if (dimensions == null)
                throw new ArgumentNullException(nameof(dimensions));

            int length = (int)tensors[0].Length;
            ValidateDimensions(length, dimensions);

            foreach (var tensor in tensors)
            {
                if (tensor.Length != length)
                    throw new ArgumentException("All tensors must have the same length");
            }

            try
            {
                var result = new float[length];

                // Initialize with first tensor
                var firstArray = tensors[0].ToArray();
                Buffer.BlockCopy(firstArray, 0, result, 0, length * sizeof(float));

                // Add remaining tensors
                for (int i = 1; i < tensors.Length; i++)
                {
                    var current = tensors[i].ToArray();
                    for (int j = 0; j < length; j++)
                    {
                        result[j] += current[j];
                    }
                }

                return new DenseTensor<float>(result, dimensions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to sum tensors", ex);
            }
        }

        public static (DenseTensor<T>, DenseTensor<T>) SplitTensor<T>(DenseTensor<T> tensor, int[] splitDimensions) where T : struct
        {
            var data = tensor.ToArray();
            int elementsPerSplit = data.Length / 2;
            
            var firstHalf = new T[elementsPerSplit];
            var secondHalf = new T[elementsPerSplit];
            
            Array.Copy(data, 0, firstHalf, 0, elementsPerSplit);
            Array.Copy(data, elementsPerSplit, secondHalf, 0, elementsPerSplit);
            
            return (
                new DenseTensor<T>(firstHalf, splitDimensions),
                new DenseTensor<T>(secondHalf, splitDimensions)
            );
        }

        public static int GetTotalSize(int[] dimensions)
        {
            if (dimensions == null || dimensions.Length == 0)
                return 0;
                
            int size = 1;
            foreach (var dim in dimensions)
                size *= dim;
            return size;
        }

        private static void ValidateDimensions(int dataLength, int[] dimensions)
        {
            var expectedSize = GetTotalSize(dimensions);
            if (dataLength != expectedSize)
            {
                throw new ArgumentException(
                    $"Data length {dataLength} does not match expected size {expectedSize} for dimensions {FormatDimensions(dimensions)}");
            }
        }

        private static string FormatDimensions(int[] dimensions)
        {
            return $"[{string.Join(",", dimensions)}]";
        }

        public static DenseTensor<float> DuplicateTextEmbeddings(DenseTensor<float> embeddings)
        {
            if (embeddings == null)
                throw new ArgumentNullException(nameof(embeddings));

            // Convert to array first
            var embeddingsArray = embeddings.ToArray();
            
            // Use Concat like Microsoft does
            var duplicatedArray = embeddingsArray.Concat(embeddingsArray).ToArray();
            
            // Create dimensions with batch size 2
            var origDims = embeddings.Dimensions.ToArray();
            var newDimensions = new[] { 2, origDims[1], origDims[2] };
            
            // Create new tensor directly
            return new DenseTensor<float>(duplicatedArray, newDimensions);
        }
    }
}
