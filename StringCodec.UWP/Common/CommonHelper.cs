﻿using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Svg;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.WiFi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.System.UserProfile;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace StringCodec.UWP.Common
{
    #region SVG File Extensions
    public class IMAGE
    {
        private byte[] bytes = null;
        public byte[] Bytes
        {
            get { return (bytes); }
            set { bytes = value; }
        }

        private ImageSource source = null;
        public ImageSource Source
        {
            get { return (source); }
            set { source = value; }
        }
    }

    public class WriteableBitmapObject
    {
        public WriteableBitmap Image { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class SVG
    {
        private byte[] bytes = null;
        public byte[] Bytes
        {
            get { return (bytes); }
            set { bytes = value; }
        }

        private SvgImageSource source = null;
        public SvgImageSource Source
        {
            get { return (source); }
            set { source = value; }
        }

        private ImageSource imagesource = null;
        public ImageSource Image
        {
            get { return (imagesource); }
            set { imagesource = value; }
        }

        public KeyValuePair<byte[], SvgImageSource> Data
        {
            get
            {
                return (new KeyValuePair<byte[], SvgImageSource>(Bytes, Source));
            }
            set
            {
                Bytes = value.Key;
                Source = value.Value;
            }
        }

        private static string FixStyle(string content, bool stretch = true)
        {
            string xmlsrc = content;

            string xmlHeader = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
            if (!xmlsrc.StartsWith(xmlHeader, StringComparison.CurrentCultureIgnoreCase))
            {
                string patternSvg = @"<svg.*?\n*\r*.*?>(\n*\r*.*?)*</svg>";
                string nodeSvg = Regex.Match(xmlsrc, patternSvg, RegexOptions.IgnoreCase | RegexOptions.Multiline).Value;
                xmlsrc = $"{xmlHeader}{nodeSvg}";
            }
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(xmlsrc);

            Dictionary<string, Dictionary<string, string>> attrs = new Dictionary<string, Dictionary<string, string>>();
            var styles = xml.GetElementsByTagName("style");
            foreach (XmlNode style in styles)
            {
                if (style.HasChildNodes)
                {
                    var values = style.FirstChild.Value.Trim();
                    if (string.IsNullOrEmpty(values)) continue;
                    //var s = values.Replace("\n", "").Replace("\r", "").Replace("\t", "");
                    var s = Regex.Replace(values, @"[\n\r\t]", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    s = Regex.Replace(s, @"(\ *)([:;\{\}])(\ *)", "$2", RegexOptions.IgnoreCase);
                    var mo = Regex.Matches(s, @"\.([\w_-]+)\{(.*?)\}", RegexOptions.IgnoreCase);
                    foreach (Match m in mo)
                    {
                        var k = m.Groups[1].Value.Trim();
                        var vmo = Regex.Matches(m.Groups[2].Value.Trim(), @"((\w+):([0-9a-zA-Z#]+)\;)+?", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                        var vl = new Dictionary<string, string>();
                        foreach (Match vm in vmo)
                        {
                            vl.Add(vm.Groups[2].Value.Trim(), vm.Groups[3].Value.Trim());
                        }
                        attrs.Add(k, vl);
                    }
                }
            }
            //for(int i = 0; i < styles.Count; i++)
            //    styles[i].ParentNode.RemoveChild(styles[i]);

            int w = 1;
            int h = 1;
            var childs = xml.GetElementsByTagName("*");
            foreach (XmlNode child in childs)
            {
                if (child.LocalName.Equals("svg", StringComparison.CurrentCultureIgnoreCase))
                {
                    var vs = child.Attributes.GetNamedItem("style");
                    var vv = child.Attributes.GetNamedItem("version");
                    var vb = child.Attributes.GetNamedItem("viewBox");
                    var vw = child.Attributes.GetNamedItem("width");
                    var vh = child.Attributes.GetNamedItem("height");
                    var va = child.Attributes.GetNamedItem("preserveAspectRatio");

                    if (vv == null)
                    {
                        var a = xml.CreateAttribute("version");
                        a.Value = $"1.1";
                        child.Attributes.InsertBefore(a, child.Attributes[0]);// Append(a);
                    }

                    if (vw != null && stretch)
                    {
                        var v = (XmlAttribute)vw;
                        int.TryParse(v.Value.Trim(), out w);
                        w = w == 0 ? 1 : w;
                        //w = Convert.ToInt32(v.Value.Trim());
                        //if(vb == null) v.Value = "auto";
                        v.Value = "auto";
                    }

                    if (vh != null && stretch)
                    {
                        var v = (XmlAttribute)vh;
                        int.TryParse(v.Value.Trim(), out h);
                        h = h == 0 ? 1 : h;
                        //h = Convert.ToInt32(v.Value.Trim());
                        //if (vb == null) v.Value = "auto";
                        v.Value = "auto";
                    }

                    if (vb == null)
                    {
                        var a = xml.CreateAttribute("viewBox");
                        a.Value = $"0 0 {w} {h}";
                        child.Attributes.Append(a);
                    }

                    if (va == null)
                    {
                        var a = xml.CreateAttribute("preserveAspectRatio");
                        a.Value = $"xMinYMin meet";
                        child.Attributes.Append(a);
                    }

                    //if(vs == null)
                    //{
                    //    var a = xml.CreateAttribute("style");
                    //    a.Value = $"background: transparent;";
                    //    child.Attributes.Append(a);
                    //}
                    continue;
                }

                var attrClass = (XmlAttribute)child.Attributes.GetNamedItem("class");
                if (attrClass != null)
                {
                    var v = attrClass.Value.Trim();
                    if (attrs.ContainsKey(v))
                    {
                        foreach (var kv in attrs[v])
                        {
                            var a = xml.CreateAttribute(kv.Key);
                            a.Value = kv.Value;
                            child.Attributes.InsertAfter(a, attrClass);
                            //child.Attributes.Append(a);
                        }
                    }
                    child.Attributes.Remove(attrClass);
                }
            }

            using (var stringWriter = new StringWriter())
            {
                using (var xmlTextWriter = XmlWriter.Create(stringWriter))
                {
                    xml.WriteTo(xmlTextWriter);
                    xmlTextWriter.Flush();
                    xmlsrc = stringWriter.GetStringBuilder().ToString();
                }
            }
            return (xmlsrc);
        }

        private static byte[] FixStyle(byte[] bytes, bool stretch=true)
        {
            var svgDoc = Encoding.UTF8.GetString(bytes);
            var xmlsrc = FixStyle(svgDoc, stretch);
            var arr = Encoding.UTF8.GetBytes(xmlsrc);
            return (arr);
        }

        public async Task<string> ToBase64(bool LineBreak)
        {
            string b64 = string.Empty;

            var lb = LineBreak ? Base64FormattingOptions.InsertLineBreaks : Base64FormattingOptions.None;
            var svgPrefix = $"data:image/svg+xml;base64,";

            if (bytes is byte[])
            {
                b64 = $"{svgPrefix}{Convert.ToBase64String(bytes, lb)}";
            }
            else if (source.UriSource != null)// && !string.IsNullOrEmpty(svg.UriSource.AbsoluteUri))
            {
                var svgRef = RandomAccessStreamReference.CreateFromUri(source.UriSource);
                var rms = await svgRef.OpenReadAsync();
                await rms.FlushAsync();

                MemoryStream ms = new MemoryStream();
                rms.Seek(0);
                await rms.AsStreamForRead().CopyToAsync(ms);
                byte[] bytes = ms.ToArray();
                await ms.FlushAsync();
                b64 = $"{svgPrefix}{Convert.ToBase64String(bytes, lb)}";
            }
            return (b64);
        }

        public async Task<SVG> Load(string svgXml)
        {
            var arr = Encoding.UTF8.GetBytes(svgXml);
            return(await Load(arr));
        }

        public async Task<SVG> Load(byte[] svgBytes)
        {
            var result = await CreateFromBytes(svgBytes);
            bytes = result.Bytes;
            source = result.Source;
            //if(source != null) { source.RasterizePixelWidth = 512; source.RasterizePixelHeight = 512; }
            return (result);
        }

        public async Task<SVG> Load(IRandomAccessStream svgStream)
        {
            var result = await CreateFromStream(svgStream);
            bytes = result.Bytes;
            source = result.Source;
            return (result);
        }

        public async Task<SVG> Load(StorageFile svgFile)
        {
            var result = await CreateFromStorageFile(svgFile);
            bytes = result.Bytes;
            source = result.Source;
            return (result);
        }

        public static async Task<SVG> CreateFromXml(string svgXml, bool stretch = true)
        {
            var arr = Encoding.UTF8.GetBytes(svgXml);
            return (await CreateFromBytes(arr, stretch));
        }

        public static async Task<SVG> CreateFromBytes(byte[] svgBytes, bool stretch = true)
        {
            SVG result = new SVG
            {
                Bytes = FixStyle(svgBytes, stretch)
            };

            try
            {
                using (MemoryStream ms = new MemoryStream(result.Bytes))
                {
                    using (var rms = new InMemoryRandomAccessStream())
                    {
                        await RandomAccessStream.CopyAsync(ms.AsInputStream(), rms.GetOutputStreamAt(0));
                        var bitmapImage = new SvgImageSource();
                        await bitmapImage.SetSourceAsync(rms);
                        await rms.FlushAsync();
                        result.Source = bitmapImage;
                        result.Image = bitmapImage;
                    }
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        public static async Task<SVG> CreateFromStream(IRandomAccessStream svgStream, bool stretch = true)
        {
            SVG result = new SVG();
            using (var byteStream = WindowsRuntimeStreamExtensions.AsStreamForRead(svgStream.GetInputStreamAt(0)))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    await byteStream.CopyToAsync(ms);
                    result = await CreateFromBytes(ms.ToArray(), stretch);
                }
            }
            return (result);
        }

        public static async Task<SVG> CreateFromStorageFile(StorageFile svgFile, bool stretch = true)
        {
            SVG result = new SVG();
            if (Utils.image_ext.Contains(svgFile.FileType.ToLower()))
            {
                if (svgFile.FileType.ToLower().Equals(".svg"))
                {
                    byte[] bytes = WindowsRuntimeBufferExtensions.ToArray(await FileIO.ReadBufferAsync(svgFile));
                    result = await CreateFromBytes(bytes, stretch);
                }
            }
            return (result);
        }
    }

    public static class SvgExts
    {
        public static async Task<string> ToBase64(this SvgImageSource svg, byte[] bytes, bool LineBreak=false)
        {
            var lb = LineBreak ? Base64FormattingOptions.InsertLineBreaks : Base64FormattingOptions.None;
            var svgPrefix = $"data:image/svg+xml;base64,";
            string b64 = string.Empty;
            if (bytes is byte[])
            {
                b64 = $"{svgPrefix}{Convert.ToBase64String(bytes, lb)}";
            }
            else if (svg.UriSource != null)
            {
                var svgRef = RandomAccessStreamReference.CreateFromUri(svg.UriSource);
                var rms = await svgRef.OpenReadAsync();
                await rms.FlushAsync();

                MemoryStream ms = new MemoryStream();
                rms.Seek(0);
                await rms.AsStreamForRead().CopyToAsync(ms);
                byte[] arr = ms.ToArray();
                await ms.FlushAsync();
                b64 = $"{svgPrefix}{Convert.ToBase64String(arr, lb)}";
            }
            else
            {
                //var parent = VisualTreeHelper.GetParent(svg);
                //if(parent is Image)
                //{
                //    var wb = await (parent as Image).ToWriteableBitmap();
                //    if (wb != null)
                //    {
                //        b64 = await wb.ToBase64(".png", true, true);
                //    }
                //}
            }
            return (b64);
        }

        public static async Task<string> ToHTML(this SvgImageSource svg, byte[] bytes, string alt="")
        {
            var b64 = await svg.ToBase64(bytes, true);
            return($"<img src=\"{b64}\" alt=\"{alt}\" />");
        }

        public static SVG ToSVG(this Image image)
        {
            SVG result = new SVG();
            if (image.Source is SvgImageSource)
            {
                if (image.Tag is byte[])
                {
                    result.Bytes = image.Tag as byte[];
                }
                result.Source = image.Source as SvgImageSource;
            }
            return (result);
        }
    }
    #endregion

    #region XAML UI/ICON Extensions
    public static class XAMLExtentions
    {
        public static UIElement LoadXAML(this string xaml)
        {
            UIElement result = null;

            var dpo = XamlReader.Load(xaml) as DependencyObject;
            if (dpo is Viewbox)
            {

            }
            else if (dpo is UIElement)
            {
                result = dpo as UIElement;
            }

            return (result);
        }
    }
    #endregion

    public static class StreamExtentions
    {
        #region Stream / RandomAccessStream / byte[] Converter
        public static byte[] ToBytes(this Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public static async Task<byte[]> ToBytes(this IRandomAccessStream RandomStream)
        {
            Stream stream = WindowsRuntimeStreamExtensions.AsStreamForRead(RandomStream.GetInputStreamAt(0));
            MemoryStream ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            //await ms.FlushAsync();
            byte[] bytes = ms.ToArray();
            return (bytes);
        }

        public static Stream ToStream(this byte[] bytes)
        {
            Stream stream = new MemoryStream(bytes);
            return stream;
        }

        public static IBuffer ToBuffer(this byte[] bytes)
        {
            return (WindowsRuntimeBufferExtensions.AsBuffer(bytes, 0, bytes.Length));
        }

        public static async Task<IRandomAccessStream> ToRandomAccessStream(this IBuffer buffer)
        {
            InMemoryRandomAccessStream inStream = new InMemoryRandomAccessStream();
            using (DataWriter datawriter = new DataWriter(inStream.GetOutputStreamAt(0)))
            {
                datawriter.WriteBuffer(buffer, 0, buffer.Length);
                await datawriter.StoreAsync();
            }
            return (inStream);
        }

        public static async Task<IRandomAccessStream> ToRandomAccessStream(this byte[] bytes)
        {
            return (await bytes.ToBuffer().ToRandomAccessStream());
        }

        public static MemoryStream ToMemoryStream(this Stream stream)
        {
            MemoryStream result = null;
            if (stream != null)
            {
                byte[] buffer = stream.ToBytes();
                result = new MemoryStream(buffer);
            }
            return (result);
        }
        #endregion
    }

    public static class WriteableBitmapExtentions
    {
        #region Color Extentions
        public static string ToHTML(this Color color)
        {
            return ($"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}");
        }

        public static string ToCssRGBA(this Color color)
        {
            return ($"rgba({color.R},{color.G},{color.B},{color.A/255.0:F2})");
        }
        #endregion

        #region FrameworkElement UIElement to WriteableBitmap
        public static async Task<WriteableBitmap> ToWriteableBitmap(this FrameworkElement element)
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

        public static async Task<WriteableBitmap> ToWriteableBitmap(this FrameworkElement element, double scale)
        {
            var w = (int)Math.Ceiling(element.ActualWidth * scale);
            var h = (int)Math.Ceiling(element.ActualHeight * scale);

            return (await element.ToWriteableBitmap(w, h));
        }

        public static async Task<WriteableBitmap> ToWriteableBitmap(this FrameworkElement element, int size)
        {
            double aspect = element.ActualWidth / element.ActualHeight;

            var w = size;
            var h = (int)Math.Ceiling(size / aspect);
            return (await element.ToWriteableBitmap(w, h));
        }

        public static async Task<WriteableBitmap> ToWriteableBitmap(this FrameworkElement element, Size size)
        {
            return (await element.ToWriteableBitmap((int)(size.Width), (int)(size.Height)));
        }

        public static async Task<WriteableBitmap> ToWriteableBitmap(this FrameworkElement element, int width, int height)
        {
            WriteableBitmap result = null;
            using (var fileStream = new InMemoryRandomAccessStream())
            {
                var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;

                RenderTargetBitmap rtb = new RenderTargetBitmap();
                await rtb.RenderAsync(element, width, height);
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

        public static async Task<WriteableBitmap> ToWriteableBitmap(this FrameworkElement element, Color bgcolor)
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

        public static async Task<WriteableBitmap> ToWriteableBitmap(this FrameworkElement element, int width, int height, Color bgcolor)
        {
            WriteableBitmap result = null;
            using (var fileStream = new InMemoryRandomAccessStream())
            {
                var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;

                RenderTargetBitmap rtb = new RenderTargetBitmap();
                await rtb.RenderAsync(element, width, height);
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
        public static async Task<WriteableBitmap> ToWriteableBitmap(this string text, Panel root, string fontfamily, int fontsize, Color fgcolor, Color bgcolor)
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
            var wb = await border.ToWriteableBitmap();
            root.Children.Remove(border);
            #endregion

            return (result);
        }

        private static Rect CalcRect(this string text, string fontfamily, FontStyle fontstyle, int fontsize, bool compact)
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

        public static async Task<WriteableBitmap> ToWriteableBitmap(this string text, string fontfamily, FontStyle fontstyle, int fontsize, Color fgcolor, Color bgcolor)
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

        #region Create WriteableBitmap wiht Color 
        public static WriteableBitmap ToWriteableBitmap(this Color bgcolor, int width, int height)
        {
            if (width <= 0 || height <= 0) return (null);
            WriteableBitmap result = new WriteableBitmap(width, height);
            result.FillRectangle(0, 0, width, height, bgcolor);
            return (result);
        }
        #endregion

        #region DrawText to WriteableBitmap
        public static async void DrawText(this WriteableBitmap image, int x, int y, string text, string fontfamily, FontStyle fontstyle, int fontsize, Color fgcolor, Color bgcolor)
        {
            var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;

            var twb = await text.ToWriteableBitmap(fontfamily, fontstyle, fontsize, fgcolor, bgcolor);
            image.Blit(new Rect(x, y, twb.PixelWidth, twb.PixelHeight), twb, new Rect(0, 0, twb.PixelWidth, twb.PixelHeight));
            //image.BlitRender(twb, false);
            return;
        }

        public static async void DrawText(this UIElement target, int x, int y, string text, string fontfamily, int fontsize, Color fgcolor, Color bgcolor)
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

        public static async void DrawText(this WriteableBitmap image, UIElement target, int x, int y, string text, string fontfamily, int fontsize, Color fgcolor, Color bgcolor)
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
        public static WriteableBitmap Extend(this WriteableBitmap image, double percent, Color bgcolor)
        {
            return (image.Extend(percent, percent, percent, percent, bgcolor));
        }

        public static WriteableBitmap Extend(this WriteableBitmap image, double percent_left, double percent_top, double percent_right, double percent_bottom, Color bgcolor)
        {
            var iw = image.PixelWidth;
            var ih = image.PixelHeight;
            int left = (int)Math.Ceiling(iw * percent_left);
            int top = (int)Math.Ceiling(ih * percent_top);
            int right = (int)Math.Ceiling(iw * percent_right);
            int bottom = (int)Math.Ceiling(ih * percent_bottom);
            return (image.Extend(left, top, right, bottom, bgcolor));
        }

        public static WriteableBitmap Extend(this WriteableBitmap image, int size, Color bgcolor)
        {
            return(image.Extend(size, size, size, size, bgcolor));
        }

        public static WriteableBitmap Extend(this WriteableBitmap image, int size_left, int size_top, int size_right, int size_bottom, Color bgcolor)
        {
            WriteableBitmap result = null;

            result = new WriteableBitmap(image.PixelWidth + size_left + size_right, image.PixelHeight + size_top + size_bottom);
            result.FillRectangle(0, 0, result.PixelWidth, result.PixelHeight, bgcolor);
            result.Blit(new Rect(size_left, size_top, image.PixelWidth, image.PixelHeight), image, new Rect(0, 0, image.PixelWidth, image.PixelHeight));

            return (result);
        }

        public static Rect Boundary(this WriteableBitmap image, int space=0)
        {
            return (image.Boundary(image.GetPixel(0, 0), image.GetPixel(image.PixelWidth - 1, image.PixelHeight - 1), space));
        }

        public static Rect Boundary(this WriteableBitmap image, Color color, int space = 0)
        {
            return(image.Boundary(color, color, space));
        }

        public static Rect Boundary(this WriteableBitmap image, Color color_0, Color color_1, int space = 0)
        {
            int w = image.PixelWidth;
            int h = image.PixelHeight;

            byte[] arr = image.PixelBuffer.ToArray();

            int c = arr.Length - 1;
            uint co0 = (uint)(color_0.B) + ((uint)(color_0.G) << 8) + ((uint)(color_0.R) << 16) + ((uint)(color_0.A) << 24);
            uint co1 = (uint)(color_1.B) + ((uint)(color_1.G) << 8) + ((uint)(color_1.R) << 16) + ((uint)(color_1.A) << 24);

            #region Pixels to Matrix
            uint[,] matC = new uint[w, h];
            int i = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    matC[x, y] = (uint)(arr[i]) + ((uint)(arr[i + 1]) << 8) + ((uint)(arr[i + 2]) << 16) + ((uint)(arr[i + 3]) << 24);
                    i += 4;
                }
            }
            #endregion

            #region Finding left boundary
            int l = 0;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (matC[x, y] != co0)
                    {
                        l = x;
                        break;
                    }
                }
                if (l > 0) break;
            }
            #endregion

            #region Finding right boundary
            int r = w - 1;
            for (int x = w - 1; x >= 0; x--)
            {
                for (int y = 0; y < h; y++)
                {
                    if (matC[x, y] != co1)
                    {
                        r = x;
                        break;
                    }
                }
                if (r < w - 1) break;
            }
            #endregion

            #region Finding top boundary
            int t = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = l; x <= r; x++)
                {
                    if (matC[x, y] != co0)
                    {
                        t = y;
                        break;
                    }
                }
                if (t > 0) break;
            }
            #endregion

            #region Finding bottom boundary
            int b = h - 1;
            for (int y = h - 1; y >= 0; y--)
            {
                for (int x = l; x <= r; x++)
                {
                    if (matC[x, y] != co1)
                    {
                        b = y;
                        break;
                    }
                }
                if (b < h - 1) break;
            }
            #endregion

            l = Math.Max(l - space, 0);
            r = Math.Min(r + space, w - 1);
            t = Math.Max(t - space, 0);
            b = Math.Min(b + space, h - 1);

            return (new Rect(l, t, r - l + 1, b - t + 1));
        }

        public static WriteableBitmap Crop(this WriteableBitmap image, int space = 0)
        {
            var rect = image.Boundary(space);
            var result = image.Crop(rect);
            return (result);
        }

        public static WriteableBitmap Crop(this WriteableBitmap image, double space)
        {
            var tol = Math.Max(0, Math.Min(1.0, space));
            var sp = Math.Ceiling(Math.Min(image.PixelWidth * tol, image.PixelHeight * tol));

            var result = image.Crop(sp);
            return (result);
        }
        #endregion

        #region WriteableBitmmap Converter
        private static ImageSource _UnknownImage = new BitmapImage(new Uri("ms-appx:///Assets/unknown_file.png"));

        public static ImageSource UnknownFile()
        {
            if(!(_UnknownImage is ImageSource))
                _UnknownImage = new BitmapImage(new Uri("ms-appx:///Assets/unknown_file.png"));
            return (_UnknownImage);
        }

        public static ImageSource UnknownFile(this StorageFile file)
        {
            if (!(_UnknownImage is ImageSource))
                _UnknownImage = new BitmapImage(new Uri("ms-appx:///Assets/unknown_file.png"));
            return (_UnknownImage);
        }

        public static byte[] ToBytes(this WriteableBitmap image)
        {
            if (image == null) return (null);

            byte[] result = image.PixelBuffer.ToArray();
            return (result);
        }

        public static async Task<byte[]> ToBytes(this WriteableBitmap image, string fmt)
        {
            byte[] result = null;

            var ms = await image.ToRandomAccessStream(fmt);
            result = await ms.ToBytes();

            return (result);
        }

        public static async Task<string> ToBase64String(this WriteableBitmap image, string fmt, bool prefix, bool linebreak)
        {
            var b64 = await image.ToBase64(fmt, prefix, linebreak);
            return (b64);
        }

        public static async Task<string> ToBase64String(this Image image, string fmt, bool prefix, bool linebreak)
        {
            return(await (await image.ToWriteableBitmap()).ToBase64(fmt, prefix, linebreak));
        }

        public static async Task<string> ToHTML(this WriteableBitmap image, string alt="", string fmt=".png")
        {
            var b64 = await image.ToBase64(fmt, true, true);
            if(string.IsNullOrEmpty(alt))
                return ($"<img src=\"{b64.Trim()}\" />");
            else
                return ($"<img src=\"{b64.Trim()}\" alt=\"{await alt.Trim().Encoder(TextCodecs.CODEC.HTML)}\" />");
        }

        public static async Task<string> ToHTML(this Image image, string alt = "", string fmt = ".png")
        {
            var wb = await image.ToWriteableBitmap();
            return (await wb.ToHTML(alt, fmt));
        }

        public static async Task<WriteableBitmap> ToWriteableBitmap(this ImageSource Source, byte[] Bytes=null)
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
                        session.DrawSvg(svgDocument, new Size(svg.RasterizePixelWidth, svg.RasterizePixelHeight), 0, 0);
                        using (var imras = new InMemoryRandomAccessStream())
                        {
                            await offscreen.SaveAsync(imras, CanvasBitmapFileFormat.Png);
                            result = await imras.ToWriteableBitmap();
                        }
                    }
                }
            }
            return (result);
        }

        public static async Task<WriteableBitmap> ToWriteableBitmap(this StorageFile file)
        {
            var image = new WriteableBitmap(1, 1);
            await image.SetSourceAsync(await file.OpenReadAsync());
            byte[] arr = WindowsRuntimeBufferExtensions.ToArray(image.PixelBuffer, 0, (int)image.PixelBuffer.Length);
            return (image);
        }

        public static async Task<WriteableBitmap> ToWriteableBitmap(this IRandomAccessStream stream)
        {
            var image = new WriteableBitmap(1, 1);
            await image.SetSourceAsync(stream);
            byte[] arr = WindowsRuntimeBufferExtensions.ToArray(image.PixelBuffer, 0, (int)image.PixelBuffer.Length);
            return (image);
        }

        public static async Task<WriteableBitmap> ToWriteableBitmap(this BitmapImage image)
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
            else
            {
                //var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                //CanvasDevice device = CanvasDevice.GetSharedDevice();

                //using (var offscreen = new CanvasRenderTarget(device, (float)image.PixelWidth, (float)image.PixelHeight, dpi))
                //{
                //    using (CanvasDrawingSession ds = offscreen.CreateDrawingSession())
                //    {
                //        //ds.Clear(Colors.Black);
                //        //ds.DrawRectangle(100, 200, 5, 6, Colors.Red);
                //        ds.Clear(Colors.Transparent);
                //        //ds.DrawImage(CanvasBitmap.)
                //    }

                //    var session = offscreen.CreateDrawingSession();
                //    //session.DrawSvg(svgDocument, new Size(image.PixelWidth, image.PixelHeight));

                //    InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
                //    var encId = BitmapEncoder.PngEncoderId;
                //    var encoder = await BitmapEncoder.CreateAsync(encId, stream);
                //    encoder.SetPixelData(
                //        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                //        (uint)image.PixelWidth, (uint)image.PixelHeight,
                //        dpi, dpi,
                //        image.PixelBuffer.ToArray());
                //    await encoder.FlushAsync();

                //    await image.SetSourceAsync(stream);
                //    byte[] arr = await stream.ToBytes();


                //    CanvasBitmap bitmap = await CanvasBitmap.LoadAsync(device, stream);
                //    session.DrawImage(bitmap, 0, 0);                    
                //    //bitmap.sav
                //}
            }
            return (result);
        }

        public static async Task<WriteableBitmap> ToWriteableBitmap(this Image image)
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
            else if (image.Source is SvgImageSource)
            {
                var svg = image.Source as SvgImageSource;
                var width = (int)svg.RasterizePixelWidth;
                var height = (int)svg.RasterizePixelHeight;
                var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;

                if(width <=0 || height <= 0)
                {
                    width = (int)image.ActualWidth;
                    height = (int)image.ActualHeight;
                }

                if (svg.UriSource != null)
                {
                    result = new WriteableBitmap(width, height);
                    var imgRef = RandomAccessStreamReference.CreateFromUri(svg.UriSource);
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

                //var sharedDevice = CanvasDevice.GetSharedDevice();
                //var svgDocument = new CanvasSvgDocument(sharedDevice);
                //if (image.Tag is byte[])
                //    svgDocument = CanvasSvgDocument.LoadFromXml(sharedDevice, (image.Tag as byte[]).ToString());

                //using (var offscreen = new CanvasRenderTarget(sharedDevice, (float)svg.RasterizePixelWidth, (float)svg.RasterizePixelHeight, 96))
                //{
                //    //svg.
                //}
            }
            return (result);
        }

        public static async Task<BitmapImage> ToBitmapImage(this WriteableBitmap image)
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

        public static async Task<StorageFile> StoreTemporaryFile(this WriteableBitmap image, string prefix="", string suffix = "")
        {
            return(await image.StoreTemporaryFile(image.PixelBuffer, image.PixelWidth, image.PixelHeight, prefix, suffix));
        }

        public static async Task<StorageFile> StoreTemporaryFile(this WriteableBitmap image, int width, int height, string prefix = "", string suffix = "")
        {
            return (await image.StoreTemporaryFile(image.PixelBuffer, width, height, prefix, suffix));
        }

        public static async Task<StorageFile> StoreTemporaryFile(this WriteableBitmap image, IBuffer pixelBuffer, int width, int height, string prefix="", string suffix="")
        {
            var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
            var now = DateTime.Now;
            if (!string.IsNullOrEmpty(prefix)) prefix = $"{prefix}_";
            if (!string.IsNullOrEmpty(suffix)) suffix = $"_{suffix}";
            var fn = $"{prefix}{now.ToString("yyyyMMddHHmmssff")}{suffix}.png";
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

        public static async void SaveAsync(this WriteableBitmap image, StorageFile storageitem)
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

        public static void SaveAsync(this WriteableBitmap image, StorageFile storageitem, int width, int height)
        {
            if (width > 0 && height > 0)
            {
                double iw = image.PixelWidth;
                double ih = image.PixelHeight;
                double factor = ih / iw;
                if (iw != ih)
                {
                    height = (int)(width * factor);
                }
                var wb = image.Resize(width, height, WriteableBitmapExtensions.Interpolation.Bilinear);
                wb.SaveAsync(storageitem);
            }
            else
            {
                image.SaveAsync(storageitem);
            }
        }

        public static async Task<IRandomAccessStream> ToRandomAccessStream(this WriteableBitmap image, string fmt = "")
        {
            if (string.IsNullOrEmpty(fmt))
            {
                var bytes = image.PixelBuffer.ToArray();
                var imras = await bytes.ToRandomAccessStream();
                return (imras);
            }
            else
            {
                return (await image.ToRandomAccessStream(image.PixelWidth, image.PixelHeight, fmt));
            }
        }

        public static async Task<IRandomAccessStream> ToRandomAccessStream(this WriteableBitmap image, int width, int height, string fmt = "")
        {
            InMemoryRandomAccessStream result = new InMemoryRandomAccessStream();

            var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
            var encId = BitmapEncoder.PngEncoderId;
            switch (fmt)
            {
                case "image/bmp":
                case "image/bitmap":
                case "CF_BITMAP":
                case "CF_DIB":
                case ".bmp":
                    encId = BitmapEncoder.BmpEncoderId;
                    break;
                case "image/gif":
                case "gif":
                case ".gif":
                    encId = BitmapEncoder.GifEncoderId;
                    break;
                case "image/png":
                case "png":
                case ".png":
                    encId = BitmapEncoder.PngEncoderId;
                    break;
                case "image/jpg":
                case ".jpg":
                    encId = BitmapEncoder.JpegEncoderId;
                    break;
                case "image/jpeg":
                case ".jpeg":
                    encId = BitmapEncoder.JpegEncoderId;
                    break;
                case "image/tif":
                case ".tif":
                    encId = BitmapEncoder.TiffEncoderId;
                    break;
                case "image/tiff":
                case ".tiff":
                    encId = BitmapEncoder.TiffEncoderId;
                    break;
                default:
                    encId = BitmapEncoder.PngEncoderId;
                    break;
            }
            var encoder = await BitmapEncoder.CreateAsync(encId, result);
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                (uint)width, (uint)height,
                dpi, dpi,
                image.PixelBuffer.ToArray());
            await encoder.FlushAsync();

            return (result);
        }
        #endregion
    }

    public enum DialogResult { YES=1, NO=2, CANCEL=3};
    public static class TextExtentions
    {
        #region I18N
        public static string _(this string text)
        {
            return (AppResources.GetString(text).Replace("\\n", "\n").Replace("\\r", "\r"));
        }

        public static string T(this string text)
        {
            return (AppResources.GetString(text).Replace("\\n", "\n").Replace("\\r", "\r"));
        }

        public static string GetString(this string text)
        {
            return (AppResources.GetString(text).Replace("\\n", "\n").Replace("\\r", "\r"));
        }

        public static string GetText(this string text)
        {
            return (AppResources.GetString(text).Replace("\\n", "\n").Replace("\\r", "\r"));
        }
        #endregion

        public static async void ShowMessage(this string content, string title)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(title)) return;
            await new MessageDialog(content, title).ShowAsync();
        }

        public static async Task<DialogResult> ShowConfirm(this string content, string title)
        {
            DialogResult result = DialogResult.CANCEL;

            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(title)) return(result);

            var dlg = new MessageDialog(content, title);
            dlg.Commands.Add(new UICommand { Label = "Yes".T(), Id = DialogResult.YES });
            dlg.Commands.Add(new UICommand { Label = "No".T(), Id = DialogResult.NO });

            var selected = await dlg.ShowAsync();

            //Enum.TryParse(typeof(DialogResult), (int)selected?.Id, result);
            result = (DialogResult)((int)selected?.Id);

            return (result);
        }

    }

    class Settings
    {
        private static PropertySet AppSetting = new PropertySet();
        #region Local Setting Helper
        public static dynamic Get(string key, dynamic value = null)
        {
            dynamic result = value;

            try
            {
                if (AppSetting.ContainsKey(key) && AppSetting[key] != null)
                    result = AppSetting[key];
                else if (ApplicationData.Current.LocalSettings.Values.ContainsKey(key) && ApplicationData.Current.LocalSettings.Values[key] != null)
                    result = ApplicationData.Current.LocalSettings.Values[key];
                else
                {
                    if (value != null)
                    {
                        ApplicationData.Current.LocalSettings.Values.Add(key, value);
                        AppSetting[key] = value;
                    }
                }
                if (result is string && string.IsNullOrEmpty(result as string)) result = value;
            }
            catch(Exception ex)
            {
                ex.Message.ShowMessage("ERROR".T());
            }
            return (result);
        }

        public static bool Set(string key, dynamic value)
        {
            AppSetting[key] = value;
            ApplicationData.Current.LocalSettings.Values[key] = value;
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
                return (true);
            else
                return (false);
        }
        #endregion

        #region Helper Routines
        private static KeyValuePair<int, string> DetectLanguage(string lang)
        {
            var LanguageIndex = 0;

            if (string.IsNullOrEmpty(lang)) lang = "default";
            switch (lang.ToLower())
            {
                case "default":
                    LanguageIndex = 0;
                    break;
                case "en-us":
                    LanguageIndex = 1;
                    break;
                case "zh-hans":
                    LanguageIndex = 2;
                    break;
                case "zh-hant":
                    LanguageIndex = 3;
                    break;
                case "ja":
                    LanguageIndex = 4;
                    break;
                default:
                    LanguageIndex = 0;
                    break;
            }

            var langs = GlobalizationPreferences.Languages;
            var cl = langs.First().Split("-");

            if (LanguageIndex == 0 && cl.Length > 0)
                lang = cl.Length > 1 ? $"{cl[0]}-{cl[1]}" : $"{cl[0]}";

            return (new KeyValuePair<int, string>(LanguageIndex, lang));
        }

        public static int LoadUILanguage(string lang)
        {
            try
            {
                var syslang = DetectLanguage(lang);

                var LanguageIndex = syslang.Key;
                var LanguageValue = syslang.Value;

                ApplicationLanguages.PrimaryLanguageOverride = LanguageValue;

                return (LanguageIndex);
            }
            catch(Exception)
            {
                ApplicationLanguages.PrimaryLanguageOverride = GlobalizationPreferences.Languages.First();
                return (0);
            }
        }

        public static int SaveUILanguage(string lang)
        {
            return(SetUILanguage(lang, true));
        }

        public static int SetUILanguage(string lang, bool save = false)
        {
            try
            {
                var syslang = DetectLanguage(lang);

                var LanguageIndex = syslang.Key;
                var LanguageValue = syslang.Value;

                ApplicationLanguages.PrimaryLanguageOverride = LanguageValue;

                if (save)
                {
                    Set("UILanguage", lang);
                    "Language will be changed on next startup".T().ShowMessage("INFO".T());
                }

                return (LanguageIndex);
            }
            catch(Exception)
            {
                ApplicationLanguages.PrimaryLanguageOverride = GlobalizationPreferences.Languages.First();
                return (0);
            }
        }

        public static string GetUILanguage()
        {
            return ((string)Get("UILanguage", string.Empty));
        }

        private static Page rootPage = null;

        public static void SetTheme(ElementTheme theme, Page page, bool save = true)
        {
            if (rootPage == null && page == null) return;
            if (page != null) rootPage = page;

            //remove the solid-colored backgrounds behind the caption controls and system back button
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            //titleBar.ButtonForegroundColor = (Color)Resources["SystemBaseHighColor"];

            #region Set Theme & TitleBar button color
            rootPage.RequestedTheme = theme;
            if (rootPage.RequestedTheme == ElementTheme.Dark)
            {
                titleBar.ButtonForegroundColor = Colors.White;
            }
            else if (rootPage.RequestedTheme == ElementTheme.Light)
            {
                titleBar.ButtonForegroundColor = Colors.Black;
            }
            if (save) Settings.Set("AppTheme", (int)rootPage.RequestedTheme);
            //ApplicationData.Current.LocalSettings.Values["AppTheme"] = (int)RequestedTheme;
            #endregion
        }

        public static ElementTheme GetTheme()
        {
            var value = Get("AppTheme", null);

            if (value == null)
            {
                var systemTheme = Application.Current.RequestedTheme;
                if (systemTheme == ApplicationTheme.Light)
                    value = ElementTheme.Light;
                else if (systemTheme == ApplicationTheme.Dark)
                    value = ElementTheme.Dark;
            }
            return ((ElementTheme)value);
        }
        #endregion
    }

    public static class Utils
    {
        //public static string[] image_ext = new string[] { ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff", ".gif", ".svg", ".xaml" };
        public static string[] image_ext = new string[] { ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff", ".gif", ".svg", ".spa", ".sph", ".ico" };
        public static string[] image_exts = new string[] { ".pcx", ".tga", ".ras", ".sun", ".ppm", ".pgm", ".pbm", ".pnm", ".sgi" };
        public static string[] text_ext = new string[] {
            ".txt", ".text", ".base64", ".md", ".me", ".htm", ".html", ".xhtml", ".css", ".rst", ".xml", ".csv", ".ini", ".cfg",
            ".cs", ".xaml", ".js", ".ts", ".json", ".cpp", ".hpp", ".c", ".cc", ".h", ".vb", ".vbs", ".py", ".pyw", ".pas", ".r",
            ".hx", ".kt", ".go", ".java",
            ".url",
            ".ass", ".ssa", ".srt", ".lrc"
        };
        public static string[] url_ext = new string[] { ".url" };

        public static void Beep()
        {
            var player = new MediaElement(){ AutoPlay = false };
            player.Source = new Uri("ms-winsoundevent:Notification.Default");
            player.Play();
        }


        #region Share Extentions
        private static DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
        private static StorageFile _tempExportFile;
        //private static InMemoryRandomAccessStream _tempExportStream;
        private static bool SHARE_INITED = false;
        private static WriteableBitmap SHARED_IMAGE = null;
        private static string SHARED_TEXT = string.Empty;
        private static Uri SHARED_LINK = null;

        private static async void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            try
            {
                DataPackage requestData = args.Request.Data;
                requestData.Properties.Title = "ShareTitle".T();
                requestData.Properties.Description = "ShareDescription".T();

                if (!string.IsNullOrEmpty(SHARED_TEXT) && SHARED_IMAGE == null)
                {
                    requestData.Properties.Description += $" {"Text".T()}";
                    requestData.SetText(SHARED_TEXT);
                }
                else if (string.IsNullOrEmpty(SHARED_TEXT) && SHARED_IMAGE != null)
                {
                    requestData.Properties.Description += $" {"Image".T()}";
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
                else if(SHARED_LINK is Uri)
                {
                    requestData.Properties.Description += $" {"Link".T()}";
                    requestData.SetWebLink(SHARED_LINK);
                }
                else if(_tempExportFile != null)
                {
                    requestData.Properties.Description += $" {"File".T()}";
                    List<IStorageItem> imageItems = new List<IStorageItem> { _tempExportFile };
                    requestData.SetStorageItems(imageItems);
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
        }

        public static async Task<FileUpdateStatus> Share(WriteableBitmap image, string prefix="", string suffix="")
        {
            if (!SHARE_INITED)
            {
                dataTransferManager.DataRequested += DataTransferManager_DataRequested;
                SHARE_INITED = true;
            }

            //ApplicationData.Current.TemporaryFolder.
            SHARED_TEXT = string.Empty;
            SHARED_IMAGE = null;
            SHARED_LINK = null;
            _tempExportFile = null;

            FileUpdateStatus status = FileUpdateStatus.Failed;
            if (image == null || image.PixelWidth <= 0 || image.PixelHeight <= 0) return (status);

            SHARED_IMAGE = image;
            #region Save image to a temporary file for Share
            StorageFile tempFile = await image.StoreTemporaryFile(image.PixelBuffer, image.PixelWidth, image.PixelHeight, prefix, suffix);
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

        public async static Task<FileUpdateStatus> Share(string text, bool AsLink)
        {
            var result = FileUpdateStatus.Failed;
            if (AsLink)
            {
                try
                {
                    if (!string.IsNullOrEmpty(text))
                    {
                        var url = new Uri(text);
                        result = (Share(url));
                    }
                }
                catch(Exception ex)
                {
                    await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
                }
            }
            else
            {
                result = Share(text);
            }
            return (result);
        }

        public static FileUpdateStatus Share(string text)
        {
            if (!SHARE_INITED)
            {
                dataTransferManager.DataRequested += DataTransferManager_DataRequested;
                SHARE_INITED = true;
            }

            SHARED_TEXT = string.Empty;
            SHARED_IMAGE = null;
            SHARED_LINK = null;
            _tempExportFile = null;

            FileUpdateStatus status = FileUpdateStatus.Failed;
            if (string.IsNullOrEmpty(text)) return (status);

            SHARED_TEXT = text;
            DataTransferManager.ShowShareUI();
            status = FileUpdateStatus.Complete;
            //return status;
            return status;
        }

        public static FileUpdateStatus Share(Uri url)
        {
            if (!SHARE_INITED)
            {
                dataTransferManager.DataRequested += DataTransferManager_DataRequested;
                SHARE_INITED = true;
            }

            SHARED_TEXT = string.Empty;
            SHARED_IMAGE = null;
            SHARED_LINK = null;
            _tempExportFile = null;

            FileUpdateStatus status = FileUpdateStatus.Failed;
            if (string.IsNullOrEmpty(url.AbsolutePath)) return (status);

            SHARED_LINK = url;
            DataTransferManager.ShowShareUI();
            status = FileUpdateStatus.Complete;
            //return status;
            return status;
        }

        public static FileUpdateStatus Share(StorageFile file)
        {
            if (!SHARE_INITED)
            {
                dataTransferManager.DataRequested += DataTransferManager_DataRequested;
                SHARE_INITED = true;
            }

            SHARED_TEXT = string.Empty;
            SHARED_IMAGE = null;
            SHARED_LINK = null;
            _tempExportFile = null;

            FileUpdateStatus status = FileUpdateStatus.Failed;
            if (file == null) return (status);

            _tempExportFile = file;
            DataTransferManager.ShowShareUI();
            status = FileUpdateStatus.Complete;
            //return status;
            return status;
        }

        #endregion

        #region Clipboard Extentions
        public static async void SetClipboard(string text, bool html=false)
        {
            DataPackage dataPackage = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            try
            {
                if (html)
                {
                    string content = HtmlFormatHelper.CreateHtmlFormat(text);
                    dataPackage.SetData("text/html", content);
                    dataPackage.SetData(StandardDataFormats.Html, content);
                    dataPackage.SetData(StandardDataFormats.Text, text);
                }
                else
                {
                    dataPackage.SetText(text);
                }
                Clipboard.Clear();
                Clipboard.SetContent(dataPackage);
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
        }

        public static void SetClipboard(WriteableBitmap wb)
        {
            if(wb is WriteableBitmap)
            {
                SetClipboard(wb, string.Empty);
            }
        }

        public static async void SetClipboard(WriteableBitmap wb, string text, bool html = false)
        {
            DataPackage dataPackage = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Copy
            };

            try
            {
                #region Create a temporary file Copy to Clipboard
                //StorageFile tempFile = await (image.Source as WriteableBitmap).StoreTemporaryFile(pixelBuffer, r_width, r_height);

                //if (tempFile != null)
                //{
                //    dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(tempFile));
                //    //await tempFile.DeleteAsync();
                //}
                #endregion

                #region Create in memory image data Copy to Clipboard
                try
                {
                    var cistream = await wb.ToRandomAccessStream(".png");
                    if (cistream != null)
                    {
                        dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(cistream));
                    }
                }
                catch (Exception ex)
                {
                    await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
                }
                #endregion

                #region Create other MIME format data Copy to Clipboard
                //string[] fmts = new string[] { "CF_DIB", "CF_BITMAP", "BITMAP", "DeviceIndependentBitmap", "image/png", "image/bmp", "image/jpg", "image/jpeg" };
                string[] fmts = new string[] { "image/png", "image/bmp", "image/jpg", "image/jpeg", "PNG" };
                foreach (var fmt in fmts)
                {
                    if (fmt.Equals("CF_DIBV5", StringComparison.CurrentCultureIgnoreCase))
                    {
                        try
                        {
                            byte[] arr = wb.ToBytes();
                            byte[] dib = arr.Skip(14).ToArray();
                            var rms = await dib.ToRandomAccessStream();
                            dataPackage.SetData(fmt, rms);
                        }
                        catch (Exception ex)
                        {
                            await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
                        }
                    }
                    else
                    {
                        try
                        {
                            byte[] arr = await wb.ToBytes(fmt);
                            var rms = await arr.ToRandomAccessStream();
                            dataPackage.SetData(fmt, rms);
                        }
                        catch (Exception ex)
                        {
                            await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
                        }
                    }
                }
                #endregion

                #region Add Html/Text content to clipboard datapackage
                if (!string.IsNullOrEmpty(text.Trim()))
                {
                    if (html)
                    {
                        string content = HtmlFormatHelper.CreateHtmlFormat(text.Trim());
                        dataPackage.SetData("text/html", content);
                        dataPackage.SetData(StandardDataFormats.Html, content);
                    }
                    dataPackage.SetData(StandardDataFormats.Text, text);
                }
                #endregion

                Clipboard.Clear();
                Clipboard.SetContent(dataPackage);
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
        }

        public static void SetClipboard(Image image)
        {
            if (image is Image)
            {
                SetClipboard(image, (int)(image.ActualWidth), (int)(image.ActualHeight));
            }
        }

        public static void SetClipboard(Image image, int size)
        {
            if (image is Image)
            {
                SetClipboard(image, size, size);
            }
        }

        public static async void SetClipboard(Image image, int width, int height)
        {
            if (image.Source == null) return;

            try
            {
                //把控件变成图像
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                //传入参数Image控件
                var wb = await image.ToWriteableBitmap();
                var bw = wb.PixelWidth;
                var bh = wb.PixelHeight;
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

                SetClipboard(wb);
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
        }

        public static async Task<string> GetClipboard(string text)
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

        public static async Task<string> GetClipboard(string text, Image image = null)
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                string content = await dataPackageView.GetTextAsync();
                // To output the text from this example, you need a TextBlock control
                return (content);
            }
            else if (dataPackageView.Contains(StandardDataFormats.Bitmap) || dataPackageView.Contains("PNG") || dataPackageView.Contains("image/png"))
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
                    if (dataPackageView.Contains("PNG"))
                    {
                        using (var fileStream = new InMemoryRandomAccessStream())
                        {
                            var data = await dataPackageView.GetDataAsync("PNG");
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
                    else if (dataPackageView.Contains("DeviceIndependentBitmapV5_"))
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
                                0x36, 0x00, 0x00, 0x00//0x28
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
                                0x36, 0x00, 0x00, 0x00//0x28
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
                    await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
                }
            }
            return (text);
        }
        #endregion

        #region ContentDialog Extentions
        public static async void ShowAboutDialog()
        {
            AboutDialog dlgAbout = new AboutDialog();
            ContentDialogResult ret = await dlgAbout.ShowAsync();
            if (ret == ContentDialogResult.Primary)
            {
            }
        }

        public static async Task<Color> ShowColorDialog()
        {
            return (await ShowColorDialog(Colors.White));
        }

        public static async Task<Color> ShowColorDialog(Color color)
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

        public static async Task<int> ShowProgressDialog(IProgress<int> progress)
        {
            var dlgProgress = new ProgressDialog();
            await dlgProgress.ShowAsync();

            return 0;
        }

        public static async Task<StorageFolder> SelectFolder(string folder = "")
        {
            StorageFolder result = null;

            try
            {
                FolderPicker fp = new FolderPicker
                {
                    SuggestedStartLocation = PickerLocationId.Desktop
                };
                fp.FileTypeFilter.Add("*");

                result = await fp.PickSingleFolderAsync();
                if (!(result is StorageFolder))
                {
                    result = null;
                }
            }
            catch (Exception ex)
            {
                ex.Message.T().ShowMessage("ERROR".T());
            }

            return (result);
        }

        public static async Task<IReadOnlyList<StorageFile>> OpenFiles(string[] exts = null)
        {
            IReadOnlyList<StorageFile> result = null;

            try
            {
                FileOpenPicker fp = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.Desktop
                };
                if (exts is string[])
                {
                    foreach (var ext in exts)
                        fp.FileTypeFilter.Add(ext);
                }
                fp.FileTypeFilter.Add("*");

                result = await fp.PickMultipleFilesAsync();
                if (!(result is IReadOnlyList<StorageFile>))
                {
                    result = new List<StorageFile>();
                }

            }
            catch (Exception ex)
            {
                ex.Message.T().ShowMessage("ERROR".T());
            }

            return (result);
        }

        public static async Task<StorageFile> OpenFile(string[] exts = null)
        {
            StorageFile result = null;

            try
            {
                FileOpenPicker fp = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.Desktop,
                };
                if (exts is string[])
                {
                    foreach (var ext in exts)
                        fp.FileTypeFilter.Add(ext);
                }
                fp.FileTypeFilter.Add("*");

                result = await fp.PickSingleFileAsync();
            }
            catch (Exception ex)
            {
                ex.Message.T().ShowMessage("ERROR".T());
            }

            return (result);
        }

        public static async Task<string> ShowOpenDialog(Encoding enc, string ext)
        {
            string result = string.Empty;

            var bytes = await ShowOpenDialog(ext);
            if (bytes is byte[] && bytes.Length > 0)
                result = enc.GetString(bytes);

            return (result);
        }

        public static async Task<byte[]> ShowOpenDialog(string ext)
        {
            byte[] result = null;

            var now = DateTime.Now;
            FileOpenPicker fp = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            fp.FileTypeFilter.Add(ext);
            fp.FileTypeFilter.Add(".txt");

            StorageFile TargetFile = await fp.PickSingleFileAsync();
            if (TargetFile != null)
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Add(TargetFile, TargetFile.Name);
                if (StorageApplicationPermissions.FutureAccessList.Entries.Count >= 1000)
                    StorageApplicationPermissions.FutureAccessList.Remove(StorageApplicationPermissions.FutureAccessList.Entries.Last().Token);
                StorageApplicationPermissions.FutureAccessList.Add(TargetFile, TargetFile.Name);

                // 在用户完成更改并调用CompleteUpdatesAsync之前，阻止对文件的更新
                CachedFileManager.DeferUpdates(TargetFile);
                var content = await FileIO.ReadBufferAsync(TargetFile);
                result = content.ToArray();
                //await FileIO.WriteTextAsync(TargetFile, content);
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(TargetFile);
            }
            return (result);
        }

        public static async Task<string> ShowSaveDialog(string content)
        {
            return (await ShowSaveDialog(content, Encoding.Default, ".txt"));
        }

        public static async Task<string> ShowSaveDialog(string content, Encoding enc, string ext)
        {
            string result = string.Empty;

            if (content.Length <= 0) return (result);

            var now = DateTime.Now;
            FileSavePicker fp = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                SuggestedFileName = $"{now.ToString("yyyyMMddHHmmssff")}{ext}"
            };
            fp.FileTypeChoices.Add("Text File".T(), new List<string>() { ext });
            StorageFile TargetFile = await fp.PickSaveFileAsync();
            if (TargetFile != null)
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Add(TargetFile, TargetFile.Name);
                if (StorageApplicationPermissions.FutureAccessList.Entries.Count >= 1000)
                    StorageApplicationPermissions.FutureAccessList.Remove(StorageApplicationPermissions.FutureAccessList.Entries.Last().Token);
                StorageApplicationPermissions.FutureAccessList.Add(TargetFile, TargetFile.Name);

                // 在用户完成更改并调用CompleteUpdatesAsync之前，阻止对文件的更新
                CachedFileManager.DeferUpdates(TargetFile);
                if (ext.Equals(".uue", StringComparison.CurrentCultureIgnoreCase) || ext.Equals(".xxe", StringComparison.CurrentCultureIgnoreCase))
                    content = $"begin 644 {TargetFile.Name}{Environment.NewLine}{content}{Environment.NewLine}end";
                await FileIO.WriteBytesAsync(TargetFile, enc.GetBytes(content));
                //await FileIO.WriteTextAsync(TargetFile, content);
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(TargetFile);

                result = TargetFile.Name;
            }
            return (result);
        }

        public static async Task<string> ShowSaveDialog(byte[] content, string ext, Encoding enc)
        {
            string result = string.Empty;

            if (content.Length <= 0) return (result);

            var now = DateTime.Now;
            FileSavePicker fp = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                SuggestedFileName = $"{now.ToString("yyyyMMddHHmmssff")}{ext}"
            };
            fp.FileTypeChoices.Add("Text File".T(), new List<string>() { ext });
            StorageFile TargetFile = await fp.PickSaveFileAsync();
            if (TargetFile != null)
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Add(TargetFile, TargetFile.Name);
                if (StorageApplicationPermissions.FutureAccessList.Entries.Count >= 1000)
                    StorageApplicationPermissions.FutureAccessList.Remove(StorageApplicationPermissions.FutureAccessList.Entries.Last().Token);
                StorageApplicationPermissions.FutureAccessList.Add(TargetFile, TargetFile.Name);

                // 在用户完成更改并调用CompleteUpdatesAsync之前，阻止对文件的更新
                byte[] header;
                byte[] footer;
                byte[] body;
                if (ext.Equals(".uue", StringComparison.CurrentCultureIgnoreCase) || ext.Equals(".xxe", StringComparison.CurrentCultureIgnoreCase))
                {
                    header = enc.GetBytes($"begin 644 {TargetFile.Name}{Environment.NewLine}");
                    footer = enc.GetBytes($"{Environment.NewLine}end");
                    body = header.Concat(content).Concat(footer).ToArray();
                }
                else body = content;

                CachedFileManager.DeferUpdates(TargetFile);
                await FileIO.WriteBytesAsync(TargetFile, body);
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(TargetFile);

                result = TargetFile.Name;
            }
            return (result);
        }

        public static async Task<string> ShowSaveDialog(Image image, string prefix = "", string suffix="")
        {
            if(image.Source is SvgImageSource)
            {
                var svg = image.Source as SvgImageSource;
                return (await ShowSaveDialog(image, (int)svg.RasterizePixelWidth, (int)svg.RasterizePixelHeight, prefix, suffix));
            }
            else
            {
                var bmp = await image.ToWriteableBitmap();
                if (bmp != null)
                {
                    var width = bmp.PixelWidth;
                    var height = bmp.PixelHeight;
                    return (await ShowSaveDialog(image, width, height, prefix, suffix));
                }
                else return (string.Empty);
            }               
        }

        public static async Task<string> ShowSaveDialog(Image image, int size, string prefix = "", string suffix = "")
        {
            return (await ShowSaveDialog(image, size, size, prefix, suffix));
        }

        public static async Task<string> ShowSaveDialog(Image image, int width, int height, string prefix = "", string suffix = "")
        {
            if (image.Source == null) return (string.Empty);
            string result = string.Empty;

            if (!string.IsNullOrEmpty(prefix)) prefix = $"{prefix}_";
            if (!string.IsNullOrEmpty(suffix)) suffix = $"_{suffix}";

            var now = DateTime.Now;
            FileSavePicker fp = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                SuggestedFileName = $"{prefix}{now.ToString("yyyyMMddHHmmssff")}{suffix}.png"
            };
            fp.FileTypeChoices.Add("Image File".T(), image_ext);
            StorageFile TargetFile = await fp.PickSaveFileAsync();
            if (TargetFile != null)
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Add(TargetFile, TargetFile.Name);
                if (StorageApplicationPermissions.FutureAccessList.Entries.Count >= 1000)
                    StorageApplicationPermissions.FutureAccessList.Remove(StorageApplicationPermissions.FutureAccessList.Entries.Last().Token);
                StorageApplicationPermissions.FutureAccessList.Add(TargetFile, TargetFile.Name);

                // 在用户完成更改并调用CompleteUpdatesAsync之前，阻止对文件的更新
                CachedFileManager.DeferUpdates(TargetFile);

                if (TargetFile.FileType.Equals(".svg", StringComparison.CurrentCultureIgnoreCase) && image.Source is SvgImageSource)
                {
                    SvgImageSource svg = image.Source as SvgImageSource;
                    if (image.Tag is byte[])
                    {
                        await FileIO.WriteBytesAsync(TargetFile, image.Tag as byte[]);
                    }
                    else if (svg.UriSource != null)// && !string.IsNullOrEmpty(svg.UriSource.AbsoluteUri))
                    {
                        var svgRef = RandomAccessStreamReference.CreateFromUri(svg.UriSource);
                        var rms = await svgRef.OpenReadAsync();
                        await rms.FlushAsync();

                        MemoryStream ms = new MemoryStream();
                        rms.Seek(0);
                        await rms.AsStreamForRead().CopyToAsync(ms);
                        byte[] bytes = ms.ToArray();
                        await ms.FlushAsync();

                        await FileIO.WriteBytesAsync(TargetFile, bytes);
                    }
                    else
                    {
                        var wb = await image.ToWriteableBitmap();
                        if (wb != null)
                        {
                            var targetName = $"{ TargetFile.Name }.png";
                            $"{"SaveSvgToPngConfirm".T()} :\n  {targetName}".ShowMessage("INFO".T());
                            //await new MessageDialog($"{"SaveSvgToPngConfirm".T()} :\n  {targetName}", "INFO".T()).ShowAsync();
                            await TargetFile.RenameAsync($"{targetName}", NameCollisionOption.GenerateUniqueName);
                            wb.SaveAsync(TargetFile, width, height);
                        }
                    }
                }
                else if(TargetFile.FileType.Equals(".svg", StringComparison.CurrentCultureIgnoreCase))
                {
                    var wb = await image.ToWriteableBitmap();
                    if (wb != null)
                    {
                        var targetName = $"{ TargetFile.Name }.png";
                        $"{"SaveSvgToPngConfirm".T()} :\n  {targetName}".ShowMessage("INFO".T());
                        //await new MessageDialog($"{"SaveSvgToPngConfirm".T()} :\n  {targetName}", "INFO".T()).ShowAsync();
                        await TargetFile.RenameAsync($"{targetName}", NameCollisionOption.GenerateUniqueName);
                        wb.SaveAsync(TargetFile, width, height);
                    }
                }
                else
                {
                    #region Save Image control display with specified size
                    var wb = await image.ToWriteableBitmap();
                    wb.SaveAsync(TargetFile, width, height);
                    result = TargetFile.Name;
                    #endregion
                }
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(TargetFile);
            }
            return (result);
        }

        public static async Task<string> ShowSaveDialog(WriteableBitmap image, string prefix = "", string suffix = "")
        {
            if (!(image is WriteableBitmap)) return (string.Empty);

            string result = string.Empty;

            if (!string.IsNullOrEmpty(prefix)) prefix = $"{prefix}_";
            if (!string.IsNullOrEmpty(suffix)) suffix = $"_{suffix}";

            var now = DateTime.Now;
            FileSavePicker fp = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                SuggestedFileName = $"{prefix}{now.ToString("yyyyMMddHHmmssff")}{suffix}.png"
            };

            fp.FileTypeChoices.Add("Image File".T(), image_ext);
            StorageFile TargetFile = await fp.PickSaveFileAsync();
            if (TargetFile != null)
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Add(TargetFile, TargetFile.Name);
                if (StorageApplicationPermissions.FutureAccessList.Entries.Count >= 1000)
                    StorageApplicationPermissions.FutureAccessList.Remove(StorageApplicationPermissions.FutureAccessList.Entries.Last().Token);
                StorageApplicationPermissions.FutureAccessList.Add(TargetFile, TargetFile.Name);

                // 在用户完成更改并调用CompleteUpdatesAsync之前，阻止对文件的更新
                CachedFileManager.DeferUpdates(TargetFile);

                if (!TargetFile.FileType.Equals(".svg", StringComparison.CurrentCultureIgnoreCase))
                {
                    #region Save WriteableBitmap
                    image.SaveAsync(TargetFile);
                    result = TargetFile.Name;
                    #endregion
                }
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(TargetFile);
            }
            return (result);
        }

        public static async void ShowFileHashDialog(StorageFile file = null)
        {
            var hashDlg = new HashCalcDialog(){ InputFile = file };
            await hashDlg.ShowAsync();
        }

        public static async Task<bool> ConvertFile(StorageFile file, Encoding SrcEnc, Encoding DstEnc, bool overwrite = false)
        {
            bool result = false;
            try
            {
                if (text_ext.Contains(file.FileType.ToLower()))
                {
                    IBuffer buffer = await FileIO.ReadBufferAsync(file);
                    DataReader reader = DataReader.FromBuffer(buffer);
                    byte[] fileContent = new byte[reader.UnconsumedBufferLength];
                    reader.ReadBytes(fileContent);
                    var fs = fileContent.ToString(SrcEnc);

                    byte[] BOM = DstEnc.GetBOM();
                    byte[] fa = DstEnc.GetBytes(fs);
                    fa = BOM.Concat(fa).ToArray();

                    if (overwrite)
                    {
                        await FileIO.WriteBytesAsync(file, fa);
                        //using (var ws = await file.OpenAsync(FileAccessMode.ReadWrite))
                        //{
                        //    DataWriter writer = new DataWriter(ws.GetOutputStreamAt(0));
                        //    writer.WriteBytes(BOM);
                        //    writer.WriteBytes(fa);
                        //    await ws.FlushAsync();
                        //}
                        result = true;
                    }
                    else
                    {
                        FileSavePicker fsp = new FileSavePicker
                        {
                            SuggestedStartLocation = PickerLocationId.Unspecified,
                            SuggestedFileName = file.Name,
                            SuggestedSaveFile = file
                        };
                        StorageFile TargetFile = await fsp.PickSaveFileAsync();
                        if (TargetFile != null)
                        {
                            StorageApplicationPermissions.MostRecentlyUsedList.Add(TargetFile, TargetFile.Name);
                            if (StorageApplicationPermissions.FutureAccessList.Entries.Count >= 1000)
                                StorageApplicationPermissions.FutureAccessList.Remove(StorageApplicationPermissions.FutureAccessList.Entries.Last().Token);
                            StorageApplicationPermissions.FutureAccessList.Add(TargetFile, TargetFile.Name);

                            // 在用户完成更改并调用CompleteUpdatesAsync之前，阻止对文件的更新
                            CachedFileManager.DeferUpdates(TargetFile);
                            await FileIO.WriteBytesAsync(TargetFile, fa);
                            result = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }
        #endregion

        #region Suggestion Routines
        private static ObservableCollection<string> suggestions = new ObservableCollection<string>();
        public static ObservableCollection<string> LinkSuggestion(string content)
        {
            content = content.Trim();
            ///
            /// Regex patterns
            ///
            //var tel = @"\+(9[976]\d|8[987530]\d|6[987]\d|5[90]\d|42\d|3[875]\d|2[98654321]\d|9[8543210]|8[6421]|6[6543210]|5[87654321]|4[987654310]|3[9643210]|2[70]|7|1)\d{1,14}$";
            var tel_us = @"^(\+{0,1}1){0,1} {0,1}(\(){0,1}(\d{3})(-| |\)){0,1}(\d{3})(-| ){0,1}(\d{4})$";
            var tel_cn_fix = @"^((0(((\d{2}-\d{8})|(\d{3}-\d{7,8}))|((\d{2}\ \d{8})|(\d{3}\ \d{7,8}))))|((\(0\d{2}\)\d{8})|(\(0\d{3}\)\d{7,8}))|(0(([12]\d\d{8})|([3-9]\d{2}\d{8})|([3-9]\d{2}\d{7}))))|((\+{0,1}86)(((\(\d{2}\)\d{8})|(\(\d{3}\)\d{7,8}))|(-((\d{2}-\d{8})|(\d{3}-\d{7,8})))|(\ ((\d{2}\ \d{8})|(\d{3}\ \d{7,8})))))$";
            var tel_cn_400 = @"^([91]\d{4}$)|([468]00[678109]\d{6})|([468]00[678109]-\d{3}-\d{3})|([468]00-[678109]\d{2}-\d{4})|([468]00-[678109]\d{3}-\d{3})$";
            var mobile_cn = @"^((\+{0,1}86)(\ ){0,1}){0,1}(((13[0-9])|(15[0-3, 5-9])|(18[0,2,3,5-9])|(17[0-8])|(147))\d{8})$";
            //var mobile_cn = @"(\+{0.1}86){0,1}( ){0,1}(\d{11})";
            var skype = @"^\d{6,15}$";
            //var protocol = @"^((http)|(ftp)|(git))s{0,1}://";
            var url = @"(((www)|(cn))\.){0,1}(.*?)(\.((com)|(net)|(org)|(tv)|(cn)|(jp)|(me))+)";
            var mailto = @"^[A-Za-z0-9!#$%&'+/=?^_`{|}~-]+(.[A-Za-z0-9!#$%&'+/=?^_`{|}~-]+)*@([A-Za-z0-9]+(?:-[A-Za-z0-9]+)?.)+[A-Za-z0-9]+(-[A-Za-z0-9]+)?$";

            //var rr = new Regex(tel_cn_fix, RegexOptions.IgnoreCase | RegexOptions.Singleline|RegexOptions.Compiled);

            //Set the ItemsSource to be your filtered dataset
            //sender.ItemsSource = dataset;
            suggestions.Clear();
            if (content.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
            {
                suggestions.Add("https://twitter.com");
                suggestions.Add("https://facebook.com");
            }
            if (Regex.IsMatch(content, mailto, RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                suggestions.Add($"mailto:{content}");
            }
            if (Regex.IsMatch(content, skype, RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                suggestions.Add($"skype:{content}?call");
            }
            if (Regex.IsMatch(content, tel_cn_fix, RegexOptions.IgnoreCase | RegexOptions.Singleline) ||
                Regex.IsMatch(content, tel_cn_400, RegexOptions.IgnoreCase | RegexOptions.Singleline) ||
                Regex.IsMatch(content, mobile_cn, RegexOptions.IgnoreCase | RegexOptions.Singleline) ||
                Regex.IsMatch(content, tel_us, RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                // International Telephone
                suggestions.Add($"tel:{Regex.Replace(content, @"[ |\(|\)|-]", "", RegexOptions.IgnoreCase|RegexOptions.Singleline)}");

            }
            if (Regex.IsMatch(content, url, RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                suggestions.Add($"https://{content}");
                suggestions.Add($"http://{content}");
                suggestions.Add($"ftps://{content}");
                suggestions.Add($"ftp://{content}");
                suggestions.Add($"git://{content}");
            }
            if (Regex.IsMatch(content, @"[a-zA-Z@!@#$%&()-=_+]{3,}", RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                if(!Regex.IsMatch(content, @"^((http)|(https)|(ftp)|(ftps)|(skype)|(mailto)|(tel)|(weixin)):", RegexOptions.IgnoreCase | RegexOptions.Singleline))
                {
                    suggestions.Add($"weixin://contacts/profile/{content}");
                    suggestions.Add($"https://weibo.come/{content}");
                    suggestions.Add($"https://www.facebook.com/{content}");
                    suggestions.Add($"https://twitter.com/{content}");
                    suggestions.Add($"https://github.com/{content}");
                }
            }

            //
            // if no any matched, so return "no suggestions" result
            //
            if (suggestions.Count <= 0) suggestions.Add("No Suggestions!");

            return (suggestions);
        }
        #endregion

        #region Temporary File
        public static async Task<bool> CleanTemporary()
        {
            bool result = false;

            var queryOptions = new QueryOptions
            {
                FolderDepth = FolderDepth.Shallow
            };
            var queryFolders =  ApplicationData.Current.TemporaryFolder.CreateItemQueryWithOptions(queryOptions);
            var sItems = await queryFolders.GetItemsAsync();
            List<string> flist = new List<string>();
            foreach(var item in sItems)
            {
                await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
                flist.Add(item.Name);
            }
            if (flist.Count() >= 0)
                await new MessageDialog($"{flist.Count()} {"file(s)".T()} {"has been deleted".T()}\n\n{string.Join(", ", flist)}", "INFO".T()).ShowAsync();

            result = true;

            return (result);
        }
        #endregion
    }

    class WIFI
    {
        #region Network Info
        /// <summary>
        /// Gets connection ssid for the current Wifi Connection.
        /// </summary>
        /// <returns> string value of current ssid/></returns>
        /// 
        public static async Task<string> GetNetwoksSSID()
        {
            string result = string.Empty;
            try
            {
                var access = await WiFiAdapter.RequestAccessAsync();
                if (access != WiFiAccessStatus.Allowed)
                {
#if DEBUG
                    //result = "Acess Denied for wifi Control";
#endif
                }
                else
                {
                    var ret = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
                    if (ret.Count >= 1)
                    {
                        WiFiAdapter firstAdapter = await WiFiAdapter.FromIdAsync(ret[0].Id);
                        var connectedProfile = await firstAdapter.NetworkAdapter.GetConnectedProfileAsync();
                        if (connectedProfile != null)
                        {
                            result = connectedProfile.ProfileName;
                        }
                        else if (connectedProfile == null)
                        {
#if DEBUG
                            //result = "WiFi adapter disconnected";
#endif
                        }
                    }
                    else
                    {
#if DEBUG
                        //result = "No WiFi Adapters detected on this machine";
#endif
                    }
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }
        #endregion
    }

    class UWPLogos
    {
        public class Logo
        {
            public string Name { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public Size Size { get; set; }
            public double Scale { get; set; }
            public string File { get; set; }

            public Logo()
            {
                Name = string.Empty;
            }
        }

        private List<Logo> logolist = new List<Logo>();
        public List<Logo> Items
        {
            get { return (logolist); }
        }

        public UWPLogos()
        {
            if(logolist is List<Logo> ) logolist.Clear();
        }

        public static UWPLogos Create()
        {
            UWPLogos result = new UWPLogos();

            var item = new Logo();
            result.Items.Add(item);

            return (result);
        }
    }
}
