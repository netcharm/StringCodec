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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace StringCodec.UWP.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class TextPage : Page
    {
        private TextCodecs.CODEC CURRENT_CODEC = TextCodecs.CODEC.URL;
        private Encoding CURRENT_ENC = Encoding.UTF8;
        private bool CURRENT_LINEBREAK = false;

        private void SetClipboardText(string text)
        {
            DataPackage dataPackage = new DataPackage();

            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
            //dataPackage.RequestedOperation = DataPackageOperation.Copy;
        }

        private async Task<string> GetClipboardText(string text)
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

        public TextPage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //var enc_list = Encoding.GetEncodings();
            //foreach (EncodingInfo ei in Encoding.GetEncodings())
            //{
            //    Encoding enc = ei.GetEncoding();
            //}

            optURL.IsChecked = true;
            optUTF8.IsChecked = true;

            optUUE.Visibility = Visibility.Collapsed;
            optUUE.IsEnabled = false;
            optXXE.Visibility = Visibility.Collapsed;
            optXXE.IsEnabled = false;
            optQuoted.Visibility = Visibility.Collapsed;
            optQuoted.IsEnabled = false;
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            switch (btn.Name)
            {
                case "btnEncode":
                    edDst.Text = TextCodecs.Encode(edSrc.Text, CURRENT_CODEC, CURRENT_LINEBREAK);
                    break;
                case "btnDecode":
                    edDst.Text = TextCodecs.Decode(edSrc.Text, CURRENT_CODEC, CURRENT_ENC);
                    break;
                case "btnCopy":
                    SetClipboardText(edDst.Text);
                    break;
                case "btnPaste":
                    edSrc.Text = await GetClipboardText(edSrc.Text);
                    break;
                case "btnSave":
                    break;
                default:
                    break;
            }
        }

        private void Codec_Checked(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] opts = new ToggleMenuFlyoutItem[] { optBase64, optUUE, optXXE, optURL, optThunder, optRaw, optQuoted, optFlashGet };

            var btn = sender as ToggleMenuFlyoutItem;

            foreach (ToggleMenuFlyoutItem opt in opts)
            {
                if (string.Equals(opt.Name, btn.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    opt.IsChecked = true;
                }
                else opt.IsChecked = false;
            }
            CURRENT_CODEC = (TextCodecs.CODEC)Enum.Parse(typeof(TextCodecs.CODEC), btn.Name.ToUpper().Substring(3));
        }

        private void Opt_Checked(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] opts = new ToggleMenuFlyoutItem[] { optUTF8, optUnicode, optGBK, optBIG5, optJIS, optAscii };

            var btn = sender as ToggleMenuFlyoutItem;
            var ENC_NAME = btn.Name.Substring(3);

            if (string.Equals(ENC_NAME, "LineBreak", StringComparison.CurrentCultureIgnoreCase))
            {
                CURRENT_LINEBREAK = btn.IsChecked;
                return;
            }

            foreach (ToggleMenuFlyoutItem opt in opts)
            {
                if (string.Equals(opt.Name, btn.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    opt.IsChecked = true;
                }
                else opt.IsChecked = false;
            }

            if (string.Equals(ENC_NAME, "UTF8", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_ENC = Encoding.UTF8;
            else if (string.Equals(ENC_NAME, "Unicode", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_ENC = Encoding.Unicode;
            else if (string.Equals(ENC_NAME, "GBK", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_ENC = Encoding.GetEncoding("GBK");
            else if (string.Equals(ENC_NAME, "BIG5", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_ENC = Encoding.GetEncoding("BIG5");
            else if (string.Equals(ENC_NAME, "JIS", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_ENC = Encoding.GetEncoding("Shift-JIS");
            else if (string.Equals(ENC_NAME, "ASCII", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_ENC = Encoding.ASCII;
        }

    }
}
