using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class ProgressDialog : ContentDialog
    {

        public double Value
        {
            get { return Bar.Value; }
            set
            {
                Bar.Value = value;
                Percent.Text = $"{value}%";
            }
        }

        private IProgress<int> progress;

        public ProgressDialog()
        {
            this.InitializeComponent();

            progress = new Progress<int>(percent => {
                Bar.Value = percent;
                Percent.Text = $"{percent}%";
            });
        }

        public void Report(int value)
        {
            progress.Report(value);
        }

        public void Report(double value)
        {
            progress.Report(Convert.ToInt32(value));
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
