using StringCodec.UWP.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
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
    public sealed partial class ImagePage : Page
    {
        private bool CURRENT_PREFIX = true;
        private bool CURRENT_LINEBREAK = false;
        private string CURRENT_FORMAT = ".png";

        static private string text_src = string.Empty;
        static public string Text
        {
            get { return text_src; }
            set { text_src = value; }
        }

        private async Task<bool> IsSVG(Image image)
        {
            bool result = false;

            if(image.Source is SvgImageSource)
            {
                btnImageSvg.Visibility = Visibility.Visible;
                optFmtSvg.Visibility = Visibility.Visible;

                if (optFmtSvg.IsChecked)
                {
                    optFmtPng.IsChecked = false;
                    optFmtBmp.IsChecked = false;
                    optFmtGif.IsChecked = false;
                    optFmtJpg.IsChecked = false;
                    optFmtTif.IsChecked = false;
                }
                result = true;
            }
            else
            {
                btnImageSvg.Visibility = Visibility.Collapsed;
                optFmtSvg.Visibility = Visibility.Collapsed;
                if (optFmtSvg.IsChecked)
                {
                    optFmtSvg.IsChecked = false;
                    optFmtPng.IsChecked = true;
                    optFmtBmp.IsChecked = false;
                    optFmtGif.IsChecked = false;
                    optFmtJpg.IsChecked = false;
                    optFmtTif.IsChecked = false;
                }

                result = false;
            }
            int w = 0, h = 0;
            if(imgBase64.Source is ImageSource)
            {
                var wb = await imgBase64.ToWriteableBitmap();
                w = wb.PixelWidth;
                h = wb.PixelHeight;
            }
            lblInfo.Text = $"{"Count".T()}: {edBase64.Text.Length}, {"Size".T()}: {w}x{h}";
            return (result);
        }

        public ImagePage()
        {
            this.InitializeComponent();

            NavigationCacheMode = NavigationCacheMode.Enabled;

            optPrefix.IsChecked = CURRENT_PREFIX;
            optLinbeBreak.IsChecked = CURRENT_LINEBREAK;

            optFmtPng.IsChecked = true;

            //if (!string.IsNullOrEmpty(text_src)) edBase64.Text = text_src;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                var data = e.Parameter;
                if (data is string)
                {
                    //imgBase64.Stretch = Stretch.Uniform;
                    edBase64.Text = data.ToString();
                    imgBase64.Source = await edBase64.Text.Decoder();
                }
                else if (data is WriteableBitmap)
                {
                    imgBase64.Source = data as WriteableBitmap;
                    if (CURRENT_LINEBREAK) edBase64.TextWrapping = TextWrapping.NoWrap;
                    else edBase64.TextWrapping = TextWrapping.Wrap;
                    var wb = await imgBase64.ToWriteableBitmap();
                    edBase64.Text = await wb.ToBase64(CURRENT_FORMAT, CURRENT_PREFIX, CURRENT_LINEBREAK);
                }
                else if (data is WriteableBitmapObject)
                {
                    var obj = data as WriteableBitmapObject;
                    if (obj.Image is WriteableBitmap)
                    {
                        imgBase64.Source = obj.Image;
                        if (CURRENT_LINEBREAK) edBase64.TextWrapping = TextWrapping.NoWrap;
                        else edBase64.TextWrapping = TextWrapping.Wrap;
                        var wb = await imgBase64.ToWriteableBitmap();
                        edBase64.Text = await wb.ToBase64(CURRENT_FORMAT, CURRENT_PREFIX, CURRENT_LINEBREAK);
                    }
                    if (!string.IsNullOrEmpty(obj.Title))
                        imgBase64.Tag = obj.Title;
                }
                else if (data is SVG)
                {
                    if (CURRENT_LINEBREAK) edBase64.TextWrapping = TextWrapping.NoWrap;
                    else edBase64.TextWrapping = TextWrapping.Wrap;

                    var svg = data as SVG;
                    if(svg.Source is SvgImageSource)
                    {
                        imgBase64.Source = svg.Source;
                        edBase64.Text = await svg.ToBase64(CURRENT_LINEBREAK);
                        optFmtSvg.IsChecked = true;
                    }
                    else if(svg.Image is WriteableBitmap)
                    {
                        imgBase64.Source = svg.Image;
                        var wb = await imgBase64.ToWriteableBitmap();
                        edBase64.Text = await wb.ToBase64(CURRENT_FORMAT, CURRENT_PREFIX, CURRENT_LINEBREAK);
                    }
                    imgBase64.Tag = svg.Bytes is byte[] ? svg.Bytes : null;

                    await IsSVG(imgBase64);
                }
                await IsSVG(imgBase64);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void EdBase64_TextChanged(object sender, TextChangedEventArgs e)
        {
            //text_src = edBase64.Text;
        }

        private void OptFmt_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] opts = new ToggleMenuFlyoutItem[] { optFmtBmp, optFmtGif, optFmtJpg, optFmtPng, optFmtTif, optFmtSvg };

            var btn = sender as ToggleMenuFlyoutItem;
            var FMT_NAME = btn.Name.Substring(btn.Name.Length - 3).ToLower();

            foreach (ToggleMenuFlyoutItem opt in opts)
            {
                if (string.Equals(opt.Name, btn.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    opt.IsChecked = true;
                }
                else opt.IsChecked = false;
            }
            CURRENT_FORMAT = $".{FMT_NAME}";
        }

        private void Opt_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as ToggleMenuFlyoutItem;
            var C_NAME = btn.Name;

            switch (C_NAME)
            {
                case "optPrefix":
                    CURRENT_PREFIX = btn.IsChecked;
                    break;
                case "optLinbeBreak":
                    CURRENT_LINEBREAK = btn.IsChecked;
                    if (CURRENT_LINEBREAK) edBase64.TextWrapping = TextWrapping.NoWrap;
                    else edBase64.TextWrapping = TextWrapping.Wrap;
                    break;
                default:
                    break;
            }
        }

        private async void ImageAction_Click(object sender, RoutedEventArgs e)
        {
            if (imgBase64.Source == null) return;
            if (sender == btnImageQRCode)
            {
                //Frame.Navigate(typeof(QRCodePage), imgBase64.Source as WriteableBitmap);
                Frame.Navigate(typeof(QRCodePage), await imgBase64.ToWriteableBitmap());
            }
            else if (sender == btnImageOneD)
            {
                //Frame.Navigate(typeof(CommonOneDPage), imgBase64.Source as WriteableBitmap);
                Frame.Navigate(typeof(CommonOneDPage), await imgBase64.ToWriteableBitmap());
            }
            else if (sender == btnImageSvg)
            {
                if (!(imgBase64.Source is SvgImageSource) || !(imgBase64.Tag is byte[])) return;
                var svg = imgBase64.ToSVG();
                Frame.Navigate(typeof(SvgPage), svg);
            }
            else if (sender == btnImageAsHtml)
            {
                var alt = string.Empty;
                if (imgBase64.Tag is string) alt = (string)(imgBase64.Tag);
                Utils.SetClipboard(await (await imgBase64.ToWriteableBitmap()).ToHTML(alt, ".png"));
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            switch (btn.Name)
            {
                case "btnEncode":
                    string b64 = string.Empty;
                    edBase64.TextWrapping = TextWrapping.NoWrap;
                    if(imgBase64.Source is SvgImageSource && CURRENT_FORMAT.Equals(".svg"))
                    {
                        try
                        {
                            var svg = new SVG();
                            if (imgBase64.Tag is byte[])
                                svg.Bytes = imgBase64.Tag as byte[];
                            svg.Source = imgBase64.Source as SvgImageSource;
                            b64 = await svg.ToBase64(CURRENT_LINEBREAK);
                        }
                        catch (Exception ex)
                        {
                            await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
                        }
                        if(string.IsNullOrEmpty(b64))
                        {
                            var wb = await imgBase64.ToWriteableBitmap();
                            if (wb != null)
                            {
                                b64 = await wb.ToBase64(CURRENT_FORMAT, CURRENT_PREFIX, CURRENT_LINEBREAK);
                            }
                        }
                    }
                    else
                    {
                        var wb = await imgBase64.ToWriteableBitmap();
                        b64 = await wb.ToBase64(CURRENT_FORMAT, CURRENT_PREFIX, CURRENT_LINEBREAK);
                    }
                    edBase64.Text = b64;
                    if (CURRENT_LINEBREAK) edBase64.TextWrapping = TextWrapping.NoWrap;
                    else edBase64.TextWrapping = TextWrapping.Wrap;

                    //
                    // Maybe TextBox bug: If lines > 3500, the text maybe displayed as white but infact
                    // the content is right, you can select & copy. it's ok, but only display white
                    // 
                    break;
                case "btnDecode":
                    if(edBase64.Text.StartsWith("data:image/svg", StringComparison.CurrentCultureIgnoreCase) ||
                       Regex.IsMatch(edBase64.Text, @"<svg.*?>", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                    {
                        var svg = await edBase64.Text.DecodeSvg();
                        imgBase64.Source = svg.Source;
                        imgBase64.Tag = svg.Bytes;
                    }
                    else
                    {
                        imgBase64.Source = await edBase64.Text.Decoder();
                    }
                    break;
                case "btnCopy":
                    //Utils.SetClipboard(edBase64.Text);
                    Utils.SetClipboard(imgBase64, -1);
                    break;
                case "btnPaste":
                    edBase64.Text = await Utils.GetClipboard(edBase64.Text, imgBase64);
                    break;
                case "btnSave":
                    await Utils.ShowSaveDialog(imgBase64);
                    break;
                case "btnShare":
                    await Utils.Share(await imgBase64.ToWriteableBitmap());
                    break;
                default:
                    break;
            }
            await IsSVG(imgBase64);
        }

        #region Drag/Drop routines
        private bool canDrop = true;
        private async void OnDragEnter(object sender, DragEventArgs e)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Drag Enter Sender:{sender}");
#endif
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
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Drag count:{items.Count}, {filename}");
#endif
                        if (sender == imgBase64 || sender == rectDrop)
                        {
                            if (Utils.image_ext.Contains(extension))
                            {
                                canDrop = true;
#if DEBUG
                                System.Diagnostics.Debug.WriteLine("Drag image to Image Control");
#endif
                            }
                        }
                        else if(sender == edBase64)
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

        private void OnDragOver(object sender, DragEventArgs e)
        {
            //if (canDrop && !e.Handled)
            //{
            //    { e.AcceptedOperation = DataPackageOperation.Copy; }
            //    System.Diagnostics.Debug.WriteLine("drag ok");
            //}
            //return;
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Drag Over Sender:{sender}");
#endif

            if (sender == imgBase64 || sender == rectDrop)
            {
                if (e.DataView.Contains(StandardDataFormats.WebLink))
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
                else if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    if (canDrop) e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }
            else if (sender == edBase64)
            {
                if (e.DataView.Contains(StandardDataFormats.Text))
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
                else if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    if (canDrop) e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            // 需要异步拖放时记得获取Deferral对象
            //var def = e.GetDeferral();
            if (sender == imgBase64 || sender == rectDrop)
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
                                    imgBase64.Tag = svg.Bytes;
                                }
                                else if (e.DataView.Contains(StandardDataFormats.Text))
                                {
                                    var content = await e.DataView.GetTextAsync();
                                    if (content.Length > 0)
                                    {
                                        var rms = await RandomAccessStreamReference.CreateFromUri(new Uri(content)).OpenReadAsync();
                                        var svg = await SVG.CreateFromStream(rms);
                                        bitmapImage = svg.Source;
                                        imgBase64.Tag = svg.Bytes;
                                    }
                                }
                                else
                                {
                                    var svg = await SVG.CreateFromStorageFile(storageFile);
                                    bitmapImage = svg.Source;
                                    imgBase64.Tag = svg.Bytes;
                                }
                                imgBase64.Source = bitmapImage;
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
                                imgBase64.Source = bitmapImage;
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
                                imgBase64.Tag = svg.Bytes;
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
                                    imgBase64.Tag = svg.Bytes;
                                }
                            }
                            else
                            {
                                await bitmapImage.SetSourceAsync(await storageFile.OpenReadAsync());
                                byte[] bytes = WindowsRuntimeBufferExtensions.ToArray(await FileIO.ReadBufferAsync(storageFile));
                                imgBase64.Tag = bytes;
                            }
                            imgBase64.Source = bitmapImage;
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
                            imgBase64.Source = bitmapImage;
                        }
                    }
                }
            }
            else if (sender == edBase64)
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
                                if (url.IsAbsoluteUri)
                                {
                                    edBase64.Text = url.ToString();
                                }
                                else if (url.IsFile || url.IsUnc)
                                {
                                    edBase64.Text = await FileIO.ReadTextAsync(storageFile);
                                }
                            }
                            else if (e.DataView.Contains(StandardDataFormats.Text))
                            {
                                //var content = await e.DataView.GetHtmlFormatAsync();
                                var content = await e.DataView.GetTextAsync();
                                if (content.Length > 0)
                                {
                                    edBase64.Text = content;
                                }
                            }
                        }
                        else if (Utils.text_ext.Contains(storageFile.FileType.ToLower()))
                            edBase64.Text = await FileIO.ReadTextAsync(storageFile);
                    }
                }
                else if (e.DataView.Contains(StandardDataFormats.WebLink))
                {
                    var uri = await e.DataView.GetWebLinkAsync();

                    StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                    if (!file.FileType.ToLower().Equals(".txt")) return;
                    edBase64.Text = await FileIO.ReadTextAsync(file);
                }
                else if (e.DataView.Contains(StandardDataFormats.Text))
                {
                    var content = await e.DataView.GetTextAsync();
                    if (content.Length > 0)
                    {
                        edBase64.Text = content;
                    }
                }
            }
            await IsSVG(imgBase64);
            //def.Complete();
        }
        #endregion

    }
}
