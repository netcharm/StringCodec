using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace StringCodec.UWP.Common
{
    public sealed partial class CommonQRDialog : ContentDialog
    {
        public int SelectedIndex
        {
            get { return pivot.SelectedIndex; }
            set { pivot.SelectedIndex = value; }
        }

        public object SelectedItem
        {
            get { return pivot.SelectedItem; }
            set { pivot.SelectedItem = value; }
        }

        public object Items
        {
            get { return pivot.Items; }
        }

        private string result = string.Empty;
        public string ResultText
        {
            get { return result; }
        }

        public CommonQRDialog()
        {
            this.InitializeComponent();
            this.RequestedTheme = (ElementTheme)Settings.Get("AppTheme", ElementTheme.Default);
        }

        private void Dialog_Loaded(object sender, RoutedEventArgs e)
        {
            var selecteditem = pivot.SelectedItem as PivotItem;
            pivot.Items.Clear();
            pivot.Items.Add(selecteditem);
            pivot.Title = string.Empty;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            //pivot.SelectedItem
            result = "";
            var item = pivot.SelectedItem as PivotItem;
            if (item == piLink)
            {
                var content = string.IsNullOrEmpty(edLinkContent.Text) ? string.Empty : $"/{edLinkContent.Text}";
                result = $"{edLinkUrl.Text}{content.Replace("//", "", StringComparison.CurrentCultureIgnoreCase)}";
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void edLinkUrl_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                sender.ItemsSource = Utils.GetSuggestion(edLinkUrl.Text);
            }
        }

        private void edLinkUrl_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                // User selected an item from the suggestion list, take an action on it here.
            }
            else
            {
                // Use args.QueryText to determine what to do.
            }
        }

        private void edLinkUrl_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Set sender.Text. You can use args.SelectedItem to build your text string.

        }

    }
}
