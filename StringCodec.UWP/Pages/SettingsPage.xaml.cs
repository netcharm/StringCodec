using StringCodec.UWP.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.UI;
using Windows.UI.ViewManagement;
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
    public sealed partial class SettingsPage : Page
    {
        MainPage rootPage = null;

        private void SetUITheme(ElementTheme theme, bool save = true)
        {
            if (rootPage == null) return;

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
            #endregion
        }

        public SettingsPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null && e.Parameter is MainPage)
            {
                #region Display Theme Toggle with Current Theme 
                rootPage = e.Parameter as MainPage;
                UIThemeSwitch.IsTapEnabled = false;
                if (rootPage.RequestedTheme == ElementTheme.Dark)
                {
                    UIThemeSwitch.IsOn = true;
                }
                else if (rootPage.RequestedTheme == ElementTheme.Light)
                {
                    UIThemeSwitch.IsOn = false;
                }
                UIThemeSwitch.IsTapEnabled = true;
                #endregion

                #region Display UI Language with Current Language
                var lang = (string)Settings.Get("UILanguage", string.Empty);
                UILanguageSwitch.IsEnabled = false;
                var langIndex = Settings.LoadUILanguage(lang);
                if (UILanguageSwitch.Items != null && UILanguageSwitch.Items.Count > 0 &&
                    langIndex >= 0 && langIndex < UILanguageSwitch.Items.Count)
                    UILanguageSwitch.SelectedIndex = langIndex;
                else
                    UILanguageSwitch.SelectedIndex = -1;
                UILanguageSwitch.IsEnabled = true;
                #endregion
            }
        }

        private void UITheme_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender == UIThemeSwitch)
            {
                var theme = sender as ToggleSwitch;
                if (!theme.IsTapEnabled) return;
                if (theme.IsOn)
                    SetUITheme(ElementTheme.Dark);
                else
                    SetUITheme(ElementTheme.Light);
            }
        }

        private async void UILanguage_Chganged(object sender, SelectionChangedEventArgs e)
        {
            var lang = "Default";
            switch(UILanguageSwitch.SelectedIndex)
            {
                case 0:
                    lang = "";
                    break;
                case 1:
                    lang = "en-US";
                    break;
                case 2:
                    lang = "zh-Hans";
                    break;
                case 3:
                    lang = "zh-Hant";
                    break;
                case 4:
                    lang = "ja";
                    break;
                default:
                    lang = "";
                    break;
            }           
            await Settings.SetUILanguage(lang, UILanguageSwitch.IsEnabled);
            if(rootPage != null && UILanguageSwitch.IsEnabled)
            {
                return;

                //AppResources.Reload();

                //
                // if NavigationCacheMode set to "Required"
                //
                //ApplicationLanguages.PrimaryLanguageOverride = lang;
                //await Task.Delay(300);
                //Frame rootFrame = Window.Current.Content as Frame;
                //rootFrame.Content = null;
                //rootFrame = null;
                //rootFrame = new Frame();
                //rootFrame.Navigate(typeof(MainPage), null);
                //Window.Current.Content = rootFrame;

                //rootPage.NavigationCacheMode = NavigationCacheMode.Disabled;
                //rootPage.Frame.CacheSize = 0;
                //rootPage.Frame.Navigate(rootPage.GetType());
                //rootPage.Container.CacheSize = 0;
                //rootPage.Container.Navigate(this.GetType(), rootPage);
            }
        }

        private async void CleanTempFiles_Click(object sender, RoutedEventArgs e)
        {
            await Utils.CleanTemporary();
        }
    }
}
