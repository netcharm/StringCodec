using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
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
    public sealed partial class ColorDialog : ContentDialog
    {
        private Color color_selected;
        public Color Color
        {
            get {
                return color_selected;
            }
            set {
                color_selected = value;
                ColorPicker.Color = value;
            }
        }
        public bool Alpha
        {
            get { return ColorPicker.IsAlphaEnabled; }
            set { ColorPicker.IsAlphaEnabled = value; }
        }
        public bool Hex
        {
            get { return ColorPicker.IsHexInputVisible; }
            set { ColorPicker.IsHexInputVisible = value; }
        }

        public ColorDialog()
        {
            this.InitializeComponent();
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("AppTheme"))
                this.RequestedTheme = (ElementTheme)ApplicationData.Current.LocalSettings.Values["AppTheme"];
        }

        private void ColorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            color_selected = ColorPicker.Color;
        }

        private void ColorDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }

    }
}
