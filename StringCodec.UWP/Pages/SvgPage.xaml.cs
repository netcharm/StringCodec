using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Svg;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using StringCodec.UWP.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Windows.UI.Xaml.Markup;
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
        private string CURRENT_ICONS = "win";
        private bool CURRENT_SQUARE = false;

        private string SourceFileName = string.Empty;

        private CanvasBitmap backgroundImage32;
        private CanvasImageBrush backgroundBrush32;
        //private bool resourcesLoaded32 = false;

        private CanvasBitmap backgroundImage16;
        private CanvasImageBrush backgroundBrush16;
        //private bool resourcesLoaded16 = false;

        private Image target = null;

        //private List<int> sizelist = new List<int>() { 1024, 512, 256, 128, 96, 64, 48, 32, 24, 16 };

        private ObservableCollection<ImageItem> imagelist = new ObservableCollection<ImageItem>();
        public ObservableCollection<ImageItem> Images
        {
            get { return (imagelist); }
            //set { imagelist = value; }
        }

        private bool CheckFlyoutValid()
        {
            bool result = false;
            bool IsImage = target is Image;
            bool IsValid = false;
            bool HasValid = false;

            foreach (var item in Images)
            {
                if (IsImage && item.Source == target.Source && item.IsValid)
                {
                    IsValid = true;
                }

                if (item.IsValid)
                {
                    HasValid = true;
                    //break;
                }
            }

            if (IsImage && IsValid)
            {
                ImageFlyoutCopy.IsEnabled = true;
                ImageFlyoutShare.IsEnabled = true;
                ImageFlyoutExport.IsEnabled = true;
            }
            else
            {
                ImageFlyoutCopy.IsEnabled = false;
                ImageFlyoutShare.IsEnabled = false;
                ImageFlyoutExport.IsEnabled = false;
            }

            if (HasValid)
            {
                ImageFlyoutExportAll.IsEnabled = true;
            }
            else
            {
                ImageFlyoutExportAll.IsEnabled = false;
            }
            return (result);
        }

        private async void MakeImages(List<int> sizelist)
        {
            var wb = await imgSvg.ToWriteableBitmap();
            var valid = true;
            if (!(wb is WriteableBitmap))
            {
                wb = new WriteableBitmap(1, 1);
                valid = false;
            }

            if (wb is WriteableBitmap)
            {
                var w = (int)imgSvg.ActualWidth;
                var h = (int)imgSvg.ActualHeight;
                var factor = (double)h / (double)w;
                if (CURRENT_SQUARE)
                {
                    if (w < h)
                    {
                        var delta = (int)((h - w) / 2);
                        wb = wb.Extend(delta, 0, delta, 0, Colors.Transparent);
                    }
                    else if (w > h)
                    {
                        var delta = (int)((w - h) / 2);
                        wb = wb.Extend(0, delta, 0, delta, Colors.Transparent);
                    }
                    factor = 1;
                }

                //ImageList.ItemsSource = null;
                //List<int> sizelist = new List<int>() { 256, 128, 96, 72, 64, 48, 32, 24, 16 };
                imagelist.Clear();
                foreach (var s in sizelist)
                {
                    var item = new ImageItem()
                    {
                        IsValid = valid,
                        Size = s,
                        Text = $"{s}x{s}",
                        Margin = new Thickness(-12, 0, 0, 0),
                        MinHeight = 96,
                        MaxHeight = 256,
                        Width = 256,
                        Source = wb.Resize((int)s, (int)(s * factor), WriteableBitmapExtensions.Interpolation.Bilinear)
                    };
                    if (imgSvg.Tag is byte[]) item.Bytes = imgSvg.Tag as byte[];
                    if (s > MinHeight) item.Height = s;
                    if (!string.IsNullOrEmpty(SourceFileName)) item.SourceName = SourceFileName;
                    imagelist.Add(item);
                }
                //ImageList.ItemsSource = imagelist;
                //ImageList.SelectionMode = ListViewSelectionMode.None;
            }
        }

        private async void MakeImages(List<Size> sizelist)
        {
            var wb = await imgSvg.ToWriteableBitmap();
            var valid = true;
            if (!(wb is WriteableBitmap))
            {
                wb = new WriteableBitmap(1, 1);
                valid = false;
            }

            if (wb is WriteableBitmap)
            {
                var w = (int)imgSvg.ActualWidth;
                var h = (int)imgSvg.ActualHeight;
                var factor = (double)h / (double)w;

                double l = 0;
                double t = 0;
                double r = 0;
                double b = 0;
                if (w < h) l = r = (h - w) / 2.0;
                else t = b = (w - h) / 2.0;
                wb = wb.Extend((int)l, (int)t, (int)r, (int)b, Colors.Transparent);

                imagelist.Clear();
                foreach (var s in sizelist)
                {

                    var tw = s.Width;
                    var th = s.Height;
                    var item = new ImageItem()
                    {
                        IsValid = valid,
                        Size = (int)Math.Max(s.Width, s.Height),
                        Text = $"{tw}x{th}",
                        Margin = new Thickness(-12, 0, 0, 0),
                        MinHeight = 96,
                        MaxHeight = 256,
                        Width = 256,
                        Source = wb.Resize((int)tw, (int)th, WriteableBitmapExtensions.Interpolation.Bilinear)
                    };
                    if (imgSvg.Tag is byte[]) item.Bytes = imgSvg.Tag as byte[];
                    if (tw > MinHeight) item.Height = tw;
                    if (!string.IsNullOrEmpty(SourceFileName)) item.SourceName = SourceFileName;
                    imagelist.Add(item);
                }
            }
        }

        public SvgPage()
        {
            this.InitializeComponent();

            NavigationCacheMode = NavigationCacheMode.Enabled;

            optFmtPng.IsChecked = true;

            MakeImages(new List<int>() { 256, 128 });
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

        private void OptSquareIcon_Click(object sender, RoutedEventArgs e)
        {
            CURRENT_SQUARE = optIconSquare.IsChecked;
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
        private async void ImageFlyout_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ImageFlyoutCopy)
            {
                if (target is Image && target.Source != null && (target.Tag is bool) && ((bool)target.Tag == true))
                {
                    Utils.SetClipboard(target, -1);
                }
            }
            else if (sender == ImageFlyoutShare)
            {
                if (target is Image && target.Source != null && (target.Tag is bool) && ((bool)target.Tag == true))
                {
                    await Utils.Share(await target.ToWriteableBitmap(), Path.GetFileNameWithoutExtension(SourceFileName));
                }
            }
            else if (sender == ImageFlyoutExport)
            {
                if (target is Image && target.Source != null && (target.Tag is bool) && ((bool)target.Tag == true))
                {
                    await Utils.ShowSaveDialog(target, Path.GetFileNameWithoutExtension(SourceFileName));
                }
            }
            else if (sender == ImageFlyoutExportAll)
            {
                bool icon_valid = false;
                foreach (var item in imagelist)
                {
                    if (item.Source != null && item.IsValid)
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
                            if (!item.IsValid) continue;
                            var wb = await item.ToWriteableBitmap();
                            if (wb is WriteableBitmap)
                            {
                                //var fn = $"{DateTime.Now.ToString("yyyyMMddHHmmssff")}_{item.Size}{CURRENT_FORMAT}";
                                var fn = $"{Path.GetFileNameWithoutExtension(SourceFileName)}_{item.Size}{CURRENT_FORMAT}";
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
            if (sender is MenuFlyout)
            {
                var ft = (sender as MenuFlyout).Target;
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
            CheckFlyoutValid();
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
                        SourceFileName = file.Name;
                        if (file.FileType.ToLower().Equals(".svg"))
                        {
                            var svgData = await SVG.CreateFromStorageFile(file);
                            imgSvg.Source = svgData.Source;
                            imgSvg.Tag = svgData.Bytes;
                        }
                        //else if (file.FileType.ToLower().Equals(".xaml"))
                        //{
                        //    var xamldoc = await FileIO.ReadTextAsync(file);
                        //    var xaml = XamlReader.Load(xamldoc);
                        //    //DrawingBrush db = new DrawingBrush();
                        //}
                        else
                        {
                            imgSvg.Source = await file.ToWriteableBitmap();
                        }
                    }
                    break;
                case "btnMake":
                    if (CURRENT_ICONS.Equals("win", StringComparison.CurrentCultureIgnoreCase))
                    {
                        MakeImages(new List<int>() { 256, 128, 96, 72, 64, 48, 32, 24, 16 });
                    }
                    else if (CURRENT_ICONS.Equals("uwp", StringComparison.CurrentCultureIgnoreCase))
                    {
                        MakeImages(new List<int>() { 1024, 512 });
                    }
                    break;
                case "btnCopy":
                    Utils.SetClipboard(imgSvg, -1);
                    break;
                case "btnPaste":
                    var svgdoc = await Utils.GetClipboard("", imgSvg);
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
                        if (sender == imgSvg || sender == BackgroundCanvas)
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

            if (sender == imgSvg || sender == BackgroundCanvas)
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
            if (sender == imgSvg || sender == BackgroundCanvas)
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        try
                        {
                            var storageFile = items[0] as StorageFile;
                            SourceFileName = storageFile.Name;
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
                            await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
                        }
                    }
                }
                else if (e.DataView.Contains(StandardDataFormats.WebLink))
                {
                    var uri = await e.DataView.GetWebLinkAsync();

                    try
                    {
                        StorageFile storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
                        SourceFileName = storageFile.Name;
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
                        await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
                    }
                }
            }
            //def.Complete();
        }
        #endregion

    }

    public sealed class ImageItem : FrameworkElement
    {
        public ImageSource Source { get; set; }
        public string Text { get; set; }
        public int Size { get; set; }
        public byte[] Bytes { get; set; }
        public bool IsValid = true;
        public string SourceName = string.Empty;

        public ImageItem()
        {
            Text = string.Empty;
        }

        public async Task<WriteableBitmap> ToWriteableBitmap()
        {
            WriteableBitmap result = null;
            if (IsValid)
            {
                if (Source is WriteableBitmap)
                {
                    result = Source as WriteableBitmap;
                }
                else if (Source is BitmapSource)
                {
                    var bmp = Source as BitmapImage;
                    result = await bmp.ToWriteableBitmap();
                }
                else if (Source is SvgImageSource)
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
                            using (var imras = new InMemoryRandomAccessStream())
                            {
                                await offscreen.SaveAsync(imras, CanvasBitmapFileFormat.Png);
                                result = await imras.ToWriteableBitmap();
                            }
                        }
                    }
                }
            }
            return (result);
        }
    }
}
