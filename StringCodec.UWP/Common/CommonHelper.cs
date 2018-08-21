using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace StringCodec.UWP.Common
{
    static public class WriteableBitmapExtetions
    {
        #region FrameworkElement UIElement to WriteableBitmap
        static public async Task<WriteableBitmap> ToBitmap(this FrameworkElement element)
        {
            WriteableBitmap result = null;
            using (var fileStream = new InMemoryRandomAccessStream())
            {
                var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;

                RenderTargetBitmap rtb = new RenderTargetBitmap();
                await rtb.RenderAsync(element);
                var pixelBuffer = await rtb.GetPixelsAsync();
                var r_width = rtb.PixelWidth;
                var r_height = rtb.PixelHeight;

                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                    (uint)r_width, (uint)r_height,
                    dpi, dpi,
                    pixelBuffer.ToArray());
                await encoder.FlushAsync();

                result = new WriteableBitmap(r_width, r_height);
                await result.SetSourceAsync(fileStream);
                await fileStream.FlushAsync();
                byte[] arr = WindowsRuntimeBufferExtensions.ToArray(result.PixelBuffer, 0, (int)result.PixelBuffer.Length);
            }
            return (result);
        }

        static public async Task<WriteableBitmap> ToBitmap(this FrameworkElement element, Color bgcolor)
        {
            WriteableBitmap result = null;
            using (var fileStream = new InMemoryRandomAccessStream())
            {
                var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;

                RenderTargetBitmap rtb = new RenderTargetBitmap();
                await rtb.RenderAsync(element);
                var pixelBuffer = await rtb.GetPixelsAsync();
                var r_width = rtb.PixelWidth;
                var r_height = rtb.PixelHeight;

                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                    (uint)r_width, (uint)r_height,
                    dpi, dpi,
                    pixelBuffer.ToArray());
                await encoder.FlushAsync();

                result = new WriteableBitmap(r_width, r_height);
                result.FillRectangle(0, 0, r_width, r_height, bgcolor);

                var wb = new WriteableBitmap(1, 1);
                await wb.SetSourceAsync(fileStream);
                await fileStream.FlushAsync();
                byte[] arr = WindowsRuntimeBufferExtensions.ToArray(wb.PixelBuffer, 0, (int)wb.PixelBuffer.Length);

                result.BlitRender(wb, false);
            }
            return (result);
        }
        #endregion

        #region Text with family size style color to WriteableBitmap
        static public async Task<WriteableBitmap> ToBitmap(this string text, Panel root, string fontfamily, int fontsize, Color fgcolor, Color bgcolor)
        {
            WriteableBitmap result = null;

            #region try using control off screen render but failed, RenderTargetBitmap not support control without UIElement
            TextBlock textBlock = new TextBlock()
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalTextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(fgcolor),
                FontFamily = new FontFamily(fontfamily),
                FontSize = fontsize,
                Text = text
            };
            Border border = new Border()
            {
                Child = textBlock,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(bgcolor)
            };
            root.Children.Add(border);
            var wb = await border.ToBitmap();
            root.Children.Remove(border);
            #endregion

            return (result);
        }

        static private Rect CalcRect(this string text, string fontfamily, FontStyle fontstyle, int fontsize, bool compact)
        {
            Rect result = Rect.Empty;
            using (var fileStream = new InMemoryRandomAccessStream())
            {
                var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;

                CanvasDevice device = CanvasDevice.GetSharedDevice();
                CanvasRenderTarget offscreen = new CanvasRenderTarget(device, 1024, 1024, 96);
                using (CanvasDrawingSession ds = offscreen.CreateDrawingSession())
                {
                    CanvasTextFormat fmt = new CanvasTextFormat()
                    {
                        FontFamily = fontfamily,
                        FontSize = fontsize,
                        FontStyle = fontstyle,
                        WordWrapping = CanvasWordWrapping.NoWrap,
                        HorizontalAlignment = CanvasHorizontalAlignment.Left,
                        VerticalAlignment = CanvasVerticalAlignment.Top
                    };
                    CanvasTextLayout layout = new CanvasTextLayout(ds, text, fmt, 0.0f, 0.0f);
                    if(compact)
                        result = new Rect(layout.DrawBounds.X, layout.DrawBounds.Y, layout.DrawBounds.Width, layout.DrawBounds.Height);
                    else
                        result = layout.LayoutBounds;
                }
            }
            return (result);
        }

        static public async Task<WriteableBitmap> ToBitmap(this string text, string fontfamily, FontStyle fontstyle, int fontsize, Color fgcolor, Color bgcolor)
        {
            WriteableBitmap result = null;
            using (var fileStream = new InMemoryRandomAccessStream())
            {
                bool compact = false;
                var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                Rect rect = CalcRect(text, fontfamily, fontstyle, fontsize, compact);

                CanvasDevice device = CanvasDevice.GetSharedDevice();
                CanvasRenderTarget offscreen = new CanvasRenderTarget(device, (int)Math.Ceiling(rect.Width), (int)Math.Ceiling(rect.Height), 96);
                using (CanvasDrawingSession ds = offscreen.CreateDrawingSession())
                {
                    CanvasTextFormat fmt = new CanvasTextFormat()
                    {
                        FontFamily = fontfamily,
                        FontSize = fontsize,
                        FontStyle = fontstyle,
                        WordWrapping = CanvasWordWrapping.NoWrap,
                        HorizontalAlignment = CanvasHorizontalAlignment.Left,
                        VerticalAlignment = CanvasVerticalAlignment.Top
                    };
                    ds.Clear(bgcolor);
                    ds.DrawText(text, (int)(-rect.X), (int)(-rect.Y), fgcolor, fmt);
                }
                await offscreen.SaveAsync(fileStream, CanvasBitmapFileFormat.Png);
                await fileStream.FlushAsync();

                //var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                //encoder.SetPixelData(
                //    BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                //    (uint)offscreen.Size.Width, (uint)offscreen.Size.Height,
                //    dpi, dpi,
                //    offscreen.GetPixelBytes());
                //await encoder.FlushAsync();

                result = new WriteableBitmap(1, 1);
                await result.SetSourceAsync(fileStream);
                await fileStream.FlushAsync();
                byte[] arr = WindowsRuntimeBufferExtensions.ToArray(result.PixelBuffer, 0, (int)result.PixelBuffer.Length);
            }
            return (result);
        }
        #endregion

        #region DrawText to WriteableBitmap
        static public async void DrawText(this WriteableBitmap image, int x, int y, string text, string fontfamily, FontStyle fontstyle, int fontsize, Color fgcolor, Color bgcolor)
        {
            var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;

            var twb = await text.ToBitmap(fontfamily, fontstyle, fontsize, fgcolor, bgcolor);
            image.Blit(new Rect(x, y, twb.PixelWidth, twb.PixelHeight), twb, new Rect(0, 0, twb.PixelWidth, twb.PixelHeight));
            //image.BlitRender(twb, false);
            return;
        }

        static public async void DrawText(this UIElement target, int x, int y, string text, string fontfamily, int fontsize, Color fgcolor, Color bgcolor)
        {
            using (var fileStream = new InMemoryRandomAccessStream())
            {
                var textblock = new TextBlock { Text = text, FontSize = 10, Foreground = new SolidColorBrush(fgcolor) };
                //textblock.Paren
                //result.Render(txt1, new RotateTransform { Angle = 0, CenterX = width / 2, CenterY = height - 14 });                    
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                await renderTargetBitmap.RenderAsync(target);
                var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
                var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                var r_width = renderTargetBitmap.PixelWidth;
                var r_height = renderTargetBitmap.PixelHeight;

                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                    (uint)r_width, (uint)r_height,
                    dpi, dpi,
                    pixelBuffer.ToArray());
                await encoder.FlushAsync();
                WriteableBitmap bitmap = new WriteableBitmap(1, 1);
                await bitmap.SetSourceAsync(fileStream);
                await fileStream.FlushAsync();
                byte[] arr = WindowsRuntimeBufferExtensions.ToArray(bitmap.PixelBuffer, 0, (int)bitmap.PixelBuffer.Length);
                x = (int)target.RenderSize.Width / 2;
                y = (int)target.RenderSize.Height - 14;
                //target.BlitRender(bitmap, false, 1, new RotateTransform { Angle = 0, CenterX = x, CenterY = y });
            }
        }

        static public async void DrawText(this WriteableBitmap image, UIElement target, int x, int y, string text, string fontfamily, int fontsize, Color fgcolor, Color bgcolor)
        {
            using (var fileStream = new InMemoryRandomAccessStream())
            {
                var textblock = new TextBlock { Text = text, FontSize = 10, Foreground = new SolidColorBrush(fgcolor) };
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                await renderTargetBitmap.RenderAsync(textblock);
                var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
                var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                var r_width = renderTargetBitmap.PixelWidth;
                var r_height = renderTargetBitmap.PixelHeight;

                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                    (uint)r_width, (uint)r_height,
                    dpi, dpi,
                    pixelBuffer.ToArray());
                await encoder.FlushAsync();
                WriteableBitmap bitmap = new WriteableBitmap(1, 1);
                await bitmap.SetSourceAsync(fileStream);
                await fileStream.FlushAsync();
                byte[] arr = WindowsRuntimeBufferExtensions.ToArray(bitmap.PixelBuffer, 0, (int)bitmap.PixelBuffer.Length);
                x = image.PixelWidth / 2;
                y = image.PixelHeight - 14;
                image.BlitRender(bitmap, false, 1, new RotateTransform { Angle = 0, CenterX = x, CenterY = y });
            }
        }
        #endregion

        #region WriteableBitmmap Effect
        static public WriteableBitmap Extend(this WriteableBitmap image, double percent, Color bgcolor)
        {
            return (image.Extend(percent, percent, percent, percent, bgcolor));
        }

        static public WriteableBitmap Extend(this WriteableBitmap image, double percent_left, double percent_top, double percent_right, double percent_bottom, Color bgcolor)
        {
            var iw = image.PixelWidth;
            var ih = image.PixelHeight;
            int left = (int)Math.Ceiling(iw * percent_left);
            int top = (int)Math.Ceiling(ih * percent_top);
            int right = (int)Math.Ceiling(iw * percent_right);
            int bottom = (int)Math.Ceiling(ih * percent_bottom);
            return (image.Extend(left, top, right, bottom, bgcolor));
        }

        static public WriteableBitmap Extend(this WriteableBitmap image, int size, Color bgcolor)
        {
            return(image.Extend(size, size, size, size, bgcolor));
        }

        static public WriteableBitmap Extend(this WriteableBitmap image, int size_left, int size_top, int size_right, int size_bottom, Color bgcolor)
        {
            WriteableBitmap result = null;

            result = new WriteableBitmap(image.PixelWidth + size_left + size_right, image.PixelHeight + size_top + size_bottom);
            result.FillRectangle(0, 0, result.PixelWidth, result.PixelHeight, bgcolor);
            result.Blit(new Rect(size_left, size_top, image.PixelWidth, image.PixelHeight), image, new Rect(0, 0, image.PixelWidth, image.PixelHeight));

            return (result);
        }

        #endregion

        #region WriteableBitmmap Converter
        static public byte[] ToBytes(this WriteableBitmap image)
        {
            if (image == null) return (null);

            byte[] result = image.PixelBuffer.ToArray();
            return (result);
        }

        static public async Task<WriteableBitmap> ToWriteableBitmmap(this BitmapImage image)
        {
            WriteableBitmap result = null;
            if (image.UriSource != null)
            {
                result = new WriteableBitmap(1, 1);
                var imgRef = RandomAccessStreamReference.CreateFromUri(image.UriSource);
                var ms = await imgRef.OpenReadAsync();
                await ms.FlushAsync();
                await result.SetSourceAsync(ms.AsStream().AsRandomAccessStream());
            }
            return (result);
        }

        static public async Task<WriteableBitmap> ToWriteableBitmmap(this Image image)
        {
            WriteableBitmap result = null;

            if (image.Source == null) return result;
            else if (image.Source is BitmapImage)
            {
                var bitmap = image.Source as BitmapImage;
                var width = bitmap.PixelWidth;
                var height = bitmap.PixelHeight;
                var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;

                if (bitmap.UriSource != null)
                {
                    result = new WriteableBitmap(width, height);
                    var imgRef = RandomAccessStreamReference.CreateFromUri(bitmap.UriSource);
                    var ms = await imgRef.OpenReadAsync();
                    await ms.FlushAsync();
                    await result.SetSourceAsync(ms.AsStream().AsRandomAccessStream());
                    return (result);
                }
                else
                {
                    //把控件变成图像
                    RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                    //传入参数Image控件
                    await renderTargetBitmap.RenderAsync(image, width, height);
                    var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                    using (var fileStream = new InMemoryRandomAccessStream())
                    {
                        var encId = BitmapEncoder.PngEncoderId;
                        var encoder = await BitmapEncoder.CreateAsync(encId, fileStream);
                        encoder.SetPixelData(
                            BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                            (uint)width, (uint)height, dpi, dpi,
                            pixelBuffer.ToArray()
                        );
                        //刷新图像
                        await encoder.FlushAsync();

                        result = new WriteableBitmap(width, height);
                        await result.SetSourceAsync(fileStream);
                    }
                    return (result);
                }
            }
            else if (image.Source is WriteableBitmap)
            {
                return (image.Source as WriteableBitmap);
            }
            return (result);
        }

        static public async Task<BitmapImage> ToBitmapImage(this WriteableBitmap image)
        {
            BitmapImage result = null;
            using (var fileStream = new InMemoryRandomAccessStream())
            {
                var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                    (uint)image.PixelWidth, (uint)image.PixelHeight,
                    dpi, dpi,
                    image.PixelBuffer.ToArray());
                await encoder.FlushAsync();

                result = new BitmapImage();
                await result.SetSourceAsync(fileStream);
                await fileStream.FlushAsync();
            }
            return (result);
        }

        static public async Task<StorageFile> StoreTemporaryFile(this WriteableBitmap image, string prefix="")
        {
            return(await image.StoreTemporaryFile(image.PixelBuffer, image.PixelWidth, image.PixelHeight, prefix));
        }

        static public async Task<StorageFile> StoreTemporaryFile(this WriteableBitmap image, int width, int height, string prefix = "")
        {
            return (await image.StoreTemporaryFile(image.PixelBuffer, width, height, prefix));
        }

        static public async Task<StorageFile> StoreTemporaryFile(this WriteableBitmap image, IBuffer pixelBuffer, int width, int height, string prefix="")
        {
            var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
            var now = DateTime.Now;
            if (!string.IsNullOrEmpty(prefix)) prefix = $"{prefix}_";
            var fn = $"{prefix}{now.ToString("yyyyMMddHHmmssff")}.png";
            StorageFile tempFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(fn, CreationCollisionOption.ReplaceExisting);
            if (tempFile != null)
            {
                CachedFileManager.DeferUpdates(tempFile);

                using (var fileStream = await tempFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                    Stream pixelStream = image.PixelBuffer.AsStream();
                    byte[] pixels = new byte[pixelStream.Length];
                    await pixelStream.ReadAsync(pixels, 0, pixels.Length);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                        (uint)width, (uint)height,
                        dpi, dpi,
                        pixelBuffer.ToArray());
                    await encoder.FlushAsync();
                }

                var status = await CachedFileManager.CompleteUpdatesAsync(tempFile);
                //_tempExportFile = tempFile;
                return (tempFile);
            }
            return (null);
        }

        static public async void SaveAsync(this WriteableBitmap image, StorageFile storageitem)
        {
            if (storageitem != null)
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Add(storageitem, storageitem.Name);
                if (StorageApplicationPermissions.FutureAccessList.Entries.Count >= 1000)
                    StorageApplicationPermissions.FutureAccessList.Remove(StorageApplicationPermissions.FutureAccessList.Entries.Last().Token);
                StorageApplicationPermissions.FutureAccessList.Add(storageitem, storageitem.Name);

                // 在用户完成更改并调用CompleteUpdatesAsync之前，阻止对文件的更新
                CachedFileManager.DeferUpdates(storageitem);

                #region Save Image Control source data
                using (var fileStream = await storageitem.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                    var w = image.PixelWidth;
                    var h = image.PixelHeight;

                    // Get pixels of the WriteableBitmap object 
                    Stream pixelStream = image.PixelBuffer.AsStream();
                    byte[] pixels = new byte[pixelStream.Length];
                    await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                    var encId = BitmapEncoder.PngEncoderId;
                    var fext = Path.GetExtension(storageitem.Name).ToLower();
                    switch (fext)
                    {
                        case ".bmp":
                            encId = BitmapEncoder.BmpEncoderId;
                            break;
                        case ".gif":
                            encId = BitmapEncoder.GifEncoderId;
                            break;
                        case ".png":
                            encId = BitmapEncoder.PngEncoderId;
                            break;
                        case ".jpg":
                            encId = BitmapEncoder.JpegEncoderId;
                            break;
                        case ".jpeg":
                            encId = BitmapEncoder.JpegEncoderId;
                            break;
                        case ".tif":
                            encId = BitmapEncoder.TiffEncoderId;
                            break;
                        case ".tiff":
                            encId = BitmapEncoder.TiffEncoderId;
                            break;
                        default:
                            encId = BitmapEncoder.PngEncoderId;
                            break;
                    }
                    var encoder = await BitmapEncoder.CreateAsync(encId, fileStream);
                    // Save the image file with jpg extension 
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                        (uint)w, (uint)h,
                        dpi, dpi,
                        pixels);
                    await encoder.FlushAsync();
                }
                #endregion
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(storageitem);
            }
        }

        static public async Task<InMemoryRandomAccessStream> StoreMemoryStream(this WriteableBitmap image, string prefix = "")
        {
            return (await image.StoreMemoryStream(image.PixelBuffer, image.PixelWidth, image.PixelHeight, prefix));
        }

        static public async Task<InMemoryRandomAccessStream> StoreMemoryStream(this WriteableBitmap image, int width, int height, string prefix = "")
        {
            return (await image.StoreMemoryStream(image.PixelBuffer, width, height, prefix));
        }

        static public async Task<InMemoryRandomAccessStream> StoreMemoryStream(this WriteableBitmap image, IBuffer pixelBuffer, int width, int height, string prefix = "")
        {
            InMemoryRandomAccessStream result = new InMemoryRandomAccessStream();

            var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
            using (var fileStream = new InMemoryRandomAccessStream())
            {
                fileStream.Seek(0);
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                    (uint)width, (uint)height,
                    dpi, dpi,
                    pixelBuffer.ToArray());
                await encoder.FlushAsync();

                //Stream stream = WindowsRuntimeStreamExtensions.AsStreamForRead(fileStream.GetInputStreamAt(0));
                var ms = WindowsRuntimeStreamExtensions.AsStreamForRead(fileStream.GetInputStreamAt(0));
                await RandomAccessStream.CopyAsync(ms.AsInputStream(), result);
                await result.FlushAsync();
                result.Seek(0);
            }
            return (result);
        }
        #endregion
    }

    class Utils
    {
        #region Share Extentions
        static private DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
        static private StorageFile _tempExportFile;
        //static private InMemoryRandomAccessStream _tempExportStream;
        static private bool SHARE_INITED = false;
        static private WriteableBitmap SHARED_IMAGE = null;
        static private string SHARED_TEXT = string.Empty;

        static private async void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            try
            {
                DataPackage requestData = args.Request.Data;
                requestData.Properties.Title = "Share To...";
                requestData.Properties.Description = "Share the QRCode/BASE64 decoded image to other apps.";

                if (!string.IsNullOrEmpty(SHARED_TEXT) && SHARED_IMAGE == null)
                {
                    requestData.SetText(SHARED_TEXT);
                }
                else if (string.IsNullOrEmpty(SHARED_TEXT) && SHARED_IMAGE != null)
                {

                    #region Save image to a temporary file for Share
                    List<IStorageItem> imageItems = new List<IStorageItem> { _tempExportFile };
                    requestData.SetStorageItems(imageItems);

                    RandomAccessStreamReference imageStreamRef = RandomAccessStreamReference.CreateFromFile(_tempExportFile);
                    requestData.Properties.Thumbnail = imageStreamRef;
                    requestData.SetBitmap(imageStreamRef);
                    #endregion

                    #region Create in memory image data for Share
                    //RandomAccessStreamReference imageStreamRef = RandomAccessStreamReference.CreateFromStream(_tempExportStream);
                    //requestData.Properties.Thumbnail = imageStreamRef;
                    //requestData.SetBitmap(imageStreamRef);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "ERROR").ShowAsync();
            }
        }

        static public async Task<FileUpdateStatus> Share(WriteableBitmap image, string prefix="")
        {
            if (!SHARE_INITED)
            {
                dataTransferManager.DataRequested += DataTransferManager_DataRequested;
                SHARE_INITED = true;
            }

            //ApplicationData.Current.TemporaryFolder.
            SHARED_TEXT = string.Empty;
            SHARED_IMAGE = null;
            FileUpdateStatus status = FileUpdateStatus.Failed;
            if (image == null || image.PixelWidth <= 0 || image.PixelHeight <= 0) return (status);

            SHARED_IMAGE = image;
            #region Save image to a temporary file for Share
            StorageFile tempFile = await image.StoreTemporaryFile(image.PixelBuffer, image.PixelWidth, image.PixelHeight, prefix);
            if (tempFile != null)
            {
                _tempExportFile = tempFile;
                DataTransferManager.ShowShareUI();
            }
            #endregion

            #region Create in memory image data for Share
            //var cistream = await image.StoreMemoryStream(image.PixelBuffer, image.PixelWidth, image.PixelHeight);
            //if (cistream != null || cistream.Size > 0)
            //{
            //    if (cistream.Position > 0) cistream.Seek(0);
            //    _tempExportStream = cistream;
            //    DataTransferManager.ShowShareUI();
            //}
            #endregion

            return status;
        }

        static public FileUpdateStatus Share(string text)
        {
            if (!SHARE_INITED)
            {
                dataTransferManager.DataRequested += DataTransferManager_DataRequested;
                SHARE_INITED = true;
            }

            SHARED_TEXT = string.Empty;
            SHARED_IMAGE = null;
            FileUpdateStatus status = FileUpdateStatus.Failed;
            if (string.IsNullOrEmpty(text)) return (status);

            SHARED_TEXT = text;
            DataTransferManager.ShowShareUI();
            status = FileUpdateStatus.Complete;
            //return status;
            return status;
        }
        #endregion

        #region Clipboard Extentions
        static public void SetClipboard(string text)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
        }

        static public void SetClipboard(Image image, int size)
        {
            SetClipboard(image, size, size);
        }

        static public async void SetClipboard(Image image, int width, int height)
        {
            if (image.Source == null) return;

            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;

            try
            {
                //把控件变成图像
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                //传入参数Image控件
                var bw = (image.Source as WriteableBitmap).PixelWidth;
                var bh = (image.Source as WriteableBitmap).PixelHeight;
                var factor = (float)bh / (float)bw;

                var r_width = width;
                var r_height = Convert.ToInt32(height * factor);

                if (r_width < 0 || r_height < 0)
                {
                    r_width = bw;
                    r_height = Convert.ToInt32(bh * factor);
                }

                await renderTargetBitmap.RenderAsync(image, r_width, r_height);
                var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                r_width = renderTargetBitmap.PixelWidth;
                r_height = renderTargetBitmap.PixelHeight;
                if (width > 0 && height > 0)
                {
                    r_width = width;
                    r_height = Convert.ToInt32(height * factor);
                }

                #region Create a temporary file Copy to Clipboard
                //StorageFile tempFile = await (image.Source as WriteableBitmap).StoreTemporaryFile(pixelBuffer, r_width, r_height);

                //if (tempFile != null)
                //{
                //    dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(tempFile));
                //    //await tempFile.DeleteAsync();
                //}
                #endregion

                #region Create in memory image data Copy to Clipboard
                var cistream = await (image.Source as WriteableBitmap).StoreMemoryStream(pixelBuffer, r_width, r_height);
                if(cistream != null || cistream.Size>0 )
                {
                    if (cistream.Position > 0) cistream.Seek(0); 
                    dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(cistream));
                    cistream.CloneStream();
                }
                #endregion

                #region Create other MIME format data Copy to Clipboard
                ////string[] fmts = new string[] { "CF_DIB", "CF_BITMAP", "BITMAP", "DeviceIndependentBitmap", "image/png", "image/bmp", "image/jpg", "image/jpeg" };
                //string[] fmts = new string[] { "image/png", "image/bmp", "image/jpg", "image/jpeg" };

                //foreach (var fmt in fmts)
                //{
                //    using (var fileStream = new InMemoryRandomAccessStream())
                //    {
                //        fileStream.Seek(0);

                //        var encId = BitmapEncoder.PngEncoderId;
                //        switch (fmt)
                //        {
                //            case "image/png":
                //                encId = BitmapEncoder.PngEncoderId;
                //                break;
                //            case "image/jpg":
                //                encId = BitmapEncoder.JpegEncoderId;
                //                break;
                //            case "image/jpeg":
                //                encId = BitmapEncoder.JpegEncoderId;
                //                break;
                //            case "image/bmp":
                //                encId = BitmapEncoder.BmpEncoderId;
                //                break;
                //            case "Bitmap":
                //                encId = BitmapEncoder.BmpEncoderId;
                //                break;
                //            case "CF_DIB":
                //                encId = BitmapEncoder.BmpEncoderId;
                //                break;
                //            case "CF_BITMAP":
                //                encId = BitmapEncoder.BmpEncoderId;
                //                break;
                //            case "DeviceIndependentBitmap":
                //                encId = BitmapEncoder.BmpEncoderId;
                //                break;
                //            default:
                //                break;
                //        }
                //        var encoder = await BitmapEncoder.CreateAsync(encId, fileStream);
                //        encoder.SetPixelData(
                //            BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                //            (uint)r_width, (uint)r_height,
                //            dpi, dpi,
                //            pixelBuffer.ToArray());
                //        await encoder.FlushAsync();

                //        //Stream stream = WindowsRuntimeStreamExtensions.AsStreamForRead(fileStream.GetInputStreamAt(0));
                //        using (Stream stream = WindowsRuntimeStreamExtensions.AsStreamForRead(fileStream.GetInputStreamAt(0)))
                //        {
                //            using (MemoryStream ms = new MemoryStream())
                //            {
                //                await stream.CopyToAsync(ms);
                //                byte[] arr = ms.ToArray();
                //                if (fmt.Equals("CF_DIB", StringComparison.CurrentCultureIgnoreCase))
                //                {
                //                    //
                //                    // here has bug when add bytes to clipboard, system will 
                //                    // insert 8 bytes before my data bytes automatic
                //                    //
                //                    byte[] dib = arr.Skip(14).ToArray();
                //                    dataPackage.SetData(fmt, dib);

                //                    //using (var mrs = new InMemoryRandomAccessStream())
                //                    //{
                //                    //    await RandomAccessStream.CopyAsync(dib.AsBuffer().AsStream().AsInputStream(), mrs.AsStream().AsOutputStream());
                //                    //    dataPackage.SetData(fmt, RandomAccessStreamReference.CreateFromStream(mrs));
                //                    //}

                //                    //var buffer = WindowsRuntimeBufferExtensions.AsBuffer(dib, 0, dib.Length);
                //                    //InMemoryRandomAccessStream inStream = new InMemoryRandomAccessStream();
                //                    //DataWriter datawriter = new DataWriter(inStream.GetOutputStreamAt(0));
                //                    //datawriter.WriteBuffer(buffer, 0, buffer.Length);
                //                    //await datawriter.StoreAsync();
                //                    //dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(inStream));
                //                }
                //                else dataPackage.SetData(fmt, arr);
                //            }
                //        }
                //    }
                //}
                #endregion


                Clipboard.SetContent(dataPackage);
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "ERROR").ShowAsync();
            }
        }

        static public async Task<string> GetClipboard(string text)
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                string content = await dataPackageView.GetTextAsync();
                // To output the text from this example, you need a TextBlock control
                return (content);
            }
            return (text);
        }

        static public async Task<string> GetClipboard(string text, Image image = null)
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                string content = await dataPackageView.GetTextAsync();
                // To output the text from this example, you need a TextBlock control
                return (content);
            }
            else if (dataPackageView.Contains(StandardDataFormats.Bitmap))
            {
                try
                {
                    var fmts = dataPackageView.AvailableFormats;
                    List<string> fl = new List<string>();
                    foreach (var fmt in fmts)
                    {
                        fl.Add(fmt.ToString());
                    }
                    //
                    // maybe UWP WriteableBitmap/BitmapImage not support loading CF_DIB/CF_DIBv5 format so...
                    //
                    if (dataPackageView.Contains("DeviceIndependentBitmapV5_"))
                    {
                        using (var fileStream = new InMemoryRandomAccessStream())
                        {
                            var data = await dataPackageView.GetDataAsync("DeviceIndependentBitmapV5");
                            var dataObj = data as IRandomAccessStream;
                            var stream = dataObj.GetInputStreamAt(0);

                            Stream dibStream = WindowsRuntimeStreamExtensions.AsStreamForRead(stream);
                            MemoryStream ms = new MemoryStream();
                            await dibStream.CopyToAsync(ms);
                            byte[] dibBytes = ms.ToArray();
                            await dibStream.FlushAsync();

                            byte[] bb = new byte[] {
                                0x42, 0x4D,
                                0x00, 0x00, 0x00, 0x00,
                                0x00, 0x00,
                                0x00, 0x00,
                                0x36, 0x00, 0x00, 0x28
                            };
                            var bs = (uint)dibBytes.Length + 14;
                            var bsb = BitConverter.GetBytes(bs);
                            bb[2] = bsb[0];
                            bb[3] = bsb[1];
                            bb[4] = bsb[2];
                            bb[5] = bsb[3];
                            var bh = WindowsRuntimeBufferExtensions.AsBuffer(bb, 0, bb.Length);                           
                            await fileStream.WriteAsync(bh);
                            await fileStream.FlushAsync();

                            var bd = WindowsRuntimeBufferExtensions.AsBuffer(dibBytes, 0, dibBytes.Length);
                            await fileStream.WriteAsync(bd);
                            await fileStream.FlushAsync();

                            WriteableBitmap bitmap = new WriteableBitmap(1, 1);
                            await bitmap.SetSourceAsync(fileStream);
                            byte[] arr = WindowsRuntimeBufferExtensions.ToArray(bitmap.PixelBuffer, 0, (int)bitmap.PixelBuffer.Length);
                            image.Source = bitmap;
                        }
                    }
                    else if (dataPackageView.Contains("DeviceIndependentBitmap_"))
                    {
                        using (var fileStream = new InMemoryRandomAccessStream())
                        {
                            var data = await dataPackageView.GetDataAsync("DeviceIndependentBitmap");
                            var dataObj = data as IRandomAccessStream;
                            var stream = dataObj.GetInputStreamAt(0);

                            Stream dibStream = WindowsRuntimeStreamExtensions.AsStreamForRead(stream);
                            MemoryStream ms = new MemoryStream();
                            await dibStream.CopyToAsync(ms);
                            byte[] dibBytes = ms.ToArray();
                            await dibStream.FlushAsync();

                            // maybe need change byte order?
                            //for(int i=0; i< dibBytes.Length; i=i+2)
                            //{
                            //    var b0 = dibBytes[i];
                            //    var b1 = dibBytes[i + 1];
                            //    dibBytes[i] = b1;
                            //    dibBytes[i + 1] = b0;
                            //}

                            byte[] bb = new byte[] {
                                0x42, 0x4D,
                                0x00, 0x00, 0x00, 0x00,
                                0x00, 0x00,
                                0x00, 0x00,
                                0x36, 0x00, 0x00, 0x28
                            };
                            var bs = (uint)dibBytes.Length + 14;
                            var bsb = BitConverter.GetBytes(bs);
                            bb[2] = bsb[0];
                            bb[3] = bsb[1];
                            bb[4] = bsb[2];
                            bb[5] = bsb[3];
                            var bh = WindowsRuntimeBufferExtensions.AsBuffer(bb, 0, bb.Length);
                            await fileStream.WriteAsync(bh);
                            await fileStream.FlushAsync();

                            var bd = WindowsRuntimeBufferExtensions.AsBuffer(dibBytes, 0, dibBytes.Length);
                            await fileStream.WriteAsync(bd);
                            await fileStream.FlushAsync();

                            //await RandomAccessStream.CopyAsync(stream, fileStream.GetOutputStreamAt(bh.Length));
                            //await fileStream.FlushAsync();
                            //fileStream

                            WriteableBitmap bitmap = new WriteableBitmap(1, 1);
                            await bitmap.SetSourceAsync(fileStream);
                            byte[] arr = WindowsRuntimeBufferExtensions.ToArray(bitmap.PixelBuffer, 0, (int)bitmap.PixelBuffer.Length);
                            image.Source = bitmap;
                        }
                    }
                    else if (dataPackageView.Contains("image/png"))
                    {
                        using (var fileStream = new InMemoryRandomAccessStream())
                        {
                            var data = await dataPackageView.GetDataAsync("image/png");
                            var dataObj = data as IRandomAccessStream;
                            var stream = dataObj.GetInputStreamAt(0);

                            await RandomAccessStream.CopyAsync(stream, fileStream.GetOutputStreamAt(0));
                            await fileStream.FlushAsync();

                            WriteableBitmap bitmap = new WriteableBitmap(1, 1);
                            await bitmap.SetSourceAsync(fileStream);
                            byte[] arr = WindowsRuntimeBufferExtensions.ToArray(bitmap.PixelBuffer, 0, (int)bitmap.PixelBuffer.Length);
                            image.Source = bitmap;
                        }
                    }
                    else
                    {
                        var bmp = await dataPackageView.GetBitmapAsync();
                        WriteableBitmap bitmap = new WriteableBitmap(1, 1);
                        await bitmap.SetSourceAsync(await bmp.OpenReadAsync());

                        if (image != null)
                        {
                            //if (bitmap.PixelWidth >= image.RenderSize.Width || bitmap.PixelHeight >= image.RenderSize.Height)
                            //    image.Stretch = Stretch.Uniform;
                            //else image.Stretch = Stretch.None;
                            byte[] arr = WindowsRuntimeBufferExtensions.ToArray(bitmap.PixelBuffer, 0, (int)bitmap.PixelBuffer.Length);
                            image.Source = bitmap;
                            text = await QRCodec.Decode(bitmap);
                        }
                    }
                }
                catch (Exception ex)
                {
                    await new MessageDialog(ex.Message, "ERROR").ShowAsync();
                }
            }
            return (text);
        }
        #endregion

        #region ContentDialog Extentions
        static public async Task<Color> ShowColorDialog()
        {
            return (await ShowColorDialog(Colors.White));
        }

        static public async Task<Color> ShowColorDialog(Color color)
        {
            Color result = color;

            ColorDialog dlgColor = new ColorDialog() { Color = color, Alpha = true };
            ContentDialogResult ret = await dlgColor.ShowAsync();
            if (ret == ContentDialogResult.Primary)
            {
                result = dlgColor.Color;
            }

            return (result);
        }

        static public async Task<string> ShowSaveDialog(string content)
        {
            string result = string.Empty;

            if (content.Length <= 0) return (result);

            var now = DateTime.Now;
            FileSavePicker fp = new FileSavePicker();
            fp.SuggestedStartLocation = PickerLocationId.Desktop;
            fp.FileTypeChoices.Add("Text File", new List<string>() { ".txt" });
            fp.SuggestedFileName = $"{now.ToString("yyyyMMddHHmmssff")}.txt";
            StorageFile TargetFile = await fp.PickSaveFileAsync();
            if (TargetFile != null)
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Add(TargetFile, TargetFile.Name);
                if (StorageApplicationPermissions.FutureAccessList.Entries.Count >= 1000)
                    StorageApplicationPermissions.FutureAccessList.Remove(StorageApplicationPermissions.FutureAccessList.Entries.Last().Token);
                StorageApplicationPermissions.FutureAccessList.Add(TargetFile, TargetFile.Name);

                // 在用户完成更改并调用CompleteUpdatesAsync之前，阻止对文件的更新
                CachedFileManager.DeferUpdates(TargetFile);
                await FileIO.WriteTextAsync(TargetFile, content);
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(TargetFile);

                result = TargetFile.Name;
            }
            return (result);
        }

        static public async Task<string> ShowSaveDialog(Image image, string prefix = "")
        {
            var bmp = await image.ToWriteableBitmmap();
            var width = bmp.PixelWidth;
            var height = bmp.PixelHeight;
            return (await ShowSaveDialog(image, width, height, prefix));
        }

        static public async Task<string> ShowSaveDialog(Image image, int size, string prefix = "")
        {
            return (await ShowSaveDialog(image, size, size, prefix));
        }

        static public async Task<string> ShowSaveDialog(Image image, int width, int height, string prefix = "")
        {
            if (image.Source == null) return (string.Empty);
            string result = string.Empty;

            var now = DateTime.Now;
            FileSavePicker fp = new FileSavePicker();
            fp.SuggestedStartLocation = PickerLocationId.Desktop;
            fp.FileTypeChoices.Add("Image File", new List<string>() { ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".gif", ".bmp" });
            if (!string.IsNullOrEmpty(prefix)) prefix = $"{prefix}_";
            fp.SuggestedFileName = $"{prefix}{now.ToString("yyyyMMddHHmmssff")}.png";
            StorageFile TargetFile = await fp.PickSaveFileAsync();
            if (TargetFile != null)
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Add(TargetFile, TargetFile.Name);
                if (StorageApplicationPermissions.FutureAccessList.Entries.Count >= 1000)
                    StorageApplicationPermissions.FutureAccessList.Remove(StorageApplicationPermissions.FutureAccessList.Entries.Last().Token);
                StorageApplicationPermissions.FutureAccessList.Add(TargetFile, TargetFile.Name);

                // 在用户完成更改并调用CompleteUpdatesAsync之前，阻止对文件的更新
                CachedFileManager.DeferUpdates(TargetFile);

                #region Save Image Control source data
                //using (var fileStream = await TargetFile.OpenAsync(FileAccessMode.ReadWrite))
                //{
                //    var bmp = imgQR.Source as WriteableBitmap;
                //    var w = bmp.PixelWidth;
                //    var h = bmp.PixelHeight;

                //    // set the source for WriteableBitmap  
                //    //await bmp.SetSourceAsync(fileStream);

                //    // Get pixels of the WriteableBitmap object 
                //    Stream pixelStream = bmp.PixelBuffer.AsStream();
                //    byte[] pixels = new byte[pixelStream.Length];
                //    await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                //    var encId = BitmapEncoder.PngEncoderId;
                //    var fext = Path.GetExtension(TargetFile.Name).ToLower();
                //    switch (fext)
                //    {
                //        case ".bmp":
                //            encId = BitmapEncoder.BmpEncoderId;
                //            break;
                //        case ".gif":
                //            encId = BitmapEncoder.GifEncoderId;
                //            break;
                //        case ".png":
                //            encId = BitmapEncoder.PngEncoderId;
                //            break;
                //        case ".jpg":
                //            encId = BitmapEncoder.JpegEncoderId;
                //            break;
                //        case ".jpeg":
                //            encId = BitmapEncoder.JpegEncoderId;
                //            break;
                //        case ".tif":
                //            encId = BitmapEncoder.TiffEncoderId;
                //            break;
                //        case ".tiff":
                //            encId = BitmapEncoder.TiffEncoderId;
                //            break;
                //        default:
                //            encId = BitmapEncoder.PngEncoderId;
                //            break;
                //    }
                //    var encoder = await BitmapEncoder.CreateAsync(encId, fileStream);
                //    // Save the image file with jpg extension 
                //    encoder.SetPixelData(
                //        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                //        //(uint)bmp.PixelWidth, (uint)bmp.PixelHeight, 
                //        (uint)size, (uint)size,
                //        96.0, 96.0, 
                //        pixels);
                //    await encoder.FlushAsync();
                //}
                //result = TargetFile.Name;
                #endregion

                #region Save Image control display with specified size
                //把控件变成图像
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                //传入参数Image控件
                await renderTargetBitmap.RenderAsync(image, width, height);
                var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                using (var fileStream = await TargetFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var r_width = renderTargetBitmap.PixelWidth;
                    var r_height = renderTargetBitmap.PixelHeight;
                    var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                    if (width > 0 && height > 0)
                    {
                        r_width = width;
                        r_height = height;
                    }
                    var encId = BitmapEncoder.PngEncoderId;
                    var fext = Path.GetExtension(TargetFile.Name).ToLower();
                    switch (fext)
                    {
                        case ".bmp":
                            encId = BitmapEncoder.BmpEncoderId;
                            break;
                        case ".gif":
                            encId = BitmapEncoder.GifEncoderId;
                            break;
                        case ".png":
                            encId = BitmapEncoder.PngEncoderId;
                            break;
                        case ".jpg":
                            encId = BitmapEncoder.JpegEncoderId;
                            break;
                        case ".jpeg":
                            encId = BitmapEncoder.JpegEncoderId;
                            break;
                        case ".tif":
                            encId = BitmapEncoder.TiffEncoderId;
                            break;
                        case ".tiff":
                            encId = BitmapEncoder.TiffEncoderId;
                            break;
                        default:
                            encId = BitmapEncoder.PngEncoderId;
                            break;
                    }
                    var encoder = await BitmapEncoder.CreateAsync(encId, fileStream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                        (uint)r_width, (uint)r_height, dpi, dpi,
                        pixelBuffer.ToArray()
                    );
                    //刷新图像
                    await encoder.FlushAsync();
                }
                result = TargetFile.Name;
                #endregion

                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(TargetFile);
            }
            return (result);
        }
        #endregion

    }
}
