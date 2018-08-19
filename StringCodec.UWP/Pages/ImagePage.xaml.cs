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

        public ImagePage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            optPrefix.IsChecked = CURRENT_PREFIX;
            optLinbeBreak.IsChecked = CURRENT_LINEBREAK;

            optFmtPng.IsChecked = true;

            if (!string.IsNullOrEmpty(text_src))
            {
                edBase64.Text = text_src;
            }

            #region Add small image to Image control for dragdrop target
            var wb = new WriteableBitmap(1, 1);
            imgBase64.Stretch = Stretch.Uniform;
            imgBase64.Source = wb;
            #endregion
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

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            switch (btn.Name)
            {
                case "btnEncode":
                    if (CURRENT_LINEBREAK) edBase64.TextWrapping = TextWrapping.NoWrap;
                    else edBase64.TextWrapping = TextWrapping.Wrap;
                    edBase64.Text = await TextCodecs.Encode(imgBase64.Source as WriteableBitmap, CURRENT_FORMAT, CURRENT_PREFIX, CURRENT_LINEBREAK);
                    break;
                case "btnDecode":
                    //imgBase64.Source = await TextCodecs.Decode(edBase64.Text);
                    var bmp = await TextCodecs.Decode(edBase64.Text);
                    //if (bmp.PixelWidth >= imgBase64.RenderSize.Width || bmp.PixelHeight >= imgBase64.RenderSize.Height) imgBase64.Stretch = Stretch.Uniform;
                    //else imgBase64.Stretch = Stretch.None;
                    imgBase64.Source = bmp;
                    break;
                case "btnCopy":
                    Utils.SetClipboard(edBase64.Text);
                    break;
                case "btnPaste":
                    edBase64.Text = await Utils.GetClipboard(edBase64.Text, imgBase64);
                    break;
                case "btnSave":
                    await Utils.ShowSaveDialog(imgBase64);
                    break;
                case "btnShare":
                    await Utils.Share(imgBase64.Source as WriteableBitmap);
                    break;
                default:
                    break;
            }
        }

        private void edBase64_TextChanged(object sender, TextChangedEventArgs e)
        {
            text_src = edBase64.Text;
        }

        #region Drag/Drop routines
        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (sender == imgBase64)
            {
                if (e.DataView.Contains(StandardDataFormats.WebLink) ||
                e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }
            else if (sender == edBase64)
            {
                if (e.DataView.Contains(StandardDataFormats.Text))
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            if (sender == imgBase64)
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        var storageFile = items[0] as StorageFile;
                        var bitmapImage = new WriteableBitmap(1,1);
                        bitmapImage.SetSource(await storageFile.OpenAsync(FileAccessMode.Read));
                        // Set the image on the main page to the dropped image
                        //if (bitmapImage.PixelWidth >= imgBase64.RenderSize.Width || bitmapImage.PixelHeight >= imgBase64.RenderSize.Height)
                        //    imgBase64.Stretch = Stretch.Uniform;
                        //else imgBase64.Stretch = Stretch.None;
                        byte[] arr = WindowsRuntimeBufferExtensions.ToArray(bitmapImage.PixelBuffer, 0, (int)bitmapImage.PixelBuffer.Length);
                        imgBase64.Source = bitmapImage;
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
                    //if (bitmapImage.PixelWidth >= imgBase64.RenderSize.Width || bitmapImage.PixelHeight >= imgBase64.RenderSize.Height)
                    //    imgBase64.Stretch = Stretch.Uniform;
                    //else imgBase64.Stretch = Stretch.None;
                    byte[] arr = WindowsRuntimeBufferExtensions.ToArray(bitmapImage.PixelBuffer, 0, (int)bitmapImage.PixelBuffer.Length);
                    imgBase64.Source = bitmapImage;
                }
            }
            else if (sender == edBase64)
            {
                if (e.DataView.Contains(StandardDataFormats.Text))
                {
                    var content = await e.DataView.GetTextAsync();
                    if (content.Length > 0)
                    {
                        edBase64.Text = content;
                    }
                }
            }
            #endregion
        }
    }
}
