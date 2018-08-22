using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;

namespace StringCodec.UWP.Common
{
    static public class TextCodecs
    {
        public enum CODEC {URL, BASE64, UUE, XXE, RAW, QUOTED, THUNDER, FLASHGET};

        static public class BASE64
        {
            static public string Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;

                byte[] arr = Encoding.Default.GetBytes(text);
                var opt = Base64FormattingOptions.None;
                if (LineBreak) opt = Base64FormattingOptions.InsertLineBreaks;
                result = Convert.ToBase64String(arr, opt);

                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                byte[] arr = Convert.FromBase64String(text);
                //result = Encoding.UTF8.GetString(arr);
                result = enc.GetString(arr);

                return (result);
            }

        }

        static public class URL
        {
            static public string Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;

                result = Uri.EscapeDataString(text);

                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                result = Uri.UnescapeDataString(text);

                return (result);
            }

        }

        static public class UUE
        {
            static public string Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;

                result = Uri.EscapeDataString(text);

                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                result = Uri.UnescapeDataString(text);

                return (result);
            }

        }

        static public class XXE
        {
            static public string Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;

                result = Uri.EscapeDataString(text);

                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                result = Uri.UnescapeDataString(text);

                return (result);
            }

        }

        static public class RAW
        {
            static public string Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;

                result = Uri.EscapeDataString(text).Replace("%","\\x");

                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                result = Uri.UnescapeDataString(text.Replace("\\x","%"));

                return (result);
            }

        }

        static public class QUOTED
        {
            static public string Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;

                result = Uri.EscapeDataString(text);

                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                result = Uri.UnescapeDataString(text);

                return (result);
            }

        }

        static public class THUNDER
        {
            static public string Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;
                result = $"thunder://{BASE64.Encode($"AA{text}ZZ", LineBreak)}";
                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;
                var url = Regex.Replace(text, @"^thunder://(.*?)$", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                url = BASE64.Decode(url, enc);                
                result = Regex.Replace(url, @"^AA(.*?)ZZ$", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                return (result);
            }

        }

        static public class FLASHGET
        {
            static public string Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;
                result = $"flashget://{BASE64.Encode($"[FLASHGET]{text}[FLASHGET]", LineBreak)}";
                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;
                var url = Regex.Replace(text, @"^flashget://(.*?)$", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                url = BASE64.Decode(url, enc);
                result = Regex.Replace(url, @"^\[FLASHGET\](.*?)\[FLASHGET\]$", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                return (result);
            }

        }

        static public string Encode(string content, CODEC Codec, bool LineBreak = false)
        {
            string result = string.Empty;
            switch (Codec)
            {
                case CODEC.URL:
                    result = URL.Encode(content);
                    break;
                case CODEC.BASE64:
                    result = BASE64.Encode(content, LineBreak);
                    break;
                case CODEC.UUE:
                    result = UUE.Encode(content);
                    break;
                case CODEC.XXE:
                    result = XXE.Encode(content);
                    break;
                case CODEC.RAW:
                    result = RAW.Encode(content);
                    break;
                case CODEC.QUOTED:
                    result = QUOTED.Encode(content);
                    break;
                case CODEC.THUNDER:
                    result = THUNDER.Encode(content);
                    break;
                case CODEC.FLASHGET:
                    result = FLASHGET.Encode(content);
                    break;
                default:
                    break;
            }
            return (result);
        }

        static public string Encoder(this string content, CODEC Codec, bool LineBreak = false)
        {
            return (Encode(content, Codec, LineBreak));
        }

        static public async Task<string> Encode(WriteableBitmap image, string format = ".png", bool prefix = true, bool LineBreak = false)
        {
            string result = string.Empty;
            try
            {
                using (var fileStream = new InMemoryRandomAccessStream())
                {
                    // Get pixels of the WriteableBitmap object 
                    Stream pixelStream = image.PixelBuffer.AsStream();
                    byte[] pixels = new byte[pixelStream.Length];
                    await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                    var encId = BitmapEncoder.PngEncoderId;
                    var fext = format.ToLower();
                    var mime = "image/png";
                    switch (fext)
                    {
                        case ".bmp":
                            encId = BitmapEncoder.BmpEncoderId;
                            mime = "image/bmp";
                            break;
                        case ".gif":
                            encId = BitmapEncoder.GifEncoderId;
                            mime = "image/gif";
                            break;
                        case ".png":
                            encId = BitmapEncoder.PngEncoderId;
                            mime = "image/png";
                            break;
                        case ".jpg":
                            encId = BitmapEncoder.JpegEncoderId;
                            mime = "image/jpeg";
                            break;
                        case ".jpeg":
                            encId = BitmapEncoder.JpegEncoderId;
                            mime = "image/jpeg";
                            break;
                        case ".tif":
                            encId = BitmapEncoder.TiffEncoderId;
                            mime = "image/tiff";
                            break;
                        case ".tiff":
                            encId = BitmapEncoder.TiffEncoderId;
                            mime = "image/tiff";
                            break;
                        default:
                            encId = BitmapEncoder.PngEncoderId;
                            mime = "image/png";
                            break;
                    }
                    var encoder = await BitmapEncoder.CreateAsync(encId, fileStream);
                    // Save the image file with jpg extension 
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                        //(uint)bmp.PixelWidth, (uint)bmp.PixelHeight, 
                        (uint)image.PixelWidth, (uint)image.PixelHeight,
                        96.0, 96.0,
                        pixels);
                    await encoder.FlushAsync();

                    #region Convert InMemoryRandomAccessStream/IRandomAccessStream to byte[]/Array
                    Stream stream = WindowsRuntimeStreamExtensions.AsStreamForRead(fileStream.GetInputStreamAt(0));
                    MemoryStream ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    byte[] arr = ms.ToArray();
                    #endregion

                    var opt = Base64FormattingOptions.None;
                    if (LineBreak) opt = Base64FormattingOptions.InsertLineBreaks;

                    string base64 = string.Empty;

                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < arr.Length; i += 57)
                    {
                        byte[] segment = arr.Skip(i).Take(57).ToArray();
                        sb.AppendLine(Convert.ToBase64String(segment, opt));
                    }
                    if (LineBreak) base64 = string.Join("\n", sb);
                    else base64 = string.Join("", sb);
                    //base64 = Convert.ToBase64String(arr, opt);

                    if (prefix) result = $"data:{mime};base64,{base64}";
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "ERROR").ShowAsync();
            }
            return (result);
        }

        static public async Task<string> ProgressEncode(WriteableBitmap image, string format = ".png", bool prefix = true, bool LineBreak = false)
        {
            string result = string.Empty;
            int processCount = 0;
            try
            {
                using (var fileStream = new InMemoryRandomAccessStream())
                {
                    #region Encode Image to format
                    // Get pixels of the WriteableBitmap object 
                    Stream pixelStream = image.PixelBuffer.AsStream();
                    byte[] pixels = new byte[pixelStream.Length];
                    await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                    var encId = BitmapEncoder.PngEncoderId;
                    var fext = format.ToLower();
                    var mime = "image/png";
                    switch (fext)
                    {
                        case ".bmp":
                            encId = BitmapEncoder.BmpEncoderId;
                            mime = "image/bmp";
                            break;
                        case ".gif":
                            encId = BitmapEncoder.GifEncoderId;
                            mime = "image/gif";
                            break;
                        case ".png":
                            encId = BitmapEncoder.PngEncoderId;
                            mime = "image/png";
                            break;
                        case ".jpg":
                            encId = BitmapEncoder.JpegEncoderId;
                            mime = "image/jpeg";
                            break;
                        case ".jpeg":
                            encId = BitmapEncoder.JpegEncoderId;
                            mime = "image/jpeg";
                            break;
                        case ".tif":
                            encId = BitmapEncoder.TiffEncoderId;
                            mime = "image/tiff";
                            break;
                        case ".tiff":
                            encId = BitmapEncoder.TiffEncoderId;
                            mime = "image/tiff";
                            break;
                        default:
                            encId = BitmapEncoder.PngEncoderId;
                            mime = "image/png";
                            break;
                    }
                    var encoder = await BitmapEncoder.CreateAsync(encId, fileStream);
                    // Save the image file with jpg extension 
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                        //(uint)bmp.PixelWidth, (uint)bmp.PixelHeight, 
                        (uint)image.PixelWidth, (uint)image.PixelHeight,
                        96.0, 96.0,
                        pixels);
                    await encoder.FlushAsync();
                    #endregion

                    #region Convert InMemoryRandomAccessStream/IRandomAccessStream to byte[]/Array
                    Stream stream = WindowsRuntimeStreamExtensions.AsStreamForRead(fileStream.GetInputStreamAt(0));
                    MemoryStream ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    byte[] arr = ms.ToArray();
                    #endregion

                    var opt = Base64FormattingOptions.None;
                    if (LineBreak) opt = Base64FormattingOptions.InsertLineBreaks;

                    var dlgProgress = new ProgressDialog();
                    IProgress<int> progress = new Progress<int>(percent => dlgProgress.Value = percent);

                    var dlgResult = dlgProgress.ShowAsync();

                    string base64 = string.Empty;
                    int totalCount = arr.Count();
                    processCount = await Task.Run<int>(() =>
                    {
                        StringBuilder sb = new StringBuilder();

                        int tempCount = 0;
                        for (int i = 0; i < totalCount; i += 57)
                        {
                            byte[] segment = arr.Skip(i).Take(57).ToArray();
                            sb.AppendLine(Convert.ToBase64String(segment, opt));
                            tempCount += segment.Length;

                            if (dlgProgress != null)
                                dlgProgress.Report(tempCount * 100 / totalCount);
                        }
                        if (LineBreak) base64 = string.Join("\n", sb);
                        else           base64 = string.Join("", sb);
                        if (dlgProgress != null)
                            dlgProgress.Report(100);

                        return tempCount;
                    });
                    if (prefix) result = $"data:{mime};base64,{base64}";
                    dlgResult.Cancel();
                    dlgProgress.Hide();
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "ERROR").ShowAsync();
            }
            return(result);
        }

        static public async Task<string> Encoder(this WriteableBitmap image, string format = ".png", bool prefix = true, bool LineBreak = false)
        {
            return (await Encode(image, format, prefix, LineBreak));
        }

        static public string Decode(string content, CODEC Codec, Encoding enc)
        {
            string result = string.Empty;
            switch (Codec)
            {
                case CODEC.URL:
                    result = URL.Decode(content, enc);
                    break;
                case CODEC.BASE64:
                    result = BASE64.Decode(content, enc);
                    break;
                case CODEC.UUE:
                    result = UUE.Decode(content, enc);
                    break;
                case CODEC.XXE:
                    result = XXE.Decode(content, enc);
                    break;
                case CODEC.RAW:
                    result = RAW.Decode(content, enc);
                    break;
                case CODEC.QUOTED:
                    result = QUOTED.Decode(content, enc);
                    break;
                case CODEC.THUNDER:
                    result = THUNDER.Decode(content, enc);
                    break;
                case CODEC.FLASHGET:
                    result = FLASHGET.Decode(content, enc);
                    break;
                default:
                    break;
            }
            return (result);
        }

        static public string Decoder(this string content, CODEC Codec, Encoding enc)
        {
            return (Decode(content, Codec, enc));
        }

        static public async Task<WriteableBitmap> Decode(string content)
        {
            WriteableBitmap result = new WriteableBitmap(1, 1);
            if (content.Length <= 0) return (result);

            try
            {
                string bs = Regex.Replace(content, @"data:image/.*?;base64,", "", RegexOptions.IgnoreCase);
                byte[] arr = Convert.FromBase64String(bs.Trim());
                using (MemoryStream ms = new MemoryStream(arr))
                {
                    byte[] buf = ms.ToArray();
                    using (InMemoryRandomAccessStream fileStream = new InMemoryRandomAccessStream())
                    {
                        using (DataWriter writer = new DataWriter(fileStream.GetOutputStreamAt(0)))
                        {
                            writer.WriteBytes((byte[])buf);
                            writer.StoreAsync().GetResults();
                            await fileStream.FlushAsync();
                        }
                        result.SetSource(fileStream);
                    }
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "ERROR").ShowAsync();
            }
            return (result);
        }

        static public async Task<WriteableBitmap> Decoder(this string content)
        {
            return (await Decode(content));
        }

        static public async Task<string> ToBase64(this WriteableBitmap wb, string format, bool prefix, bool linebreak)
        {
            string result = string.Empty;
            if (wb.PixelWidth > 512 && wb.PixelHeight > 512)
            {
                var dlgMessage = new MessageDialog("Image size > 256x256, continued?", "Comfirm") { Options = MessageDialogOptions.AcceptUserInputAfterDelay };
                dlgMessage.Commands.Add(new UICommand("OK") { Id = 0 });
                dlgMessage.Commands.Add(new UICommand("Cancel") { Id = 1 });
                // Set the command that will be invoked by default
                dlgMessage.DefaultCommandIndex = 0;
                // Set the command to be invoked when escape is pressed
                dlgMessage.CancelCommandIndex = 1;
                // Show the message dialog
                var dlgResult = await dlgMessage.ShowAsync();
                if ((int)dlgResult.Id == 0)
                {
                    result = await ProgressEncode(wb, format, prefix, linebreak);
                }
            }
            else
                result = await TextCodecs.Encode(wb, format, prefix, linebreak);

            return (result);
        }

    }
}
