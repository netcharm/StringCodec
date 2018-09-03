using StringCodec.UWP.Common;
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
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI.Popups;
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
    public sealed partial class CharsetPage : Page
    {
        private Encoding CURRENT_SRCENC = Encoding.Default;
        private Encoding CURRENT_DSTENC = Encoding.Default;
        private int CURRENT_TREEDEEP = 2;
        private bool CURRENT_RENAME_REPLACE = false;
        private bool CURRENT_CONVERT_ORIGINAL = true;
        private bool IsDropped = false;

        private Dictionary<TreeViewNode, IStorageItem> flist = new Dictionary<TreeViewNode, IStorageItem>();
        private byte[] fcontent = null;

        private void ConvertFrom(TreeViewNode node, Encoding enc)
        {
            if (node.HasChildren)
            {
                foreach (var child in node.Children)
                {
                    ConvertFrom(child, enc);
                }
            }
            node.Content = flist[node].Name.ConvertFrom(CURRENT_SRCENC, true);
        }

        private void ConvertFrom(TreeView tree, Encoding enc)
        {
            if (tree.RootNodes.Count > 0)
            {
                foreach (var root in tree.RootNodes)
                {
                    ConvertFrom(root, enc);
                }
            }
        }

        private async Task<bool> RefreshFolder(TreeViewNode node)
        {
            bool result = false;
            if (flist.ContainsKey(node))
            {
                node.Children.Clear();

                var item = flist[node];
                var folder = item as StorageFolder;

                var queryOptions = new QueryOptions();
                queryOptions.FolderDepth = FolderDepth.Shallow;

                var queryFolders = folder.CreateFolderQueryWithOptions(queryOptions);
                var sfolders = await queryFolders.GetFoldersAsync();
                foreach (var sfolder in sfolders)
                {
                    var ret = await AddTo(node, sfolder, node.Depth);
                }

                var queryFiles = folder.CreateFileQueryWithOptions(queryOptions);
                var sfiles = await queryFiles.GetFilesAsync();
                foreach (var file in sfiles)
                {
                    var fnode = new TreeViewNode() { Content = file.Name.ConvertFrom(CURRENT_SRCENC, true) };
                    node.Children.Add(fnode);
                    flist.Add(fnode, file);
                }
                result = true;
            }
            return (result);
        }

        private async Task<bool> AddTo(TreeViewNode node, IStorageItem item, int deeper = -1)
        {
            if (deeper > CURRENT_TREEDEEP) return(true);

            var queryOptions = new QueryOptions();
            queryOptions.FolderDepth = FolderDepth.Shallow;

            if (item is StorageFolder)
            {
                var folder = item as StorageFolder;
                //StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace($"FolderToken_{folder.FolderRelativeId.Replace("\\", "_")}_{folder.Name}", folder);
                //StorageApplicationPermissions.FutureAccessList.AddOrReplace($"FolderToken_{folder.FolderRelativeId.Replace("\\", "_")}_{folder.Name}", folder);
                var root = new TreeViewNode() { Content = folder.Name.ConvertFrom(CURRENT_SRCENC, true) };
                root.HasUnrealizedChildren = true;
                flist.Add(root, folder);

                //var sfolders = await folder.GetFoldersAsync();
                var queryFolders = folder.CreateFolderQueryWithOptions(queryOptions);
                var sfolders = await queryFolders.GetFoldersAsync();
                foreach (var sfolder in sfolders)
                {
                    var ret = await AddTo(root, sfolder, deeper++);
                }

                var queryFiles = folder.CreateFileQueryWithOptions(queryOptions);
                var sfiles = await queryFiles.GetFilesAsync();
                foreach (var file in sfiles)
                {
                    //StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace($"FileToken_{file.FolderRelativeId.Replace("\\", "_")}_{file.Name}", file);
                    //StorageApplicationPermissions.FutureAccessList.AddOrReplace($"FileToken_{file.FolderRelativeId.Replace("\\", "_")}_{file.Name}", file);
                    var fnode = new TreeViewNode() { Content = file.Name.ConvertFrom(CURRENT_SRCENC, true) };
                    root.Children.Add(fnode);
                    flist.Add(fnode, file);
                }

                node.Children.Add(root);
            }
            else if (item is StorageFile)
            {
                var file = item as StorageFile;
                //StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace($"FileToken_{file.FolderRelativeId.Replace("\\", "_")}_{file.Name}", file);
                //StorageApplicationPermissions.FutureAccessList.AddOrReplace($"FileToken_{file.FolderRelativeId.Replace("\\", "_")}_{file.Name}", file);
                var fnode = new TreeViewNode() { Content = file.Name.ConvertFrom(CURRENT_SRCENC, true) };
                node.Children.Add(fnode);
                flist.Add(fnode, file);
            }
            return (true);
        }

        private async Task<bool> AddTo(TreeView tree, IStorageItem item)
        {
            int deeper = -1;
            var queryOptions = new QueryOptions();
            queryOptions.FolderDepth = FolderDepth.Shallow;

            if (item is StorageFolder)
            {
                var folder = item as StorageFolder;
                //StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace($"FolderToken_{folder.FolderRelativeId.Replace("\\", "_")}_{folder.Name}", folder);
                //StorageApplicationPermissions.FutureAccessList.AddOrReplace($"FolderToken_{folder.FolderRelativeId.Replace("\\", "_")}_{folder.Name}", folder);
                var root = new TreeViewNode() { Content = folder.Name.ConvertFrom(CURRENT_SRCENC, true) };
                root.HasUnrealizedChildren = true;
                root.IsExpanded = true;
                flist.Add(root, folder);

                //var sfolders = await folder.GetFoldersAsync();
                var queryFolders = folder.CreateFolderQueryWithOptions(queryOptions);
                var sfolders = await queryFolders.GetFoldersAsync();
                foreach (var sfolder in sfolders)
                {
                    var ret = await AddTo(root, sfolder, deeper++);
                }

                var queryFiles = folder.CreateFileQueryWithOptions(queryOptions);
                var sfiles = await queryFiles.GetFilesAsync();
                foreach (var file in sfiles)
                {
                    //StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace($"FileToken_{file.FolderRelativeId.Replace("\\", "_")}_{file.Name}", file);
                    //StorageApplicationPermissions.FutureAccessList.AddOrReplace($"FileToken_{file.FolderRelativeId.Replace("\\", "_")}_{file.Name}", file);
                    var fnode = new TreeViewNode() { Content = file.Name.ConvertFrom(CURRENT_SRCENC, true) };
                    root.Children.Add(fnode);
                    flist.Add(fnode, file);
                }

                tree.RootNodes.Add(root);
            }
            else if (item is StorageFile)
            {
                var file = item as StorageFile;
                //StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace($"FileToken_{file.FolderRelativeId.Replace("\\", "_")}_{file.Name}", file);
                //StorageApplicationPermissions.FutureAccessList.AddOrReplace($"FileToken_{file.FolderRelativeId.Replace("\\", "_")}_{file.Name}", file);
                //CachedFileManager.DeferUpdates(file);
                var fnode = new TreeViewNode() { Content = file.Name.ConvertFrom(CURRENT_SRCENC, true) };
                tree.RootNodes.Add(fnode);
                flist.Add(fnode, file);
            }

            return (true);
        }

        private async Task<bool> AddTo(TreeView tree, List<IStorageItem> items)
        {
            if (items == null) return(false);

            foreach (var item in items)
            {
                await AddTo(tree, item);
            }
            return (true);
        }

        private async Task<bool> RenameFile()
        {
            bool result = false;
            try
            {
                if (IsDropped) return(result);
                if (tvFiles.SelectedNodes.Count <= 0) return (result);

                var cnode = tvFiles.SelectedNodes[0];
                if (!flist.ContainsKey(cnode)) return (result);

                var f = flist[cnode];

                if (CURRENT_RENAME_REPLACE)
                    await f.RenameAsync(f.Name.ConvertFrom(CURRENT_SRCENC, true), NameCollisionOption.ReplaceExisting);
                else
                    await f.RenameAsync(f.Name.ConvertFrom(CURRENT_SRCENC, true), NameCollisionOption.GenerateUniqueName);

                cnode.Content = f.Name;
                if (cnode.HasChildren)
                {
                    await RefreshFolder(cnode);
                }
                result = true;
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        private async Task<bool> ConvertFileContent()
        {
            bool result = false;

            if (IsDropped) return(result);
            if (tvFiles.SelectedNodes.Count <= 0) return (result);

            var cnode = tvFiles.SelectedNodes[0];
            if (!flist.ContainsKey(cnode)) return (result);

            var f = flist[cnode];

            if (f is StorageFile)
            {
                var file = f as StorageFile;
                result = await Utils.ConvertFile(file, CURRENT_SRCENC, CURRENT_DSTENC, CURRENT_CONVERT_ORIGINAL);

                #region old codes
                //if (Utils.text_ext.Contains(file.FileType))
                //{
                //    IBuffer buffer = await FileIO.ReadBufferAsync(file);
                //    DataReader reader = DataReader.FromBuffer(buffer);
                //    byte[] fileContent = new byte[reader.UnconsumedBufferLength];
                //    reader.ReadBytes(fileContent);
                //    var fs = fileContent.ToString(CURRENT_SRCENC);

                //    byte[] BOM = CURRENT_DSTENC.GetBOM();
                //    byte[] fa = CURRENT_DSTENC.GetBytes(fs);
                //    fa = BOM.Concat(fa).ToArray();

                //    if (CURRENT_CONVERT_ORIGINAL)
                //    {
                //        await FileIO.WriteBytesAsync(file, fa);
                //        //using (var ws = await file.OpenAsync(FileAccessMode.ReadWrite))
                //        //{
                //        //    DataWriter writer = new DataWriter(ws.GetOutputStreamAt(0));
                //        //    writer.WriteBytes(BOM);
                //        //    writer.WriteBytes(fa);
                //        //    await ws.FlushAsync();
                //        //}
                //    }
                //    else
                //    {
                //        FileSavePicker fsp = new FileSavePicker();
                //        fsp.SuggestedStartLocation = PickerLocationId.Unspecified;
                //        fsp.SuggestedFileName = file.Name;
                //        fsp.SuggestedSaveFile = file;
                //        StorageFile TargetFile = await fsp.PickSaveFileAsync();
                //        if(TargetFile != null)
                //        {
                //            StorageApplicationPermissions.MostRecentlyUsedList.Add(TargetFile, TargetFile.Name);
                //            if (StorageApplicationPermissions.FutureAccessList.Entries.Count >= 1000)
                //                StorageApplicationPermissions.FutureAccessList.Remove(StorageApplicationPermissions.FutureAccessList.Entries.Last().Token);
                //            StorageApplicationPermissions.FutureAccessList.Add(TargetFile, TargetFile.Name);

                //            // 在用户完成更改并调用CompleteUpdatesAsync之前，阻止对文件的更新
                //            CachedFileManager.DeferUpdates(TargetFile);
                //            await FileIO.WriteBytesAsync(TargetFile, fa);
                //        }
                //    }
                //}
                #endregion
            }
            return (result);
        }

        public CharsetPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;

            optSrcAuto.IsChecked = true;
            optDstAuto.IsChecked = true;

            optActionRename.IsChecked = CURRENT_RENAME_REPLACE;
            optActionConvert.IsChecked = CURRENT_CONVERT_ORIGINAL;

            optFolderDeep2.IsChecked = true;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void OptSrc_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] btns = new ToggleMenuFlyoutItem[] { optSrcAuto, optSrcAscii, optSrcBIG5, optSrcGBK, optSrcJIS, optSrcUnicode, optSrcUTF8 };
            foreach(var btn in btns)
            {
                if (sender == btn) btn.IsChecked = true;
                else btn.IsChecked = false;
            }
            var enc = sender as ToggleMenuFlyoutItem;
            var ENC_NAME = enc.Name.Substring(6).ToUpper();
            if (string.Equals(ENC_NAME, "UTF8", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_SRCENC = Encoding.UTF8;
            else if (string.Equals(ENC_NAME, "Unicode", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_SRCENC = Encoding.Unicode;
            else if (string.Equals(ENC_NAME, "GBK", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_SRCENC = Encoding.GetEncoding("GBK");
            else if (string.Equals(ENC_NAME, "BIG5", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_SRCENC = Encoding.GetEncoding("BIG5");
            else if (string.Equals(ENC_NAME, "JIS", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_SRCENC = Encoding.GetEncoding("Shift-JIS");
            else if (string.Equals(ENC_NAME, "ASCII", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_SRCENC = Encoding.ASCII;
            else
                CURRENT_SRCENC = Encoding.Default;

            ConvertFrom(tvFiles, CURRENT_SRCENC);
            if(fcontent != null && fcontent is byte[])
                edSrc.Text = fcontent.ToString(CURRENT_SRCENC);
        }

        private void OptDst_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] btns = new ToggleMenuFlyoutItem[] { optDstAuto, optDstAscii, optDstBIG5, optDstGBK, optDstJIS, optDstUnicode, optDstUTF8 };
            foreach (var btn in btns)
            {
                if (sender == btn) btn.IsChecked = true;
                else btn.IsChecked = false;
            }
            var enc = sender as ToggleMenuFlyoutItem;
            var ENC_NAME = enc.Name.Substring(6).ToUpper();
            if (string.Equals(ENC_NAME, "UTF8", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_DSTENC = Encoding.UTF8;
            else if (string.Equals(ENC_NAME, "Unicode", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_DSTENC = Encoding.Unicode;
            else if (string.Equals(ENC_NAME, "GBK", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_DSTENC = Encoding.GetEncoding("GBK");
            else if (string.Equals(ENC_NAME, "BIG5", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_DSTENC = Encoding.GetEncoding("BIG5");
            else if (string.Equals(ENC_NAME, "JIS", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_DSTENC = Encoding.GetEncoding("Shift-JIS");
            else if (string.Equals(ENC_NAME, "ASCII", StringComparison.CurrentCultureIgnoreCase))
                CURRENT_DSTENC = Encoding.ASCII;
            else
                CURRENT_SRCENC = Encoding.Default;
        }

        private void edSrc_TextChanged(object sender, TextChangedEventArgs e)
        {
            edInfo.Text = $"{"Count".T()}: {edSrc.Text.Length}";
        }

        private void Opt_Click(object sender, RoutedEventArgs e)
        {
            if(sender == optWrapText)
            {
                if((sender as AppBarToggleButton).IsChecked == true)
                    edSrc.TextWrapping = TextWrapping.Wrap;
                else edSrc.TextWrapping = TextWrapping.NoWrap;
            }
        }

        private void OptFolderDeep_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] btns = new ToggleMenuFlyoutItem[] { optFolderDeepF, optFolderDeep0, optFolderDeep1, optFolderDeep2, optFolderDeep3, optFolderDeep4 };
            foreach (var btn in btns)
            {
                if (sender == btn) btn.IsChecked = true;
                else btn.IsChecked = false;
            }
            var deep = sender as ToggleMenuFlyoutItem;
            var DEEP = deep.Name.Substring(13).ToUpper();
            if(DEEP.Equals("F", StringComparison.CurrentCultureIgnoreCase))
            {
                CURRENT_TREEDEEP = 255;
            }
            else
            {
                CURRENT_TREEDEEP = Convert.ToInt32(DEEP);
            }
            cmdBar.IsOpen = false;
        }

        private async void TreeViewAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ActionRename || sender == TreeActionRename)
            {
                await RenameFile();
            }
            else if (sender == ActionRenameAll)
            {

            }
            else if( sender == optActionRename)
            {
                CURRENT_RENAME_REPLACE = optActionRename.IsChecked;
            }
            else if (sender == ActionConvert || sender == TeeeActionConvert)
            {
                await ConvertFileContent();
            }
            else if (sender == ActionConvertAll)
            {

            }
            else if (sender == optActionConvert)
            {
                CURRENT_CONVERT_ORIGINAL = optActionConvert.IsChecked;
            }
        }

        private async void TreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            TreeViewNode item = args.InvokedItem as TreeViewNode;
            tvFiles.SelectedNodes.Clear();
            tvFiles.SelectedNodes.Add(item);
            if (flist.ContainsKey(item))
            {
                var file = flist[item];
                if (file != null)
                {
                    if(file is StorageFile)                        
                    {
                        try
                        {
                            var f = file as StorageFile;
                            if (Utils.text_ext.Contains(f.FileType))
                            {
                                //edSrc.Text = await FileIO.ReadTextAsync(f, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                                IBuffer buffer = await FileIO.ReadBufferAsync(f);
                                DataReader reader = DataReader.FromBuffer(buffer);
                                byte[] fileContent = new byte[reader.UnconsumedBufferLength];
                                reader.ReadBytes(fileContent);
                                fcontent = (byte[])fileContent.Clone();
                                if (fcontent != null && fcontent is byte[])
                                    edSrc.Text = fcontent.ToString(CURRENT_SRCENC);
                                //string text = Encoding.Default.GetString(fileContent, 0, fileContent.Length);
                                //edSrc.Text = text;
                                args.Handled = true;
                            }
                            else
                            {
                                edSrc.Text = string.Empty;
                                fcontent = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            await new MessageDialog(ex.Message, "ERROR".T()).ShowAsync();
                        }
                    }
                }
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            switch (btn.Name)
            {
                case "btnOpenFolder":
                    FolderPicker fdp = new FolderPicker();
                    fdp.SuggestedStartLocation = PickerLocationId.Desktop;
                    fdp.FileTypeFilter.Add("*");
                    var folder = await fdp.PickSingleFolderAsync();
                    if (folder != null)
                    {
                        flist.Clear();
                        tvFiles.RootNodes.Clear();
                        var ret = await AddTo(tvFiles, folder);
                        IsDropped = false;

                    }
                    break;
                case "btnOpenFile":
                    FileOpenPicker fop = new FileOpenPicker();
                    fop.SuggestedStartLocation = PickerLocationId.Desktop;
                    foreach (var ext in Utils.text_ext)
                    {
                        fop.FileTypeFilter.Add(ext);
                    }
                    var files = await fop.PickMultipleFilesAsync();
                    if (files != null)
                    {
                        flist.Clear();
                        tvFiles.RootNodes.Clear();

                        foreach (var file in files)
                        {
                            var ret = await AddTo(tvFiles, file);
                            IsDropped = false;
                        }
                    }
                    break;
                case "btnRename":
                    await RenameFile();
                    break;
                case "btnConvert":
                    await ConvertFileContent();
                    break;
                case "btnShare":
                    if (tvFiles.SelectedNodes.Count > 0)
                    {
                        var f = tvFiles.SelectedNodes[0];
                        if (flist.ContainsKey(f))
                        {
                            if(flist[f] is StorageFile)
                                Utils.Share(flist[f] as StorageFile);
                        }
                        //Utils.Share(edSrc.Text);
                    }
                    break;
                default:
                    break;
            }
        }

        #region Drag/Drop routines
        private bool canDrop = true;
        private async void OnDragEnter(object sender, DragEventArgs e)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Drag Enter Sender:{sender}");
#endif
            //System.Diagnostics.Debug.WriteLine("drag enter.." + DateTime.Now);
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var deferral = e.GetDeferral(); // since the next line has 'await' we need to defer event processing while we wait
                try
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        canDrop = true;
                        var item = items[0];
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Drag count:{items.Count}, {item.Name}");
#endif
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine(ex.Message);
#endif
                }
                deferral.Complete();
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Drag Over Sender:{sender}");
#endif
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                if (canDrop) e.AcceptedOperation = DataPackageOperation.Copy;
            }
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            // 需要异步拖放时记得获取Deferral对象
            //var def = e.GetDeferral();

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    var sItems = new List<IStorageItem>();
                    foreach(var item in items)
                    {
                        sItems.Add(item);
                    }
                    var ret = await AddTo(tvFiles, sItems);
                    IsDropped = true;
                }
            }

            //def.Complete();
        }
        #endregion

        private void TreeNode_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Right Tapped Sender:{sender}");
            System.Diagnostics.Debug.WriteLine($"Right Tapped Name  :{e.OriginalSource}");
#endif
        }

    }
}
