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
using Windows.Storage.Streams;
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
    public sealed partial class CommonOneDPage : Page
    {
        private Color CURRENT_BGCOLOR = Colors.White; //Color.FromArgb(255, 255, 255, 255);
        private Color CURRENT_FGCOLOR = Colors.Black; //Color.FromArgb(255, 000, 000, 000);
        private int CURRENT_SIZE = 512;
        private int CURRENT_TEXT_FONTSIZE = 48;
        private string CURRENT_FORMAT = "Express";
        private bool CURRENT_CHECKSUM = false;

        static private string text_src = string.Empty;
        static public string Text
        {
            get { return text_src; }
            set { text_src = value; }
        }

        public CommonOneDPage()
        {
            this.InitializeComponent();

            NavigationCacheMode = NavigationCacheMode.Enabled;

            optSaveSizeM.IsChecked = true;
            optBarCodeExpress.IsChecked = true;
            optBarcodeTextSizeM.IsChecked = true;

            edBarcode.TextWrapping = TextWrapping.Wrap;

            //if (!string.IsNullOrEmpty(text_src)) edBarcode.Text = text_src;

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
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                var data = e.Parameter;
                if (data is string)
                {
                    imgBarcode.Stretch = Stretch.Uniform;
                    edBarcode.Text = data.ToString();
                    imgBarcode.Source = await edBarcode.Text.EncodeBarcode(CURRENT_FORMAT, CURRENT_FGCOLOR, CURRENT_BGCOLOR, CURRENT_TEXT_FONTSIZE, CURRENT_CHECKSUM);
                }
                else if (data is WriteableBitmap)
                {
                    imgBarcode.Source = data as WriteableBitmap;
                    edBarcode.Text = await QRCodec.Decode(imgBarcode.Source as WriteableBitmap);
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
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
                optBarCode39, optBarCode93, optBarCode128, optBarCodeEAN13,
                optBarCodeUPCA, optBarCodeUPCE, optBarCodeCodabar
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

        private void OptBarCodeChecksum_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as ToggleMenuFlyoutItem;
            if (btn == optBarCodeChecksum)
            {
                CURRENT_CHECKSUM = btn.IsChecked;
                return;
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
                    break;
                case "FgColor":
                    CURRENT_FGCOLOR = await Utils.ShowColorDialog(CURRENT_FGCOLOR);
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

        private void OptBarCodeTextSize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] opts = new ToggleMenuFlyoutItem[] {
                optBarcodeTextSizeXL, optBarcodeTextSizeL, optBarcodeTextSizeM, optBarcodeTextSizeS, optBarcodeTextSizeXS,
                optBarcodeTextSizeNone
            };

            var btn = sender as ToggleMenuFlyoutItem;
            var SIZE_NAME = btn.Name.Substring(18).ToUpper();

            foreach (ToggleMenuFlyoutItem opt in opts)
            {
                if (string.Equals(opt.Name, btn.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    opt.IsChecked = true;
                }
                else opt.IsChecked = false;
            }
            switch (SIZE_NAME)
            {
                case "XL":
                    CURRENT_TEXT_FONTSIZE = 96;
                    break;
                case "L":
                    CURRENT_TEXT_FONTSIZE = 72;
                    break;
                case "M":
                    CURRENT_TEXT_FONTSIZE = 48;
                    break;
                case "S":
                    CURRENT_TEXT_FONTSIZE = 36;
                    break;
                case "XS":
                    CURRENT_TEXT_FONTSIZE = 24;
                    break;
                case "NONE":
                    CURRENT_TEXT_FONTSIZE = 0;
                    break;
                default:
                    CURRENT_TEXT_FONTSIZE = 48;
                    break;
            }
            (cmdBar.SecondaryCommands as AppBarButton).Flyout.Hide();
            //optBarcodeTextSize.Flyout.Hide();
        }

        private void Base64_Click(object sender, RoutedEventArgs e)
        {
            if (sender == btnImageToBase64)
            {
                if (imgBarcode.Source == null) return;
                Frame.Navigate(typeof(ImagePage), imgBarcode.Source as WriteableBitmap);
            }
            else if (sender == btnTextToDecode)
            {
                if (string.IsNullOrEmpty(edBarcode.Text)) return;
                Frame.Navigate(typeof(TextPage), edBarcode.Text);
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            switch (btn.Name)
            {
                case "btnEncode":
                    imgBarcode.Source = await edBarcode.Text.EncodeBarcode(CURRENT_FORMAT, CURRENT_FGCOLOR, CURRENT_BGCOLOR, CURRENT_TEXT_FONTSIZE, CURRENT_CHECKSUM);
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
        private bool canDrop = true;
        private async void OnDragEnter(object sender, DragEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("drag enter.." + DateTime.Now);
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var deferral = e.GetDeferral(); // since the next line has 'await' we need to defer event processing while we wait
                try
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        canDrop = false;
                        var item = items[0] as StorageFile;
                        string filename = item.Name;
                        string extension = item.FileType.ToLower();
                        if (sender == imgBarcode || sender == rectDrop)
                        {
                            if (Utils.image_ext.Contains(extension))
                            {
                                canDrop = true;
#if DEBUG
                                System.Diagnostics.Debug.WriteLine("Drag image to Image Control");
#endif
                            }
                        }
                        else if (sender == edBarcode)
                        {
                            if (Utils.text_ext.Contains(extension))
                            {
                                canDrop = true;
#if DEBUG
                                System.Diagnostics.Debug.WriteLine("Drag text to TextBox Control");
#endif
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
                deferral.Complete();
            }
        }

        private async void OnDragOver(object sender, DragEventArgs e)
        {
            if (sender == imgBarcode || sender == rectDrop)
            {
                try
                {
                    if (e.DataView.Contains(StandardDataFormats.StorageItems))
                    {
                        if(canDrop) e.AcceptedOperation = DataPackageOperation.Copy;
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
                    if (canDrop) e.AcceptedOperation = DataPackageOperation.Copy;
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
            // 需要异步拖放时记得获取Deferral对象
            //var def = e.GetDeferral();
            if (sender == imgBarcode || sender == rectDrop)
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        var storageFile = items[0] as StorageFile;
                        if (Utils.image_ext.Contains(storageFile.FileType.ToLower()))
                        {
                            var bitmapImage = new WriteableBitmap(1, 1);

                            if (e.DataView.Contains(StandardDataFormats.WebLink))
                            {
                                var url = await e.DataView.GetWebLinkAsync();
                                await bitmapImage.SetSourceAsync(await RandomAccessStreamReference.CreateFromUri(url).OpenReadAsync());
                            }
                            else if (e.DataView.Contains(StandardDataFormats.Text))
                            {
                                //var content = await e.DataView.GetHtmlFormatAsync();
                                var content = await e.DataView.GetTextAsync();
                                if (content.Length > 0)
                                {
                                    await bitmapImage.SetSourceAsync(await RandomAccessStreamReference.CreateFromUri(new Uri(content)).OpenReadAsync());
                                }
                            }
                            else
                            {
                                await bitmapImage.SetSourceAsync(await storageFile.OpenReadAsync());
                            }

                            byte[] arr = WindowsRuntimeBufferExtensions.ToArray(bitmapImage.PixelBuffer, 0, (int)bitmapImage.PixelBuffer.Length);
                            imgBarcode.Source = bitmapImage;
                        }
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
                        if (Utils.url_ext.Contains(storageFile.FileType.ToLower()))
                        {
                            if (e.DataView.Contains(StandardDataFormats.WebLink))
                            {
                                var url = await e.DataView.GetWebLinkAsync();
                                if (url.IsUnc)
                                {
                                    edBarcode.Text = url.ToString();
                                }
                                else if (url.IsFile)
                                {
                                    edBarcode.Text = await FileIO.ReadTextAsync(storageFile);
                                }
                            }
                            else if (e.DataView.Contains(StandardDataFormats.Text))
                            {
                                var content = await e.DataView.GetTextAsync();
                                if (content.Length > 0)
                                {
                                    edBarcode.Text = content;
                                }
                            }
                        }
                        else if (Utils.text_ext.Contains(storageFile.FileType.ToLower())) 
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
            //def.Complete();
        }
        #endregion

    }
}
