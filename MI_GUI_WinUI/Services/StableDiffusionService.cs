using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using StableDiffusion.ML.OnnxRuntime;
using System.IO;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MI_GUI_WinUI.Services
{
    public partial class StableDiffusionService : ObservableObject
    {
        private readonly StableDiffusionConfig _config;
        private readonly UNet _unet;

        [ObservableProperty]
        private double _percentage;

        [ObservableProperty]
        private long _lastTimeMilliseconds;

        [ObservableProperty]
        private double _iterationsPerSecond;

        public long NumInferenceSteps => _config.NumInferenceSteps;

        public StableDiffusionService()
        {
            var modelsPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Onnx", "fp16");

            _config = new StableDiffusionConfig
            {
                NumInferenceSteps = 50,
                GuidanceScale = 7.5,
                ExecutionProviderTarget = StableDiffusionConfig.ExecutionProvider.DirectML,
                DeviceId = 1,
                TokenizerOnnxPath = $@"{modelsPath}\cliptokenizer.onnx",
                TextEncoderOnnxPath = $@"{modelsPath}\text_encoder\model.onnx",
                UnetOnnxPath = $@"{modelsPath}\unet\model.onnx",
                VaeDecoderOnnxPath = $@"{modelsPath}\vae_decoder\model.onnx",
                SafetyModelPath = $@"{modelsPath}\safety_checker\model.onnx",
                ImageOutputPath = "NONE",
            };

            _unet = new UNet(_config);
        }

        public ObservableCollection<ImageSource> GenerateFakeData(string description, int numberOfImages)
        {
            string imagePath = "pack://application:,,,/Assets/biaafpwi.png";
            var tempImages = new ObservableCollection<ImageSource>();
            for (int i = 0; i < numberOfImages; i++)
            {
                var bitmapImage = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                tempImages.Add(bitmapImage);
            }

            return tempImages;
        }

        public async Task<string[]> GenerateImages(string description, int numberOfImages, Action<int>? stepCallback = null)
        {
            Percentage = 0;
            
            if (numberOfImages == 0)
            {
                return Array.Empty<string>();
            }

            var result = await Task.Run(() =>
            {
                var timer = new Stopwatch();
                timer.Start();

                var imageDestination = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, $"Images-{DateTime.Now.Ticks}");
                var config = _config;
                config.ImageOutputPath = imageDestination;
                var totalSteps = numberOfImages * config.NumInferenceSteps;

                Directory.CreateDirectory(imageDestination);

                for (int i = 1; i <= numberOfImages; i++) 
                {
                    var output = _unet.Inference(
                        description, 
                        config, 
                        (stepIndex) => { 
                            Percentage = ((double)((stepIndex + 1) + (i - 1) * config.NumInferenceSteps) / (double)totalSteps) * 100.0;
                        }
                    );

                    IterationsPerSecond = output.IterationsPerSecond;

                    if (output.Image == null)
                    {
                        Console.WriteLine($"There was an error generating image {i}.");
                    }
                }

                var imagePaths = Directory.GetFiles(imageDestination, "*.*", SearchOption.AllDirectories)
                                        .Where(path => new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" }
                                        .Contains(Path.GetExtension(path).ToLower()))
                                        .ToArray();

                timer.Stop();
                LastTimeMilliseconds = timer.ElapsedMilliseconds;

                return imagePaths;
            });

            return result;
        }
    }
}
