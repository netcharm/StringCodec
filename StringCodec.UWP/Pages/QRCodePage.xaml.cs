using StringCodec.UWP.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Capture;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace StringCodec.UWP.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class QRCodePage : Page
    {
        private QRCodec.ERRORLEVEL CURRENT_ECL = QRCodec.ERRORLEVEL.L;
        private Color CURRENT_BGCOLOR = Color.FromArgb(255, 255, 255, 255);
        private Color CURRENT_FGCOLOR = Color.FromArgb(255, 000, 000, 000);
        private int CURRENT_SIZE = 512;

        static private string text_src = string.Empty;
        static public string Text
        {
            get { return text_src; }
            set { text_src = value; }
        }

        private async void SetClipboard(Image image, int size)
        {
            if (image.Source == null) return;

            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;

            //Uri uri = new Uri("ms-appx:///Assets/ms.png");
            //StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            //dp.SetBitmap(RandomAccessStreamReference.CreateFromUri(uri));

            using (var fileStream = new InMemoryRandomAccessStream())
            {
                //把控件变成图像
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                //传入参数Image控件
                await renderTargetBitmap.RenderAsync(image, size, size);
                var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                var width = renderTargetBitmap.PixelWidth;
                var height = renderTargetBitmap.PixelHeight;
                if (size > 0)
                {
                    width = size;
                    height = size;
                }

                //await fileStream.WriteAsync(pixelBuffer);
                //fileStream.Seek(0);

                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                    (uint)width, (uint)height,
                    dpi, dpi,
                    pixelBuffer.ToArray());
                await encoder.FlushAsync();

                dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(fileStream));
                Clipboard.SetContent(dataPackage);
            }
        }

        private async Task<string> GetClipboard(string text, Image image=null)
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                string content = await dataPackageView.GetTextAsync();
                // To output the text from this example, you need a TextBlock control
                return (content);
            }
            else if (dataPackageView.Contains(StandardDataFormats.Bitmap))
            {
                try
                {
                    var bmp = await dataPackageView.GetBitmapAsync();
                    WriteableBitmap bitmap = new WriteableBitmap(1, 1);
                    await bitmap.SetSourceAsync(await bmp.OpenReadAsync());

                    if (image != null)
                    {
                        image.Source = bitmap;
                        //text = await QRCodec.Decode(bitmap);
                    }
                }
                catch (Exception) { }
            }
            return (text);
        }

        private async Task<Color> ShowColorDialog(Color color)
        {
            Color result = color;

            ColorDialog dlgColor = new ColorDialog() { Color=color, Alpha=false };
            ContentDialogResult ret = await dlgColor.ShowAsync();
            if (ret == ContentDialogResult.Primary)
            {
                result = dlgColor.Color;
            }

            return (result);
        }

        private async Task<string> ShowSaveDialog(Image image, int size)
        {
            if (image.Source == null) return (string.Empty);

            var now = DateTime.Now;
            FileSavePicker fp = new FileSavePicker();
            fp.SuggestedStartLocation = PickerLocationId.Desktop;
            fp.FileTypeChoices.Add("Image File", new List<string>() { ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".gif", ".bmp" });
            fp.SuggestedFileName = $"QR_{now.ToString("yyyyMMddhhmmss")}.png";
            StorageFile TargetFile = await fp.PickSaveFileAsync();
            if (TargetFile != null)
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Add(TargetFile, TargetFile.Name);
                if (StorageApplicationPermissions.FutureAccessList.Entries.Count >= 1000)
                    StorageApplicationPermissions.FutureAccessList.Remove(StorageApplicationPermissions.FutureAccessList.Entries.Last().Token);
                StorageApplicationPermissions.FutureAccessList.Add(TargetFile, TargetFile.Name);

                // 在用户完成更改并调用CompleteUpdatesAsync之前，阻止对文件的更新
                CachedFileManager.DeferUpdates(TargetFile);

                #region Save Image Control source data
                //using (var fileStream = await TargetFile.OpenAsync(FileAccessMode.ReadWrite))
                //{
                //    var bmp = imgQR.Source as WriteableBitmap;
                //    var w = bmp.PixelWidth;
                //    var h = bmp.PixelHeight;

                //    // set the source for WriteableBitmap  
                //    //await bmp.SetSourceAsync(fileStream);

                //    // Get pixels of the WriteableBitmap object 
                //    Stream pixelStream = bmp.PixelBuffer.AsStream();
                //    byte[] pixels = new byte[pixelStream.Length];
                //    await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                //    var encId = BitmapEncoder.PngEncoderId;
                //    var fext = Path.GetExtension(TargetFile.Name).ToLower();
                //    switch (fext)
                //    {
                //        case ".bmp":
                //            encId = BitmapEncoder.BmpEncoderId;
                //            break;
                //        case ".gif":
                //            encId = BitmapEncoder.GifEncoderId;
                //            break;
                //        case ".png":
                //            encId = BitmapEncoder.PngEncoderId;
                //            break;
                //        case ".jpg":
                //            encId = BitmapEncoder.JpegEncoderId;
                //            break;
                //        case ".jpeg":
                //            encId = BitmapEncoder.JpegEncoderId;
                //            break;
                //        case ".tif":
                //            encId = BitmapEncoder.TiffEncoderId;
                //            break;
                //        case ".tiff":
                //            encId = BitmapEncoder.TiffEncoderId;
                //            break;
                //        default:
                //            encId = BitmapEncoder.PngEncoderId;
                //            break;
                //    }
                //    var encoder = await BitmapEncoder.CreateAsync(encId, fileStream);
                //    // Save the image file with jpg extension 
                //    encoder.SetPixelData(
                //        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                //        //(uint)bmp.PixelWidth, (uint)bmp.PixelHeight, 
                //        (uint)size, (uint)size,
                //        96.0, 96.0, 
                //        pixels);
                //    await encoder.FlushAsync();
                //}
                //return (TargetFile.Name);
                #endregion

                #region Save Image Control Screen Display Data
                //把控件变成图像
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                //传入参数Image控件
                await renderTargetBitmap.RenderAsync(image, size, size);
                var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                using (var fileStream = await TargetFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var width = renderTargetBitmap.PixelWidth;
                    var height = renderTargetBitmap.PixelHeight;
                    var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                    if (size > 0)
                    {
                        width = size;
                        height = size;
                    }
                    var encId = BitmapEncoder.PngEncoderId;
                    var fext = Path.GetExtension(TargetFile.Name).ToLower();
                    switch (fext)
                    {
                        case ".bmp":
                            encId = BitmapEncoder.BmpEncoderId;
                            break;
                        case ".gif":
                            encId = BitmapEncoder.GifEncoderId;
                            break;
                        case ".png":
                            encId = BitmapEncoder.PngEncoderId;
                            break;
                        case ".jpg":
                            encId = BitmapEncoder.JpegEncoderId;
                            break;
                        case ".jpeg":
                            encId = BitmapEncoder.JpegEncoderId;
                            break;
                        case ".tif":
                            encId = BitmapEncoder.TiffEncoderId;
                            break;
                        case ".tiff":
                            encId = BitmapEncoder.TiffEncoderId;
                            break;
                        default:
                            encId = BitmapEncoder.PngEncoderId;
                            break;
                    }
                    var encoder = await BitmapEncoder.CreateAsync(encId, fileStream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                        (uint)width, (uint)height, dpi, dpi,
                        pixelBuffer.ToArray()
                    );
                    //刷新图像
                    await encoder.FlushAsync();
                }
                return (TargetFile.Name);
                #endregion
            }
            return (string.Empty);
        }

        public QRCodePage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            optECL_L.IsChecked = true;
            optSaveSizeM.IsChecked = true;

            if (!string.IsNullOrEmpty(text_src))
            {
                edQR.Text = text_src;
                imgQR.Source = await QRCodec.Encode(edQR.Text, CURRENT_FGCOLOR, CURRENT_BGCOLOR, CURRENT_ECL);
            }

            if (!GraphicsCaptureSession.IsSupported())
            {
                // Hide the capture UI if screen capture is not supported.
                //btnDecode.Visibility = Visibility.Collapsed;
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            switch (btn.Name)
            {
                case "btnEncode":
                    imgQR.Source = await QRCodec.Encode(edQR.Text, CURRENT_FGCOLOR, CURRENT_BGCOLOR, CURRENT_ECL);
                    break;
                case "btnDecode":
                    //var sc = new ScreenCapture();
                    //sc.Preview = imgQR;
                    //await sc.StartCaptureAsync();
                    edQR.Text = await QRCodec.Decode(imgQR.Source as WriteableBitmap);
                    break;
                case "btnCopy":
                    SetClipboard(imgQR, CURRENT_SIZE);
                    break;
                case "btnPaste":
                    edQR.Text = await GetClipboard(edQR.Text, imgQR);
                    //edQR.Text = await QRCodec.Decode(imgQR.Source as WriteableBitmap);
                    break;
                case "btnSave":
                    await ShowSaveDialog(imgQR, CURRENT_SIZE);
                    break;
                default:
                    break;
            }
        }

        private void OptECL_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] opts = new ToggleMenuFlyoutItem[] { optECL_L, optECL_M, optECL_Q, optECL_H };

            var btn = sender as ToggleMenuFlyoutItem;
            var ECL_NAME = btn.Name.Substring(btn.Name.Length-1);

            foreach (ToggleMenuFlyoutItem opt in opts)
            {
                if (string.Equals(opt.Name, btn.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    opt.IsChecked = true;
                }
                else opt.IsChecked = false;
            }

            CURRENT_ECL = (QRCodec.ERRORLEVEL)Enum.Parse(typeof(QRCodec.ERRORLEVEL), ECL_NAME);
        }

        private void edQR_TextChanged(object sender, TextChangedEventArgs e)
        {
            lblInfo.Text = $"Count(<=984): {edQR.Text.Length}";
        }

        private async void OptColor_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as MenuFlyoutItem;
            var C_NAME = btn.Name.Substring(3);

            switch (C_NAME)
            {
                case "ResetColor":
                    CURRENT_BGCOLOR = Color.FromArgb(255, 255, 255, 255);
                    CURRENT_FGCOLOR = Color.FromArgb(255, 000, 000, 000);
                    break;
                case "BgColor":
                    CURRENT_BGCOLOR = await ShowColorDialog(CURRENT_BGCOLOR);
                    break;
                case "FgColor":
                    CURRENT_FGCOLOR = await ShowColorDialog(CURRENT_FGCOLOR);
                    break;
                default:
                    break;
            }
        }

        private void ConfirmColor_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)optBgColor.Tag)
            {
                CURRENT_BGCOLOR = myColorPicker.Color;
                // Close the Flyout.
                optBgColor.ContextFlyout.Hide();
                optBgColor.Tag = false;
            }
            else if ((bool)optFgColor.Tag)
            {
                CURRENT_FGCOLOR = myColorPicker.Color;
                // Close the Flyout.
                optFgColor.ContextFlyout.Hide();
                optFgColor.Tag = false;
            }
        }

        private void CancelColor_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)optBgColor.Tag)
            {
                // Close the Flyout.
                optBgColor.ContextFlyout.Hide();
                optBgColor.Tag = false;
            }
            else if ((bool)optFgColor.Tag)
            {
                // Close the Flyout.
                optFgColor.ContextFlyout.Hide();
                optFgColor.Tag = false;
            }
        }

        private void OptColor_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var btn = sender as MenuFlyoutItem;
            var C_NAME = btn.Name.Substring(3);

            optFgColor.Tag = false;
            optBgColor.Tag = false;

            switch (C_NAME)
            {
                case "BgColor":
                    optBgColor.Tag = true;
                    //FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
                    break;
                case "FgColor":
                    optFgColor.Tag = true;
                    //FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
                    break;
                default:
                    break;
            }
        }

        private void OptSave_Click(object sender, RoutedEventArgs e)
        {
            AppBarToggleButton[] opts = new AppBarToggleButton[] { optSaveSizeL, optSaveSizeM, optSaveSizeN, optSaveSizeS, optSaveSizeWindow };

            var btn = sender as AppBarToggleButton;
            var SIZE_NAME = btn.Name.Substring(btn.Name.Length - 1).ToUpper();

            foreach (AppBarToggleButton opt in opts)
            {
                if (string.Equals(opt.Name, btn.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    opt.IsChecked = true;
                }
                else opt.IsChecked = false;
            }
            switch (SIZE_NAME)
            {
                case "L":
                    CURRENT_SIZE = 1024;
                    break;
                case "M":
                    CURRENT_SIZE = 512;
                    break;
                case "N":
                    CURRENT_SIZE = 256;
                    break;
                case "S":
                    CURRENT_SIZE = 128;
                    break;
                default:
                    CURRENT_SIZE = 0;
                    break;
            }
        }
    }
}
