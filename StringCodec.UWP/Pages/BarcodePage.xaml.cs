using StringCodec.UWP.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Capture;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Text;
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
    public sealed partial class BarcodePage : Page
    {
        private Color CURRENT_BGCOLOR = Colors.White; //Color.FromArgb(255, 255, 255, 255);
        private Color CURRENT_FGCOLOR = Colors.Black; //Color.FromArgb(255, 000, 000, 000);
        private int CURRENT_SIZE = 512;
        private string CURRENT_FORMAT = "Express";

        private string[] image_ext = new string[] { ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff", ".gif" };

        static private string text_src = string.Empty;
        static public string Text
        {
            get { return text_src; }
            set { text_src = value; }
        }

        public BarcodePage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            optSaveSizeM.IsChecked = true;
            optBarCodeExpress.IsChecked = true;
            edBarcode.TextWrapping = TextWrapping.Wrap;

            if (!string.IsNullOrEmpty(text_src))
            {
                edBarcode.Text = text_src;
            }

            #region Detecting is ScreenCapture supported?
            if (!GraphicsCaptureSession.IsSupported())
            {
                // Hide the capture UI if screen capture is not supported.
                btnCapture.Visibility = Visibility.Collapsed;
                btnCapture.IsEnabled = false;
            }
            else
            {
                btnCapture.Visibility = Visibility.Visible;
                btnCapture.IsEnabled = true;
            }
            #endregion

            #region Add small image to Image control for dragdrop target
            var wb = new WriteableBitmap(1, 1);
            imgBarcode.Stretch = Stretch.Uniform;
            imgBarcode.Source = wb;
            #endregion

            #region Setup Barconde text
            txtBarcode.FontFamily = new FontFamily("Consolas");
            txtBarcode.FontSize = 20;
            txtBarcode.FontStretch = Windows.UI.Text.FontStretch.Normal;
            txtBarcode.Foreground = new SolidColorBrush(CURRENT_FGCOLOR);
            txtBarcodeBG.Background = new SolidColorBrush(CURRENT_BGCOLOR);
            txtBarcodeBG.Height = txtBarcode.Height;
            #endregion
        }

        private void edBarcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            lblInfo.Text = $"Count: {edBarcode.Text.Length}";
            text_src = edBarcode.Text;
        }

        private void OptBarCodeFormat_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] opts = new ToggleMenuFlyoutItem[] {
                optBarCodeExpress, optBarCodeISBN, optBarCodeProduct,
                optBarCodeLink, optBarCodeTele, optBarCodeMail, optBarCodeSMS,
                optBarCodeVcard, optBarCodeVcal
            };

            var btn = sender as ToggleMenuFlyoutItem;
            var FMT_NAME = btn.Name.Substring(10);
            CURRENT_FORMAT = FMT_NAME;

            foreach (ToggleMenuFlyoutItem opt in opts)
            {
                if (string.Equals(opt.Name, btn.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    opt.IsChecked = true;
                }
                else opt.IsChecked = false;
            }
        }

        private async void OptColor_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as MenuFlyoutItem;
            var C_NAME = btn.Name.Substring(3);

            switch (C_NAME)
            {
                case "ResetColor":
                    CURRENT_BGCOLOR = Colors.White; // Color.FromArgb(255, 255, 255, 255);
                    CURRENT_FGCOLOR = Colors.Black; // Color.FromArgb(255, 000, 000, 000);
                    break;
                case "BgColor":
                    CURRENT_BGCOLOR = await Utils.ShowColorDialog(CURRENT_BGCOLOR);
                    txtBarcodeBG.Background = new SolidColorBrush(CURRENT_BGCOLOR);
                    break;
                case "FgColor":
                    CURRENT_FGCOLOR = await Utils.ShowColorDialog(CURRENT_FGCOLOR);
                    txtBarcode.Foreground = new SolidColorBrush(CURRENT_FGCOLOR);
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

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            switch (btn.Name)
            {
                case "btnEncode":
                    imgBarcode.Source = await edBarcode.Text.EncodeBarcode(CURRENT_FORMAT, CURRENT_FGCOLOR, CURRENT_BGCOLOR);
                    txtBarcode.Text = edBarcode.Text.BarcodeLabel(CURRENT_FORMAT);
                    //var wb = await edBarcode.Text.ToBitmap(LabelRoot, "Consolas", 24, CURRENT_FGCOLOR, CURRENT_BGCOLOR);
                    //var wb = await txtBarcodeBG.ToBitmap();
                    var wb = await edBarcode.Text.ToBitmap("Consolas", FontStyle.Normal, 24, CURRENT_FGCOLOR, CURRENT_BGCOLOR);
                    //wb.DrawText(0, 0, txtBarcode.Text, "Consolas", 24, CURRENT_FGCOLOR, CURRENT_BGCOLOR);
                    await wb.StoreTemporaryFile();
                    break;
                case "btnDecode":
                    edBarcode.Text = await (imgBarcode.Source as WriteableBitmap).Decode();
                    break;
                case "btnCopy":
                    Utils.SetClipboard(imgBarcode, CURRENT_SIZE);
                    break;
                case "btnPaste":
                    edBarcode.Text = await Utils.GetClipboard(edBarcode.Text, imgBarcode);
                    break;
                case "btnSave":
                    await Utils.ShowSaveDialog(imgBarcode, CURRENT_SIZE, "BarCode");
                    break;
                case "btnShare":
                    await Utils.Share(imgBarcode.Source as WriteableBitmap, "BarCode");
                    break;
                default:
                    break;
            }
        }

        #region Drag/Drop routines
        private async void OnDragOver(object sender, DragEventArgs e)
        {
            if (sender == imgBarcode)
            {
                try
                {
                    if (e.DataView.Contains(StandardDataFormats.StorageItems))
                    {
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                    else if (e.DataView.Contains(StandardDataFormats.WebLink))
                    {
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                }
                catch (Exception ex)
                {
                    await new MessageDialog(ex.Message, "ERROR").ShowAsync();
                }
            }
            else if (sender == edBarcode)
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
                else if (e.DataView.Contains(StandardDataFormats.Text) ||
                    e.DataView.Contains(StandardDataFormats.WebLink) ||
                    e.DataView.Contains(StandardDataFormats.Html) ||
                    e.DataView.Contains(StandardDataFormats.Rtf))
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            if (sender == imgBarcode)
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        var storageFile = items[0] as StorageFile;
#if DEBUG
                        //await new MessageDialog($"{items.Count}, {image_ext.Contains(storageFile.FileType.ToLower())}", "INFO").ShowAsync();
#endif
                        if (!image_ext.Contains(storageFile.FileType.ToLower())) return;

                        var bitmapImage = new WriteableBitmap(1, 1);
                        await bitmapImage.SetSourceAsync(await storageFile.OpenAsync(FileAccessMode.Read));
                        // Set the image on the main page to the dropped image
                        //if (bitmapImage.PixelWidth >= imgQR.RenderSize.Width || bitmapImage.PixelHeight >= imgQR.RenderSize.Height)
                        //    imgQR.Stretch = Stretch.Uniform;
                        //else imgQR.Stretch = Stretch.None;
                        byte[] arr = WindowsRuntimeBufferExtensions.ToArray(bitmapImage.PixelBuffer, 0, (int)bitmapImage.PixelBuffer.Length);
                        imgBarcode.Source = bitmapImage;
                    }
                }
                else if (e.DataView.Contains(StandardDataFormats.WebLink))
                {
                    var uri = await e.DataView.GetWebLinkAsync();

                    StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                    //dp.SetBitmap(RandomAccessStreamReference.CreateFromUri(uri));

                    var bitmapImage = new WriteableBitmap(1, 1);
                    await bitmapImage.SetSourceAsync(await file.OpenAsync(FileAccessMode.Read));
                    // Set the image on the main page to the dropped image
                    //if (bitmapImage.PixelWidth >= imgQR.RenderSize.Width || bitmapImage.PixelHeight >= imgQR.RenderSize.Height)
                    //    imgQR.Stretch = Stretch.Uniform;
                    //else imgQR.Stretch = Stretch.None;
                    byte[] arr = WindowsRuntimeBufferExtensions.ToArray(bitmapImage.PixelBuffer, 0, (int)bitmapImage.PixelBuffer.Length);
                    imgBarcode.Source = bitmapImage;
                }
            }
            else if (sender == edBarcode)
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        var storageFile = items[0] as StorageFile;
                        if (!storageFile.FileType.ToLower().Equals(".txt")) return;

                        edBarcode.Text = await FileIO.ReadTextAsync(storageFile);
                    }
                }
                else if (e.DataView.Contains(StandardDataFormats.Text) ||
                    e.DataView.Contains(StandardDataFormats.WebLink) ||
                    e.DataView.Contains(StandardDataFormats.Html) ||
                    e.DataView.Contains(StandardDataFormats.Rtf))
                {
                    var content = await e.DataView.GetTextAsync();
                    if (content.Length > 0)
                    {
                        edBarcode.Text = content;
                    }
                }
            }
        }
        #endregion

    }
}
