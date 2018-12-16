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
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
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
        private System.Globalization.CultureInfo CURRENT_CULTURE = System.Globalization.CultureInfo.CurrentCulture;
        private bool CURRENT_LINEBREAK = false;

        static private string text_src = string.Empty;
        static public string Text
        {
            get { return text_src; }
            set { text_src = value; }
        }

        public TextPage()
        {
            this.InitializeComponent();

            NavigationCacheMode = NavigationCacheMode.Enabled;

            optURL.IsChecked = true;
            optLangUTF8.IsChecked = true;
            optWrapText.IsChecked = false;

            optUUE.Visibility = Visibility.Collapsed;
            optUUE.IsEnabled = false;
            optXXE.Visibility = Visibility.Collapsed;
            optXXE.IsEnabled = false;
            optQuoted.Visibility = Visibility.Collapsed;
            optQuoted.IsEnabled = false;

            ToggleMenuFlyoutItem[] opts = new ToggleMenuFlyoutItem[] {
                optLangAscii,
                optLang1250, optLang1251, optLang1252, optLang1253, optLang1254, optLang1255, optLang1256, optLang1257, optLang1258,
                optLangThai, optLangRu,
                optLangGBK, optLangBIG5, optLangJIS, optLangKorean,
                optLangUnicode, optLangUTF8
            };
            foreach (var lang in opts)
            {
                var ENC_NAME = lang.Name.Substring(7);
                var enc = TextCodecs.GetTextEncoder(ENC_NAME);
                ToolTipService.SetToolTip(lang, new ToolTip()
                {
                    Content =
                        $"{"EncodingName".T():-16}: {enc.EncodingName}\n" +
                        $"{"WebName".T():-16}: {enc.WebName}\n" +
                        $"{"CodePage".T():-16}: {enc.CodePage}",
                    Placement = PlacementMode.Right
                });
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //if (!string.IsNullOrEmpty(text_src)) edSrc.Text = text_src;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                if (e.Parameter is string)
                {
                    var data = e.Parameter;
                    edSrc.Text = data.ToString();
                    edDst.Text = await TextCodecs.Encode(edSrc.Text, CURRENT_CODEC, CURRENT_LINEBREAK);
                }
            }
        }

        private void Codec_Click(object sender, RoutedEventArgs e)
        {
            if (sender == optLineBreak)
            {
                CURRENT_LINEBREAK = (sender as ToggleMenuFlyoutItem).IsChecked;
                return;
            }

            ToggleMenuFlyoutItem[] opts = new ToggleMenuFlyoutItem[] {
                optBase64, optUUE, optXXE, optQuoted,
                optURL, optHtml, optRaw,
                optThunder, optFlashGet,
                optMorse, optMorseAbbr
            };

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

        private async void Case_Click(object sender, RoutedEventArgs e)
        {
            var text = edSrc.Text;
            var result = text;

            try
            {
                #region English case convert
                if (sender == optCaseUp)
                    result = text.Upper(CURRENT_CULTURE);
                else if (sender == optCaseLow)
                    result = text.Lower(CURRENT_CULTURE);
                else if (sender == optCaseCapsWord)
                    result = text.CapsWord(CURRENT_CULTURE);
                else if (sender == optCaseCapsSentence)
                    result = text.CapsSentence(CURRENT_CULTURE);
                #endregion
                #region Chinese case convert
                else if (sender == optCaseZhUpNum)
                    result = text.ChinaNumToUpper();
                else if (sender == optCaseZhLowNum)
                    result = text.ChinaNumToLower();
                else if (sender == optCaseZhUpRmbNum)
                    result = text.ChinaNumToUpper(true);
                else if (sender == optCaseZhLowRmbNum)
                    result = text.ChinaNumToLower(true);
                else if (sender == optCaseZhHalfNum)
                    result = text.ChinaFullToHalf();
                else if (sender == optCaseZhFullNum)
                    result = text.ChinaHalfToFull();
                else if (sender == optCaseZhS2T)
                    result = text.ChinaS2T();
                else if (sender == optCaseZhT2S)
                    result = text.ChinaT2S();
                #endregion
                #region Japaness case convert
                else if (sender == optCaseJaUpNum)
                    result = text.JapanNumToUpper();
                else if (sender == optCaseJaLowNum)
                    result = text.JapanNumToLower();
                else if (sender == optCaseJaUpRmbNum)
                    result = text.JapanNumToUpper(true);
                else if (sender == optCaseJaUpKana)
                    result = text.KanaToUpper();
                else if (sender == optCaseJaLowKana)
                    result = text.KanaToLower();
                else if (sender == optCaseJaHalfKana)
                    result = text.KatakanaFullToHalf();
                else if (sender == optCaseJaFullKana)
                    result = text.KatakanaHalfToFull();
                #endregion
            }
            catch (Exception ex)
            {
                await new Windows.UI.Popups.MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }

            edDst.Text = result.Trim();
        }

        private void OptLang_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] opts = new ToggleMenuFlyoutItem[] {
                optLangAscii,
                optLang1250, optLang1251, optLang1252, optLang1253, optLang1254, optLang1255, optLang1256, optLang1257, optLang1258,
                optLangEn, optLangFr, optLangDe, optLangEs, optLangPt, optLangNl, optLangRu, optLangIt, optLangGr, optLangDa, optLangCz,
                optLangThai,
                optLangGBK, optLangBIG5, optLangJIS, optLangKorean,
                optLangUnicode, optLangUTF8
            };

            var btn = sender as ToggleMenuFlyoutItem;
            foreach (ToggleMenuFlyoutItem opt in opts)
            {
                if (string.Equals(opt.Name, btn.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    opt.IsChecked = true;
                }
                else opt.IsChecked = false;
            }

            var ENC_NAME = btn.Name.Substring(7);
            CURRENT_ENC = TextCodecs.GetTextEncoder(ENC_NAME);
            CURRENT_CULTURE = TextCodecs.GetCulture(ENC_NAME);

            edSrc.Language = CURRENT_CULTURE.IetfLanguageTag;
            edDst.Language = CURRENT_CULTURE.IetfLanguageTag;
        }
        
        private void OptWrap_Click(object sender, RoutedEventArgs e)
        {
            if (sender == optWrapText)
            {
                if (optWrapText.IsChecked == true)
                {
                    edSrc.TextWrapping = TextWrapping.Wrap;
                    edDst.TextWrapping = TextWrapping.Wrap;
                }
                else
                {
                    edSrc.TextWrapping = TextWrapping.NoWrap;
                    edDst.TextWrapping = TextWrapping.NoWrap;
                }
            }
        }

        private void QRCode_Click(object sender, RoutedEventArgs e)
        {
            if (sender == btnSrcQRCode)
            {
                if (edSrc.Text.Length <= 0) return;
                Frame.Navigate(typeof(QRCodePage), edSrc.Text);
            }
            else if (sender == btnDstQRCode)
            {
                if (edDst.Text.Length <= 0) return;
                Frame.Navigate(typeof(QRCodePage), edDst.Text);
            }
        }

        private void edSrc_TextChanged(object sender, TextChangedEventArgs e)
        {
            text_src = edSrc.Text;
        }

        private void edSrc_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key.HasFlag(Windows.System.VirtualKey.Control) && e.Key == Windows.System.VirtualKey.W)
            {
                if (sender != CmdBar)
                    optWrapText.IsChecked = !optWrapText.IsChecked;
                if (optWrapText.IsChecked == true)
                {
                    edSrc.TextWrapping = TextWrapping.Wrap;
                    edDst.TextWrapping = TextWrapping.Wrap;
                }
                else
                {
                    edSrc.TextWrapping = TextWrapping.NoWrap;
                    edDst.TextWrapping = TextWrapping.NoWrap;
                }
            }
        }

        private async void AppBarShare_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem)
            {
                var btn = sender as MenuFlyoutItem;
                switch (btn.Name)
                {
                    case "btnShareSrcAsURL":
                        await Utils.Share(edSrc.Text, true);
                        break;
                    case "btnShareDstAsURL":
                        await Utils.Share(edDst.Text, true);
                        break;
                    case "btnShareSrcContent":
                        Utils.Share(edSrc.Text);
                        break;
                    case "btnShareDstContent":
                        Utils.Share(edDst.Text);
                        break;
                    default:
                        break;
                }
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is AppBarButton)
            {
                var btn = sender as AppBarButton;
                switch (btn.Name)
                {
                    case "btnEncode":
                        if (CURRENT_CODEC == TextCodecs.CODEC.UUID)
                        {
                            StringBuilder sb = new StringBuilder();
                            string guid = TextCodecs.GUID.Encode(edSrc.Text);
                            sb.AppendLine(TextCodecs.GUID.Encode(guid, "N"));
                            sb.AppendLine(TextCodecs.GUID.Encode(guid, "D"));
                            sb.AppendLine(TextCodecs.GUID.Encode(guid, "B"));
                            sb.AppendLine(TextCodecs.GUID.Encode(guid, "P"));
                            sb.AppendLine(TextCodecs.GUID.Encode(guid, "X"));
                            edDst.Text = sb.ToString();
                        }
                        else
                            edDst.Text = await TextCodecs.Encode(edSrc.Text, CURRENT_CODEC, CURRENT_LINEBREAK);
                        text_src = edDst.Text;
                        break;
                    case "btnDecode":
                        if (CURRENT_CODEC == TextCodecs.CODEC.UUID)
                        {
                            StringBuilder sb = new StringBuilder();
                            string guid = TextCodecs.GUID.Decode(edSrc.Text);
                            sb.AppendLine(TextCodecs.GUID.Decode(guid, "N"));
                            sb.AppendLine(TextCodecs.GUID.Decode(guid, "D"));
                            sb.AppendLine(TextCodecs.GUID.Decode(guid, "B"));
                            sb.AppendLine(TextCodecs.GUID.Decode(guid, "P"));
                            sb.AppendLine(TextCodecs.GUID.Decode(guid, "X"));
                            edDst.Text = sb.ToString();
                        }
                        else
                            edDst.Text = await TextCodecs.Decode(edSrc.Text, CURRENT_CODEC, CURRENT_ENC);
                        text_src = edDst.Text;
                        break;
                    case "btnCopy":
                        Utils.SetClipboard(edDst.Text);
                        break;
                    case "btnPaste":
                        edSrc.Text = await Utils.GetClipboard(edSrc.Text);
                        break;
                    case "btnSave":
                        await Utils.ShowSaveDialog(edDst.Text);
                        break;
                    case "btnShare":
                        //Utils.Share(edSrc.Text);
                        break;
                    default:
                        break;
                }
            }
        }

        #region Drag/Drop routines
        private bool canDrop = true;
        private async void OnDragEnter(object sender, DragEventArgs e)
        {
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
                        System.Diagnostics.Debug.WriteLine(filename);
#endif
                        if (sender == edSrc)
                        {
                            if (Utils.text_ext.Contains(extension))
                            {
                                canDrop = true;
#if DEBUG
                                System.Diagnostics.Debug.WriteLine("Drag text to TextBox Control");
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
            if (sender == edSrc)
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    if (canDrop) e.AcceptedOperation = DataPackageOperation.Copy;
                }
                else if (e.DataView.Contains(StandardDataFormats.Text) ||
                    e.DataView.Contains(StandardDataFormats.WebLink) ||
                    e.DataView.Contains(StandardDataFormats.Html) ||
                    e.DataView.Contains(StandardDataFormats.Rtf))
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            // 记得获取Deferral对象
            //var def = e.GetDeferral();
            if (sender == edSrc)
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        var storageFile = items[0] as StorageFile;
                        if (Utils.url_ext.Contains(storageFile.FileType.ToLower()))
                        {
                            if (e.DataView.Contains(StandardDataFormats.WebLink))
                            {
                                var url = await e.DataView.GetWebLinkAsync();
                                if (url.IsUnc)
                                {
                                    edSrc.Text = url.ToString();
                                }
                                else if (url.IsFile)
                                {
                                    edSrc.Text = await FileIO.ReadTextAsync(storageFile);
                                }
                            }
                            else if (e.DataView.Contains(StandardDataFormats.Text))
                            {
                                //var content = await e.DataView.GetHtmlFormatAsync();
                                var content = await e.DataView.GetTextAsync();
                                if (content.Length > 0)
                                {
                                    edSrc.Text = content;
                                }
                            }
                        }
                        else if (Utils.text_ext.Contains(storageFile.FileType.ToLower()))
                            edSrc.Text = await FileIO.ReadTextAsync(storageFile);
                    }
                }
                else if (e.DataView.Contains(StandardDataFormats.Text) ||
                    e.DataView.Contains(StandardDataFormats.WebLink) ||
                    e.DataView.Contains(StandardDataFormats.Html) ||
                    e.DataView.Contains(StandardDataFormats.Rtf))
                {
                    var content = await e.DataView.GetTextAsync();
                    if (content.Length > 0)
                    {
                        edSrc.Text = content;
                    }
                }
            }
            //def.Complete();
        }

        #endregion


    }
}
