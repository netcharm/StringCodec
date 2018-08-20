﻿using StringCodec.UWP.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.System;
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

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace StringCodec.UWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ShareOperation operation;
        //private Utils utils = new Utils();

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            #region Extented the supported string charsets
            //
            // Add GBK/Shift-JiS... to Encoding Supported
            // 使用CodePagesEncodingProvider去注册扩展编码。
            //
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            //var enc_list = Encoding.GetEncodings();
            //foreach (EncodingInfo ei in Encoding.GetEncodings())
            //{
            //    Encoding enc = ei.GetEncoding();
            //}
            #endregion

            #region Add Back Shortcut Key to Alt+Back
            // add keyboard accelerators for backwards navigation
            KeyboardAccelerator GoBack = new KeyboardAccelerator();
            GoBack.Key = VirtualKey.GoBack;
            GoBack.Invoked += BackInvoked;
            KeyboardAccelerator AltLeft = new KeyboardAccelerator();
            AltLeft.Key = VirtualKey.Left;
            AltLeft.Invoked += BackInvoked;
            this.KeyboardAccelerators.Add(GoBack);
            this.KeyboardAccelerators.Add(AltLeft);
            // ALT routes here
            AltLeft.Modifiers = VirtualKeyModifiers.Menu;
            #endregion

            #region 将应用扩展到标题栏
            //draw into the title bar
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

            //remove the solid-colored backgrounds behind the caption controls and system back button
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = (Color)Resources["SystemBaseHighColor"];

            this.RequestedTheme = ElementTheme.Dark;
            if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                titleBar.ButtonForegroundColor = Colors.White;
            }
            else
            {
                titleBar.ButtonForegroundColor = Colors.Black;
            }
            #endregion


            //Utils.ShareInit();
        }

        private bool OnBackRequested()
        {
            bool navigated = false;

            // don't go back if the nav pane is overlayed
            if (nvMain.IsPaneOpen && (nvMain.DisplayMode == NavigationViewDisplayMode.Compact || nvMain.DisplayMode == NavigationViewDisplayMode.Minimal))
            {
                return false;
            }
            else
            {
                if (ContentFrame.CanGoBack)
                {
                    ContentFrame.GoBack();
                    navigated = true;
                }
            }
            return navigated;
        }

        private void BackInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            OnBackRequested();
            args.Handled = true;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                if (e.Parameter == null || string.IsNullOrEmpty(e.Parameter as string)) return;
                operation = (ShareOperation)e.Parameter;
                //Get text data 
                if (operation.Data.Contains(StandardDataFormats.Text))
                {
                    string textFromShare = await operation.Data.GetTextAsync();
                    Pages.TextPage.Text = textFromShare;
                    ContentFrame.Navigate(typeof(Pages.TextPage));
                }
                //Get web link 
                else if (operation.Data.Contains(StandardDataFormats.WebLink))
                {
                    Uri uri = await operation.Data.GetWebLinkAsync();
                    Pages.TextPage.Text = uri.ToString();
                    ContentFrame.Navigate(typeof(Pages.TextPage));
                }
                //Get image 
                else if (operation.Data.Contains(StandardDataFormats.Bitmap))
                {
                    ContentFrame.Navigate(typeof(Pages.ImagePage));

                    //shareType.Text = "Bitmap";
                    //shareTitle.Text = operation.Data.Properties.Title;
                    //imgShareImage.Visibility = Visibility.Visible;
                    //tbShareData.Visibility = Visibility.Collapsed;

                    RandomAccessStreamReference imageStreamRef = await operation.Data.GetBitmapAsync();
                    IRandomAccessStreamWithContentType streamWithContentType = await imageStreamRef.OpenReadAsync();
                    WriteableBitmap bmp = new WriteableBitmap(1, 1);
                    await bmp.SetSourceAsync(streamWithContentType);
                    //imgShareImage.Source = bmp;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "ERROR").ShowAsync();
            }
        }

        private void NvMain_Loaded(object sender, RoutedEventArgs e)
        {
            nvMain.IsBackEnabled = ContentFrame.CanGoBack;

            nvMain.Header = nvMain.PaneTitle;

            ContentFrame.Navigate(typeof(Pages.TextPage));
            ContentFrame.Navigated += NvMain_Navigated;
        }

        private void NvMain_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            OnBackRequested();
        }

        private void NvMain_Navigated(object sender, NavigationEventArgs e)
        {
            nvMain.IsBackEnabled = ContentFrame.CanGoBack;

            //if (ContentFrame.SourcePageType == typeof(Pages.SettingsPage))
            //{
            //    nvMain.SelectedItem = nvMain.SettingsItem as NavigationViewItem;
            //}
            //else
            //{
            //    Dictionary<Type, string> lookup = new Dictionary<Type, string>()
            //    {
            //        {typeof(Pages.TextPage), "PageText"},
            //        {typeof(Pages.QRCodePage), "PageQR"},
            //        {typeof(Pages.ImagePage), "PageImage"},
            //        {typeof(Pages.CharsetPage), "PageWifi"},
            //        {typeof(Pages.CharsetPage), "PageBarcodet"},
            //        {typeof(Pages.CharsetPage), "PageCharset"}
            //    };

            //    string stringTag = lookup[ContentFrame.SourcePageType];

            //    // set the new SelectedItem  
            //    foreach (NavigationViewItemBase item in nvMain.MenuItems)
            //    {
            //        if (item is NavigationViewItem && item.Tag.Equals(stringTag))
            //        {
            //            item.IsSelected = true;
            //            break;
            //        }
            //    }
            //}
        }

        private void NvMain_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            ////先判断是否选中了setting
            //if (args.IsSettingsInvoked)
            //{
            //    ContentFrame.Navigate(typeof(Pages.SettingsPage));
            //}
            //else
            //{
            //    //选中项的内容
            //    switch (args.InvokedItem)
            //    {
            //        case "Home":
            //            ContentFrame.Navigate(typeof(Pages.TextPage));
            //            break;
            //        case "Text":
            //            ContentFrame.Navigate(typeof(Pages.TextPage));
            //            break;
            //        case "Charset":
            //            ContentFrame.Navigate(typeof(Pages.CharsetPage));
            //            break;
            //        case "Image":
            //            ContentFrame.Navigate(typeof(Pages.ImagePage));
            //            break;
            //        case "QR Code":
            //            ContentFrame.Navigate(typeof(Pages.QRCodePage));
            //            break;
            //        default:
            //            ContentFrame.Navigate(typeof(Pages.TextPage));
            //            break;
            //    }
            //}
        }

        private void NvMain_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            //先判断是否选中了setting
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(Pages.SettingsPage));
            }
            else
            {
                var item = args.SelectedItem as NavigationViewItem;
                switch (item.Name)
                {
                    case "nvItemText":
                        ContentFrame.Navigate(typeof(Pages.TextPage));
                        break;
                    case "nvItemQRCode":
                        ContentFrame.Navigate(typeof(Pages.QRCodePage));
                        break;
                    case "nvItemImage":
                        ContentFrame.Navigate(typeof(Pages.ImagePage));
                        break;
                    case "nvItemWifi":
                        ContentFrame.Navigate(typeof(Pages.WifiQRPage));
                        break;
                    case "nvItemBarCode":
                        ContentFrame.Navigate(typeof(Pages.BarcodePage));
                        break;
                    case "nvItemCharset":
                        ContentFrame.Navigate(typeof(Pages.CharsetPage));
                        break;
                    default:
                        ContentFrame.Navigate(typeof(Pages.TextPage));
                        break;
                }
            }
        }

        private void NvMain_Click(object sender, TappedRoutedEventArgs e)
        {
            //var tag = (string)(sender as NavigationViewItem).Tag;
            ////选中项的内容
            //switch (tag)
            //{
            //    case "PageText":
            //        ContentFrame.Navigate(typeof(Pages.TextPage));
            //        break;
            //    case "PageQR":
            //        ContentFrame.Navigate(typeof(Pages.QRCodePage));
            //        break;
            //    case "PageImage":
            //        ContentFrame.Navigate(typeof(Pages.ImagePage));
            //        break;
            //    case "PageWifi":
            //        ContentFrame.Navigate(typeof(Pages.WifiQRPage));
            //        break;
            //    case "PageBarcode":
            //        ContentFrame.Navigate(typeof(Pages.BarcodePage));
            //        break;
            //    case "PageCharset":
            //        ContentFrame.Navigate(typeof(Pages.CharsetPage));
            //        break;
            //    default:
            //        ContentFrame.Navigate(typeof(Pages.TextPage));
            //        break;
            //}
        }

        private void NvMore_Click(object sender, RoutedEventArgs e)
        {
            if (this.RequestedTheme == ElementTheme.Dark)
                this.RequestedTheme = ElementTheme.Light;
            else if (this.RequestedTheme == ElementTheme.Light)
                this.RequestedTheme = ElementTheme.Dark;
            else
                this.RequestedTheme = ElementTheme.Default;
        }

    }
}
