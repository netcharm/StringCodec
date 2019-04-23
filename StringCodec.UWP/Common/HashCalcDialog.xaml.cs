using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
    public sealed partial class HashCalcDialog : ContentDialog
    {
        StorageFile file = null;

        public HashCalcDialog()
        {
            this.InitializeComponent();
            progressHashFile.Visibility = Visibility.Collapsed;
            this.RequestedTheme = Settings.GetTheme();
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            try
            {
                if (file is StorageFile)
                {
                    progressHashFile.Visibility = Visibility.Visible;
                    progressHashFile.IsActive = true;

                    using (var ms = await file.OpenStreamForReadAsync())
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        if (chkHashCRC32.IsChecked.Value)
                        {
                            edHashCRC32.Text = TextCodecs.CalcCRC32(ms);
                        }
                        else edHashCRC32.Text = string.Empty;

                        ms.Seek(0, SeekOrigin.Begin);
                        if (chkHashMD4.IsChecked.Value)
                        {
                            edHashMD4.Text = TextCodecs.CalcMD4(ms);
                        }
                        else edHashMD4.Text = string.Empty;

                        ms.Seek(0, SeekOrigin.Begin);
                        if (chkHashMD5.IsChecked.Value)
                        {
                            edHashMD5.Text = TextCodecs.CalcMD5(ms);
                        }
                        else edHashMD5.Text = string.Empty;

                        ms.Seek(0, SeekOrigin.Begin);
                        if (chkHashSHA1.IsChecked.Value)
                        {
                            edHashSHA1.Text = TextCodecs.CalcSHA1(ms);
                        }
                        else edHashSHA1.Text = string.Empty;

                        ms.Seek(0, SeekOrigin.Begin);
                        if (chkHashSHA256.IsChecked.Value)
                        {
                            edHashSHA256.Text = TextCodecs.CalcSHA256(ms);
                        }
                        else edHashSHA256.Text = string.Empty;

                        ms.Seek(0, SeekOrigin.Begin);
                        if (chkHashSHA384.IsChecked.Value)
                        {
                            edHashSHA384.Text = TextCodecs.CalcSHA384(ms);
                        }
                        else edHashSHA384.Text = string.Empty;

                        ms.Seek(0, SeekOrigin.Begin);
                        if (chkHashSHA512.IsChecked.Value)
                        {
                            edHashSHA512.Text = TextCodecs.CalcSHA512(ms);
                        }
                        else edHashSHA512.Text = string.Empty;

                    }
                }
            }
            catch(Exception ex)
            {
                ex.Message.T().ShowMessage("ERROR".T());
            }
            finally
            {
                progressHashFile.Visibility = Visibility.Collapsed;
            }            
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private async void BtnBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            file = await Utils.OpenFile();
            if (file is StorageFile)
                edFileName.Text = file.Name;
            else
                edFileName.Text = string.Empty;
        }

    }
}
