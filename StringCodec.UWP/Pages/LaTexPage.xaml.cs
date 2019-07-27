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
        private WriteableBitmap CURRENT_IMAGE = null;

        public LaTexPage()
        {
            this.InitializeComponent();

            NavigationCacheMode = NavigationCacheMode.Enabled;

            optScale150.IsChecked = true;

            edSrc.IsEnabled = false;

            MathView.DOMContentLoaded += OnContentLoaded;
            MathView.DefaultBackgroundColor = CURRENT_BGCOLOR;
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

        private async Task<WriteableBitmap> GetMathImage(bool force = false)
        {
            if (CURRENT_IMAGE == null || force)
            {
                var wb = await MathView.ToWriteableBitmap();

                if (wb is WriteableBitmap)
                {
                    var size = await MathView.InvokeScriptAsync("GetEquationRect", null);
                    var space = 2;
                    var tolerance = space * 2;

                    if (!string.IsNullOrEmpty(size))
                    {
                        var sv = size.Split(',');
                        var l = (int)Math.Floor(double.Parse(sv[0]));
                        var t = (int)Math.Floor(double.Parse(sv[1]));
                        var w = (int)Math.Ceiling(double.Parse(sv[2]));
                        var h = (int)Math.Ceiling(double.Parse(sv[3]));
                        l = Math.Max(0, l - tolerance);
                        t = Math.Max(0, t - tolerance);
                        w = Math.Min(wb.PixelWidth, w + tolerance);
                        h = Math.Min(wb.PixelHeight, h + tolerance);
                        wb = wb.Crop(l, t, w, h);
                    }
                    else
                    {
                        //wb = wb.Crop();
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
                    //CURRENT_BGCOLOR = Colors.Transparent; // Color.FromArgb(255, 255, 255, 255);
                    CURRENT_BGCOLOR = Color.FromArgb(0, 255, 255, 255);
                    //CURRENT_BGCOLOR = Color.FromArgb(0, 0, 0, 0);
                    CURRENT_FGCOLOR = Colors.Black; // Color.FromArgb(255, 000, 000, 000);
                    MathView.DefaultBackgroundColor = CURRENT_BGCOLOR;
                    //var retbg = await MathView.InvokeScriptAsync("ChangeBG", new string[] { CURRENT_BGCOLOR.ToHTML() });
                    //var retfg = await MathView.InvokeScriptAsync("ChangeFG", new string[] { CURRENT_FGCOLOR.ToHTML() });
                    break;
                case "BgColor":
                    CURRENT_BGCOLOR = await Utils.ShowColorDialog(CURRENT_BGCOLOR);
                    //MathView.DefaultBackgroundColor = CURRENT_BGCOLOR;
                    //retbg = await MathView.InvokeScriptAsync("ChangeBG", new string[] { CURRENT_BGCOLOR.ToHTML() });
                    break;
                case "FgColor":
                    CURRENT_FGCOLOR = await Utils.ShowColorDialog(CURRENT_FGCOLOR);
                    //retfg = await MathView.InvokeScriptAsync("ChangeFG", new string[] { CURRENT_FGCOLOR.ToHTML() });
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
                Frame.Navigate(typeof(ImagePage), CURRENT_IMAGE);
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
                        await Utils.Share(await GetMathImage(), "Math");
                        break;
                    default:
                        break;
                }
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            switch (btn.Name)
            {
                case "btnGenerateMath":
                    try
                    {
                        var tex = edSrc.Text.Trim();//.Replace("\\", "\\\\");
                        var result = await MathView.InvokeScriptAsync("ChangeEquation", new string[] { tex });
                        //await GetMathImage(true);
                    }
                    catch(Exception ex)
                    {
                        ex.Message.ShowMessage("Error");
                    }
                    break;
                case "btnCopy":
                    try
                    {
                        Utils.SetClipboard(await GetMathImage(true));
                    }
                    catch (Exception ex)
                    {
                        ex.Message.ShowMessage("Error");
                    }
                    //using (Windows.Storage.Streams.InMemoryRandomAccessStream rs = new Windows.Storage.Streams.InMemoryRandomAccessStream())
                    //{
                    //    mathView.DefaultBackgroundColor = Windows.UI.Colors.Transparent;
                    //    await mathView.CapturePreviewToStreamAsync(rs);
                    //    await rs.FlushAsync();
                    //    var img = await rs.ToWriteableBitmap();
                    //    Utils.SetClipboard(img);
                    //}
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
