using StringCodec.UWP.Common;
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
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
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
        private string[] image_ext = new string[] { ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff", ".gif" };
        //private ShareOperation operation;
        //private Utils utils = new Utils();

        private void SetTheme(ElementTheme theme, bool save = true)
        {
            //remove the solid-colored backgrounds behind the caption controls and system back button
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = (Color)Resources["SystemBaseHighColor"];
            //titleBar.ForegroundColor = (Color)Resources["SystemBaseHighColor"];
            //titleBar.ForegroundColor = Colors.Transparent;

            //if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
            //{
            //    titleBar.ButtonForegroundColor = Colors.White;
            //}
            //else
            //{
            //    titleBar.ButtonForegroundColor = Colors.Black;
            //}

            #region Set Theme & TitleBar button color
            this.RequestedTheme = theme;
            if (this.RequestedTheme == ElementTheme.Dark)
            {
                titleBar.ButtonForegroundColor = Colors.White;
            }
            else if (this.RequestedTheme == ElementTheme.Light)
            {
                titleBar.ButtonForegroundColor = Colors.Black;
            }
            if (save) Settings.Set("AppTheme", (int)RequestedTheme);
            //ApplicationData.Current.LocalSettings.Values["AppTheme"] = (int)RequestedTheme;
            #endregion
        }

        public MainPage()
        {
            this.InitializeComponent();

            NavigationCacheMode = NavigationCacheMode.Enabled;

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
            //KeyboardAccelerator GoBack = new KeyboardAccelerator();
            //GoBack.Key = VirtualKey.GoBack;
            //GoBack.Invoked += BackInvoked;
            //this.KeyboardAccelerators.Add(GoBack);

            //KeyboardAccelerator AltLeft = new KeyboardAccelerator();
            //AltLeft.Key = VirtualKey.Left;
            //AltLeft.Invoked += BackInvoked;
            //this.KeyboardAccelerators.Add(AltLeft);
            //// ALT routes here
            //AltLeft.Modifiers = VirtualKeyModifiers.Menu;
            #endregion

            #region 将应用扩展到标题栏
            //draw into the title bar
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

            //var theme = ElementTheme.Default;
            var theme = (ElementTheme)Settings.Get("AppTheme", ElementTheme.Default);
            //if (ApplicationData.Current.LocalSettings.Values.ContainsKey("AppTheme"))
            //    theme = (ElementTheme)ApplicationData.Current.LocalSettings.Values["AppTheme"];
            //nvSwitchTheme.IsTapEnabled = false;
            SetTheme(theme, false);
            //nvSwitchTheme.IsTapEnabled = true;
            #endregion

            ContentFrame.Navigated += NvMain_Navigated;

            nvMain.Header = nvMain.PaneTitle;
            ContentFrame.Navigate(typeof(Pages.TextPage), this);

            nvMain.IsPaneOpen = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            nvMain.IsPaneOpen = false;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                if (e.Parameter is ShareOperation)
                {
                    ShareOperation operation = (ShareOperation)e.Parameter;
                    //Get text data 
                    if (operation.Data.Contains(StandardDataFormats.Text))
                    {
                        string textFromShare = await operation.Data.GetTextAsync();
                        //Pages.TextPage.Text = textFromShare;
                        ContentFrame.Navigate(typeof(Pages.TextPage), textFromShare);
                    }
                    //Get web link 
                    else if (operation.Data.Contains(StandardDataFormats.WebLink))
                    {
                        Uri uri = await operation.Data.GetWebLinkAsync();
                        //Pages.QRCodePage.Text = uri.ToString();
                        ContentFrame.Navigate(typeof(Pages.QRCodePage), uri.ToString());
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
                        ContentFrame.Navigate(typeof(Pages.QRCodePage), bmp);
                    }
                    else if (operation.Data.Contains(StandardDataFormats.StorageItems))
                    {
                        var files = await operation.Data.GetStorageItemsAsync();
                        if (files.Count > 0)
                        {
                            //StorageFile file = await StorageFile.GetFileFromPathAsync(files[0].Path);
                            StorageFile file = (StorageFile)files[0];
                            if (file.IsOfType(StorageItemTypes.File))
                            {
                                var ext = file.FileType.ToLower();
                                if(image_ext.Contains(ext))
                                {
                                    var bitmapImage = new WriteableBitmap(1, 1);
                                    await bitmapImage.SetSourceAsync(await file.OpenAsync(FileAccessMode.Read));
                                    byte[] arr = WindowsRuntimeBufferExtensions.ToArray(bitmapImage.PixelBuffer, 0, (int)bitmapImage.PixelBuffer.Length);
                                    ContentFrame.Navigate(typeof(Pages.QRCodePage), bitmapImage);
                                }
                                else if(ext.Equals(".txt", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    var txt = await FileIO.ReadTextAsync(file);
                                    ContentFrame.Navigate(typeof(Pages.TextPage), txt);
                                }
                            }
                        }
                    }
                    //operation.ReportCompleted();
                }
                else
                {
                    ContentFrame.Navigate(typeof(Pages.TextPage), this);
                }

            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "ERROR").ShowAsync();
            }
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

        private void NvMain_Loaded(object sender, RoutedEventArgs e)
        {
            nvMain.IsBackEnabled = ContentFrame.CanGoBack;
        }

        private void NvMain_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            OnBackRequested();
        }

        private void NvMain_Navigated(object sender, NavigationEventArgs e)
        {
            nvMain.IsBackEnabled = ContentFrame.CanGoBack;
            if (ContentFrame.SourcePageType == typeof(Pages.SettingsPage))
            {
                nvMain.SelectedItem = nvMain.SettingsItem as NavigationViewItem;
            }
            else
            {
                if(e.SourcePageType == typeof(Pages.TextPage))
                {
                    nvMain.SelectedItem = nvItemText as NavigationViewItem;
                }
                else if (e.SourcePageType == typeof(Pages.QRCodePage))
                {
                    nvMain.SelectedItem = nvItemQRCode as NavigationViewItem;
                }
                else if (e.SourcePageType == typeof(Pages.ImagePage))
                {
                    nvMain.SelectedItem = nvItemImage as NavigationViewItem;
                }
                else if (e.SourcePageType == typeof(Pages.CommonQRPage))
                {
                    nvMain.SelectedItem = nvItemCommonQR as NavigationViewItem;
                }
                else if (e.SourcePageType == typeof(Pages.CommonOneDPage))
                {
                    nvMain.SelectedItem = nvItemCommonOneD as NavigationViewItem;
                }
                else if (e.SourcePageType == typeof(Pages.CharsetPage))
                {
                    nvMain.SelectedItem = nvItemCharset as NavigationViewItem;
                }
            }
            nvMain.Header = (nvMain.SelectedItem as NavigationViewItem).Content;

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
            //    ContentFrame.Navigate(typeof(Pages.SettingsPage), this);
            //}
            //else
            //{
            //}
        }

        private void NvMain_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            //先判断是否选中了setting
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(Pages.SettingsPage), this);
            }
            else
            {
                var item = args.SelectedItem as NavigationViewItem;
                switch (item.Name)
                {
                    case "nvItemText":
                        ContentFrame.Navigate(typeof(Pages.TextPage), this);
                        break;
                    case "nvItemQRCode":
                        ContentFrame.Navigate(typeof(Pages.QRCodePage), this);
                        break;
                    case "nvItemImage":
                        ContentFrame.Navigate(typeof(Pages.ImagePage), this);
                        break;
                    case "nvItemCommonQR":
                        ContentFrame.Navigate(typeof(Pages.CommonQRPage), this);
                        break;
                    case "nvItemCommonOneD":
                        ContentFrame.Navigate(typeof(Pages.CommonOneDPage), this);
                        break;
                    case "nvItemCharset":
                        ContentFrame.Navigate(typeof(Pages.CharsetPage), this);
                        break;
                    default:
                        ContentFrame.Navigate(typeof(Pages.TextPage), this);
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
            //        ContentFrame.Navigate(typeof(Pages.TextPage), this);
            //        break;
            //    case "PageQR":
            //        ContentFrame.Navigate(typeof(Pages.QRCodePage), this);
            //        break;
            //    case "PageImage":
            //        ContentFrame.Navigate(typeof(Pages.ImagePage), this);
            //        break;
            //    case "PageCommonQR":
            //        ContentFrame.Navigate(typeof(Pages.WifiQRPage), this);
            //        break;
            //    case "PageCommonOneD":
            //        ContentFrame.Navigate(typeof(Pages.BarcodePage), this);
            //        break;
            //    case "PageCharset":
            //        ContentFrame.Navigate(typeof(Pages.CharsetPage), this);
            //        break;
            //    default:
            //        ContentFrame.Navigate(typeof(Pages.TextPage), this);
            //        break;
            //}
        }

        private void NvMore_Click(object sender, RoutedEventArgs e)
        {
            if (this.RequestedTheme == ElementTheme.Dark)
                SetTheme(ElementTheme.Light);
            else if (this.RequestedTheme == ElementTheme.Light)
                SetTheme(ElementTheme.Dark);
            else
                SetTheme(ElementTheme.Default);

        }


    }
}
