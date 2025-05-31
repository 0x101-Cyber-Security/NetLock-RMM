using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization;
using NetLock_RMM_Web_Console;

namespace GifCreator
{
    public class Handler
    {
        public static void Create(string dir, int delayInMs)
        {
            try
            {
                string inputFolder = dir;
                string gifName = new DirectoryInfo(inputFolder).Name;
                string outputPathGif = Path.Combine(inputFolder, gifName + ".gif");

                int frameDelay = Math.Max(2, delayInMs / 10);

                string[] files = Directory.GetFiles(inputFolder, "*.png")
                                          .OrderBy(f => f)
                                          .ToArray();

                if (files.Length == 0)
                {
                    Logging.Handler.Error("GifCreator.Handler.Create", "No PNG files found", $"Directory: {inputFolder}");
                    return;
                }

                ImageCodecInfo gifEncoder = GetEncoder(ImageFormat.Gif);
                EncoderParameters encoderParams = new EncoderParameters(1);

                using (Bitmap firstImage = (Bitmap)Image.FromFile(files[0]))
                using (FileStream fs = new FileStream(outputPathGif, FileMode.Create))
                {
                    SetFrameDelay(firstImage, frameDelay);

                    encoderParams.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.MultiFrame);
                    firstImage.Save(fs, gifEncoder, encoderParams);

                    encoderParams.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.FrameDimensionTime);

                    for (int i = 1; i < files.Length; i++)
                    {
                        using (Bitmap nextImage = (Bitmap)Image.FromFile(files[i]))
                        {
                            SetFrameDelay(nextImage, frameDelay);
                            firstImage.SaveAdd(nextImage, encoderParams);
                        }
                    }

                    encoderParams.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.Flush);
                    firstImage.SaveAdd(encoderParams);
                }

                Logging.Handler.Debug("GifCreator.Handler.Create", "GIF created", $"Saved to: {outputPathGif}");
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("GifCreator.Handler.Create", "Error creating GIF", ex.ToString());
                Console.WriteLine("Error creating GIF: " + ex.Message);
            }
        }

        // Helper to set the delay
        private static void SetFrameDelay(Image img, int delay)
        {
            try
            {
                // Delay in 1/100s
                byte[] delayBytes = new byte[] {
                    (byte)(delay & 0xFF), (byte)((delay >> 8) & 0xFF),
                    0x00, 0x00 // for next frame
                };

                // 0x5100 is the property id for frame delay
                PropertyItem prop = img.PropertyItems.FirstOrDefault();
                prop.Id = 0x5100;
                prop.Type = 4;
                prop.Len = 4;
                prop.Value = delayBytes;
                img.SetPropertyItem(prop);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("GifCreator.Handler.SetFrameDelay", "Error setting frame delay", ex.ToString());
                throw;
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageDecoders().FirstOrDefault(c => c.FormatID == format.Guid);
        }
    }
}