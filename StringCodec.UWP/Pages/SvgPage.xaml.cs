using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Svg;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using StringCodec.UWP.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
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
    public sealed partial class SvgPage : Page
    {
        private CanvasBitmap backgroundImage32;
        private CanvasImageBrush backgroundBrush32;
        //private bool resourcesLoaded32 = false;

        private CanvasBitmap backgroundImage16;
        private CanvasImageBrush backgroundBrush16;
        //private bool resourcesLoaded16 = false;

        private Image target = null;

        List<int> sizelist = new List<int>() { 1024, 512, 256, 128, 96, 64, 48, 32, 24, 16 };
        List<ImageItem> imagelist = new List<ImageItem>();

        private string CURRENT_FORMAT = ".png";
        private string CURRENT_ICONS = "win";

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

            if (CURRENT_ICONS.Equals("win", StringComparison.CurrentCultureIgnoreCase))
            {
                List<int> sizes = new List<int>() { 256, 128, 96 };
                imagelist.Clear();
                foreach (var s in sizes)
                {
                    var item = new ImageItem()
                    {
                        Size = s,
                        Text = $"{s}x{s}",
                        Margin = new Thickness(-12, 0, 0, 0),
                        MinHeight = 96,
                        Width = 256,
                        Source = new WriteableBitmap(1, 1)
                    };
                    if (s > MinHeight) item.Height = s;
                    imagelist.Add(item);
                }
                ImageList.ItemsSource = imagelist;
            }
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

        #region Draw Canvas Background Checkboard
        private void BackgroundCanvas16_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(Task.Run(async () =>
            {
                // Load the background image and create an image brush from it
                this.backgroundImage16 = await CanvasBitmap.LoadAsync(sender, new Uri("ms-appx:///Assets/CheckboardPattern_1664.png"));
                this.backgroundBrush16 = new CanvasImageBrush(sender, this.backgroundImage16) { Opacity = 0.3f };

                // Set the brush's edge behaviour to wrap, so the image repeats if the drawn region is too big
                this.backgroundBrush16.ExtendX = this.backgroundBrush16.ExtendY = CanvasEdgeBehavior.Wrap;

                //this.resourcesLoaded16 = true;
            }).AsAsyncAction());
        }

        private void BackgroundCanvas16_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            // Just fill a rectangle with our tiling image brush, covering the entire bounds of the canvas control
            var session = args.DrawingSession;
            session.FillRectangle(new Rect(new Point(), sender.RenderSize), this.backgroundBrush16);
        }

        private void BackgroundCanvas32_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(Task.Run(async () =>
            {
                // Load the background image and create an image brush from it
                backgroundImage32 = await CanvasBitmap.LoadAsync(sender, new Uri("ms-appx:///Assets/CheckboardPattern_3264.png"));
                backgroundBrush32 = new CanvasImageBrush(sender, backgroundImage32) { Opacity = 0.3f };

                // Set the brush's edge behaviour to wrap, so the image repeats if the drawn region is too big
                backgroundBrush32.ExtendX = backgroundBrush32.ExtendY = CanvasEdgeBehavior.Wrap;

                //this.resourcesLoaded32 = true;
            }).AsAsyncAction());
        }

        private void BackgroundCanvas32_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            // Just fill a rectangle with our tiling image brush, covering the entire bounds of the canvas control
            var session = args.DrawingSession;
            session.FillRectangle(new Rect(new Point(), sender.RenderSize), backgroundBrush32);
        }
        #endregion

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

        private void OptIcon_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] opts = new ToggleMenuFlyoutItem[] { optIconWin, optIconUwp };
            var btn = sender as ToggleMenuFlyoutItem;
            var ICO_NAME = btn.Name.Substring(btn.Name.Length - 3).ToLower();

            foreach (ToggleMenuFlyoutItem opt in opts)
            {
                if (string.Equals(opt.Name, btn.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    opt.IsChecked = true;
                }
                else opt.IsChecked = false;
            }
            CURRENT_ICONS = ICO_NAME;
        }

        private async void QRCode_Click(object sender, RoutedEventArgs e)
        {
            if (imgSvg.Source == null) return;
            if (sender == btnImageQRCode)
            {
                Frame.Navigate(typeof(QRCodePage), await imgSvg.ToWriteableBitmap());
            }
            else if (sender == btnImageOneD)
            {
                Frame.Navigate(typeof(CommonOneDPage), await imgSvg.ToWriteableBitmap());
            }
        }

        private async void BASE64_Click(object sender, RoutedEventArgs e)
        {
            if (imgSvg.Source is SvgImageSource)
            {
                var svg = new SVG
                {
                    Bytes = imgSvg.Tag as byte[],
                    Source = imgSvg.Source as SvgImageSource
                };
                Frame.Navigate(typeof(ImagePage), svg);
            }
            else if (imgSvg.Source is WriteableBitmap)
            {
                Frame.Navigate(typeof(ImagePage), await imgSvg.ToWriteableBitmap());
            }
        }

        #region Image List ContextFlyout
        private void Image_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var uiSender = sender as UIElement;
            var flyout = (FlyoutBase)uiSender.GetValue(FlyoutBase.AttachedFlyoutProperty);
            flyout.ShowAt(uiSender as FrameworkElement);
        }

        private async void ImageFlyout_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender == ImageFlyoutCopy)
            {
                if (target is Image && target.Source != null)
                {
                    Utils.SetClipboard(target, -1);
                }
            }
            else if (sender == ImageFlyoutShare)
            {
                if (target is Image && target.Source != null)
                {
                    await Utils.Share(await target.ToWriteableBitmap());
                }
            }
            else if (sender == ImageFlyoutExport)
            {
                if (target is Image && target.Source != null)
                {
                    await Utils.ShowSaveDialog(target);
                }
            }
            else if (sender == ImageFlyoutExportAll)
            {
                bool icon_valid = false;
                foreach (var item in imagelist)
                {
                    if (item.Source != null)
                    {
                        icon_valid = true;
                        break;
                    }
                }
                if (icon_valid)
                {
                    FolderPicker fdp = new FolderPicker();
                    fdp.SuggestedStartLocation = PickerLocationId.Desktop;
                    fdp.FileTypeFilter.Add("*");
                    var folder = await fdp.PickSingleFolderAsync();
                    if (folder != null)
                    {
                        foreach (var item in imagelist)
                        {
                            var wb = await item.ToWriteableBitmap();
                            if (wb is WriteableBitmap)
                            {
                                var fn = $"{DateTime.Now.ToString("yyyyMMddHHmmssff")}_{item.Size}{CURRENT_FORMAT}";
                                var file = await folder.CreateFileAsync(fn, CreationCollisionOption.GenerateUniqueName);
                                wb.SaveAsync(file);
                            }
                        }
                    }
                }
            }
        }

        private void ImageContextFlyout_Opened(object sender, object e)
        {
            if (sender is Flyout)
            {
                var ft = (sender as Flyout).Target;
                if (ft is Viewbox)
                {
                    var ftc = (ft as Viewbox).Child;
                    if (ftc is Image)
                    {
                        target = ftc as Image;
                    }
                }
                else if (ft is CanvasControl)
                {
                    var ftc = ft as CanvasControl;
                    var parent = VisualTreeHelper.GetParent(ft);
                    if (parent is Grid)
                    {
                        int index = (parent as Grid).Children.IndexOf(ftc);
                        var ftv = VisualTreeHelper.GetChild(parent as Grid, index + 1);
                        if (ftv is Viewbox)
                        {
                            var fti = (ftv as Viewbox).Child;
                            if (fti is Image)
                            {
                                target = fti as Image;
                            }
                        }
                    }
                }
                else if (ft is TextBlock)
                {
                    var ftt = ft as TextBlock;
                    var parent = VisualTreeHelper.GetParent(ft);
                    if (parent is Grid)
                    {
                        int index = (parent as Grid).Children.IndexOf(ftt);
                        var ftv = VisualTreeHelper.GetChild(parent as Grid, index - 1);
                        if (ftv is Viewbox)
                        {
                            var fti = (ftv as Viewbox).Child;
                            if (fti is Image)
                            {
                                target = fti as Image;
                            }
                        }
                    }
                }
            }
        }

        private void ImageContextFlyout_Closed(object sender, object e)
        {
            target = null;
        }
        #endregion

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            switch (btn.Name)
            {
                case "btnOpenFile":
                    FileOpenPicker fop = new FileOpenPicker();
                    fop.SuggestedStartLocation = PickerLocationId.Desktop;
                    foreach (var ext in Utils.image_ext)
                    {
                        fop.FileTypeFilter.Add(ext);
                    }
                    var file = await fop.PickSingleFileAsync();
                    if (file != null)
                    {
                        if (file.FileType.ToLower().Equals(".svg"))
                        {
                            var svgData = await SVG.CreateFromStorageFile(file);
                            imgSvg.Source = svgData.Source;
                            imgSvg.Tag = svgData.Bytes;
                        }
                        else
                        {
                            imgSvg.Source = await file.ToWriteableBitmap();
                        }
                    }
                    break;
                case "btnMake":
                    var wb = await imgSvg.ToWriteableBitmap();
                    if(wb is WriteableBitmap)
                    {
                        var w = (int)imgSvg.ActualWidth;
                        var h = (int)imgSvg.ActualHeight;
                        var factor = (double)h / (double)w;

                        if (CURRENT_ICONS.Equals("win", StringComparison.CurrentCultureIgnoreCase))
                        {
                            ImageList.ItemsSource = null;
                            List<int> sizes = new List<int>() { 256, 128, 96, 72, 64, 48, 32, 24, 16 };
                            imagelist.Clear();
                            foreach (var s in sizes)
                            {
                                var item = new ImageItem()
                                {
                                    Size = s,
                                    Text = $"{s}x{s}",
                                    Margin = new Thickness(-12, 0, 0, 0),
                                    MinHeight = 96,
                                    Width = 256,
                                    Source = wb.Resize((int)s, (int)(s * factor), WriteableBitmapExtensions.Interpolation.Bilinear)
                                };
                                if (imgSvg.Tag is byte[]) item.Bytes = imgSvg.Tag as byte[];
                                if (s > MinHeight) item.Height = s;
                                imagelist.Add(item);
                            }
                            ImageList.ItemsSource = imagelist;
                        }
                    }
                    break;
                case "btnCopy":
                    Utils.SetClipboard(imgSvg, -1);
                    break;
                case "btnPaste":
                    var svgdoc = await Utils.GetClipboard("");
                    var svg = await svgdoc.DecodeSvg();
                    if (svg.Source is SvgImageSource)
                        imgSvg.Source = svg.Source;
                    else if (svg.Image is BitmapSource || svg.Image is WriteableBitmap)
                        imgSvg.Source = svg.Image;
                    imgSvg.Tag = svg.Bytes is byte[] ? svg.Bytes.Clone() : null;
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
                            if (Utils.image_ext.Contains(extension))
                            {
                                canDrop = true;
#if DEBUG
                                System.Diagnostics.Debug.WriteLine("Drag picture to Image Control");
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
                        try
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
                                        imgSvg.Tag = svg.Bytes;
                                    }
                                    else if (e.DataView.Contains(StandardDataFormats.Text))
                                    {
                                        var content = await e.DataView.GetTextAsync();
                                        if (content.Length > 0)
                                        {
                                            var rms = await RandomAccessStreamReference.CreateFromUri(new Uri(content)).OpenReadAsync();
                                            var svg = await SVG.CreateFromStream(rms);
                                            bitmapImage = svg.Source;
                                            imgSvg.Tag = svg.Bytes;
                                        }
                                    }
                                    else
                                    {
                                        var svg = await SVG.CreateFromStorageFile(storageFile);
                                        bitmapImage = svg.Source;
                                        imgSvg.Tag = svg.Bytes;
                                    }
                                    imgSvg.Source = bitmapImage;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await new MessageDialog(ex.Message, "ERROR".T()).ShowAsync();
                        }
                    }
                }
                else if (e.DataView.Contains(StandardDataFormats.WebLink))
                {
                    var uri = await e.DataView.GetWebLinkAsync();

                    try
                    {
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
                                    imgSvg.Tag = svg.Bytes;
                                }
                                else if (e.DataView.Contains(StandardDataFormats.Text))
                                {
                                    var content = await e.DataView.GetTextAsync();
                                    if (content.Length > 0)
                                    {
                                        var rms = await RandomAccessStreamReference.CreateFromUri(new Uri(content)).OpenReadAsync();

                                        var svg = await SVG.CreateFromStream(rms);
                                        bitmapImage = svg.Source;
                                        imgSvg.Tag = svg.Bytes;
                                    }
                                }
                                else
                                {
                                    var svg = await SVG.CreateFromStorageFile(storageFile);
                                    bitmapImage = svg.Source;
                                    imgSvg.Tag = svg.Bytes;
                                }
                                imgSvg.Source = bitmapImage;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await new MessageDialog(ex.Message, "ERROR".T()).ShowAsync();
                    }
                }
            }
            //def.Complete();
        }
        #endregion

    }

    public sealed class ImageItem:FrameworkElement
    {
        public ImageSource Source { get; set; }
        public string Text { get; set; }
        public int Size { get; set; }
        public byte[] Bytes { get; set; }

        public ImageItem()
        {
            Size = 0;
            Text = string.Empty;
        }

        public async Task<WriteableBitmap> ToWriteableBitmap()
        {
            WriteableBitmap result = null;
            if (Source is WriteableBitmap)
            {
                result = Source as WriteableBitmap;
            }
            else if (Source is BitmapSource)
            {
                var bmp = Source as BitmapImage;
                result = await bmp.ToWriteableBitmap();
            }
            else if(Source is SvgImageSource)
            {
                var svg = Source as SvgImageSource;

                if (Bytes is byte[])
                {
                    CanvasDevice device = CanvasDevice.GetSharedDevice();
                    var svgDocument = new CanvasSvgDocument(device);
                    svgDocument = CanvasSvgDocument.LoadFromXml(device, Encoding.UTF8.GetString(Bytes));

                    using (var offscreen = new CanvasRenderTarget(device, (float)svg.RasterizePixelWidth, (float)svg.RasterizePixelHeight, 96))
                    {
                        var session = offscreen.CreateDrawingSession();
                        session.DrawSvg(svgDocument, new Size(Size, Size), 0, 0);
                        using(var imras = new InMemoryRandomAccessStream())
                        {
                            await offscreen.SaveAsync(imras, CanvasBitmapFileFormat.Png);
                            result = await imras.ToWriteableBitmap();
                        }
                    }
                }
            }
            return (result);
        }
    }
}
