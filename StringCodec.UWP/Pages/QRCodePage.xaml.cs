using StringCodec.UWP.Common;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Capture;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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

        static private string text_src = string.Empty;
        static public string Text
        {
            get { return text_src; }
            set { text_src = value; }
        }

        public QRCodePage()
        {
            this.InitializeComponent();

            NavigationCacheMode = NavigationCacheMode.Enabled;

            optECL_L.IsChecked = true;
            optSaveSizeM.IsChecked = true;

            //if (!string.IsNullOrEmpty(text_src)) edQR.Text = text_src;

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
                if(data is string)
                {
                    edQR.Text = data.ToString();
                    imgQR.Source = edQR.Text.EncodeQR(CURRENT_FGCOLOR, CURRENT_BGCOLOR, CURRENT_ECL);
                }
                else if(data is WriteableBitmap)
                {
                    imgQR.Source = data as WriteableBitmap;
                    edQR.Text = await QRCodec.Decode(imgQR.Source as WriteableBitmap);
                }
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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void edQR_TextChanged(object sender, TextChangedEventArgs e)
        {
            lblInfo.Text = $"{"Count".T()}: {edQR.Text.Length}";
            text_src = edQR.Text;
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

        private async void Base64_Click(object sender, RoutedEventArgs e)
        {
            if(sender == btnImageToBase64)
            {
                if (imgQR.Source == null) return;
                Frame.Navigate(typeof(ImagePage), await imgQR.ToWriteableBitmap());
            }
            else if(sender == btnTextToDecode)
            {
                if (string.IsNullOrEmpty(edQR.Text)) return;
                Frame.Navigate(typeof(TextPage), edQR.Text);
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            switch (btn.Name)
            {
                case "btnEncode":
                    imgQR.Source = QRCodec.EncodeQR(edQR.Text, CURRENT_FGCOLOR, CURRENT_BGCOLOR, CURRENT_ECL);
                    break;
                case "btnDecode":
                    edQR.Text = await QRCodec.Decode(await imgQR.ToWriteableBitmap());
                    
                    break;
                case "btnCopy":
                    Utils.SetClipboard(imgQR, CURRENT_SIZE);
                    break;
                case "btnPaste":
                    edQR.Text = await Utils.GetClipboard(edQR.Text, imgQR);
                    break;
                case "btnCapture":
                    var sc = new ScreenCapture(imgQR);
                    await sc.StartCaptureAsync();
                    break;
                case "btnSave":
                    await Utils.ShowSaveDialog(imgQR, CURRENT_SIZE, "QR");
                    break;
                case "btnShare":
                    await Utils.Share(await imgQR.ToWriteableBitmap(), "QR");
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
                        if (sender == imgQR || sender == rectDrop)
                        {
                            if (Utils.image_ext.Contains(extension))
                            {
                                canDrop = true;
#if DEBUG
                                System.Diagnostics.Debug.WriteLine("Drag image to Image Control");
#endif
                            }
                        }
                        else if (sender == edQR)
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
            if (sender == imgQR || sender == rectDrop)
            {
                try
                {
                    if (e.DataView.Contains(StandardDataFormats.StorageItems))
                    {
                        if (canDrop) e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                    else if(e.DataView.Contains(StandardDataFormats.Bitmap))
                    {
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                    else if (e.DataView.Contains(StandardDataFormats.WebLink))
                    {
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                }
                catch(Exception ex)
                {
                    await new MessageDialog(ex.Message, "ERROR".T()).ShowAsync();
                }
            }
            else if (sender == edQR)
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
            if (sender == imgQR || sender == rectDrop)
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        var storageFile = items[0] as StorageFile;
                        if (Utils.image_ext.Contains(storageFile.FileType.ToLower()))
                        {
                            if (storageFile.FileType.ToLower().Equals(".svg"))
                            {
                                var bitmapImage = new SvgImageSource();

                                if (e.DataView.Contains(StandardDataFormats.WebLink))
                                {
                                    var url = await e.DataView.GetWebLinkAsync();
                                    var rms = await RandomAccessStreamReference.CreateFromUri(url).OpenReadAsync();

                                    var svg = await SVG.CreateFromStream(rms);
                                    bitmapImage = svg.Source;
                                    imgQR.Tag = svg.Bytes;

                                    //await bitmapImage.SetSourceAsync(rms);
                                    //await rms.FlushAsync();

                                    //var bytes = WindowsRuntimeStreamExtensions.AsStreamForRead(rms.GetInputStreamAt(0));
                                    //imgQR.Tag = bytes;
                                }
                                else if (e.DataView.Contains(StandardDataFormats.Text))
                                {
                                    //var content = await e.DataView.GetHtmlFormatAsync();
                                    var content = await e.DataView.GetTextAsync();
                                    if (content.Length > 0)
                                    {
                                        var rms = await RandomAccessStreamReference.CreateFromUri(new Uri(content)).OpenReadAsync();

                                        var svg = await SVG.CreateFromStream(rms);
                                        bitmapImage = svg.Source;
                                        imgQR.Tag = svg.Bytes;

                                        //await bitmapImage.SetSourceAsync(rms);
                                        //await rms.FlushAsync();

                                        //var bytes = WindowsRuntimeStreamExtensions.AsStreamForRead(rms.GetInputStreamAt(0));
                                        //imgQR.Tag = bytes;
                                    }
                                }
                                else
                                {
                                    var svg = await SVG.CreateFromStorageFile(storageFile);
                                    bitmapImage = svg.Source;
                                    imgQR.Tag = svg.Bytes;

                                    //await bitmapImage.SetSourceAsync(await storageFile.OpenReadAsync());
                                    //byte[] bytes = WindowsRuntimeBufferExtensions.ToArray(await FileIO.ReadBufferAsync(storageFile));
                                    //imgQR.Tag = bytes;
                                }
                                imgQR.Source = bitmapImage;
                            }
                            else
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
                                imgQR.Source = bitmapImage;
                            }
                        }
                    }
                }
                else if (e.DataView.Contains(StandardDataFormats.WebLink))
                {
                    var uri = await e.DataView.GetWebLinkAsync();

                    StorageFile storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
                    if (Utils.image_ext.Contains(storageFile.FileType.ToLower()))
                    {
                        if (storageFile.FileType.ToLower().Equals(".svg"))
                        {
                            var bitmapImage = new SvgImageSource();

                            if (e.DataView.Contains(StandardDataFormats.WebLink))
                            {
                                var url = await e.DataView.GetWebLinkAsync();
                                var rms = await RandomAccessStreamReference.CreateFromUri(url).OpenReadAsync();

                                var svg = await SVG.CreateFromStream(rms);
                                bitmapImage = svg.Source;
                                imgQR.Tag = svg.Bytes;

                                //await bitmapImage.SetSourceAsync(rms);
                                //await rms.FlushAsync();

                                //var bytes = WindowsRuntimeStreamExtensions.AsStreamForRead(rms.GetInputStreamAt(0));
                                //imgQR.Tag = bytes;
                            }
                            else if (e.DataView.Contains(StandardDataFormats.Text))
                            {
                                //var content = await e.DataView.GetHtmlFormatAsync();
                                var content = await e.DataView.GetTextAsync();
                                if (content.Length > 0)
                                {
                                    var rms = await RandomAccessStreamReference.CreateFromUri(new Uri(content)).OpenReadAsync();

                                    var svg = await SVG.CreateFromStream(rms);
                                    bitmapImage = svg.Source;
                                    imgQR.Tag = svg.Bytes;

                                    //await bitmapImage.SetSourceAsync(rms);
                                    //await rms.FlushAsync();

                                    //var bytes = WindowsRuntimeStreamExtensions.AsStreamForRead(rms.GetInputStreamAt(0));
                                    //imgQR.Tag = bytes;
                                }
                            }
                            else
                            {
                                var svg = await SVG.CreateFromStorageFile(storageFile);
                                bitmapImage = svg.Source;
                                imgQR.Tag = svg.Bytes;

                                //await bitmapImage.SetSourceAsync(await storageFile.OpenReadAsync());
                                //byte[] bytes = WindowsRuntimeBufferExtensions.ToArray(await FileIO.ReadBufferAsync(storageFile));
                                //imgQR.Tag = bytes;
                            }
                            imgQR.Source = bitmapImage;
                        }
                        else
                        {
                            var bitmapImage = new WriteableBitmap(1, 1);

                            if (e.DataView.Contains(StandardDataFormats.WebLink))
                            {
                                var url = await e.DataView.GetWebLinkAsync();
                                var src = await RandomAccessStreamReference.CreateFromUri(url).OpenReadAsync();
                                await bitmapImage.SetSourceAsync(src);
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
                            imgQR.Source = bitmapImage;
                        }
                    }
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
                        if (Utils.url_ext.Contains(storageFile.FileType.ToLower()))
                        {
                            if (e.DataView.Contains(StandardDataFormats.WebLink))
                            {
                                var url = await e.DataView.GetWebLinkAsync();
                                if (url.IsUnc)
                                {
                                    edQR.Text = url.ToString();
                                }
                                else if (url.IsFile)
                                {
                                    edQR.Text = await FileIO.ReadTextAsync(storageFile);
                                }
                            }
                            else if (e.DataView.Contains(StandardDataFormats.Text))
                            {
                                //var content = await e.DataView.GetHtmlFormatAsync();
                                var content = await e.DataView.GetTextAsync();
                                if (content.Length > 0)
                                {
                                    edQR.Text = content;
                                }
                            }
                        }
                        else if (Utils.text_ext.Contains(storageFile.FileType.ToLower()))
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
            //def.Complete();
        }
        #endregion

    }
}
