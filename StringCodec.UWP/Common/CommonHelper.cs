using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace StringCodec.UWP.Common
{
    static public class WriteableImageExtetions
    {
        #region WriteableBitmmap Extensions
        static public WriteableBitmap GetWriteableBitmmap(Image image)
        {
            WriteableBitmap result = null;

            if (image.Source == null) return result;
            else if (image.Source is BitmapImage)
            {
                return (result);
            }
            else if (image.Source is WriteableBitmap)
            {
                return (image.Source as WriteableBitmap);
            }
            return (result);
        }

        static public async Task<StorageFile> StoreTemporaryFile(this WriteableBitmap image, IBuffer pixelBuffer, int width, int height)
        {
            var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
            var now = DateTime.Now;
            var fn = $"{now.ToString("yyyyMMddhhmmss")}.png";
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
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
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
        #endregion
    }

    class Utils
    {
        #region Share Extentions
        static private DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
        static private StorageFile _tempExportFile;
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
                    List<IStorageItem> imageItems = new List<IStorageItem> { _tempExportFile };
                    requestData.SetStorageItems(imageItems);

                    RandomAccessStreamReference imageStreamRef = RandomAccessStreamReference.CreateFromFile(_tempExportFile);
                    requestData.Properties.Thumbnail = imageStreamRef;
                    requestData.SetBitmap(imageStreamRef);
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "ERROR").ShowAsync();
            }
        }

        static public async Task<FileUpdateStatus> Share(WriteableBitmap image)
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
            #region Save image to a temporary file
            var now = DateTime.Now;
            var fn = $"{now.ToString("yyyyMMddhhmmss")}.png";
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
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                        (uint)image.PixelWidth,
                        (uint)image.PixelHeight,
                        96.0,
                        96.0,
                        pixels);
                    await encoder.FlushAsync();
                }

                status = await CachedFileManager.CompleteUpdatesAsync(tempFile);
                _tempExportFile = tempFile;

                DataTransferManager.ShowShareUI();
            }
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

            //Uri uri = new Uri("ms-appx:///Assets/ms.png");
            //StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            //dp.SetBitmap(RandomAccessStreamReference.CreateFromUri(uri));

            try
            {
                //把控件变成图像
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                //传入参数Image控件
                await renderTargetBitmap.RenderAsync(image, width, height);
                var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                var r_width = renderTargetBitmap.PixelWidth;
                var r_height = renderTargetBitmap.PixelHeight;
                if (width > 0 && height > 0)
                {
                    r_width = width;
                    r_height = height;
                }

                #region Create a temporary file for Clipboard Copy
                StorageFile tempFile = await (image.Source as WriteableBitmap).StoreTemporaryFile(pixelBuffer, r_width, r_height);

                ////dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(fileStream));
                //var now = DateTime.Now;
                //var fn = $"{now.ToString("yyyyMMddhhmmss")}.png";
                //StorageFile tempFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(fn, CreationCollisionOption.ReplaceExisting);
                if (tempFile != null)
                {
                //    CachedFileManager.DeferUpdates(tempFile);

                //    using (var fileStream = await tempFile.OpenAsync(FileAccessMode.ReadWrite))
                //    {
                //        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                //        Stream pixelStream = (image.Source as WriteableBitmap).PixelBuffer.AsStream();
                //        byte[] pixels = new byte[pixelStream.Length];
                //        await pixelStream.ReadAsync(pixels, 0, pixels.Length);
                //        encoder.SetPixelData(
                //            BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                //            (uint)r_width, (uint)r_height,
                //            dpi, dpi,
                //            pixelBuffer.ToArray());
                //        await encoder.FlushAsync();
                //    }

                //    var status = await CachedFileManager.CompleteUpdatesAsync(tempFile);
                //    //_tempExportFile = tempFile;
                    dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(tempFile));
                    //await tempFile.DeleteAsync();
                }
                #endregion

                #region Create other MIME format to Clipboard Copy
                //string[] fmts = new string[] { "CF_DIB", "CF_BITMAP", "BITMAP", "DeviceIndependentBitmap", "image/png", "image/bmp", "image/jpg", "image/jpeg" };
                string[] fmts = new string[] { "image/png", "image/bmp", "image/jpg", "image/jpeg" };

                foreach (var fmt in fmts)
                {
                    using (var fileStream = new InMemoryRandomAccessStream())
                    {
                        fileStream.Seek(0);

                        var encId = BitmapEncoder.PngEncoderId;
                        switch (fmt)
                        {
                            case "image/png":
                                encId = BitmapEncoder.PngEncoderId;
                                break;
                            case "image/jpg":
                                encId = BitmapEncoder.JpegEncoderId;
                                break;
                            case "image/jpeg":
                                encId = BitmapEncoder.JpegEncoderId;
                                break;
                            case "image/bmp":
                                encId = BitmapEncoder.BmpEncoderId;
                                break;
                            case "Bitmap":
                                encId = BitmapEncoder.BmpEncoderId;
                                break;
                            case "CF_DIB":
                                encId = BitmapEncoder.BmpEncoderId;
                                break;
                            case "CF_BITMAP":
                                encId = BitmapEncoder.BmpEncoderId;
                                break;
                            case "DeviceIndependentBitmap":
                                encId = BitmapEncoder.BmpEncoderId;
                                break;
                            default:
                                break;
                        }
                        var encoder = await BitmapEncoder.CreateAsync(encId, fileStream);
                        encoder.SetPixelData(
                            BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                            (uint)r_width, (uint)r_height,
                            dpi, dpi,
                            pixelBuffer.ToArray());
                        await encoder.FlushAsync();

                        //Stream stream = WindowsRuntimeStreamExtensions.AsStreamForRead(fileStream.GetInputStreamAt(0));
                        using (Stream stream = WindowsRuntimeStreamExtensions.AsStreamForRead(fileStream.GetInputStreamAt(0)))
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                await stream.CopyToAsync(ms);
                                byte[] arr = ms.ToArray();
                                if (fmt.Equals("CF_DIB", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    byte[] dib = arr.Skip(14).ToArray();
                                    dataPackage.SetData(fmt, dib);

                                    //var buffer = WindowsRuntimeBufferExtensions.AsBuffer(dib, 0, dib.Length);
                                    //InMemoryRandomAccessStream inStream = new InMemoryRandomAccessStream();
                                    //DataWriter datawriter = new DataWriter(inStream.GetOutputStreamAt(0));
                                    //datawriter.WriteBuffer(buffer, 0, buffer.Length);
                                    //await datawriter.StoreAsync();
                                    //dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(inStream));
                                }
                                else dataPackage.SetData(fmt, arr);
                            }
                        }
                    }
                }
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
                    if (dataPackageView.Contains("image/png"))
                    {
                        using (var fileStream = new InMemoryRandomAccessStream())
                        {
                            //var data = await dataPackageView.GetDataAsync("DeviceIndependentBitmapV5");
                            var data = await dataPackageView.GetDataAsync("image/png");
                            var dataObj = data as IRandomAccessStream;
                            var stream = dataObj.GetInputStreamAt(0);
                            //IBuffer buff = new Windows.Storage.Streams.Buffer((uint)dataObj.Size);
                            //await stream.ReadAsync(buff, (uint)dataObj.Size, InputStreamOptions.None);

                            var outputStream = fileStream.GetOutputStreamAt(0);
                            await RandomAccessStream.CopyAsync(stream, outputStream);

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
        static public async Task<Color> ShowColorDialog(Color color)
        {
            Color result = color;

            ColorDialog dlgColor = new ColorDialog() { Color = color, Alpha = false };
            ContentDialogResult ret = await dlgColor.ShowAsync();
            if (ret == ContentDialogResult.Primary)
            {
                result = dlgColor.Color;
            }

            return (result);
        }

        static public async Task<string> ShowSaveDialog(Image image, string prefix = "")
        {
            var bmp = image.Source as WriteableBitmap;
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

            var now = DateTime.Now;
            FileSavePicker fp = new FileSavePicker();
            fp.SuggestedStartLocation = PickerLocationId.Desktop;
            fp.FileTypeChoices.Add("Image File", new List<string>() { ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".gif", ".bmp" });
            if (!string.IsNullOrEmpty(prefix)) prefix = $"{prefix}_";
            fp.SuggestedFileName = $"{prefix}{now.ToString("yyyyMMddhhmmss")}.png";
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
                //        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                //        //(uint)bmp.PixelWidth, (uint)bmp.PixelHeight, 
                //        (uint)size, (uint)size,
                //        96.0, 96.0, 
                //        pixels);
                //    await encoder.FlushAsync();
                //}
                //return (TargetFile.Name);
                #endregion

                #region Save Image Control Screen Display Data
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
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                        (uint)r_width, (uint)r_height, dpi, dpi,
                        pixelBuffer.ToArray()
                    );
                    //刷新图像
                    await encoder.FlushAsync();
                }
                return (TargetFile.Name);
                #endregion
            }
            return (string.Empty);
        }

        static public async Task<string> ShowSaveDialog(string content)
        {
            if (content.Length <= 0) return (string.Empty);

            var now = DateTime.Now;
            FileSavePicker fp = new FileSavePicker();
            fp.SuggestedStartLocation = PickerLocationId.Desktop;
            fp.FileTypeChoices.Add("Text File", new List<string>() { ".txt" });
            fp.SuggestedFileName = $"{now.ToString("yyyyMMddhhmmss")}.txt";
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

                return (TargetFile.Name);
            }
            return (string.Empty);
        }

        #endregion

    }
}
