using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace StringCodec.UWP.Common
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class CommonQRContentPage : Page
    {
        private List<PivotItem> CommonItems = new List<PivotItem>();

        private PivotItem selectedItem = null;
        public PivotItem SelectedItem
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value as PivotItem;
                pivot.SelectedItem = selectedItem;
                for(int i=0; i<CommonItems.Count;i++)
                {
                    if (CommonItems[i] == selectedItem)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }
        }

        private int selectedIndex = -1;
        public int SelectedIndex
        {
            get
            {
                return selectedIndex;
            }
            set
            {
                selectedIndex = value;
                selectedItem = CommonItems[value];
                pivot.SelectedIndex = value;
            }
        }

        public ItemCollection Items
        {
            get { return pivot.Items; }
        }

        private string result = string.Empty;
        public string ResultText
        {
            get { return result; }
        }


        public CommonQRContentPage()
        {
            this.InitializeComponent();

            CommonItems.Clear();
            foreach (var item in pivot.Items)
            {
                CommonItems.Add(item as PivotItem);
            }
            pivot.Items.Clear();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public string GetContents()
        {
            //pivot.SelectedItem
            result = "";
            //var item = pivot.SelectedItem as PivotItem;
            var item = selectedItem as PivotItem;
            if (item == piLink)
            {
                var content = string.IsNullOrEmpty(edLinkContent.Text) ? string.Empty : $"/{edLinkContent.Text}";
                result = $"{edLinkUrl.Text}{content.Replace("//", "", StringComparison.CurrentCultureIgnoreCase)}";
            }
            else if (item == piWifi)
            {
                //WIFI:S:wifissid;P:wifipass;T:WPA/WPA2;H:1;
                var hidden = edWifiHidden.IsChecked == true ? "1" : string.Empty;
                var encypto = "WPA";
                switch (edWifiEncypto.SelectedIndex)
                {
                    case 0:
                        encypto = "WPA";
                        break;
                    case 1:
                        encypto = "WEP";
                        break;
                    case 2:
                        encypto = "None";
                        break;
                    default:
                        break;
                }
                result = $"WIFI:S:{edWifiSSID.Text};P:{edWifiPass.Text};T:{encypto};H:{hidden};";
            }
            else if (item == piMail)
            {
                result = $"mailto:{edMailTo.Text.Trim()}?subject={edMailSubject.Text.TrimEnd()}&body={edMailContent.Text.TrimEnd()}";
            }
            else if (item == piGeo)
            {
                result = string.Empty;
            }
            else if (item == piContact)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"BEGIN:VCARD");
                sb.AppendLine($"VERSION:3.0");
                sb.AppendLine($"FN:{edContactNameFirst.Text.Trim()} {edContactNameMiddle.Text.Trim()} {edContactNameLast.Text.Trim()}");
                sb.AppendLine($"TEL;CELL;VOICE:{edContactMobile.Text.Trim()}");
                sb.AppendLine($"TEL;HOME;VOICE:{edContactTelHome.Text.Trim()}");
                sb.AppendLine($"EMAIL:{edContactMail.Text.Trim()}");
                sb.AppendLine($"URL:{edContactUrl.Text.Trim()}");
                sb.AppendLine($"ADR;HOME;POSTAL:{edContactAdrHome.Text.Trim()};{edContactAdrWorkPostal.Text.Trim()}");
                sb.AppendLine($"ORG:{edContactOrg.Text.Trim()}");
                sb.AppendLine($"TITLE:{edContactTitle.Text.Trim()}");
                sb.AppendLine($"TEL;WORK;VOICE:{edContactTelWork.Text.Trim()}");
                sb.AppendLine($"ADR;WORK;POSTAL:{edContactAdrWork.Text.Trim()};{edContactAdrWorkPostal.Text.Trim()}");
                sb.AppendLine($"NOTE:{edContactNote.Text.Trim()}");
                sb.AppendLine($"END:VCARD");
                result = string.Join("\n", sb);
            }
            else if (item == piEvent)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"BEGIN:VCALENDAR");
                sb.AppendLine($"VERSION:2.0");
                sb.AppendLine($"BEGIN:VEVENT");
                sb.AppendLine($"UID:{edEventUid.Text.Trim()}");
                sb.AppendLine($"LOCATION;CHARSET=utf-8:{edEventLocation.Text.Trim()}");
                sb.AppendLine($"ADR;TYPE=WORK:{edEventAddress.Text.Trim()}");
                sb.AppendLine($"DTSTART:{edEventDTStart.Date.ToString()}");
                sb.AppendLine($"DTEND:{edEventDTEnd.Date.ToString()}");
                //sb.AppendLine($"RRULE:FREQ={};INTERVAL={};BYMONTH={};BYMONTHDAY={}");
                sb.AppendLine($"URL:{edEventLink.Text.Trim()}");
                sb.AppendLine($"GEO:{edEventGeoLon.Text.Trim()};{edEventGeoLat.Text.Trim()}");
                sb.AppendLine($"SUMMARY;CHARSET=utf-8:{edEventSummary.Text.Trim()}");
                sb.AppendLine($"DESCRIPTION;CHARSET=utf-8:{edEventDesc.Text.Trim()}");
                sb.AppendLine($"END:VEVENT");
                sb.AppendLine($"END:VCALENDAR");
                result = string.Join("\n", sb);
            }
            return (result);
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
