using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
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
        private StorageFile file = null;
        public StorageFile InputFile
        {
            get { return (file); }
            set
            {
                file = value;
                if (value is StorageFile)
                {
                    edFileName.Text = file.Name;
                }
                else
                {
                    edFileName.Text = string.Empty;
                }
            }
        }

        public HashCalcDialog()
        {
            this.InitializeComponent();
            this.RequestedTheme = Settings.GetTheme();

            progressHashFile.Visibility = Visibility.Collapsed;

            try
            {
                var opts = (Settings.Get("HashSelected") as string).Split(",");
                var hashlist = opts.Select(o => (TextCodecs.HASH)Enum.Parse(typeof(TextCodecs.HASH), o));
                foreach (var h in hashlist)
                {
                    if (h == TextCodecs.HASH.CRC32) chkHashCRC32.IsChecked = true;
                    else if (h == TextCodecs.HASH.MD4) chkHashMD4.IsChecked = true;
                    else if (h == TextCodecs.HASH.MD5) chkHashMD5.IsChecked = true;
                    else if (h == TextCodecs.HASH.SHA1) chkHashSHA1.IsChecked = true;
                    else if (h == TextCodecs.HASH.SHA256) chkHashSHA256.IsChecked = true;
                    else if (h == TextCodecs.HASH.SHA384) chkHashSHA384.IsChecked = true;
                    else if (h == TextCodecs.HASH.SHA512) chkHashSHA512.IsChecked = true;
                }

                symHashCRC32.SetValue(ToolTipService.ToolTipProperty, "HashCopy".T());
                symHashMD4.SetValue(ToolTipService.ToolTipProperty, "HashCopy".T());
                symHashMD5.SetValue(ToolTipService.ToolTipProperty, "HashCopy".T());
                symHashSHA1.SetValue(ToolTipService.ToolTipProperty, "HashCopy".T());
                symHashSHA256.SetValue(ToolTipService.ToolTipProperty, "HashCopy".T());
                symHashSHA384.SetValue(ToolTipService.ToolTipProperty, "HashCopy".T());
                symHashSHA512.SetValue(ToolTipService.ToolTipProperty, "HashCopy".T());

                symHashPaste.SetValue(ToolTipService.ToolTipProperty, "HashPaste".T());
                symHashCompare.SetValue(ToolTipService.ToolTipProperty, "HashCompare".T());

            }
            catch(Exception ex)
            {
                ex.Message.T().ShowMessage("ERROR".T());
            }
        }

        private async void FileHashDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            try
            {
                if (file is StorageFile)
                {
                    var hashlist = new List<TextCodecs.HASH>();

                    var props = await file.GetBasicPropertiesAsync();
                    if (props.Size > 1024 * 1024 * 5)
                    {
                        progressHashFile.Visibility = Visibility.Visible;
                        progressHashFile.IsActive = true;
                    }                    

                    using (var ms = await file.OpenStreamForReadAsync())
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        if (chkHashCRC32.IsChecked.Value)
                        {
                            edHashCRC32.Text = TextCodecs.CalcCRC32(ms);
                            hashlist.Add(TextCodecs.HASH.CRC32);
                        }
                        else edHashCRC32.Text = string.Empty;

                        ms.Seek(0, SeekOrigin.Begin);
                        if (chkHashMD4.IsChecked.Value)
                        {
                            edHashMD4.Text = TextCodecs.CalcMD4(ms);
                            hashlist.Add(TextCodecs.HASH.MD4);
                        }
                        else edHashMD4.Text = string.Empty;

                        ms.Seek(0, SeekOrigin.Begin);
                        if (chkHashMD5.IsChecked.Value)
                        {
                            edHashMD5.Text = TextCodecs.CalcMD5(ms);
                            hashlist.Add(TextCodecs.HASH.MD5);
                        }
                        else edHashMD5.Text = string.Empty;

                        ms.Seek(0, SeekOrigin.Begin);
                        if (chkHashSHA1.IsChecked.Value)
                        {
                            edHashSHA1.Text = TextCodecs.CalcSHA1(ms);
                            hashlist.Add(TextCodecs.HASH.SHA1);
                        }
                        else edHashSHA1.Text = string.Empty;

                        ms.Seek(0, SeekOrigin.Begin);
                        if (chkHashSHA256.IsChecked.Value)
                        {
                            edHashSHA256.Text = TextCodecs.CalcSHA256(ms);
                            hashlist.Add(TextCodecs.HASH.SHA256);
                        }
                        else edHashSHA256.Text = string.Empty;

                        ms.Seek(0, SeekOrigin.Begin);
                        if (chkHashSHA384.IsChecked.Value)
                        {
                            edHashSHA384.Text = TextCodecs.CalcSHA384(ms);
                            hashlist.Add(TextCodecs.HASH.SHA384);
                        }
                        else edHashSHA384.Text = string.Empty;

                        ms.Seek(0, SeekOrigin.Begin);
                        if (chkHashSHA512.IsChecked.Value)
                        {
                            edHashSHA512.Text = TextCodecs.CalcSHA512(ms);
                            hashlist.Add(TextCodecs.HASH.SHA512);
                        }
                        else edHashSHA512.Text = string.Empty;

                    }

                    Compare();

                    var opts = hashlist.Select(h => h.ToString());
                    Settings.Set("HashSelected", string.Join(",", opts));
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

        private async void FileHashDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            try
            {
                if (file is StorageFile)
                {
                    var props = await file.GetBasicPropertiesAsync();

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"{"File".T()}  : {file.Name}");
                    sb.AppendLine($"{"Size".T()}  : {props.Size} {"Bytes".T()}");
                    if(!string.IsNullOrEmpty(edHashCRC32.Text))
                        sb.AppendLine($"CRC32 : {edHashCRC32.Text}");
                    if (!string.IsNullOrEmpty(edHashMD4.Text))
                        sb.AppendLine($"MD4   : {edHashMD4.Text}");
                    if (!string.IsNullOrEmpty(edHashMD5.Text))
                        sb.AppendLine($"MD5   : {edHashMD5.Text}");
                    if (!string.IsNullOrEmpty(edHashSHA1.Text))
                        sb.AppendLine($"SHA1  : {edHashSHA1.Text}");
                    if (!string.IsNullOrEmpty(edHashSHA256.Text))
                        sb.AppendLine($"SHA256: {edHashSHA256.Text}");
                    if (!string.IsNullOrEmpty(edHashSHA384.Text))
                        sb.AppendLine($"SHA384: {edHashSHA384.Text}");
                    if (!string.IsNullOrEmpty(edHashSHA512.Text))
                        sb.AppendLine($"SHA512: {edHashSHA512.Text}");

                    Utils.SetClipboard(sb.ToString());
                    Utils.Share(sb.ToString());
                }
            }
            catch(Exception ex)
            {
                ex.Message.T().ShowMessage("ERROR".T());
            }
        }

        private void FileHashDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }

        private async void BtnBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var file_n = await Utils.OpenFile();
            if (file_n is StorageFile)
            {
                file = file_n;
                edFileName.Text = file.Name;
                edFileName.Focus(FocusState.Pointer);
                //btnBrowseFile.Focus(FocusState.Unfocused);

                edHashCRC32.Text = string.Empty;
                edHashMD4.Text = string.Empty;
                edHashMD5.Text = string.Empty;
                edHashSHA1.Text = string.Empty;
                edHashSHA256.Text = string.Empty;
                edHashSHA384.Text = string.Empty;
                edHashSHA512.Text = string.Empty;

                Compare();
            }
        }

        private async void SymHash_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender == symHashMD4)
            {
                if (!string.IsNullOrEmpty(edHashMD4.Text)) Utils.SetClipboard(edHashMD4.Text);
            }
            else if (sender == symHashMD5)
            {
                if (!string.IsNullOrEmpty(edHashMD5.Text)) Utils.SetClipboard(edHashMD5.Text);
            }
            else if (sender == symHashSHA1)
            {
                if (!string.IsNullOrEmpty(edHashSHA1.Text)) Utils.SetClipboard(edHashSHA1.Text);
            }
            else if (sender == symHashSHA256)
            {
                if (!string.IsNullOrEmpty(edHashSHA256.Text)) Utils.SetClipboard(edHashSHA256.Text);
            }
            else if (sender == symHashSHA384)
            {
                if (!string.IsNullOrEmpty(edHashSHA384.Text)) Utils.SetClipboard(edHashSHA384.Text);
            }
            else if (sender == symHashSHA512)
            {
                if (!string.IsNullOrEmpty(edHashSHA512.Text)) Utils.SetClipboard(edHashSHA512.Text);
            }
            else if (sender == symHashCRC32)
            {
                if (!string.IsNullOrEmpty(edHashCRC32.Text)) Utils.SetClipboard(edHashCRC32.Text);
            }
            else if (sender == symHashPaste)
            {
                edHashCompare.Text = await Utils.GetClipboard(string.Empty);
                Compare();
            }
            else if (sender == symHashCompare)
            {
                Compare();
            }
        }

        private void SymHashCompare_TextChanged(object sender, TextChangedEventArgs e)
        {
            Compare();
        }

        private void Compare()
        {
            //edHashCompare.Text = await Utils.GetClipboard(string.Empty);
            if (!string.IsNullOrEmpty(edHashCompare.Text))
            {
                var vs = edHashCompare.Text.Trim();

                if (vs.Equals(edHashCRC32.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)) symHashCRC32Compare.Visibility = Visibility.Visible;
                else symHashCRC32Compare.Visibility = Visibility.Collapsed;

                if (vs.Equals(edHashMD4.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)) symHashMD4Compare.Visibility = Visibility.Visible;
                else symHashMD4Compare.Visibility = Visibility.Collapsed;

                if (vs.Equals(edHashMD5.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)) symHashMD5Compare.Visibility = Visibility.Visible;
                else symHashMD5Compare.Visibility = Visibility.Collapsed;


                if (vs.Equals(edHashSHA1.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)) symHashSHA1Compare.Visibility = Visibility.Visible;
                else symHashSHA1Compare.Visibility = Visibility.Collapsed;


                if (vs.Equals(edHashSHA256.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)) symHashSHA256Compare.Visibility = Visibility.Visible;
                else symHashSHA256Compare.Visibility = Visibility.Collapsed;


                if (vs.Equals(edHashSHA384.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)) symHashSHA384Compare.Visibility = Visibility.Visible;
                else symHashSHA384Compare.Visibility = Visibility.Collapsed;


                if (vs.Equals(edHashSHA512.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)) symHashSHA512Compare.Visibility = Visibility.Visible;
                else symHashSHA512Compare.Visibility = Visibility.Collapsed;
            }
            else
            {
                symHashCRC32Compare.Visibility = Visibility.Collapsed;
                symHashMD4Compare.Visibility = Visibility.Collapsed;
                symHashMD5Compare.Visibility = Visibility.Collapsed;
                symHashSHA1Compare.Visibility = Visibility.Collapsed;
                symHashSHA256Compare.Visibility = Visibility.Collapsed;
                symHashSHA384Compare.Visibility = Visibility.Collapsed;
                symHashSHA512Compare.Visibility = Visibility.Collapsed;
            }
        }

        #region Drag/Drop events routines
#if DEBUG
        private async void OnDragEnter(object sender, DragEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("drag enter.." + DateTime.Now);
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var deferral = e.GetDeferral(); // since the next line has 'await' we need to defer event processing while we wait
                try
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        var item = items[0] as StorageFile;
                        string filename = item.Name;
                        string extension = item.FileType.ToLower();
                        System.Diagnostics.Debug.WriteLine(filename);

                        e.AcceptedOperation = DataPackageOperation.Copy;
                        e.DragUIOverride.Caption = "File".T();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
                deferral.Complete();
            }
        }
#else
        private void OnDragEnter(object sender, DragEventArgs e)
        {
        }
#endif

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = "File".T();
                //e.DragUIOverride.IsGlyphVisible = true;
                //e.DragUIOverride.IsCaptionVisible = false;
                //e.DragUIOverride.IsContentVisible = false;
            }
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            // 记得获取Deferral对象
            var deferral = e.GetDeferral();
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    try
                    {
                        file = items[0] as StorageFile;
                        edFileName.Text = file.Name;
                    }
                    catch(Exception ex)
                    {
                        ex.Message.T().ShowMessage("ERROR".T());
                    }
                }
            }
            deferral.Complete();
        }
        #endregion

    }
}
