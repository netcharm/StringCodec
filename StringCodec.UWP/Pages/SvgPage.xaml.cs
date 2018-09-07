using StringCodec.UWP.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
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
    public sealed partial class SvgPage : Page
    {
        private string CURRENT_FORMAT = ".png";

        static private string text_src = string.Empty;
        static public string Text
        {
            get { return text_src; }
            set { text_src = value; }
        }

        public SvgPage()
        {
            this.InitializeComponent();

            NavigationCacheMode = NavigationCacheMode.Enabled;

            optFmtPng.IsChecked = true;

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                var data = e.Parameter;
                if (data is SVG)
                {
                    imgSvg.Source = (data as SVG).Source;
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void OptFmt_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] opts = new ToggleMenuFlyoutItem[] { optFmtBmp, optFmtGif, optFmtJpg, optFmtPng, optFmtTif };

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
                default:
                    break;
            }
        }

        private async void QRCode_Click(object sender, RoutedEventArgs e)
        {
            if (sender == btnImageQRCode)
            {
                if (imgSvg.Source == null) return;
                //Frame.Navigate(typeof(QRCodePage), imgBase64.Source as WriteableBitmap);
                Frame.Navigate(typeof(QRCodePage), await imgSvg.ToWriteableBitmap());
            }
            else if (sender == btnImageOneD)
            {
                if (imgSvg.Source == null) return;
                //Frame.Navigate(typeof(CommonOneDPage), imgBase64.Source as WriteableBitmap);
                Frame.Navigate(typeof(CommonOneDPage), await imgSvg.ToWriteableBitmap());
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            switch (btn.Name)
            {
                case "btnCopy":
                    //Utils.SetClipboard(edBase64.Text);
                    Utils.SetClipboard(imgSvg, -1);
                    break;
                case "btnPaste":
                    break;
                case "btnSave":
                    await Utils.ShowSaveDialog(imgSvg);
                    break;
                case "btnShare":
                    await Utils.Share(await imgSvg.ToWriteableBitmap());
                    break;
                default:
                    break;
            }
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
                        if (sender == imgSvg || sender == rectDrop)
                        {
                            if (Utils.image_ext.Contains(extension) && extension.Equals(".svg"))
                            {
                                canDrop = true;
#if DEBUG
                                System.Diagnostics.Debug.WriteLine("Drag Svg to Image Control");
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
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Drag Over Sender:{sender}");
#endif

            if (sender == imgSvg || sender == rectDrop)
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
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            // 需要异步拖放时记得获取Deferral对象
            //var def = e.GetDeferral();
            if (sender == imgSvg || sender == rectDrop)
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

                                    var svg = await SVG.Load(rms);
                                    bitmapImage = svg.Source;
                                    imgSvg.Tag = svg.Bytes;

                                    //await bitmapImage.SetSourceAsync(rms);
                                    //await rms.FlushAsync();

                                    //var bytes = WindowsRuntimeStreamExtensions.AsStreamForRead(rms.GetInputStreamAt(0));
                                    //imgSvg.Tag = bytes;
                                }
                                else if (e.DataView.Contains(StandardDataFormats.Text))
                                {
                                    //var content = await e.DataView.GetHtmlFormatAsync();
                                    var content = await e.DataView.GetTextAsync();
                                    if (content.Length > 0)
                                    {
                                        var rms = await RandomAccessStreamReference.CreateFromUri(new Uri(content)).OpenReadAsync();

                                        var svg = await SVG.Load(rms);
                                        bitmapImage = svg.Source;
                                        imgSvg.Tag = svg.Bytes;

                                        //await bitmapImage.SetSourceAsync(rms);
                                        //await rms.FlushAsync();

                                        //var bytes = WindowsRuntimeStreamExtensions.AsStreamForRead(rms.GetInputStreamAt(0));
                                        //imgSvg.Tag = bytes;
                                    }
                                }
                                else
                                {
                                    var svg = await SVG.Load(storageFile);
                                    bitmapImage = svg.Source;
                                    imgSvg.Tag = svg.Bytes;

                                    //await bitmapImage.SetSourceAsync(await storageFile.OpenReadAsync());
                                    //byte[] bytes = WindowsRuntimeBufferExtensions.ToArray(await FileIO.ReadBufferAsync(storageFile));
                                    //imgSvg.Tag = bytes;
                                }
                                imgSvg.Source = bitmapImage;
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

                                var svg = await SVG.Load(rms);
                                bitmapImage = svg.Source;
                                imgSvg.Tag = svg.Bytes;

                                //await bitmapImage.SetSourceAsync(rms);
                                //await rms.FlushAsync();

                                //var bytes = WindowsRuntimeStreamExtensions.AsStreamForRead(rms.GetInputStreamAt(0));
                                //imgSvg.Tag = bytes;
                            }
                            else if (e.DataView.Contains(StandardDataFormats.Text))
                            {
                                //var content = await e.DataView.GetHtmlFormatAsync();
                                var content = await e.DataView.GetTextAsync();
                                if (content.Length > 0)
                                {
                                    var rms = await RandomAccessStreamReference.CreateFromUri(new Uri(content)).OpenReadAsync();

                                    var svg = await SVG.Load(rms);
                                    bitmapImage = svg.Source;
                                    imgSvg.Tag = svg.Bytes;

                                    //await bitmapImage.SetSourceAsync(rms);
                                    //await rms.FlushAsync();

                                    //var bytes = WindowsRuntimeStreamExtensions.AsStreamForRead(rms.GetInputStreamAt(0));
                                    //imgSvg.Tag = bytes;
                                }
                            }
                            else
                            {
                                var svg = await SVG.Load(storageFile);
                                bitmapImage = svg.Source;
                                imgSvg.Tag = svg.Bytes;

                                //await bitmapImage.SetSourceAsync(await storageFile.OpenReadAsync());
                                //byte[] bytes = WindowsRuntimeBufferExtensions.ToArray(await FileIO.ReadBufferAsync(storageFile));
                                //imgSvg.Tag = bytes;
                            }
                            imgSvg.Source = bitmapImage;
                        }
                    }
                }
            }
            //def.Complete();
        }
        #endregion
    }
}
