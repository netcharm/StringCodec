using StringCodec.UWP.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private ObservableCollection<KeyValuePair<string, string>> TableS2T = new ObservableCollection<KeyValuePair<string, string>>();
        private ObservableCollection<KeyValuePair<string, string>> TableT2S = new ObservableCollection<KeyValuePair<string, string>>();

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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TableS2T is ObservableCollection<KeyValuePair<string, string>>)
                    TableS2T.Clear();
                else
                    TableS2T = new ObservableCollection<KeyValuePair<string, string>>();
                foreach (var kv in Common.TongWen.Core.CustomPS2T)
                {
                    var k = Common.TongWen.Core.ConvertPhrase(kv.Key, Common.TongWen.ChineseConversionDirection.TraditionalToSimplified);
                    TableS2T.Add(new KeyValuePair<string, string>(k, kv.Value));
                }
                lvS2T.ItemsSource = TableS2T;

                if (TableT2S is ObservableCollection<KeyValuePair<string, string>>)
                    TableT2S.Clear();
                else
                    TableT2S = new ObservableCollection<KeyValuePair<string, string>>();
                foreach (var kv in Common.TongWen.Core.CustomPT2S)
                {
                    var k = Common.TongWen.Core.ConvertPhrase(kv.Key, Common.TongWen.ChineseConversionDirection.SimplifiedToTraditional);
                    TableT2S.Add(new KeyValuePair<string, string>(k, kv.Value));
                }
                lvT2S.ItemsSource = TableT2S;
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessage("ERROR".T());
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                ex.Message.ShowMessage("ERROR".T());
            }

        }

        private void UITheme_Toggled(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                ex.Message.ShowMessage("ERROR".T());
            }
        }

        private void UILanguage_Chganged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var lang = "Default";
                switch (UILanguageSwitch.SelectedIndex)
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
                Settings.SetUILanguage(lang, UILanguageSwitch.IsEnabled);
                if (rootPage != null && UILanguageSwitch.IsEnabled)
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
            catch (Exception ex)
            {
                ex.Message.ShowMessage("ERROR".T());
            }
        }

        private async void CleanTempFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Utils.CleanTemporary();
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessage("ERROR".T());
            }
        }

        #region Custom Phrase routines
        private async void CustomPhraseAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var k = CustomPhraseBefore.Text.Trim();
                var v = CustomPhraseAfter.Text.Trim();
                var kv = new KeyValuePair<string, string>(k, v);

                if (CustomPhraseList.SelectedItem == CustomPhraseListS2T)
                {
                    var kn = Common.TongWen.Core.ConvertPhrase(kv.Key, Common.TongWen.ChineseConversionDirection.SimplifiedToTraditional);
                    if (Common.TongWen.Core.CustomPS2T.ContainsKey(kn))
                    {
                        var result = await "PhraseExistsConfirm".T().ShowConfirm("CONFIRM".T());
                        IEnumerable<KeyValuePair<string, string>> items = TableS2T.Where(o => o.Key == k);
                        if (items.Count() > 0)
                        {
                            var idx = TableS2T.IndexOf(items.First());
                            lvS2T.SelectedIndex = idx;
                            lvS2T.ScrollIntoView(items.First());

                            if(result == DialogResult.YES)
                            {
                                TableS2T.Insert(lvS2T.SelectedIndex, kv);
                                TableS2T.RemoveAt(lvS2T.SelectedIndex);
                                Common.TongWen.Core.CustomPS2T[kn] = v;
                            }
                        }
                    }
                    else
                    {
                        TableS2T.Add(kv);
                        Common.TongWen.Core.CustomPS2T.Add(kn, kv.Value);
                    }
                }
                else if (CustomPhraseList.SelectedItem == CustomPhraseListT2S)
                {
                    var kn = Common.TongWen.Core.ConvertPhrase(kv.Key, Common.TongWen.ChineseConversionDirection.TraditionalToSimplified);
                    if (Common.TongWen.Core.CustomPT2S.ContainsKey(kn))
                    {
                        var result = await "PhraseExistsConfirm".T().ShowConfirm("CONFIRM".T());
                        IEnumerable<KeyValuePair<string, string>> items = TableT2S.Where(o => o.Key == k);
                        if (items.Count() > 0)
                        {
                            var idx = TableT2S.IndexOf(items.First());
                            lvT2S.SelectedIndex = idx;
                            lvT2S.ScrollIntoView(items.First());

                            if (result == DialogResult.YES)
                            {
                                TableT2S.Insert(lvT2S.SelectedIndex, kv);
                                TableT2S.RemoveAt(lvT2S.SelectedIndex);
                                Common.TongWen.Core.CustomPT2S[kn] = v;
                            }
                        }
                    }
                    else
                    {
                        TableT2S.Add(kv);
                        Common.TongWen.Core.CustomPT2S.Add(kn, kv.Value);
                    }
                }
                Common.TongWen.Core.SaveCustomPhrase();
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessage("ERROR".T());
            }
        }

        private void CustomPhraseEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CustomPhraseList.SelectedItem == CustomPhraseListS2T)
                {
                    if (lvS2T.SelectedItem != null)
                    {
                        var k = CustomPhraseBefore.Text.Trim();
                        var v = CustomPhraseAfter.Text.Trim();
                        var kv = new KeyValuePair<string, string>(k, v);

                        var ko = Common.TongWen.Core.ConvertPhrase(TableS2T[lvS2T.SelectedIndex].Key, Common.TongWen.ChineseConversionDirection.SimplifiedToTraditional);
                        var kn = Common.TongWen.Core.ConvertPhrase(k, Common.TongWen.ChineseConversionDirection.SimplifiedToTraditional);

                        TableS2T.Insert(lvS2T.SelectedIndex, kv);
                        TableS2T.RemoveAt(lvS2T.SelectedIndex);

                        if (Common.TongWen.Core.CustomPS2T.ContainsKey(kn))
                            Common.TongWen.Core.CustomPS2T[kn] = v;
                        else
                        {
                            if (Common.TongWen.Core.CustomPS2T.ContainsKey(ko))
                                Common.TongWen.Core.CustomPS2T.Remove(ko);
                            Common.TongWen.Core.CustomPS2T.TryAdd(kn, v);
                        }
                    }
                }
                else if (CustomPhraseList.SelectedItem == CustomPhraseListT2S)
                {
                    if (lvT2S.SelectedItem != null)
                    {
                        var k = CustomPhraseBefore.Text.Trim();
                        var v = CustomPhraseAfter.Text.Trim();
                        var kv = new KeyValuePair<string, string>(k, v);

                        var ko = Common.TongWen.Core.ConvertPhrase(TableT2S[lvT2S.SelectedIndex].Key, Common.TongWen.ChineseConversionDirection.TraditionalToSimplified);
                        var kn = Common.TongWen.Core.ConvertPhrase(k, Common.TongWen.ChineseConversionDirection.TraditionalToSimplified);

                        TableT2S.Insert(lvT2S.SelectedIndex, kv);
                        TableT2S.RemoveAt(lvT2S.SelectedIndex);

                        if (Common.TongWen.Core.CustomPT2S.ContainsKey(kn))
                            Common.TongWen.Core.CustomPT2S[kn] = v;
                        else
                        {
                            if(Common.TongWen.Core.CustomPT2S.ContainsKey(ko))
                                Common.TongWen.Core.CustomPT2S.Remove(ko);
                            Common.TongWen.Core.CustomPT2S.TryAdd(kn, v);
                        }
                    }
                }
                Common.TongWen.Core.SaveCustomPhrase();
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessage("ERROR".T());
            }
        }

        private void CustomPhraseRemove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CustomPhraseList.SelectedItem == CustomPhraseListS2T)
                {
                    if (lvS2T.SelectedItem != null)
                    {
                        var k = Common.TongWen.Core.ConvertPhrase(TableS2T[lvS2T.SelectedIndex].Key, Common.TongWen.ChineseConversionDirection.TraditionalToSimplified);

                        TableS2T.RemoveAt(lvS2T.SelectedIndex);
                        if (Common.TongWen.Core.CustomPS2T.ContainsKey(k))
                            Common.TongWen.Core.CustomPS2T.Remove(k);

                        CustomPhraseBefore.Text = string.Empty;
                        CustomPhraseAfter.Text = string.Empty;
                    }
                }
                else if (CustomPhraseList.SelectedItem == CustomPhraseListT2S)
                {
                    if (lvT2S.SelectedItem != null)
                    {
                        var k = Common.TongWen.Core.ConvertPhrase(TableT2S[lvT2S.SelectedIndex].Key, Common.TongWen.ChineseConversionDirection.TraditionalToSimplified);

                        TableT2S.RemoveAt(lvT2S.SelectedIndex);
                        if(Common.TongWen.Core.CustomPT2S.ContainsKey(k))
                            Common.TongWen.Core.CustomPT2S.Remove(k);

                        CustomPhraseBefore.Text = string.Empty;
                        CustomPhraseAfter.Text = string.Empty;
                    }
                }
                Common.TongWen.Core.SaveCustomPhrase();
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessage("ERROR".T());
            }
        }

        private void CustomPhraseClear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CustomPhraseList.SelectedItem == CustomPhraseListS2T)
                {
                    TableS2T.Clear();
                    Common.TongWen.Core.CustomPS2T.Clear();

                    CustomPhraseBefore.Text = string.Empty;
                    CustomPhraseAfter.Text = string.Empty;
                }
                else if (CustomPhraseList.SelectedItem == CustomPhraseListT2S)
                {
                    TableT2S.Clear();
                    Common.TongWen.Core.CustomPT2S.Clear();

                    CustomPhraseBefore.Text = string.Empty;
                    CustomPhraseAfter.Text = string.Empty;
                }
                Common.TongWen.Core.SaveCustomPhrase();
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessage("ERROR".T());
            }
        }

        private void CustomPhraseImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CustomPhraseList.SelectedItem == CustomPhraseListS2T)
                {
                }
                else if (CustomPhraseList.SelectedItem == CustomPhraseListT2S)
                {
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessage("ERROR".T());
            }
        }

        private void CustomPhraseExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CustomPhraseList.SelectedItem == CustomPhraseListS2T)
                {
                }
                else if (CustomPhraseList.SelectedItem == CustomPhraseListT2S)
                {
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessage("ERROR".T());
            }
        }

        private void PhraseList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CustomPhraseList.SelectedItem == CustomPhraseListS2T)
                {
                    if (lvS2T.SelectedItem != null)
                    {
                        CustomPhraseBefore.Text = TableS2T[lvS2T.SelectedIndex].Key;
                        CustomPhraseAfter.Text = TableS2T[lvS2T.SelectedIndex].Value;
                    }
                }
                else if (CustomPhraseList.SelectedItem == CustomPhraseListT2S)
                {
                    if (lvT2S.SelectedItem != null)
                    {
                        CustomPhraseBefore.Text = TableT2S[lvT2S.SelectedIndex].Key;
                        CustomPhraseAfter.Text = TableT2S[lvT2S.SelectedIndex].Value;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessage("ERROR".T());
            }
        }

        private void CustomPhraseList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CustomPhraseList.SelectedItem == CustomPhraseListS2T)
                {
                    if (lvS2T.SelectedItem != null)
                    {
                        CustomPhraseBefore.Text = TableS2T[lvS2T.SelectedIndex].Key;
                        CustomPhraseAfter.Text = TableS2T[lvS2T.SelectedIndex].Value;
                    }
                    else
                    {
                        CustomPhraseBefore.Text = string.Empty;
                        CustomPhraseAfter.Text = string.Empty;
                    }
                }
                else if (CustomPhraseList.SelectedItem == CustomPhraseListT2S)
                {
                    if (lvT2S.SelectedItem != null)
                    {
                        CustomPhraseBefore.Text = TableT2S[lvT2S.SelectedIndex].Key;
                        CustomPhraseAfter.Text = TableT2S[lvT2S.SelectedIndex].Value;
                    }
                    else
                    {
                        CustomPhraseBefore.Text = string.Empty;
                        CustomPhraseAfter.Text = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessage("ERROR".T());
            }
        }
        #endregion

    }
}
