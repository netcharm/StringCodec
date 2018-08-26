using StringCodec.UWP.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

        private void SetTheme(ElementTheme theme, bool save = true)
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
            //ApplicationData.Current.LocalSettings.Values["AppTheme"] = (int)RequestedTheme;
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
                rootPage = e.Parameter as MainPage;
                nvSwitchTheme.IsTapEnabled = false;
                if (rootPage.RequestedTheme == ElementTheme.Dark)
                {
                    nvSwitchTheme.IsOn = true;
                }
                else if (rootPage.RequestedTheme == ElementTheme.Light)
                {
                    nvSwitchTheme.IsOn = false;
                }
                nvSwitchTheme.IsTapEnabled = true;
            }
        }

        private void NvTheme_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender == nvSwitchTheme)
            {
                var theme = sender as ToggleSwitch;
                if (!theme.IsTapEnabled) return;
                if (theme.IsOn)
                    SetTheme(ElementTheme.Dark);
                else
                    SetTheme(ElementTheme.Light);
            }
        }
    }
}
