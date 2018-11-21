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
            static public async Task<string> Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;
                try
                {
                    byte[] arr = Encoding.Default.GetBytes(text);
                    var opt = Base64FormattingOptions.None;
                    if (LineBreak) opt = Base64FormattingOptions.InsertLineBreaks;
                    result = Convert.ToBase64String(arr, opt);
                }
                catch(Exception ex)
                {
                    await new MessageDialog($"{"BASE64ERROR".T()}\n{ex.Message}", "ERROR".T()).ShowAsync();
                }

                return (result);
            }

            static public async Task<string> Decode(string text, Encoding enc)
            {
                string result = string.Empty;
                try
                {
                    byte[] arr = Convert.FromBase64String(text);
                    //result = Encoding.UTF8.GetString(arr);
                    result = enc.GetString(arr);
                }
                catch(Exception ex)
                {
                    await new MessageDialog($"{"BASE64ERROR".T()}\n{ex.Message}", "ERROR".T()).ShowAsync();
                }

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
            static public async Task<string> Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;
                result = $"thunder://{await BASE64.Encode($"AA{text}ZZ", LineBreak)}";
                return (result);
            }

            static public async Task<string> Decode(string text, Encoding enc)
            {
                string result = string.Empty;
                var url = Regex.Replace(text, @"^thunder://(.*?)$", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                url = await BASE64.Decode(url, enc);
                result = Regex.Replace(url, @"^AA(.*?)ZZ$", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                return (result);
            }

        }

        static public class FLASHGET
        {
            static public async Task<string> Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;
                result = $"flashget://{await BASE64.Encode($"[FLASHGET]{text}[FLASHGET]", LineBreak)}";
                return (result);
            }

            static public async Task<string> Decode(string text, Encoding enc)
            {
                string result = string.Empty;
                var url = Regex.Replace(text, @"^flashget://(.*?)$", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                url = await BASE64.Decode(url, enc);
                result = Regex.Replace(url, @"^\[FLASHGET\](.*?)\[FLASHGET\]$", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                return (result);
            }

        }

        static public async Task<string> Encode(string content, CODEC Codec, bool LineBreak = false)
        {
            string result = string.Empty;
            try
            {
                switch (Codec)
                {
                    case CODEC.URL:
                        result = URL.Encode(content);
                        break;
                    case CODEC.BASE64:
                        result = await BASE64.Encode(content, LineBreak);
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
                        result = await THUNDER.Encode(content);
                        break;
                    case CODEC.FLASHGET:
                        result = await FLASHGET.Encode(content);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        static public async Task<string> Encoder(this string content, CODEC Codec, bool LineBreak = false)
        {
            return (await Encode(content, Codec, LineBreak));
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
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
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
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        static public async Task<string> Encoder(this WriteableBitmap image, string format = ".png", bool prefix = true, bool LineBreak = false)
        {
            return (await Encode(image, format, prefix, LineBreak));
        }

        static public async Task<string> Decode(string content, CODEC Codec, Encoding enc)
        {
            string result = string.Empty;
            try
            {
                switch (Codec)
                {
                    case CODEC.URL:
                        result = URL.Decode(content, enc);
                        break;
                    case CODEC.BASE64:
                        result = await BASE64.Decode(content, enc);
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
                        result = await THUNDER.Decode(content, enc);
                        break;
                    case CODEC.FLASHGET:
                        result = await FLASHGET.Decode(content, enc);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        static public async Task<string> Decoder(this string content, CODEC Codec, Encoding enc)
        {
            return (await Decode(content, Codec, enc));
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
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
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
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
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
            try
            {
                if (wb == null || !(wb is WriteableBitmap)) return (result);
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
                        if (string.IsNullOrEmpty(format)) format = ".png";
                        result = await ProgressEncode(wb, format, prefix, linebreak);
                    }
                }
                else
                    result = await Encode(wb, format, prefix, linebreak);
            }
            catch(Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        static public async Task<string> ConvertTo(this string text, Encoding SrcEnc, Encoding DstEnc)
        {
            var result = string.Empty;

            try
            {
                if (DstEnc == SrcEnc) result = text;
                else
                {
                    byte[] arr = SrcEnc.GetBytes(text);
                    result = DstEnc.GetString(arr);
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        static public async Task<string> ConvertTo(this string text, Encoding enc)
        {
            var result = string.Empty;
            try
            {
                byte[] arr = Encoding.Default.GetBytes(text);
                if (arr != null && arr.Length > 0)
                    result = enc.GetString(arr);
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        static private Dictionary<char, char> KanaCaseMap = new Dictionary<char, char>()
        {
            { '\uFF65', '\u30FB' }, // '･' : '・'
            { '\uFF66', '\u30F2' }, // 'ｦ' : 'ヲ'
            { '\uFF67', '\u30A1' }, // 'ｧ' : 'ァ'
            { '\uFF68', '\u30A3' }, // 'ｨ' : 'ィ'
            { '\uFF69', '\u30A5' }, // 'ｩ' : 'ゥ'
            { '\uFF6A', '\u30A7' }, // 'ｪ' : 'ェ'
            { '\uFF6B', '\u30A9' }, // 'ｫ' : 'ォ'
            { '\uFF6C', '\u30E3' }, // 'ｬ' : 'ャ'
            { '\uFF6D', '\u30E5' }, // 'ｭ' : 'ュ'
            { '\uFF6E', '\u30E7' }, // 'ｮ' : 'ョ'
            { '\uFF6F', '\u30C3' }, // 'ｯ' : 'ッ'
            { '\uFF70', '\u30FC' }, // 'ｰ' : 'ー'
            { '\uFF71', '\u30A2' }, // 'ｱ' : 'ア'
            { '\uFF72', '\u30A4' }, // 'ｲ' : 'イ'
            { '\uFF73', '\u30A6' }, // 'ｳ' : 'ウ'
            { '\uFF74', '\u30A8' }, // 'ｴ' : 'エ'
            { '\uFF75', '\u30AA' }, // 'ｵ' : 'オ'
            { '\uFF76', '\u30AB' }, // 'ｶ' : 'カ'
            { '\uFF77', '\u30AD' }, // 'ｷ' : 'キ'
            { '\uFF78', '\u30AF' }, // 'ｸ' : 'ク'
            { '\uFF79', '\u30B1' }, // 'ｹ' : 'ケ'
            { '\uFF7A', '\u30B3' }, // 'ｺ' : 'コ'
            { '\uFF7B', '\u30B5' }, // 'ｻ' : 'サ'
            { '\uFF7C', '\u30B7' }, // 'ｼ' : 'シ'
            { '\uFF7D', '\u30B9' }, // 'ｽ' : 'ス'
            { '\uFF7E', '\u30BB' }, // 'ｾ' : 'セ'
            { '\uFF7F', '\u30BD' }, // 'ｿ' : 'ソ'
            { '\uFF80', '\u30BF' }, // 'ﾀ' : 'タ'
            { '\uFF81', '\u30C1' }, // 'ﾁ' : 'チ'
            { '\uFF82', '\u30C4' }, // 'ﾂ' : 'ツ'
            { '\uFF83', '\u30C6' }, // 'ﾃ' : 'テ'
            { '\uFF84', '\u30C8' }, // 'ﾄ' : 'ト'
            { '\uFF85', '\u30CA' }, // 'ﾅ' : 'ナ'
            { '\uFF86', '\u30CB' }, // 'ﾆ' : 'ニ'
            { '\uFF87', '\u30CC' }, // 'ﾇ' : 'ヌ'
            { '\uFF88', '\u30CD' }, // 'ﾈ' : 'ネ'
            { '\uFF89', '\u30CE' }, // 'ﾉ' : 'ノ'
            { '\uFF8A', '\u30CF' }, // 'ﾊ' : 'ハ'
            { '\uFF8B', '\u30D2' }, // 'ﾋ' : 'ヒ'
            { '\uFF8C', '\u30D5' }, // 'ﾌ' : 'フ'
            { '\uFF8D', '\u30D8' }, // 'ﾍ' : 'ヘ'
            { '\uFF8E', '\u30DB' }, // 'ﾎ' : 'ホ'
            { '\uFF8F', '\u30DE' }, // 'ﾏ' : 'マ'
            { '\uFF90', '\u30DF' }, // 'ﾐ' : 'ミ'
            { '\uFF91', '\u30E0' }, // 'ﾑ' : 'ム'
            { '\uFF92', '\u30E1' }, // 'ﾒ' : 'メ'
            { '\uFF93', '\u30E2' }, // 'ﾓ' : 'モ'
            { '\uFF94', '\u30E4' }, // 'ﾔ' : 'ヤ'
            { '\uFF95', '\u30E6' }, // 'ﾕ' : 'ユ'
            { '\uFF96', '\u30E8' }, // 'ﾖ' : 'ヨ'
            { '\uFF97', '\u30E9' }, // 'ﾗ' : 'ラ'
            { '\uFF98', '\u30EA' }, // 'ﾘ' : 'リ'
            { '\uFF99', '\u30EB' }, // 'ﾙ' : 'ル'
            { '\uFF9A', '\u30EC' }, // 'ﾚ' : 'レ'
            { '\uFF9B', '\u30ED' }, // 'ﾛ' : 'ロ'
            { '\uFF9C', '\u30EF' }, // 'ﾜ' : 'ワ'
            { '\uFF9D', '\u30F3' }, // 'ﾝ' : 'ン'
            { '\uFF9E', '\u309B' }, // 'ﾞ' : '゛'
            { '\uFF9F', '\u309C' }, // 'ﾟ' : '゜'
        };

        static public string KatakanaHalfToFull(this string text)
        {
            var result = string.Empty;
            for (var i = 0; i < text.Length; i++)
            {               
                if (text[i] == 32)
                {
                    result += (char)12288;
                }
                if (text[i] < 127)
                {
                    result += (char)(text[i] + 65248);
                }
            }

            if (string.IsNullOrEmpty(result))
                result = text;
            return result;
        }

        static public string KatakanaFullToHalf(this string text)
        {
            var result = string.Empty;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] > 65248 && text[i] < 65375)
                {
                    result += (char)(text[i] - 65248);
                }
                else
                {
                    result += (char)text[i];
                }
            }
            return result;
        }

        static public string KanaToUpper(this string text)
        {
            var result = string.Empty;
            for (var i = 0; i < text.Length; i++)
            {
                if (KanaCaseMap.ContainsKey(text[i])) result += KanaCaseMap[text[i]];
                else result += text[i];
            }
            return (result);
        }

        static public string KanaToLower(this string text)
        {
            var result = string.Empty;
            for (var i = 0; i < text.Length; i++)
            {                
                if (KanaCaseMap.ContainsValue(text[i])) result += KanaCaseMap.FirstOrDefault(x => x.Value == text[i]).Key;
                else result += text[i];
            }
            return (result);
        }

        static public async Task<string> ConvertFrom(this string text, Encoding enc, bool IsOEM = false)
        {
            var result = string.Empty;
            try
            {
                if (IsOEM)
                {
                    int cp = System.Globalization.CultureInfo.InstalledUICulture.TextInfo.OEMCodePage;

                    //// 判断韩语
                    //if (Regex.IsMatch(text, @"[\uac00-\ud7ff]+"))
                    //{
                    //    cp = 949;
                    //}
                    //// 判断日语
                    //else if (Regex.IsMatch(text, @"[\u0800-\u4e00]+"))
                    //{
                    //    cp = 932;
                    //}
                    //// 判断中文
                    //else if (Regex.IsMatch(text, @"[\u4e00-\u9fa5]+")) // 如果是中文
                    //{
                    //    cp = 936;
                    //}
                    var cpc = enc.CodePage;
                    string[] subtexts = new string[]{ text };
                    if (cpc == 932 || cpc == 936 || cpc == 950 || cpc == 949 || cpc == 65001)
                        subtexts = text.Split(new char[] { '\u30FB', '\uFF65' });

                    List<string> sl = new List<string>();
                    foreach (var t in subtexts)
                    {
                        byte[] arr = null;
                        var cpEnc = GetTextEncoder(cp);
                        arr = cpEnc.GetBytes(t);
                        //switch (cp)
                        //{
                        //    case 936:
                        //        arr = Encoding.GetEncoding("GBK").GetBytes(t);
                        //        break;
                        //    case 950:
                        //        arr = Encoding.GetEncoding("BIG5").GetBytes(t);
                        //        break;
                        //    case 932:
                        //        arr = Encoding.GetEncoding("Shift-JIS").GetBytes(t);
                        //        break;
                        //    case 949:
                        //        arr = Encoding.GetEncoding("Korean").GetBytes(t);
                        //        break;
                        //    case 1200:
                        //        arr = Encoding.Unicode.GetBytes(t);
                        //        break;
                        //    case 1201:
                        //        arr = Encoding.BigEndianUnicode.GetBytes(t);
                        //        break;
                        //    case 1250:
                        //        arr = GetTextEncoder("1250").GetBytes(t);
                        //        break;
                        //    case 1251:
                        //        arr = GetTextEncoder("1251").GetBytes(t);
                        //        break;
                        //    case 1252:
                        //        arr = GetTextEncoder("1252").GetBytes(t);
                        //        break;
                        //    case 1253:
                        //        arr = GetTextEncoder("1253").GetBytes(t);
                        //        break;
                        //    case 1254:
                        //        arr = GetTextEncoder("1254").GetBytes(t);
                        //        break;
                        //    case 1255:
                        //        arr = GetTextEncoder("1255").GetBytes(t);
                        //        break;
                        //    case 1256:
                        //        arr = GetTextEncoder("1256").GetBytes(t);
                        //        break;
                        //    case 1257:
                        //        arr = GetTextEncoder("1257").GetBytes(t);
                        //        break;
                        //    case 1258:
                        //        arr = GetTextEncoder("1258").GetBytes(t);
                        //        break;
                        //    case 65000:
                        //        arr = Encoding.UTF7.GetBytes(t);
                        //        break;
                        //    case 65001:
                        //        arr = Encoding.UTF8.GetBytes(t);
                        //        break;
                        //    default:
                        //        arr = Encoding.ASCII.GetBytes(t);
                        //        break;
                        //}
                        if (arr != null && arr.Length > 0)
                        {
                            //if(enc == Encoding.Default)
                            if (enc.CodePage == Encoding.Default.CodePage)
                                sl.Add(GetTextEncoder(cp).GetString(arr));
                            else
                                sl.Add(enc.GetString(arr));
                        }                            
                    }
                    result = string.Join("\u30FB", sl);
                }
                else
                {
                    byte[] arr = enc.GetBytes(text);
                    if (arr != null && arr.Length > 0)
                        result = Encoding.Default.GetString(arr);
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        static public async Task<string> ToStringAsync(this byte[] array, Encoding enc)
        {
            var result = string.Empty;
            try
            {
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
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
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

        static public Encoding GetTextEncoder(int codepage)
        {
            var result = Encoding.Default;
            switch (codepage)
            {
                case 936:
                    result = Encoding.GetEncoding("GBK");
                    break;
                case 950:
                    result = Encoding.GetEncoding("BIG5");
                    break;
                case 932:
                    result = Encoding.GetEncoding("Shift-JIS");
                    break;
                case 949:
                    result = Encoding.GetEncoding("Korean");
                    break;
                case 1200:
                    result = Encoding.Unicode;
                    break;
                case 1201:
                    result = Encoding.BigEndianUnicode;
                    break;
                case 1250:
                    result = GetTextEncoder("1250");
                    break;
                case 1251:
                    result = GetTextEncoder("1251");
                    break;
                case 1252:
                    result = GetTextEncoder("1252");
                    break;
                case 1253:
                    result = GetTextEncoder("1253");
                    break;
                case 1254:
                    result = GetTextEncoder("1254");
                    break;
                case 1255:
                    result = GetTextEncoder("1255");
                    break;
                case 1256:
                    result = GetTextEncoder("1256");
                    break;
                case 1257:
                    result = GetTextEncoder("1257");
                    break;
                case 1258:
                    result = GetTextEncoder("1258");
                    break;
                case 65000:
                    result = Encoding.UTF7;
                    break;
                case 65001:
                    result = Encoding.UTF8;
                    break;
                default:
                    result = Encoding.ASCII;
                    break;
            }
            return (result);
        }

        static public Encoding GetTextEncoder(string fmt)
        {
            var result = Encoding.Default;

            var ENC_NAME = fmt.Trim();
            if (string.Equals(ENC_NAME, "UTF8", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.UTF8;
            else if (string.Equals(ENC_NAME, "Unicode", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.Unicode;
            else if (string.Equals(ENC_NAME, "GBK", StringComparison.CurrentCultureIgnoreCase)||
                     string.Equals(ENC_NAME, "936", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("GBK");
            else if (string.Equals(ENC_NAME, "BIG5", StringComparison.CurrentCultureIgnoreCase) ||
                     string.Equals(ENC_NAME, "950", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("BIG5");
            else if (string.Equals(ENC_NAME, "JIS", StringComparison.CurrentCultureIgnoreCase) ||
                     string.Equals(ENC_NAME, "932", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Shift-JIS");
            else if (string.Equals(ENC_NAME, "Korean", StringComparison.CurrentCultureIgnoreCase) ||
                     string.Equals(ENC_NAME, "949", StringComparison.CurrentCultureIgnoreCase))
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
