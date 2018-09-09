using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using StringCodec.UWP.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
    public sealed partial class SvgPage : Page
    {
        private CanvasBitmap backgroundImage32;
        private CanvasImageBrush backgroundBrush32;
        private bool resourcesLoaded32 = false;

        private CanvasBitmap backgroundImage16;
        private CanvasImageBrush backgroundBrush16;
        private bool resourcesLoaded16 = false;

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

        private void BASE64_Click(object sender, RoutedEventArgs e)
        {
            if(imgSvg.Source is SvgImageSource)
            {
                var svg = new SVG
                {
                    Bytes = imgSvg.Tag as byte[],
                    Source = imgSvg.Source as SvgImageSource
                };
                Frame.Navigate(typeof(ImagePage), svg);
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            switch (btn.Name)
            {
                case "btnMake":
                    if(imgSvg.Source is SvgImageSource)
                    {
                        var svgImage = imgSvg.Source as SvgImageSource;
                        var w = (int)svgImage.RasterizePixelWidth;
                        var h = (int)svgImage.RasterizePixelHeight;
                        //if (double.IsInfinity(w) || double.IsInfinity(h))
                        if (w <= 0 || h <= 0)
                        {
                            w = (int)imgSvg.ActualWidth;
                            h = (int)imgSvg.ActualHeight;
                        }
                        var factor = (double)h / (double)w;
                        var wb = await imgSvg.ToWriteableBitmap();
                        if(CURRENT_ICONS.Equals("win", StringComparison.CurrentCultureIgnoreCase))
                        {
                            List<int> sizelist = new List<int>() { 1024, 512, 256, 128, 96, 64, 48, 32, 24, 16 };
                            Dictionary<int, Image> images = new Dictionary<int, Image>()
                            {
                                //{1024, Image1024}, {512,Image512},
                                {256, Image256}, {128, Image128}, {96, Image96},
                                {64, Image64}, {48,Image48}, {32, Image32}, {24, Image24}, {16, Image16}
                            };
                            foreach (var kv in images)
                            {
                                kv.Value.Source = wb.Resize((int)kv.Key, (int)(kv.Key * factor), WriteableBitmapExtensions.Interpolation.Bilinear);                                
                            }
                        }
                    }
                    break;
                case "btnCopy":
                    Utils.SetClipboard(imgSvg, -1);
                    break;
                case "btnPaste":
                    var svgdoc = await Utils.GetClipboard("");
                    var svg = await svgdoc.DecodeSvg();
                    imgSvg.Source = svg.Source;
                    imgSvg.Tag = svg.Bytes.Clone();
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

        private void BackgroundCanvas16_CreateResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(Task.Run(async () =>
            {
                // Load the background image and create an image brush from it
                this.backgroundImage16 = await CanvasBitmap.LoadAsync(sender, new Uri("ms-appx:///Assets/CheckboardPattern_1664.png"));
                this.backgroundBrush16 = new CanvasImageBrush(sender, this.backgroundImage16) { Opacity = 0.3f };

                // Set the brush's edge behaviour to wrap, so the image repeats if the drawn region is too big
                this.backgroundBrush16.ExtendX = this.backgroundBrush16.ExtendY = CanvasEdgeBehavior.Wrap;

                this.resourcesLoaded16 = true;
            }).AsAsyncAction());
        }

        private void BackgroundCanvas16_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            // Just fill a rectangle with our tiling image brush, covering the entire bounds of the canvas control
            var session = args.DrawingSession;
            session.FillRectangle(new Rect(new Point(), sender.RenderSize), this.backgroundBrush16);
        }

        private void BackgroundCanvas32_CreateResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(Task.Run(async () =>
            {
                // Load the background image and create an image brush from it
                this.backgroundImage32 = await CanvasBitmap.LoadAsync(sender, new Uri("ms-appx:///Assets/CheckboardPattern_3264.png"));
                this.backgroundBrush32 = new CanvasImageBrush(sender, this.backgroundImage32) { Opacity = 0.3f };

                // Set the brush's edge behaviour to wrap, so the image repeats if the drawn region is too big
                this.backgroundBrush32.ExtendX = this.backgroundBrush32.ExtendY = CanvasEdgeBehavior.Wrap;

                this.resourcesLoaded32 = true;
            }).AsAsyncAction());
        }

        private void BackgroundCanvas32_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            // Just fill a rectangle with our tiling image brush, covering the entire bounds of the canvas control
            var session = args.DrawingSession;
            session.FillRectangle(new Rect(new Point(), sender.RenderSize), this.backgroundBrush32);
        }
    }
}
