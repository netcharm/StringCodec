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
using Windows.UI.Popups;
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
        private Color CURRENT_BGCOLOR = Colors.White; //Color.FromArgb(255, 255, 255, 255);
        private Color CURRENT_FGCOLOR = Colors.Black; //Color.FromArgb(255, 000, 000, 000);
        private int CURRENT_SIZE = 512;

        private string[] image_ext = new string[] { ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff", ".gif" };

        static private string text_src = string.Empty;
        static public string Text
        {
            get { return text_src; }
            set { text_src = value; }
        }

        public QRCodePage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            optECL_L.IsChecked = true;
            optSaveSizeM.IsChecked = true;

            if (!string.IsNullOrEmpty(text_src))
            {
                edQR.Text = text_src;
                //imgQR.Stretch = Stretch.Uniform;
                //imgQR.Source = QRCodec.Encode(edQR.Text, CURRENT_FGCOLOR, CURRENT_BGCOLOR, CURRENT_ECL);
            }

            if (!GraphicsCaptureSession.IsSupported())
            {
                // Hide the capture UI if screen capture is not supported.
                btnCapture.Visibility = Visibility.Collapsed;
            }
            else btnCapture.Visibility = Visibility.Visible;

            #region Add small image to Image control for dragdrop target
            var wb = new WriteableBitmap(1, 1);
            imgQR.Stretch = Stretch.Uniform;
            imgQR.Source = wb;
            #endregion
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            switch (btn.Name)
            {
                case "btnEncode":
                    imgQR.Stretch = Stretch.Uniform;
                    imgQR.Source = QRCodec.Encode(edQR.Text, CURRENT_FGCOLOR, CURRENT_BGCOLOR, CURRENT_ECL);
                    break;
                case "btnDecode":
                    edQR.Text = await QRCodec.Decode(imgQR.Source as WriteableBitmap);
                    break;
                case "btnCopy":
                    Utils.SetClipboard(imgQR, CURRENT_SIZE);
                    break;
                case "btnPaste":
                    edQR.Text = await Utils.GetClipboard(edQR.Text, imgQR);
                    break;
                case "btnCapture":
                    imgQR.Stretch = Stretch.Uniform;
                    var sc = new ScreenCapture();
                    sc.Preview = imgQR;
                    await sc.StartCaptureAsync();
                    break;
                case "btnSave":
                    await Utils.ShowSaveDialog(imgQR, CURRENT_SIZE, "QR");
                    break;
                case "btnShare":
                    await Utils.Share(imgQR.Source as WriteableBitmap);
                    break;
                default:
                    break;
            }
        }

        private void OptECL_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] opts = new ToggleMenuFlyoutItem[] { optECL_L, optECL_M, optECL_Q, optECL_H };

            var btn = sender as ToggleMenuFlyoutItem;
            var ECL_NAME = btn.Name.Substring(btn.Name.Length - 1);

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
            lblInfo.Text = $"Count: {edQR.Text.Length}";
            text_src = edQR.Text;
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

        #region Drag/Drop routines
        private async void OnDragOver(object sender, DragEventArgs e)
        {
            if (sender == imgQR)
            {
                try
                {
                    if (e.DataView.Contains(StandardDataFormats.StorageItems))
                    {
                        //e.DragUIOverride.IsCaptionVisible = true;
                        //e.DragUIOverride.IsContentVisible = true;
                        //e.DragUIOverride.IsGlyphVisible = true;
                        e.AcceptedOperation = DataPackageOperation.Copy;

                        //var items = await e.DataView.GetStorageItemsAsync();
                        //var content = (StorageFile)items[0];
                        //switch (content.ContentType)
                        //{
                        //    case "image/png":
                        //    case "image/jpeg":
                        //        break;
                        //}
                        //if (items.Count > 0)
                        //{
                        //    var storageFile = items[0] as StorageFile;
                        //    if (!image_ext.Contains(storageFile.FileType.ToLower()))
                        //        e.AcceptedOperation = DataPackageOperation.None;
                        //}
                        //e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                    else if (e.DataView.Contains(StandardDataFormats.WebLink))
                    {
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                }
                catch(Exception ex)
                {
                    await new MessageDialog(ex.Message, "ERROR").ShowAsync();
                }
            }
            else if (sender == edQR)
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    //var items = await e.DataView.GetStorageItemsAsync();
                    //if (items.Count > 0)
                    //{
                    //    var storageFile = items[0] as StorageFile;
                    //    if (!storageFile.FileType.ToLower().Equals(".txt")) return;
                    //    e.AcceptedOperation = DataPackageOperation.Copy;
                    //}
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
            if (sender == imgQR)
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
                        imgQR.Source = bitmapImage;
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
                    imgQR.Source = bitmapImage;
                }
            }
            else if (sender == edQR)
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        var storageFile = items[0] as StorageFile;
                        if (!storageFile.FileType.ToLower().Equals(".txt")) return;

                        edQR.Text = await FileIO.ReadTextAsync(storageFile);
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
                        edQR.Text = content;
                    }
                }
            }
        }
        #endregion
    }
}
