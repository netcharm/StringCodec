using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


using StringCodec.UWP.Common;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Graphics.Display;
using Windows.UI;
using System.Threading.Tasks;
using System.Text;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace StringCodec.UWP.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class LaTexPage : Page
    {
        //private Color CURRENT_BGCOLOR = Colors.Transparent; //Color.FromArgb(255, 255, 255, 255);
        private Color CURRENT_BGCOLOR = Color.FromArgb(0, 255, 255, 255);
        private Color CURRENT_FGCOLOR = Colors.Black; //Color.FromArgb(255, 000, 000, 000);
        private int CURRENT_SCALE = 150;

        private string DEFAULT_FORMULAR = string.Empty;
        private string CURRENT_FORMULAR = string.Empty;
        private WriteableBitmap CURRENT_IMAGE = null;

        public LaTexPage()
        {
            this.InitializeComponent();

            NavigationCacheMode = NavigationCacheMode.Enabled;

            optScale150.IsChecked = true;

            edSrc.IsEnabled = false;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"\begin{equation}");
            sb.AppendLine(@"\frac{\partial\psi}{\partial t} = \kappa\mathrm{\nabla}^2\psi \\");
            sb.AppendLine(@"");
            sb.AppendLine(@"\begin{aligned}");
            sb.AppendLine(@"");
            sb.AppendLine(@"x &= r (cos(t) + t sin(t)) \\");
            sb.AppendLine(@"y &= r (sin(t) - t cos(t)) \\");
            sb.AppendLine(@"");
            sb.AppendLine(@"\end{aligned}");
            sb.AppendLine(@"\end{equation}");


            DEFAULT_FORMULAR = sb.ToString();
            CURRENT_FORMULAR = DEFAULT_FORMULAR;

            MathView.DOMContentLoaded += OnContentLoaded;
            MathView.DefaultBackgroundColor = CURRENT_BGCOLOR;
            MathView.CanDrag = false;
            //MathView.CompositeMode = ElementCompositeMode.SourceOver;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var ret = await MathView.InvokeScriptAsync("ChangeColor", new string[] { CURRENT_FGCOLOR.ToCssRGBA(), CURRENT_BGCOLOR.ToCssRGBA() });
                //var ret = await mathView.InvokeScriptAsync("LoadMathJax", null);
                //if (ret.Equals("OK", StringComparison.CurrentCultureIgnoreCase))
                {
                    //edSrc.IsEnabled = true;
                }
            }
            catch
            {

            }
        }

        private void OnContentLoaded(object sender, WebViewDOMContentLoadedEventArgs e)
        {
            edSrc.IsEnabled = true;
        }

        private async Task<WriteableBitmap> GetMathCapture()
        {
            using (Windows.Storage.Streams.InMemoryRandomAccessStream rs = new Windows.Storage.Streams.InMemoryRandomAccessStream())
            {
                //MathView.DefaultBackgroundColor = Windows.UI.Colors.Transparent;
                if (MathView.DefaultBackgroundColor.A == 0x00)
                {
                    await MathView.InvokeScriptAsync("ChangeColor", new string[] { CURRENT_FGCOLOR.ToCssRGBA(), Colors.White.ToCssRGBA() });
                }

                await MathView.CapturePreviewToStreamAsync(rs);
                await rs.FlushAsync();
                var wb = await rs.ToWriteableBitmap();

                if (wb is WriteableBitmap)
                {
                    wb = wb.Crop(2);
                }

                if (MathView.DefaultBackgroundColor.A == 0x00)
                {
                    await MathView.InvokeScriptAsync("ChangeColor", new string[] { CURRENT_FGCOLOR.ToCssRGBA(), CURRENT_BGCOLOR.ToCssRGBA() });
                }

                return (wb);
            }
        }

        private async Task<WriteableBitmap> GetMathImage(bool force = false)
        { 
            if (CURRENT_IMAGE == null || force)
            {
                var scale = await MathView.InvokeScriptAsync("GetPageZoomRatio", null);
                var ratio = 1.0;
                double.TryParse(scale, out ratio);
                if (ratio == 0.0) ratio = 1;
                else ratio = Math.Ceiling(ratio);

                var wb = await MathView.ToWriteableBitmap();
                if (wb is WriteableBitmap)
                {
                    var space = 2;
                    var tolerance = space * 2;

                    var size = await MathView.InvokeScriptAsync("GetEquationRect", null);
                    if (!string.IsNullOrEmpty(size))
                    {
                        try
                        {
                            var sv = size.Split(',');
                            var l = (int)Math.Floor(double.Parse(sv[0].Trim()));
                            var t = (int)Math.Floor(double.Parse(sv[1].Trim()));
                            var w = (int)Math.Ceiling(double.Parse(sv[2].Trim()));
                            var h = (int)Math.Ceiling(double.Parse(sv[3].Trim()));
                            l = Math.Max(0, l - tolerance);
                            t = Math.Max(0, t - tolerance);
                            w = Math.Min(wb.PixelWidth, (int)(w * ratio) + tolerance * 2);
                            h = Math.Min(wb.PixelHeight, (int)(h * ratio) + tolerance * 2);
                            wb = wb.Crop(l, t, w, h);
                        }
                        catch (Exception)
                        {

                        }
                    }
                    wb = wb.Crop(space);
                }
                CURRENT_IMAGE = wb;
            }
            return (CURRENT_IMAGE);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                var data = e.Parameter;
            }
        }

        private async void OptColor_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as MenuFlyoutItem;
            var C_NAME = btn.Name.Substring(3);

            switch (C_NAME)
            {
                case "ResetColor":
                    CURRENT_BGCOLOR = Color.FromArgb(0, 255, 255, 255);
                    CURRENT_FGCOLOR = Colors.Black; // Color.FromArgb(255, 000, 000, 000);
                    MathView.DefaultBackgroundColor = CURRENT_BGCOLOR;
                    break;
                case "BgColor":
                    CURRENT_BGCOLOR = await Utils.ShowColorDialog(CURRENT_BGCOLOR);
                    MathView.DefaultBackgroundColor = CURRENT_BGCOLOR;
                    break;
                case "FgColor":
                    CURRENT_FGCOLOR = await Utils.ShowColorDialog(CURRENT_FGCOLOR);
                    break;
                default:
                    break;
            }
            var ret = await MathView.InvokeScriptAsync("ChangeColor", new string[] { CURRENT_FGCOLOR.ToCssRGBA(), CURRENT_BGCOLOR.ToCssRGBA() });
            //await GetMathImage(true);
        }

        private async void OptScale_Click(object sender, RoutedEventArgs e)
        {
            var ScaleItems = new ToggleMenuFlyoutItem[] {
                optScale100,
                optScale125, optScale133, optScale150,
                optScale200, optScale250,
                optScale300,
                optScale400
            };

            foreach(var item in ScaleItems)
            {
                if (item == sender)
                {
                    item.IsChecked = true;
                    var btnName = item.Name;
                    CURRENT_SCALE = int.Parse(btnName.Substring("optScale".Length));
                    var ret = await MathView.InvokeScriptAsync("ChangeScale", new string[] { $"{CURRENT_SCALE}" });
                }
                else
                    item.IsChecked = false;
            }
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            await GetMathImage();
            if (sender == btnSrcText)
            {
                if (string.IsNullOrEmpty(edSrc.Text)) return;
                Frame.Navigate(typeof(QRCodePage), edSrc.Text);
            }
            else if (sender == btnDstImage)
            {
                if (CURRENT_IMAGE == null) return;
                //Frame.Navigate(typeof(ImagePage), await GetMathImage());
                var obj = new WriteableBitmapObject()
                {
                    Image = await GetMathImage(),
                    Title = edSrc.Text
                };
                Frame.Navigate(typeof(ImagePage), obj);
            }
            else if (sender == btnDstCapture)
            {
                if (CURRENT_IMAGE == null) return;
                //Frame.Navigate(typeof(ImagePage), await GetMathImage());
                var obj = new WriteableBitmapObject()
                {
                    Image = await GetMathCapture(),
                    Title = edSrc.Text
                };
                Frame.Navigate(typeof(ImagePage), obj);
            }
        }

        private async void AppBarShare_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem)
            {
                var text = edSrc.SelectionLength > 0 ? edSrc.SelectedText : edSrc.Text;
                var btn = sender as MenuFlyoutItem;
                switch (btn.Name)
                {
                    case "btnShareSrcContent":
                        Utils.Share(text);
                        break;
                    case "btnShareDstContent":
                        await Utils.Share(await GetMathImage(), "Math".T());
                        break;
                    default:
                        break;
                }
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            try
            {
                switch (btn.Name)
                {
                    case "btnGenerateMath":
                        var tex = edSrc.Text.Trim();//.Replace("\\", "\\\\");
                        if (!string.IsNullOrEmpty(tex))
                        {
                            var result = await MathView.InvokeScriptAsync("ChangeEquation", new string[] { tex });
                            CURRENT_FORMULAR = tex;
                            //await GetMathImage(true);
                        }
                        break;
                    case "btnCopy":
                        Utils.SetClipboard(await GetMathImage(true));
                        break;
                    case "btnCaptureMathToClip":
                        Utils.SetClipboard(await GetMathCapture());
                        break;
                    case "btnImageAsHtmlToClip":
                        Utils.SetClipboard(await (await GetMathImage()).ToHTML(CURRENT_FORMULAR));
                        break;
                    case "btnCaptureAsHtmlToClip":
                        Utils.SetClipboard(await (await GetMathCapture()).ToHTML(CURRENT_FORMULAR));
                        break;
                    case "btnCaptureMathToShare":
                        await Utils.Share(await GetMathCapture(), "Math".T());
                        break;
                    case "btnPaste":
                        edSrc.Text = await Utils.GetClipboard(edSrc.Text.Trim());
                        break;
                    case "btnSave":
                        await Utils.ShowSaveDialog(await GetMathImage(true), "Math");
                        break;
                    case "btnShare":
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessage("Error");
            }
        }

        #region Drag/Drop routines
        //private bool canDrop = true;
        private void OnDragEnter(object sender, DragEventArgs e)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Drag Enter Sender:{sender}");
#endif

        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Drag Over Sender:{sender}");
#endif


        }

        private void OnDrop(object sender, DragEventArgs e)
        {

        }

        #endregion


    }
}
