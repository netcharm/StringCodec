using System;
using System.Collections.Generic;
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

        static async public Task<WriteableBitmap> EncodeBarcode(this string content, string format, Color fgcolor, Color bgcolor, int textsize)
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
                case "link":
                    fmt = BarcodeFormat.QR_CODE;
                    maxlen = 984;
                    if (!content.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||
                       !content.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                    {
                        content = "http://" + content;
                    }
                    break;
                case "tele":
                    fmt = BarcodeFormat.QR_CODE;
                    maxlen = 984;
                    if (!content.StartsWith("tel:", StringComparison.InvariantCultureIgnoreCase))
                    {
                        content = "TEL:" + content;
                    }
                    break;
                case "mail":
                    fmt = BarcodeFormat.QR_CODE;
                    maxlen = 984;
                    if (!content.StartsWith("mailto:", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var mail = "abc@abc.com";
                        var subject = "main to ...";
                        content = $"MAILTO:{mail}?SUBJECT={subject}&BODY={content}";
                    }
                    break;
                case "sms":
                    fmt = BarcodeFormat.QR_CODE;
                    maxlen = 984;
                    if (!content.StartsWith("smsto:", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var phone = "1234567890";
                        content = $"SMSTO:{phone}:{content}";
                    }
                    break;
                case "vcard":
                    fmt = BarcodeFormat.QR_CODE;
                    maxlen = 984;
                    break;
                case "vcal":
                    fmt = BarcodeFormat.QR_CODE;
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
                bw.Options.PureBarcode = false;
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

                var label = content.BarcodeLabel(format);
                var labelimage = await label.ToBitmap("Consolas", FontStyle.Normal, textsize, fgcolor, bgcolor);
                result = result.Extend(l, t, r, b + labelimage.PixelHeight, bgcolor);
                var dstRect = new Rect((result.PixelWidth - labelimage.PixelWidth) / 2, result.PixelHeight - labelimage.PixelHeight - b, labelimage.PixelWidth, labelimage.PixelHeight);
                var srcRect = new Rect(0, 0, labelimage.PixelWidth, labelimage.PixelHeight);
                result.Blit(dstRect, labelimage, srcRect, WriteableBitmapExtensions.BlendMode.None);
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "ERROR").ShowAsync();
            }
            return (result);
        }

        static public string BarcodeLabel(this string content, string format)
        {
            string result = content;
            switch (format.ToLower())
            {
                case "express":
                    result = content;
                    break;
                case "isbn":
                    string isbn13 = content.Length >= 12 ? CalcISBN_13(content.Substring(0, 12)) : string.Empty;
                    if (string.IsNullOrEmpty(isbn13))
                        return (string.Empty);
                    result = isbn13; //OneDimensionalCodeWriter.CalculateChecksumDigitModulo10(content);
                    result = result.Insert(7, "   ");
                    result = result.Insert(1, "   ");
                    break;
                case "product":
                    result = content;
                    break;
                default:
                    result = string.Empty;
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
