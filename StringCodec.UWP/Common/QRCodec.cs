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
    public static class QRCodec
    {
        public enum ERRORLEVEL { L, M, Q, H };

        #region Older codes
        #endregion

        private static string monofontname = "Consolas";
        #region Calc ISBN checksum
        private static string CalcISBN_10(string text)
        {
            text = Regex.Replace(text, @"[^0-9]", "");

            if (text.Length < 9) return (string.Empty);
            else if (text.Length > 9) text = text.Substring(0, 9);

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

        private static string CalcISBN_13(string text)
        {
            text = Regex.Replace(text, @"[^0-9]", "");

            if (text.Length < 12) return (string.Empty);
            else if (text.Length > 12) text = text.Substring(0, 12);

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

        #region Calc Code 39 checksum
        private static string CalcCode39(string text, bool sum=false)
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
            result = Regex.Replace(result, @"[^a-zA-Z\d\+\-\/\%\$\.\ ]", "");
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
        
        #region Calc UPC checksum
        private static string CalcUPC_A(string text)
        {
            string result = Regex.Replace(text, @"[^0-9]", "");
            if (result.Length < 11)
            {
                result = string.Empty;
                return (result);
            }
            else if (result.Length > 11) result = result.Substring(0, 11);

            var even = 0;
            var odd = 0;
            for (int i = 0; i < result.Length; i += 2)
                even += Convert.ToInt16($"{result[i]}");
            for (int i = 1; i < result.Length; i += 2)
                odd += Convert.ToInt16($"{result[i]}");
            var sum = even * 3 + odd;
            var checksum = sum % 10;
            if (checksum == 0) checksum = 0;
            else checksum = 10 - checksum;

            return ($"{result}{checksum}");
        }

        private static string CalcUPC_E(string text)
        {
            string result = Regex.Replace(text, @"[^0-9]", "");
            if (result.Length < 6)
            {
                result = string.Empty;
                return (result);
            }
            else if (result.Length > 6) result = result.Substring(0, 6);

            var upca = string.Empty;
            switch (result.Substring(5, 1))
            {
                case "0":
                case "1":
                case "2":
                    upca = Regex.Replace(result, @"([0-9]{2})([1-9]{3})([0-2])", "0$1-$3-0000$2").Replace("-", "");
                    break;
                case "3":
                    upca = Regex.Replace(result, @"([0-9]{3})([1-9]{2})3", "0$1-00000$2").Replace("-", "");
                    break;
                case "4":
                    upca = Regex.Replace(result, @"([0-9]{4})([1-9])4", "0$1-00000$2").Replace("-", "");
                    break;
                default:
                    //
                    // infact, the manufactory code must't contain '0' in this condition, and product code 
                    // must in 5-9, but...
                    //
                    upca = Regex.Replace(result, @"([0-9]{5})([5-9])", "0$1-0000$2").Replace("-", "");
                    break;
            }
            upca = CalcUPC_A(upca);
            if (string.IsNullOrEmpty(upca)) return (string.Empty);
            else return ($"0{result}{upca.Substring(11,1)}");
        }
        #endregion

        #region
        private static string CalcCode93(string text)
        {
            return (Regex.Replace(text, @"[^a-z,A-Z,0-9,_\-\.\$\/\+\%]", "").ToUpper());
        }
        #endregion

        private static void SetDecodeOptions(BarcodeReader br)
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
                    maxlen = 24;
                    height = 300;
                    break;
                case "isbn":
                    fmt = BarcodeFormat.EAN_13;
                    maxlen = 13;
                    margin = 16;
                    height = (int)(width * 26.26 / 37.29);
                    string isbn13 = CalcISBN_13(content);
                    if (string.IsNullOrEmpty(isbn13))
                        return (result);
                    content = isbn13;
                    break;
                case "product":
                    fmt = BarcodeFormat.EAN_13;
                    maxlen = 12;
                    margin = 16;
                    height = (int)(width * 26.26 / 37.29);
                    string prod13 = CalcISBN_13(content);
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
                    content = CalcCode93(content);
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
                    string ean13 = CalcISBN_13(content);
                    if (string.IsNullOrEmpty(ean13))
                        return (result);
                    else
                        content = ean13;
                    break;
                case "upca":
                    fmt = BarcodeFormat.UPC_A;
                    maxlen = 12;
                    margin = 16;
                    height = (int)(width * 25.908 / 37.3126);
                    string upca = CalcUPC_A(content);
                    if (string.IsNullOrEmpty(upca))
                        return (result);
                    else
                        content = upca;
                    break;
                case "upce":
                    fmt = BarcodeFormat.UPC_E;
                    maxlen = 8;
                    margin = 16;
                    height = (int)(width * 0.5);
                    string upce = CalcUPC_E(content);
                    if (string.IsNullOrEmpty(upce))
                        return (result);
                    else
                        content = upce.Substring(0, upce.Length-1);
                    break;
                case "codabar":
                    fmt = BarcodeFormat.CODABAR;
                    maxlen = 984;
                    break;
                case "itf":
                    fmt = BarcodeFormat.ITF;
                    maxlen = 984;
                    height = (int)(width * 0.33333);
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

                #region if EAN-13 then set Mark Space to Barcode
                if (fmt == BarcodeFormat.EAN_13)
                {
                    var mark = bgcolor.ToWriteableBitmap(380, 40);
                    var markRect = new Rect(0, 0, mark.PixelWidth, mark.PixelHeight);
                    var rectMarkL = new Rect(result.PixelWidth / 2 - 15 - mark.PixelWidth, result.PixelHeight - mark.PixelHeight, mark.PixelWidth, mark.PixelHeight);
                    var rectMarkR = new Rect(result.PixelWidth / 2 + 15, result.PixelHeight - mark.PixelHeight, mark.PixelWidth, mark.PixelHeight);
                    result.Blit(rectMarkL, mark, markRect, WriteableBitmapExtensions.BlendMode.None);
                    result.Blit(rectMarkR, mark, markRect, WriteableBitmapExtensions.BlendMode.None);
                }
                else if (fmt == BarcodeFormat.UPC_A)
                {
                    var mark = bgcolor.ToWriteableBitmap(315, 40);
                    var markRect = new Rect(0, 0, mark.PixelWidth, mark.PixelHeight);
                    var rectMarkL = new Rect(result.PixelWidth / 2 - 15 - mark.PixelWidth, result.PixelHeight - mark.PixelHeight, mark.PixelWidth, mark.PixelHeight);
                    var rectMarkR = new Rect(result.PixelWidth / 2 + 15, result.PixelHeight - mark.PixelHeight, mark.PixelWidth, mark.PixelHeight);
                    result.Blit(rectMarkL, mark, markRect, WriteableBitmapExtensions.BlendMode.None);
                    result.Blit(rectMarkR, mark, markRect, WriteableBitmapExtensions.BlendMode.None);
                }
                else if (fmt == BarcodeFormat.UPC_E)
                {
                    var mark = bgcolor.ToWriteableBitmap(600, 40);
                    var markRect = new Rect(0, 0, mark.PixelWidth, mark.PixelHeight);
                    var rectMark = new Rect((result.PixelWidth - mark.PixelWidth) / 2 - 15, result.PixelHeight - mark.PixelHeight, mark.PixelWidth, mark.PixelHeight);
                    result.Blit(rectMark, mark, markRect, WriteableBitmapExtensions.BlendMode.None);
                }
                #endregion

                var labels = content.BarcodeLabel(format, checksum);
                if (textsize <= 0) labels.Clear();
                switch (labels.Count)
                {
                    case 1:
                        var label = labels[0];
                        if (!string.IsNullOrEmpty(label))
                        {
                            var labelimage = await label.ToWriteableBitmap(monofontname, FontStyle.Normal, textsize, fgcolor, bgcolor);
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
                            var labelimage_t = await label_t.ToWriteableBitmap(monofontname, FontStyle.Normal, textsize, fgcolor, bgcolor);
                            var labelimage_b = await label_b.ToWriteableBitmap(monofontname, FontStyle.Normal, textsize, fgcolor, bgcolor);
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
                            var labelimage_t = await label_t.ToWriteableBitmap(monofontname, FontStyle.Normal, textsize, fgcolor, bgcolor);
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
                            var labelimage_b = await label_t.ToWriteableBitmap(monofontname, FontStyle.Normal, textsize, fgcolor, bgcolor);
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
                await new MessageDialog(ex.Message, "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        public static List<string> BarcodeLabel(this string content, string format, bool checksum)
        {
            List<string> result = new List<string>();
            switch (format.ToLower())
            {
                case "express":
                    result.Add(content);
                    break;
                case "isbn":
                    string isbn13 = CalcISBN_13(content);
                    if (string.IsNullOrEmpty(isbn13)) result.Clear();
                    else
                    {
                        //OneDimensionalCodeWriter.CalculateChecksumDigitModulo10(content);
                        result.Add($"ISBN {isbn13.Insert(12, "-").Insert(9, "-").Insert(4, "-").Insert(3, "-")}");
                        result.Add($"{isbn13.Insert(7, "     ").Insert(1, "   ")}   >");
                    }
                    break;
                case "product":
                    string prod = CalcISBN_13(content);
                    if (string.IsNullOrEmpty(prod)) result.Clear();
                    else result.Add($"{prod.Insert(7, "     ").Insert(1, "   ")}   >");
                    break;
                case "ean13":
                    string ean13 = CalcISBN_13(content);
                    if (string.IsNullOrEmpty(ean13)) result.Clear();
                    else result.Add($"{ean13.Insert(7, "     ").Insert(1, "   ")}   >");
                    break;
                case "39":
                    var code39 = CalcCode39(content, checksum);
                    if (string.IsNullOrEmpty(code39)) result.Clear();
                    else
                    {
                        if (checksum) code39 = code39.Substring(0, code39.Length - 1);
                        result.Add($"*{code39}*");
                    }
                    break;
                case "93":
                    var code93 = CalcCode93(content);
                    if (string.IsNullOrEmpty(code93)) result.Clear();
                    else result.Add(code93);
                    break;
                case "128":
                    result.Add(content);
                    break;
                case "itf":
                    result.Add(content);
                    break;
                case "upca":
                    string upca = CalcUPC_A(content);
                    if (string.IsNullOrEmpty(upca)) result.Clear();
                    else result.Add(upca.Substring(0,12).Insert(11, "    ").Insert(6, "     ").Insert(1, "    "));
                    break;
                case "upce":
                    string upce = CalcUPC_E(content.Substring(1));
                    if (string.IsNullOrEmpty(upce)) result.Clear();
                    else result.Add(upce.Insert(7, "       ").Insert(1, "      "));
                    break;
                default:
                    result.Clear();
                    break;
            }
            return (result);
        }

        public static WriteableBitmap EncodeQR(this string content, Color fgcolor, Color bgcolor, ERRORLEVEL ECL)
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

        public static async Task<string> Decode(this WriteableBitmap image)
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
                await new MessageDialog(ex.Message, "ERROR".T()).ShowAsync();
            }
            return (result);
        }
    }
}
