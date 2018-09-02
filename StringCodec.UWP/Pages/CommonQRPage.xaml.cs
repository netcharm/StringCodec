using StringCodec.UWP.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
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
    public sealed partial class CommonQRPage : Page
    {
        private QRCodec.ERRORLEVEL CURRENT_ECL = QRCodec.ERRORLEVEL.L;
        private Color CURRENT_BGCOLOR = Colors.White; //Color.FromArgb(255, 255, 255, 255);
        private Color CURRENT_FGCOLOR = Colors.Black; //Color.FromArgb(255, 000, 000, 000);
        private int CURRENT_SIZE = 512;

        private bool CURRENT_USINGDIALOG = true;

        private string[] image_ext = new string[] { ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff", ".gif" };

        static private string text_src = string.Empty;
        static public string Text
        {
            get { return text_src; }
            set { text_src = value; }
        }

        private CommonQRContentPage ContentPage = new CommonQRContentPage();

        public CommonQRPage()
        {
            this.InitializeComponent();

            NavigationCacheMode = NavigationCacheMode.Enabled;

            optECL_L.IsChecked = true;
            optSaveSizeM.IsChecked = true;

            optCommonLink.IsChecked = true;
            ContentPage.SelectedIndex = 0;

            optUsingDialog.IsChecked = CURRENT_USINGDIALOG;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                var data = e.Parameter;
                if (data is string)
                {
                    edQR.Text = data.ToString();
                    imgQR.Source = edQR.Text.EncodeQR(CURRENT_FGCOLOR, CURRENT_BGCOLOR, CURRENT_ECL);
                }
                else if (data is WriteableBitmap)
                {
                    imgQR.Source = data as WriteableBitmap;
                    edQR.Text = await QRCodec.Decode(imgQR.Source as WriteableBitmap);
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void edQR_TextChanged(object sender, TextChangedEventArgs e)
        {
            lblInfo.Text = $"Count: {edQR.Text.Length}";
            text_src = edQR.Text;
        }

        private void OptSave_Click(object sender, RoutedEventArgs e)
        {
            AppBarToggleButton[] opts = new AppBarToggleButton[] { optSaveSizeL, optSaveSizeM, optSaveSizeN, optSaveSizeS, optSaveSizeWindow };

            var btn = sender as AppBarToggleButton;
            var SIZE_NAME = btn.Name.Substring(btn.Name.Length - 1).ToUpper();

            foreach (AppBarToggleButton opt in opts)
            {
                if (string.Equals(opt.Name, btn.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    opt.IsChecked = true;
                }
                else opt.IsChecked = false;
            }
            switch (SIZE_NAME)
            {
                case "L":
                    CURRENT_SIZE = 1024;
                    break;
                case "M":
                    CURRENT_SIZE = 512;
                    break;
                case "N":
                    CURRENT_SIZE = 256;
                    break;
                case "S":
                    CURRENT_SIZE = 128;
                    break;
                default:
                    CURRENT_SIZE = 0;
                    break;
            }
        }

        private void OptUsingDialog_Click(object sender, RoutedEventArgs e)
        {
            //optUsingDialog.IsChecked = !optUsingDialog.IsChecked;
            CURRENT_USINGDIALOG = optUsingDialog.IsChecked == true ? true : false;
            if (CURRENT_USINGDIALOG)
            {
                edContent.Items.Clear();
                edQR.Visibility = Visibility.Visible;
            }
            else
            {
                edQR.Visibility = Visibility.Collapsed;
                edContent.Items.Clear();
                edContent.Items.Add(ContentPage.SelectedItem);
            }
        }

        private void OptECL_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] opts = new ToggleMenuFlyoutItem[] { optECL_L, optECL_M, optECL_Q, optECL_H };

            var btn = sender as ToggleMenuFlyoutItem;
            var ECL_NAME = btn.Name.Substring(btn.Name.Length - 1);

            foreach (ToggleMenuFlyoutItem opt in opts)
            {
                if (string.Equals(opt.Name, btn.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    opt.IsChecked = true;
                }
                else opt.IsChecked = false;
            }

            CURRENT_ECL = (QRCodec.ERRORLEVEL)Enum.Parse(typeof(QRCodec.ERRORLEVEL), ECL_NAME);
        }

        private async void OptColor_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as MenuFlyoutItem;
            var C_NAME = btn.Name.Substring(3);

            switch (C_NAME)
            {
                case "ResetColor":
                    CURRENT_BGCOLOR = Colors.White; // Color.FromArgb(255, 255, 255, 255);
                    CURRENT_FGCOLOR = Colors.Black; // Color.FromArgb(255, 000, 000, 000);
                    break;
                case "BgColor":
                    CURRENT_BGCOLOR = await Utils.ShowColorDialog(CURRENT_BGCOLOR);
                    break;
                case "FgColor":
                    CURRENT_FGCOLOR = await Utils.ShowColorDialog(CURRENT_FGCOLOR);
                    break;
                default:
                    break;
            }
        }

        private async void OptCommon_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] opts = new ToggleMenuFlyoutItem[] { optCommonLink, optCommonWifi, optCommonMail, optCommonGeo, optCommonContact, optCommonEvent };
            var btn = sender as ToggleMenuFlyoutItem;
            foreach (ToggleMenuFlyoutItem opt in opts)
            {
                if(opt == btn) opt.IsChecked = true;
                else opt.IsChecked = false;
            }

            int selectedIndex = 0;

            if (sender == optCommonLink)
            {
                selectedIndex = 0;
            }
            else if (sender == optCommonWifi)
            {
                selectedIndex = 1;
                ContentPage.SSID = await WIFI.GetNetwoksSSID();
            }
            else if (sender == optCommonMail)
            {
                selectedIndex = 2;
            }
            else if (sender == optCommonGeo)
            {
                selectedIndex = 3;
            }
            else if (sender == optCommonContact)
            {
                selectedIndex = 4;
            }
            else if (sender == optCommonEvent)
            {
                selectedIndex = 5;
            }
            if (selectedIndex >= 0)
            {
                ContentPage.SelectedIndex = selectedIndex;

                if (CURRENT_USINGDIALOG)
                {
                    edContent.Items.Clear();
                    edQR.Visibility = Visibility.Visible;

                    var dlgCommon = new CommonQRDialog();
                    dlgCommon.Items.Clear();
                    dlgCommon.Items.Add(ContentPage.SelectedItem);
                    var dlgResult = await dlgCommon.ShowAsync();
                    if (dlgResult == ContentDialogResult.Primary)
                    {
                        edQR.Text = ContentPage.GetContents();
                        imgQR.Source = edQR.Text.EncodeQR(CURRENT_FGCOLOR, CURRENT_BGCOLOR, CURRENT_ECL);
                    }
                    dlgCommon.Items.Clear();
                }
                else
                {
                    edQR.Visibility = Visibility.Collapsed;
                    edContent.Items.Clear();
                    edContent.Items.Add(ContentPage.SelectedItem);
                }
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            switch (btn.Name)
            {
                case "btnEncode":
                    edQR.Text = ContentPage.GetContents();
                    imgQR.Source = QRCodec.EncodeQR(edQR.Text, CURRENT_FGCOLOR, CURRENT_BGCOLOR, CURRENT_ECL);
                    break;
                case "btnCopy":
                    Utils.SetClipboard(imgQR, CURRENT_SIZE);
                    break;
                case "btnPaste":
                    edQR.Text = await Utils.GetClipboard(edQR.Text, imgQR);
                    break;
                case "btnSave":
                    await Utils.ShowSaveDialog(imgQR, CURRENT_SIZE, "QR");
                    break;
                case "btnShare":
                    await Utils.Share(imgQR.Source as WriteableBitmap, "QR");
                    break;
                default:
                    break;
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
                        if (sender == edQR)
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
            if (sender == edQR)
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
            if (sender == edQR)
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        var storageFile = items[0] as StorageFile;
                        if (Utils.text_ext.Contains(storageFile.FileType.ToLower()))
                            edQR.Text = await FileIO.ReadTextAsync(storageFile);
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
                        edQR.Text = content;
                    }
                }
            }
            //def.Complete();
        }
        #endregion

    }
}
