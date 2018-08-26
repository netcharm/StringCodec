using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.QrCode.Internal;
using ZXing.Rendering;

namespace StringCodec.UWP.Common
{
    static public class QRCodec
    {
        public enum ERRORLEVEL { L, M, Q, H };

        #region Older codes
        #endregion

        #region Calc ISBN checksum
        static private string CalcISBN_10(string text)
        {
            text = text.Replace("-", "").Replace(" ", "");

            long value = 0;
            if (!long.TryParse(text, out value)) return (string.Empty);
            if (text.Length != 9 && text.Length != 10)
            {
                if (text.Length == 12 || text.Length == 13)
                {
                    text = text.Substring(3, 9);
                }
                else
                    return (string.Empty);
            }

            int[] w = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            var cd = 0;
            for (int i = 0; i < 9; i++)
            {
                cd += (int)Char.GetNumericValue(text[i]) * w[i];
            }
            var N = cd % 11;
            if (N == 10)
                cd = 'X';
            else
                cd = (N == 11) ? 0 : N;
            return text.Substring(0, 9) + cd.ToString();
        }

        static private string CalcISBN_13(string text)
        {
            text = text.Replace("-", "").Replace(" ", "");

            long value = 0;
            if (!long.TryParse(text, out value)) return (string.Empty);
            if (text.Length != 12 && text.Length != 13) return (string.Empty);

            int[] w = { 1, 3, 1, 3, 1, 3, 1, 3, 1, 3, 1, 3 };
            var cd = 0;
            for (int i = 0; i < 12; i++)
            {
                cd += (int)Char.GetNumericValue(text[i]) * w[i];
            }
            cd = (cd % 10 == 0) ? 0 : 10 - (cd % 10);
            return text.Substring(0, 12) + cd.ToString();
        }
        #endregion

        #region
        static private string CalcCode39(string text, bool sum=false)
        {
            string result = string.Empty;
            Dictionary<char, int> charmap_39 = new Dictionary<char, int>() {
                {'0', 0}, {'1', 1}, {'2', 2}, {'3', 3}, {'4', 4}, {'5', 5}, {'6', 6}, {'7', 7}, {'8', 8}, {'9', 9},
                {'A', 10}, {'B', 11}, {'C', 12}, {'D', 13}, {'E', 14}, {'F', 15}, {'G', 16}, {'H', 17}, {'I', 18}, {'J', 19},
                {'K', 20}, {'L', 21}, {'M', 22}, {'N', 23}, {'O', 24}, {'P', 25}, {'Q', 26}, {'R', 27}, {'S', 28}, {'T', 29},
                {'U', 30}, {'V', 31}, {'W', 32}, {'X', 33}, {'Y', 34}, {'Z', 35}, {'-', 36}, {'.', 37}, {' ', 38}, {'$', 39},
                {'/', 40}, {'+', 41}, {'%', 42}
            };
            result = Regex.Replace(text, @"[a-z]", "+$0").ToUpper();
            result = Regex.Replace(result, @"[^a-zA-Z\d\+\-\/\%\$\.\*\ ]", "");
            //var mo = Regex.Matches(text, @"([a-z])|([A-Z])|(\d)|([\+\-\*\/\%\$\.\*\ ])", RegexOptions.Multiline);

            if (sum)
            {
                var checksum = 0;
                foreach (char c in result)
                {
                    if (c == '*' || !charmap_39.ContainsKey(c)) continue;
                    checksum += charmap_39[c];
                }
                checksum = checksum % 43;
                result = $"{result}{charmap_39.FirstOrDefault(x => x.Value == checksum).Key}";
            }
            return (result);
        }
        #endregion

        static private void SetDecodeOptions(BarcodeReader br)
        {
            br.AutoRotate = true;
            br.TryInverted = true;
            br.Options.CharacterSet = "UTF-8";
            br.Options.TryHarder = true;
            br.Options.PureBarcode = false;
            br.Options.ReturnCodabarStartEnd = true;
            br.Options.UseCode39ExtendedMode = true;
            br.Options.UseCode39RelaxedExtendedMode = true;
            br.Options.PossibleFormats = new List<BarcodeFormat>
            {
                BarcodeFormat.All_1D,
                BarcodeFormat.DATA_MATRIX,
                BarcodeFormat.AZTEC,
                BarcodeFormat.PDF_417,
                BarcodeFormat.QR_CODE
            };
            //br.Options.PossibleFormats.Add(BarcodeFormat.DATA_MATRIX);
            //br.Options.PossibleFormats.Add(BarcodeFormat.QR_CODE);
        }

        static async public Task<WriteableBitmap> EncodeBarcode(this string content, string format, Color fgcolor, Color bgcolor, int textsize, bool checksum)
        {
            WriteableBitmap result = new WriteableBitmap(1,1);
            if (content.Length <= 0) return (result);

            int maxlen = 0;
            int width = 1024;
            int height = 512;
            int margin = 7;
            var fmt = BarcodeFormat.CODE_39;
            switch (format.ToLower())
            {
                case "express":
                    fmt = BarcodeFormat.CODE_128;
                    maxlen = 48;
                    height = 300;
                    break;
                case "isbn":
                    fmt = BarcodeFormat.EAN_13;
                    maxlen = 13;
                    margin = 16;
                    height = (int)(width * 26.26 / 37.29);
                    string isbn13 = content.Length >= 12 ? CalcISBN_13(content.Substring(0, 12)) : string.Empty;
                    if (string.IsNullOrEmpty(isbn13))
                        return (result);
                    content = isbn13;
                    break;
                case "product":
                    fmt = BarcodeFormat.EAN_13;
                    maxlen = 13;
                    margin = 16;
                    height = (int)(width * 26.26 / 37.29);
                    string prod13 = content.Length >= 12 ? CalcISBN_13(content.Substring(0, 12)) : string.Empty;
                    if (string.IsNullOrEmpty(prod13))
                        return (result);
                    else
                        content = prod13;
                    break;
                case "39":
                    fmt = BarcodeFormat.CODE_39;
                    maxlen = 1024;
                    content = CalcCode39(content, checksum);
                    break;
                case "93":
                    fmt = BarcodeFormat.CODE_93;
                    maxlen = 984;
                    break;
                case "128":
                    fmt = BarcodeFormat.CODE_128;
                    maxlen = 984;
                    break;
                case "ean13":
                    fmt = BarcodeFormat.EAN_13;
                    maxlen = 13;
                    margin = 16;
                    height = (int)(width * 26.26 / 37.29);
                    string ean13 = content.Length >= 12 ? CalcISBN_13(content.Substring(0, 12)) : string.Empty;
                    if (string.IsNullOrEmpty(ean13))
                        return (result);
                    else
                        content = ean13;
                    break;
                case "upca":
                    fmt = BarcodeFormat.UPC_A;
                    maxlen = 984;
                    break;
                case "upce":
                    fmt = BarcodeFormat.UPC_E;
                    maxlen = 984;
                    break;
                case "codabar":
                    fmt = BarcodeFormat.CODABAR;
                    maxlen = 984;
                    break;
                default:
                    fmt = BarcodeFormat.CODE_128;
                    maxlen = 48;
                    break;
            }

            if (fmt == BarcodeFormat.QR_CODE) return (EncodeQR(content, fgcolor, bgcolor, ERRORLEVEL.L));

            var text = content.Length > maxlen ? content.Substring(0, maxlen) : content;

            try
            {
                var bw = new BarcodeWriter();
                bw.Options.Width = width;
                bw.Options.Height = height;
                bw.Options.PureBarcode = true;
                bw.Options.GS1Format = true;
                bw.Options.Hints.Add(EncodeHintType.MARGIN, margin);
                bw.Options.Hints.Add(EncodeHintType.DISABLE_ECI, true);
                bw.Options.Hints.Add(EncodeHintType.CHARACTER_SET, "UTF-8");

                bw.Format = fmt;
                bw.Renderer = new WriteableBitmapRenderer() { Foreground = fgcolor, Background = bgcolor };

                BitMatrix bm = bw.Encode(text);
                int[] rectangle = bm.getEnclosingRectangle();
                var bmW = rectangle[2];
                var bmH = rectangle[3];
                bw.Options.Width = (int)(bmW * 1.25);
                //bw.Options.Height = (int)(bmH * 1.25);
                result = bw.Write(text);

                int l = (int)Math.Ceiling(result.PixelWidth * 0.05);
                int t = (int)Math.Ceiling(result.PixelHeight * 0.05);
                int r = (int)Math.Ceiling(result.PixelWidth * 0.05);
                int b = (int)Math.Ceiling(result.PixelHeight * 0.05);
                var rectBarcode = new Rect(l, t, result.PixelWidth, result.PixelHeight);

                var labels = content.BarcodeLabel(format, checksum);
                if (textsize <= 0) labels.Clear();
                switch (labels.Count)
                {
                    case 1:
                        var label = labels[0];
                        if (!string.IsNullOrEmpty(label))
                        {
                            var labelimage = await label.ToBitmap("Consolas", FontStyle.Normal, textsize, fgcolor, bgcolor);
                            result = result.Extend(l, t, r, b + labelimage.PixelHeight, bgcolor);
                            var dstRect = new Rect((result.PixelWidth - labelimage.PixelWidth) / 2, result.PixelHeight - labelimage.PixelHeight - b, labelimage.PixelWidth, labelimage.PixelHeight);
                            if (dstRect.Width > rectBarcode.Width)
                            {
                                labelimage = labelimage.Resize((int)rectBarcode.Width, (int)dstRect.Height, WriteableBitmapExtensions.Interpolation.Bilinear);
                                dstRect = new Rect(rectBarcode.X, dstRect.Y, rectBarcode.Width, dstRect.Height);
                            }
                            var srcRect = new Rect(0, 0, labelimage.PixelWidth, labelimage.PixelHeight);
                            result.Blit(dstRect, labelimage, srcRect, WriteableBitmapExtensions.BlendMode.None);
                        }
                        break;
                    case 2:
                        var label_t = labels[0];
                        var label_b = labels[1];
                        if (!string.IsNullOrEmpty(label_t) && !string.IsNullOrEmpty(label_b))
                        {
                            var labelimage_t = await label_t.ToBitmap("Consolas", FontStyle.Normal, textsize, fgcolor, bgcolor);
                            var labelimage_b = await label_b.ToBitmap("Consolas", FontStyle.Normal, textsize, fgcolor, bgcolor);
                            result = result.Extend(l, t + labelimage_t.PixelHeight, r, b + labelimage_b.PixelHeight, bgcolor);

                            var dstRect_t = new Rect((result.PixelWidth - labelimage_t.PixelWidth) / 2, t,
                                labelimage_t.PixelWidth, labelimage_t.PixelHeight);
                            if (dstRect_t.Width > rectBarcode.Width)
                            {
                                labelimage_t = labelimage_t.Resize((int)rectBarcode.Width, (int)dstRect_t.Height, WriteableBitmapExtensions.Interpolation.Bilinear);
                                dstRect_t = new Rect(rectBarcode.X, dstRect_t.Y, rectBarcode.Width, dstRect_t.Height);
                            }
                            var srcRect_t = new Rect(0, 0, labelimage_t.PixelWidth, labelimage_t.PixelHeight);
                            result.Blit(dstRect_t, labelimage_t, srcRect_t, WriteableBitmapExtensions.BlendMode.None);

                            var dstRect_b = new Rect((result.PixelWidth - labelimage_b.PixelWidth) / 2, result.PixelHeight - labelimage_b.PixelHeight - b,
                                labelimage_b.PixelWidth, labelimage_b.PixelHeight);
                            if (dstRect_b.Width > rectBarcode.Width)
                            {
                                labelimage_b = labelimage_b.Resize((int)rectBarcode.Width, (int)dstRect_b.Height, WriteableBitmapExtensions.Interpolation.Bilinear);
                                dstRect_b = new Rect(rectBarcode.X, dstRect_b.Y, rectBarcode.Width, dstRect_b.Height);
                            }
                            var srcRect_b = new Rect(0, 0, labelimage_b.PixelWidth, labelimage_b.PixelHeight);
                            result.Blit(dstRect_b, labelimage_b, srcRect_b, WriteableBitmapExtensions.BlendMode.None);
                        }
                        else if(!string.IsNullOrEmpty(label_t) && string.IsNullOrEmpty(label_b))
                        {
                            var labelimage_t = await label_t.ToBitmap("Consolas", FontStyle.Normal, textsize, fgcolor, bgcolor);
                            result = result.Extend(l, t + labelimage_t.PixelHeight, r, b, bgcolor);

                            var dstRect_t = new Rect((result.PixelWidth - labelimage_t.PixelWidth) / 2, t,
                                labelimage_t.PixelWidth, labelimage_t.PixelHeight);
                            if (dstRect_t.Width > rectBarcode.Width)
                            {
                                labelimage_t = labelimage_t.Resize((int)rectBarcode.Width, (int)dstRect_t.Height, WriteableBitmapExtensions.Interpolation.Bilinear);
                                dstRect_t = new Rect(rectBarcode.X, dstRect_t.Y, rectBarcode.Width, dstRect_t.Height);
                            }
                            var srcRect_t = new Rect(0, 0, labelimage_t.PixelWidth, labelimage_t.PixelHeight);
                            result.Blit(dstRect_t, labelimage_t, srcRect_t, WriteableBitmapExtensions.BlendMode.None);
                        }
                        else if (string.IsNullOrEmpty(label_t) && !string.IsNullOrEmpty(label_b))
                        {
                            var labelimage_b = await label_t.ToBitmap("Consolas", FontStyle.Normal, textsize, fgcolor, bgcolor);
                            result = result.Extend(l, t, r, b + labelimage_b.PixelHeight, bgcolor);

                            var dstRect_b = new Rect((result.PixelWidth - labelimage_b.PixelWidth) / 2, result.PixelHeight - labelimage_b.PixelHeight - b,
                                labelimage_b.PixelWidth, labelimage_b.PixelHeight);
                            if (dstRect_b.Width > rectBarcode.Width)
                            {
                                labelimage_b = labelimage_b.Resize((int)rectBarcode.Width, (int)dstRect_b.Height, WriteableBitmapExtensions.Interpolation.Bilinear);
                                dstRect_b = new Rect(rectBarcode.X, dstRect_b.Y, rectBarcode.Width, dstRect_b.Height);
                            }
                            var srcRect_b = new Rect(0, 0, labelimage_b.PixelWidth, labelimage_b.PixelHeight);
                            result.Blit(dstRect_b, labelimage_b, srcRect_b, WriteableBitmapExtensions.BlendMode.None);
                        }
                        break;
                    default:
                        result = result.Extend(l, t, r, b, bgcolor);
                        break;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "ERROR").ShowAsync();
            }
            return (result);
        }

        static public List<string> BarcodeLabel(this string content, string format, bool checksum)
        {
            List<string> result = new List<string>();
            switch (format.ToLower())
            {
                case "express":
                    result.Add(content);
                    break;
                case "isbn":
                    string isbn13 = content.Length >= 12 ? CalcISBN_13(content.Substring(0, 12)) : string.Empty;
                    if (string.IsNullOrEmpty(isbn13)) return (result);
                    //OneDimensionalCodeWriter.CalculateChecksumDigitModulo10(content);
                    result.Add($"ISBN {isbn13.Insert(12, "-").Insert(3, "-")}");
                    result.Add($"{isbn13.Insert(7, "   ").Insert(1, "   ")}   <");
                    break;
                case "product":
                    string prod13 = content.Length >= 12 ? CalcISBN_13(content.Substring(0, 12)) : string.Empty;
                    if (string.IsNullOrEmpty(prod13)) return (result);
                    result.Add($"{prod13.Insert(7, "   ").Insert(1, "   ")}   <");
                    break;
                case "39":
                    var code39 = (Regex.Replace(content, @"[^a-zA-Z\d\+\-\*\/\%\$\.\*\ ]", ""));
                    if (checksum) code39 = code39.Substring(0, code39.Length - 1);
                    result.Add(code39);
                    break;
                default:
                    result.Clear();
                    break;
            }
            return (result);
        }

        static public WriteableBitmap EncodeQR(this string content, Color fgcolor, Color bgcolor, ERRORLEVEL ECL)
        {
            WriteableBitmap result = new WriteableBitmap(1, 1);
            if (content.Length <= 0) return(result);

            ErrorCorrectionLevel ecl = ErrorCorrectionLevel.L;
            switch (ECL)
            {
                case ERRORLEVEL.L:
                    ecl = ErrorCorrectionLevel.L;
                    break;
                case ERRORLEVEL.M:
                    ecl = ErrorCorrectionLevel.M;
                    break;
                case ERRORLEVEL.Q:
                    ecl = ErrorCorrectionLevel.Q;
                    break;
                case ERRORLEVEL.H:
                    ecl = ErrorCorrectionLevel.H;
                    break;
                default:
                    ecl = ErrorCorrectionLevel.L;
                    break;
            }

            var bw = new BarcodeWriter();
            bw.Options.Width = 1024;
            bw.Options.Height = 1024;
            bw.Options.PureBarcode = false;
            bw.Options.Hints.Add(EncodeHintType.ERROR_CORRECTION, ecl);
            bw.Options.Hints.Add(EncodeHintType.MARGIN, 2);
            bw.Options.Hints.Add(EncodeHintType.DISABLE_ECI, true);
            bw.Options.Hints.Add(EncodeHintType.CHARACTER_SET, "UTF-8");

            bw.Format = BarcodeFormat.QR_CODE;
            bw.Renderer = new WriteableBitmapRenderer() { Foreground = fgcolor, Background = bgcolor };

            //var renderer = new SvgRenderer();
            //var img = renderer.Render(new BitMatrix(512), BarcodeFormat.QR_CODE, content);
            var text = content.Length>984 ? content.Substring(0, 984) : content;
            result = bw.Write(text);
            return (result);
        }

        static public async Task<string> Decode(this WriteableBitmap image)
        {
            string result = string.Empty;
            if (image == null) return (result);
            if (image.PixelWidth < 32 || image.PixelHeight < 32) return (result);

            var br = new BarcodeReader();
            SetDecodeOptions(br);
            try
            {
                //var qrResult = br.Decode(image);
                //if (qrResult != null)
                //{
                //    result = qrResult.Text;
                //}
                //var qrResults = br.DecodeMultiple(image);
                var qrResults = br.DecodeMultiple(image);
                var textList = new List<string>();
                if (qrResults == null) return (result);
                foreach (var line in qrResults)
                {
                    textList.Add(line.Text);
                }
                if (textList.Count >= 0)
                {
                    result = string.Join("\n\r", textList);
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "ERROR").ShowAsync();
            }
            return (result);
        }
    }
}
