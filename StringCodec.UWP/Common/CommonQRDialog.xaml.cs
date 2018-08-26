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
            pivot.SelectedItem = selecteditem;
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
            else if (item == piWifi)
            {
                //WIFI:S:wifissid;P:wifipass;T:WPA/WPA2;H:1;
                var hidden = edWifiHidden.IsChecked == true ? "1" : string.Empty;
                result = $"WIFI:S:{edWifiSSID.Text};P:{edWifiPass.Text};T:{edWifiEncypto.SelectedValue};H:{hidden};";
            }
            else if(item == piMail)
            {
                result = $"mailto:{edMailTo.Text.Trim()}?subject={edMailSubject.Text.TrimEnd()}&body={edMailContent.Text.TrimEnd()}";
            }
            else if(item == piContact)
            {
                //BEGIN:VCARD
                //VERSION:3.0
                //N:FirstName
                //TEL:13901234567
                //EMAIL:abc@abc.com
                //ADR;TYPE=WORK:Office Address
                //ORG:OfficeName
                //URL:https://cn.bing.com
                //TEL;TYPE=WORK,VOICE:01088888888
                //TEL;TYPE=HOME,VOICE:02099999999
                //TITLE:Staff
                //ADR;TYPE=HOME:Home Address
                //NOTE:Note
                //END:VCARD

            }
            else if(item == piEvent)
            {
                //BEGIN:VCALENDAR
                //VERSION:2.0
                //BEGIN:VEVENT
                //SUMMARY;CHARSET=utf-8:summary
                //LOCATION;CHARSET=utf-8:hongkong
                //DTSTART:20180821T160000Z
                //DTEND:20180821T160000Z
                //END:VEVENT
                //END:VCALENDAR
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void edLinkUrl_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                sender.ItemsSource = Utils.LinkSuggestion(edLinkUrl.Text);
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
