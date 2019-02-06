using StringCodec.UWP.Common;
using StringCodec.UWP.Common.TongWen;
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

        #region Custom Phrase helper routines
        private async void CustomPhrase_Add(string k, string v, ChineseConversionDirection direction, bool confirm = false)
        {
            try
            {
                var kv = new KeyValuePair<string, string>(k, v);

                if (direction == ChineseConversionDirection.SimplifiedToTraditional)
                {
                    var kn = kv.Key.ToTraditional(true);
                    if (Common.TongWen.Core.CustomPS2T.ContainsKey(kn))
                    {
                        var result = DialogResult.YES;
                        if (confirm)
                            result = await "PhraseExistsConfirm".T().ShowConfirm("CONFIRM".T());
                        IEnumerable<KeyValuePair<string, string>> items = TableS2T.Where(o => o.Key == k);
                        if (items.Count() > 0)
                        {
                            var idx = TableS2T.IndexOf(items.First());
                            lvS2T.SelectedIndex = idx;
                            lvS2T.ScrollIntoView(items.First());

                            if (result == DialogResult.YES)
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
                else if (direction == ChineseConversionDirection.TraditionalToSimplified)
                {
                    var kn = kv.Key.ToSimplified(true);
                    if (Common.TongWen.Core.CustomPT2S.ContainsKey(kn))
                    {
                        var result = DialogResult.YES;
                        if (confirm)
                            result = await "PhraseExistsConfirm".T().ShowConfirm("CONFIRM".T());
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

        private async void CustomPhrase_Add(List<KeyValuePair<string, string>> kvs, ChineseConversionDirection direction, bool confirm = false)
        {
            try
            {
                foreach (var kv in kvs)
                {
                    if (direction == ChineseConversionDirection.SimplifiedToTraditional)
                    {
                        var kn = kv.Key.ToTraditional(true);
                        if (Common.TongWen.Core.CustomPS2T.ContainsKey(kn))
                        {
                            var result = DialogResult.YES;
                            if (confirm)
                                result = await "PhraseExistsConfirm".T().ShowConfirm("CONFIRM".T());
                            IEnumerable<KeyValuePair<string, string>> items = TableS2T.Where(o => o.Key == kv.Key);
                            if (items.Count() > 0)
                            {
                                var idx = TableS2T.IndexOf(items.First());

                                if (result == DialogResult.YES)
                                {
                                    TableS2T.Insert(idx, kv);
                                    TableS2T.RemoveAt(idx);
                                    Common.TongWen.Core.CustomPS2T[kn] = kv.Value;
                                }
                            }
                        }
                        else
                        {
                            TableS2T.Add(kv);
                            Common.TongWen.Core.CustomPS2T.Add(kn, kv.Value);
                        }
                    }
                    else if (direction == ChineseConversionDirection.TraditionalToSimplified)
                    {
                        var kn = kv.Key.ToSimplified(true);
                        if (Common.TongWen.Core.CustomPT2S.ContainsKey(kn))
                        {
                            var result = DialogResult.YES;
                            if (confirm)
                                result = await "PhraseExistsConfirm".T().ShowConfirm("CONFIRM".T());
                            IEnumerable<KeyValuePair<string, string>> items = TableT2S.Where(o => o.Key == kv.Key);
                            if (items.Count() > 0)
                            {
                                var idx = TableT2S.IndexOf(items.First());
                                if (result == DialogResult.YES)
                                {
                                    TableT2S.Insert(idx, kv);
                                    TableT2S.RemoveAt(idx);
                                    Common.TongWen.Core.CustomPT2S[kn] = kv.Value;
                                }
                            }
                        }
                        else
                        {
                            TableT2S.Add(kv);
                            Common.TongWen.Core.CustomPT2S.Add(kn, kv.Value);
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

        private void CustomPhrase_Edit(string k, string v, ChineseConversionDirection direction)
        {
            try
            {
                if (direction == ChineseConversionDirection.SimplifiedToTraditional)
                {
                    var kv = new KeyValuePair<string, string>(k, v);

                    var items = TableS2T.Where(o => o.Key == k);
                    if (items.Count() > 0)
                    {
                        var idx = TableS2T.IndexOf(items.First());
                        TableS2T.Insert(idx, kv);
                        TableS2T.RemoveAt(idx);
                    }

                    var ko = TableS2T[lvS2T.SelectedIndex].Key.ToTraditional(true);
                    var kn = k.ToTraditional(true);


                    if (Common.TongWen.Core.CustomPS2T.ContainsKey(kn))
                        Common.TongWen.Core.CustomPS2T[kn] = v;
                    else
                    {
                        if (Common.TongWen.Core.CustomPS2T.ContainsKey(ko))
                            Common.TongWen.Core.CustomPS2T.Remove(ko);
                        Common.TongWen.Core.CustomPS2T.TryAdd(kn, v);
                    }

                }
                else if (direction == ChineseConversionDirection.TraditionalToSimplified)
                {
                    if (lvT2S.SelectedItem != null)
                    {
                        var kv = new KeyValuePair<string, string>(k, v);

                        var items = TableT2S.Where(o => o.Key == k);
                        if (items.Count() > 0)
                        {
                            var idx = TableT2S.IndexOf(items.First());
                            TableT2S.Insert(idx, kv);
                            TableT2S.RemoveAt(idx);
                        }

                        var ko = TableT2S[lvT2S.SelectedIndex].Key.ToSimplified(true);
                        var kn = k.ToSimplified(true);

                        if (Common.TongWen.Core.CustomPT2S.ContainsKey(kn))
                            Common.TongWen.Core.CustomPT2S[kn] = v;
                        else
                        {
                            if (Common.TongWen.Core.CustomPT2S.ContainsKey(ko))
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

        private void CustomPhrase_Remove(string k, string v, ChineseConversionDirection direction)
        {
            try
            {
                if (direction == ChineseConversionDirection.SimplifiedToTraditional)
                {
                    var items = TableS2T.Where(o => o.Key == k);
                    if (items.Count() > 0)
                        TableS2T.Remove(items.First());

                    var kn = k.ToTraditional(true);
                    if (Common.TongWen.Core.CustomPS2T.ContainsKey(kn))
                        Common.TongWen.Core.CustomPS2T.Remove(kn);

                    CustomPhraseBefore.Text = string.Empty;
                    CustomPhraseAfter.Text = string.Empty;
                }
                else if (direction == ChineseConversionDirection.TraditionalToSimplified)
                {
                    if (lvT2S.SelectedItem != null)
                    {
                        var items = TableT2S.Where(o => o.Key == k);
                        if (items.Count() > 0)
                            TableT2S.Remove(items.First());

                        var kn = k.ToSimplified(true);
                        if (Common.TongWen.Core.CustomPT2S.ContainsKey(kn))
                            Common.TongWen.Core.CustomPT2S.Remove(kn);

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

        private void CustomPhrase_Refresh()
        {
            try
            {
                if (TableS2T is ObservableCollection<KeyValuePair<string, string>>)
                    TableS2T.Clear();
                else
                    TableS2T = new ObservableCollection<KeyValuePair<string, string>>();
                foreach (var kv in Common.TongWen.Core.CustomPS2T)
                {
                    var k = kv.Key.ToSimplified(true);
                    TableS2T.Add(new KeyValuePair<string, string>(k, kv.Value));
                }
                lvS2T.ItemsSource = TableS2T;

                if (TableT2S is ObservableCollection<KeyValuePair<string, string>>)
                    TableT2S.Clear();
                else
                    TableT2S = new ObservableCollection<KeyValuePair<string, string>>();
                foreach (var kv in Common.TongWen.Core.CustomPT2S)
                {
                    var k = kv.Key.ToTraditional(true);
                    TableT2S.Add(new KeyValuePair<string, string>(k, kv.Value));
                }
                lvT2S.ItemsSource = TableT2S;
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessage("ERROR".T());
            }
        }
        #endregion

        public SettingsPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                CustomPhrase_Refresh();
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
        private void CustomPhraseAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var k = CustomPhraseBefore.Text.Trim();
                var v = CustomPhraseAfter.Text.Trim();

                if (CustomPhraseList.SelectedItem == CustomPhraseListS2T)
                {
                    CustomPhrase_Add(k, v, ChineseConversionDirection.SimplifiedToTraditional, true);
                }
                else if (CustomPhraseList.SelectedItem == CustomPhraseListT2S)
                {
                    CustomPhrase_Add(k, v, ChineseConversionDirection.TraditionalToSimplified, true);
                }
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
                var k = CustomPhraseBefore.Text.Trim();
                var v = CustomPhraseAfter.Text.Trim();

                if (CustomPhraseList.SelectedItem == CustomPhraseListS2T)
                {
                    if (lvS2T.SelectedItem != null)
                    {
                        CustomPhrase_Edit(k, v, ChineseConversionDirection.SimplifiedToTraditional);
                    }
                }
                else if (CustomPhraseList.SelectedItem == CustomPhraseListT2S)
                {
                    if (lvT2S.SelectedItem != null)
                    {
                        CustomPhrase_Edit(k, v, ChineseConversionDirection.TraditionalToSimplified);
                    }
                }
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
                        var k = TableS2T[lvS2T.SelectedIndex].Key;
                        var v = TableS2T[lvS2T.SelectedIndex].Value;
                        CustomPhrase_Remove(k, v, ChineseConversionDirection.SimplifiedToTraditional);
                    }
                }
                else if (CustomPhraseList.SelectedItem == CustomPhraseListT2S)
                {
                    if (lvT2S.SelectedItem != null)
                    {
                        var k = TableT2S[lvT2S.SelectedIndex].Key;
                        var v = TableT2S[lvT2S.SelectedIndex].Value;
                        CustomPhrase_Remove(k, v, ChineseConversionDirection.TraditionalToSimplified);
                    }
                }
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

        private async void CustomPhraseImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CustomPhraseList.SelectedItem == CustomPhraseListS2T)
                {
                    var importItems = await Utils.OpenFiles(new string[] { ".csv" });
                    if (importItems.Count > 0)
                    {
                        List<KeyValuePair<string, string>> kvs = new List<KeyValuePair<string, string>>();
                        foreach (var item in importItems)
                        {
                            var lines = await Windows.Storage.FileIO.ReadLinesAsync(item);
                            foreach (var line in lines)
                            {
                                if (line.Trim().StartsWith("//") || line.StartsWith("#") || line.StartsWith(";")) continue;

                                var words = line.Trim().Split(",");
                                if (words.Length == 2)
                                {
                                    kvs.Add(new KeyValuePair<string, string>(words[0].Trim(), words[1].Trim()));
                                }
                            }
                        }
                        CustomPhrase_Add(kvs, ChineseConversionDirection.SimplifiedToTraditional, false);
                        lvS2T.ScrollIntoView(TableS2T.Last());
                    }
                }
                else if (CustomPhraseList.SelectedItem == CustomPhraseListT2S)
                {
                    var importItems = await Utils.OpenFiles(new string[] { ".csv" });
                    if (importItems.Count > 0)
                    {
                        List<KeyValuePair<string, string>> kvs = new List<KeyValuePair<string, string>>();
                        foreach (var item in importItems)
                        {
                            var lines = await Windows.Storage.FileIO.ReadLinesAsync(item);
                            foreach (var line in lines)
                            {
                                if (line.Trim().StartsWith("//") || line.StartsWith("#") || line.StartsWith(";")) continue;

                                var words = line.Trim().Split(",");
                                if (words.Length == 2)
                                {
                                    kvs.Add(new KeyValuePair<string, string>(words[0].Trim(), words[1].Trim()));
                                }
                            }
                        }
                        CustomPhrase_Add(kvs, ChineseConversionDirection.TraditionalToSimplified, false);
                        lvS2T.ScrollIntoView(TableT2S.Last());
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Message.ShowMessage("ERROR".T());
            }
        }

        private async void CustomPhraseExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CustomPhraseList.SelectedItem == CustomPhraseListS2T)
                {
                    List<string> contents = new List<string>();
                    foreach (var p in TableS2T)
                    {
                        contents.Add($"{p.Key}, {p.Value}");
                    }
                    var result = await Utils.ShowSaveDialog(string.Join(Environment.NewLine, contents), System.Text.Encoding.UTF8, ".csv");
                }
                else if (CustomPhraseList.SelectedItem == CustomPhraseListT2S)
                {
                    List<string> contents = new List<string>();
                    foreach (var p in TableT2S)
                    {
                        contents.Add($"{p.Key}, {p.Value}");
                    }
                    var result = await Utils.ShowSaveDialog(string.Join(Environment.NewLine, contents), System.Text.Encoding.UTF8, ".csv");
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
