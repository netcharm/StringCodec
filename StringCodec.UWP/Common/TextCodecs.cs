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
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace StringCodec.UWP.Common
{
    static public class TextCodecs
    {
        public enum CODEC { URL, BASE64, UUE, XXE, RAW, QUOTED, THUNDER, FLASHGET };

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

                result = Uri.EscapeDataString(text).Replace("%", "\\x");

                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                result = Uri.UnescapeDataString(text.Replace("\\x", "%"));

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
                await new MessageDialog(ex.Message, "ERROR".T()).ShowAsync();
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
                        else base64 = string.Join("", sb);
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
                await new MessageDialog(ex.Message, "ERROR".T()).ShowAsync();
            }
            return (result);
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

        static public async Task<SVG> DecodeSvg(this string content)
        {
            SVG result = new SVG();
            if (content.Length <= 0) return (result);

            try
            {
                //var pattern = @"<svg.*?>";
                var patternSvg = @"<svg.*?\n*\r*.*?>(\n*\r*.*?)*</svg>";
                var patternBase64Svg = @"^data:image/svg.*?;base64,";
                var patternBase64 = @"^data:image/.*?;base64,";
                if (Regex.IsMatch(content, patternSvg, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    await result.Load(content);
                }
                else if (Regex.IsMatch(content, patternBase64Svg, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    string bs = Regex.Replace(content, patternBase64Svg, "", RegexOptions.IgnoreCase);
                    byte[] arr = Convert.FromBase64String(bs.Trim());
                    await result.Load(arr);
                }
                else if (Regex.IsMatch(content, patternBase64, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    string bs = Regex.Replace(content, patternBase64, "", RegexOptions.IgnoreCase);
                    byte[] arr = Convert.FromBase64String(bs.Trim());
                    result.Source = null;
                    result.Image = await Decode(content);
                    result.Bytes = arr;
                    result.Source = null;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "ERROR".T()).ShowAsync();
            }
            return (result);
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
                            writer.WriteBytes(buf);
                            writer.StoreAsync().GetResults();
                            await fileStream.FlushAsync();
                        }
                        result.SetSource(fileStream);
                    }
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "ERROR".T()).ShowAsync();
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
            var size = wb.PixelWidth * wb.PixelHeight;
            if (size > 196608)
            {
                var dlgMessage = new MessageDialog("Image is big, it's maybe too slower to encoding & filling textbox. \n Continued?".T(), "Confirm".T()) { Options = MessageDialogOptions.AcceptUserInputAfterDelay };
                dlgMessage.Commands.Add(new UICommand("OK".T()) { Id = 0 });
                dlgMessage.Commands.Add(new UICommand("Cancel".T()) { Id = 1 });
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
                result = await Encode(wb, format, prefix, linebreak);

            return (result);
        }

        static public string ConvertTo(this string text, Encoding SrcEnc, Encoding DstEnc)
        {
            var result = string.Empty;

            if (DstEnc == SrcEnc) result = text;
            else
            {
                byte[] arr = SrcEnc.GetBytes(text);
                result = DstEnc.GetString(arr);
            }

            return (result);
        }

        static public string ConvertTo(this string text, Encoding enc)
        {
            var result = string.Empty;

            //if (Encoding.Default.CodePage == 936)
            //{
            //    byte[] arr = Encoding.GetEncoding("GBK").GetBytes(text);
            //    result = DstEnc.GetString(arr);
            //}
            //else if (Encoding.Default.CodePage == 950)
            //{
            //    byte[] arr = Encoding.GetEncoding("BIG5").GetBytes(text);
            //    result = DstEnc.GetString(arr);
            //}
            //else if (Encoding.Default.CodePage == 932)
            //{
            //    byte[] arr = Encoding.GetEncoding("Shift-JIS").GetBytes(text);
            //    result = DstEnc.GetString(arr);
            //}
            //else if (Encoding.Default.CodePage == 949)
            //{
            //    byte[] arr = Encoding.GetEncoding("Korean").GetBytes(text);
            //    result = DstEnc.GetString(arr);
            //}
            //else if (Encoding.Default.CodePage == 1200)
            //{
            //    byte[] arr = Encoding.Unicode.GetBytes(text);
            //    result = DstEnc.GetString(arr);
            //}
            //else if (Encoding.Default.CodePage == 1201)
            //{
            //    byte[] arr = Encoding.BigEndianUnicode.GetBytes(text);
            //    result = DstEnc.GetString(arr);
            //}
            //else if (Encoding.Default.CodePage == 65000)
            //{
            //    byte[] arr = Encoding.UTF7.GetBytes(text);
            //    result = DstEnc.GetString(arr);
            //}
            //else if (Encoding.Default.CodePage == 65001)
            //{
            //    byte[] arr = Encoding.UTF8.GetBytes(text);
            //    result = DstEnc.GetString(arr);
            //}
            //else
            //{
            //    byte[] arr = Encoding.ASCII.GetBytes(text);
            //    result = DstEnc.GetString(arr);
            //}

            byte[] arr = Encoding.Default.GetBytes(text);
            if (arr != null && arr.Length > 0)
                result = enc.GetString(arr);

            return (result);
        }

        static public string ConvertFrom(this string text, Encoding enc, bool IsOEM = false)
        {
            var result = string.Empty;

            if (IsOEM)
            {
                int cp = System.Globalization.CultureInfo.InstalledUICulture.TextInfo.OEMCodePage;

                byte[] arr = null;
                switch (cp)
                {
                    case 936:
                        arr = Encoding.GetEncoding("GBK").GetBytes(text);
                        break;
                    case 950:
                        arr = Encoding.GetEncoding("BIG5").GetBytes(text);
                        break;
                    case 932:
                        arr = Encoding.GetEncoding("Shift-JIS").GetBytes(text);
                        break;
                    case 949:
                        arr = Encoding.GetEncoding("Korean").GetBytes(text);
                        break;
                    case 1200:
                        arr = arr = Encoding.Unicode.GetBytes(text);
                        break;
                    case 1201:
                        arr = Encoding.BigEndianUnicode.GetBytes(text);
                        break;
                    case 65000:
                        arr = Encoding.UTF7.GetBytes(text);
                        break;
                    case 65001:
                        arr = Encoding.UTF8.GetBytes(text);
                        break;
                    default:
                        arr = Encoding.ASCII.GetBytes(text);
                        break;
                }
                if (arr != null && arr.Length > 0)
                    result = enc.GetString(arr);
            }
            else
            {
                byte[] arr = enc.GetBytes(text);
                if (arr != null && arr.Length > 0)
                    result = Encoding.Default.GetString(arr);
            }
            return (result);
        }

        static public string ToString(this byte[] array, Encoding enc)
        {
            var result = string.Empty;

            if (array != null && array.Length > 0)
            {
                // UTF-16
                if (array[0] == 0xFF && array[1] == 0xFE)
                    result = Encoding.Unicode.GetString(array.Skip(2).ToArray());
                // UTF-16 Big-Endian
                else if (array[0] == 0xFE && array[1] == 0xFF)
                    result = Encoding.BigEndianUnicode.GetString(array.Skip(2).ToArray());
                // UTF-8
                else if (array[0] == 0xEF && array[1] == 0xBB && array[2] == 0xBF)
                    result = Encoding.UTF8.GetString(array.Skip(3).ToArray());
                // UTF-32
                else if (array[0] == 0xFF && array[1] == 0xFE && array[2] == 0x00 && array[3] == 0x00)
                    result = Encoding.UTF32.GetString(array.Skip(4).ToArray());
                // UTF-32 Big-Endian
                else if (array[0] == 0x00 && array[1] == 0x00 && array[2] == 0xFE && array[3] == 0xFF)
                    result = Encoding.GetEncoding("utf-32BE").GetString(array.Skip(4).ToArray());
                // UTF-7
                else if (array[0] == 0x2B && array[1] == 0x2F && array[2] == 0x76 && (array[2] == 0x38 || array[2] == 0x39 || array[2] == 0x2B || array[2] == 0x2F))
                    result = Encoding.UTF7.GetString(array.Skip(4).ToArray());
                // UTF-1
                //else if (array[0] == 0xF7 && array[1] == 0x64 && array[2] == 0x4C)
                //    result = Encoding.GetEncoding("UTF-1").GetString(array.Skip(3).ToArray());
                // GB-18030
                else if (array[0] == 0x84 && array[1] == 0x31 && array[2] == 0x95 && array[3] == 0x33)
                    result = Encoding.GetEncoding("GB18030").GetString(array.Skip(4).ToArray());
                else
                    result = enc.GetString(array);
            }

            return (result);
        }

        static public byte[] GetBOM(this Encoding enc)
        {
            byte[] result = new byte[] { };

            if (enc == Encoding.UTF8 || enc.WebName.Equals("utf-8", StringComparison.CurrentCultureIgnoreCase))
                result = new byte[3] { 0xEF, 0xBB, 0xBF };
            else if (enc == Encoding.Unicode || enc.WebName.Equals("utf-16", StringComparison.CurrentCultureIgnoreCase))
                result = new byte[2] { 0xFF, 0xFE };
            else if (enc == Encoding.BigEndianUnicode || enc.WebName.Equals("unicodeFFFE", StringComparison.CurrentCultureIgnoreCase))
                result = new byte[2] { 0xFE, 0xFF };
            else if (enc == Encoding.UTF32 || enc.WebName.Equals("utf-32", StringComparison.CurrentCultureIgnoreCase))
                result = new byte[4] { 0xFF, 0xFE, 0x00, 0x00 };
            else if (enc == Encoding.GetEncoding("utf-32BE") || enc.WebName.Equals("utf-32BE", StringComparison.CurrentCultureIgnoreCase))
                result = new byte[4] { 0x00, 0x00, 0xFE, 0xFF };

            return (result);
        }

        static private Dictionary<int, System.Globalization.CultureInfo> CodePageInfo = new Dictionary<int, System.Globalization.CultureInfo>();
        static public System.Globalization.CultureInfo GetCodePageInfo(int codepage)
        {
            if (CodePageInfo.Count() <= 0)
            {
                var cultures = System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.AllCultures);
                foreach (var culture in cultures)
                {
                    try
                    {
                        CodePageInfo.TryAdd(culture.TextInfo.ANSICodePage, culture);
                    }
                    catch (Exception) { }
                    try
                    {
                        CodePageInfo.TryAdd(culture.TextInfo.EBCDICCodePage, culture);
                    }
                    catch (Exception) { }
                    //try
                    //{
                    //    CodePageInfo.TryAdd(culture.TextInfo.MacCodePage, culture);
                    //}
                    //catch (Exception) { }
                    //try
                    //{
                    //    CodePageInfo.TryAdd(culture.TextInfo.OEMCodePage, culture);
                    //}
                    //catch (Exception) { }
                }
                foreach (var enc in Encoding.GetEncodings())
                {
                    try
                    {
                        CodePageInfo.TryAdd(enc.CodePage, new System.Globalization.CultureInfo(enc.CodePage));
                    }
                    catch (Exception) { }
                }
            }


            if (CodePageInfo.ContainsKey(codepage))
            {
                return (CodePageInfo[codepage]);
            }
            else
                return (null);
        }

        static public Encoding GetTextEncoder(string fmt)
        {
            var result = Encoding.Default;

            var ENC_NAME = fmt.Trim();
            if (string.Equals(ENC_NAME, "UTF8", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.UTF8;
            else if (string.Equals(ENC_NAME, "Unicode", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.Unicode;
            else if (string.Equals(ENC_NAME, "GBK", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("GBK");
            else if (string.Equals(ENC_NAME, "BIG5", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("BIG5");
            else if (string.Equals(ENC_NAME, "JIS", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Shift-JIS");
            else if (string.Equals(ENC_NAME, "Korean", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Korean");
            else if (string.Equals(ENC_NAME, "1250", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1250");
            else if (string.Equals(ENC_NAME, "1251", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1251");
            else if (string.Equals(ENC_NAME, "1252", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1252");
            else if (string.Equals(ENC_NAME, "1253", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1253");
            else if (string.Equals(ENC_NAME, "1254", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1254");
            else if (string.Equals(ENC_NAME, "1255", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1255");
            else if (string.Equals(ENC_NAME, "1256", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1256");
            else if (string.Equals(ENC_NAME, "1257", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1257");
            else if (string.Equals(ENC_NAME, "1258", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1258");
            else if (string.Equals(ENC_NAME, "Thai", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("IBM-Thai");
            else if (string.Equals(ENC_NAME, "Russian", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding(855);
            else if (string.Equals(ENC_NAME, "ASCII", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.ASCII;
            else
                result = Encoding.Default;

            return (result);
        }
    }
}
