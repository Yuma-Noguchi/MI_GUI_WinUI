using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace MI_GUI_WinUI.Utils
{
    public static class ImageHelper
    {
        public static async Task<SoftwareBitmapSource?> ByteArrayToImageSource(byte[] imageData)
        {
            try
            {
                using var stream = new InMemoryRandomAccessStream();
                using var writer = new DataWriter(stream);
                writer.WriteBytes(imageData);
                await writer.StoreAsync();
                await stream.FlushAsync();
                stream.Seek(0);

                var decoder = await BitmapDecoder.CreateAsync(stream);
                var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                // Convert to BGRA8 if needed (required format for SoftwareBitmapSource)
                if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                    softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
                {
                    softwareBitmap = SoftwareBitmap.Convert(
                        softwareBitmap, 
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied);
                }

                var source = new SoftwareBitmapSource();
                await source.SetBitmapAsync(softwareBitmap);
                return source;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
