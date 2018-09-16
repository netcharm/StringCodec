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
        private bool NODE_FILLING = false;

        //private FontFamily FontMDL2 = new FontFamily("Segoe MDL2 Assets");
        private FontIcon IconFolder = new FontIcon() { Glyph = "\uED43", FontFamily = new FontFamily("Segoe MDL2 Assets") };
        private FontIcon IconFile = new FontIcon() { Glyph = "\uF000", FontFamily = new FontFamily("Segoe MDL2 Assets") };

        private TreeViewNode target = null;

        private Dictionary<TreeViewNode, IStorageItem> flist = new Dictionary<TreeViewNode, IStorageItem>();
        private Dictionary<string, MyTreeViewNode> filenode = new Dictionary<string, MyTreeViewNode>();

        private byte[] fcontent = null;

        private bool CheckFlyoutValid()
        {
            bool result = false;
            bool IsSelected = TreeFiles.SelectedNodes.Count > 0;
            bool IsTextNode = target is MyTreeViewNode &&
                              (target as MyTreeViewNode).StorageItem is StorageFile &&
                              Utils.text_ext.Contains(((target as MyTreeViewNode).StorageItem as StorageFile).FileType);
            bool IsTextNodeSelected = IsSelected && 
                                      (TreeFiles.SelectedNodes[0] as MyTreeViewNode).StorageItem is StorageFile &&
                                      Utils.text_ext.Contains(((TreeFiles.SelectedNodes[0] as MyTreeViewNode).StorageItem as StorageFile).FileType);

            if (target is TreeViewNode || IsSelected)
            {
                TreeNodeActionRename.IsEnabled = true;
                if (IsTextNode || IsTextNodeSelected)
                    TreeNodeActionConvert.IsEnabled = true;
                else
                    TreeNodeActionConvert.IsEnabled = false;
            }
            else
            {
                TreeNodeActionRename.IsEnabled = false;
                TreeNodeActionConvert.IsEnabled = false;
            }

            if (IsSelected)
            {
                ActionRename.IsEnabled = true;
                if (IsTextNodeSelected)
                    ActionConvert.IsEnabled = true;
                else
                    ActionConvert.IsEnabled = false;
            }
            else
            {
                ActionRename.IsEnabled = false;
                ActionConvert.IsEnabled = false;
            }

            return (result);
        }

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
                await FillNode(node);

                //node.Children.Clear();

                //var item = flist[node];
                //var folder = item as StorageFolder;

                //var queryOptions = new QueryOptions();
                //queryOptions.FolderDepth = FolderDepth.Shallow;

                //var queryFolders = folder.CreateFolderQueryWithOptions(queryOptions);
                //var sfolders = await queryFolders.GetFoldersAsync();
                //foreach (var sfolder in sfolders)
                //{
                //    var ret = await AddTo(node, sfolder, node.Depth);
                //}

                //var queryFiles = folder.CreateFileQueryWithOptions(queryOptions);
                //var sfiles = await queryFiles.GetFilesAsync();
                //foreach (var file in sfiles)
                //{
                //    var fnode = new TreeViewNode() { Content = file.Name.ConvertFrom(CURRENT_SRCENC, true) };
                //    node.Children.Add(fnode);
                //    flist.Add(fnode, file);
                //}
                result = true;
            }
            return (result);
        }

        private async Task<bool> AddTo(TreeViewNode node, IStorageItem item, int deeper = -1)
        {
            if (deeper > CURRENT_TREEDEEP) return (true);

            var queryOptions = new QueryOptions();
            queryOptions.FolderDepth = FolderDepth.Shallow;

            if (item is StorageFolder)
            {
                var folder = item as StorageFolder;
                var root = new MyTreeViewNode() { Content = folder.Name.ConvertFrom(CURRENT_SRCENC, true) };
                root.Icon = IconFolder;
                root.HasUnrealizedChildren = true;
                flist.Add(root, folder);

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
                    var fnode = new MyTreeViewNode() { Content = file.Name.ConvertFrom(CURRENT_SRCENC, true) };
                    fnode.Icon = IconFile;
                    root.Children.Add(fnode);
                    flist.Add(fnode, file);
                }

                node.Children.Add(root);
            }
            else if (item is StorageFile)
            {
                var file = item as StorageFile;
                var fnode = new MyTreeViewNode() { Content = file.Name.ConvertFrom(CURRENT_SRCENC, true) };
                fnode.Icon = IconFile;
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
                var root = new MyTreeViewNode() { Content = folder.Name.ConvertFrom(CURRENT_SRCENC, true) };
                root.Icon = IconFolder;
                root.HasUnrealizedChildren = true;
                root.IsExpanded = true;
                flist.Add(root, folder);

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
                    var fnode = new MyTreeViewNode() { Content = file.Name.ConvertFrom(CURRENT_SRCENC, true) };
                    fnode.Icon = IconFile;
                    root.Children.Add(fnode);
                    flist.Add(fnode, file);
                }

                tree.RootNodes.Add(root);
            }
            else if (item is StorageFile)
            {
                var file = item as StorageFile;
                var fnode = new MyTreeViewNode() { Content = file.Name.ConvertFrom(CURRENT_SRCENC, true) };
                fnode.Icon = IconFile;
                tree.RootNodes.Add(fnode);
                flist.Add(fnode, file);
            }

            return (true);
        }

        private async Task<bool> AddTo(TreeView tree, List<IStorageItem> items)
        {
            if (items == null) return (false);

            foreach (var item in items)
            {
                await AddTo(tree, item);
            }
            return (true);
        }

        private async Task<bool> FillNode(TreeViewNode node)
        {
            bool result = false;
            NODE_FILLING = true;
            if (flist.ContainsKey(node))
            {
                var item = flist[node];

                var queryOptions = new QueryOptions(CommonFolderQuery.DefaultQuery)
                {
                    FolderDepth = FolderDepth.Shallow,
                };

                if (item is StorageFolder)
                {
                    var folder = item as StorageFolder;

                    var queryItems = folder.CreateItemQueryWithOptions(queryOptions);
                    var sItems = await queryItems.GetItemsAsync();

                    if (node.HasChildren) node.Children.Clear();
                    foreach (var sItem in sItems)
                    {
                        MyTreeViewNode fNode = filenode.ContainsKey(sItem.Path) ? filenode[sItem.Path] : new MyTreeViewNode() { Content = sItem.Name.ConvertFrom(CURRENT_SRCENC, true) };

                        if (sItem is StorageFolder)
                        {
                            fNode.Icon = IconFolder;
                            fNode.HasUnrealizedChildren = true;
                            fNode.IsExpanded = false;
                        }
                        else if (sItem is StorageFile)
                        {
                            fNode.Icon = IconFile;
                        }
                        fNode.StorageItem = sItem;
                        if (!node.Children.Contains(fNode))
                        {
                            node.Children.Add(fNode);
                            flist.TryAdd(fNode, sItem);
                            filenode.TryAdd(sItem.Path, fNode);
                        }
                    }
                }
                node.HasUnrealizedChildren = false;
                result = true;
            }
            return (result);
        }

        private void FillTree(TreeView tree, IStorageItem item)
        {
            //var fNode = new MyTreeViewNode() { Content = item.Name.ConvertFrom(CURRENT_SRCENC, true) };
            MyTreeViewNode fNode = filenode.ContainsKey(item.Path) ? filenode[item.Path] : new MyTreeViewNode() { Content = item.Name.ConvertFrom(CURRENT_SRCENC, true) };
            if (item is StorageFolder)
            {
                fNode.Icon = IconFolder;
                fNode.HasUnrealizedChildren = true;
                fNode.IsExpanded = false;
            }
            else if (item is StorageFile)
            {
                fNode.Icon = IconFile;
            }
            fNode.StorageItem = item;
            if (!tree.RootNodes.Contains(fNode))
            {
                tree.RootNodes.Add(fNode);
                flist.TryAdd(fNode, item);
                filenode.TryAdd(item.Path, fNode);
                fNode.IsExpanded = true;
            }
        }

        private void FillTree(TreeView tree, List<IStorageItem> items)
        {
            if (items == null) return;

            foreach (var item in items)
            {
                FillTree(tree, item);
            }
        }

        private void FillTree(TreeView tree, IReadOnlyList<IStorageItem> items)
        {
            if (items == null) return;

            foreach (var item in items)
            {
                FillTree(tree, item);
            }
        }

        private void ClearTree(TreeView tree)
        {
            flist.Clear();
            filenode.Clear();
            TreeFiles.RootNodes.Clear();
            edSrc.Text = string.Empty;
        }

        private async Task<bool> RenameFile()
        {
            bool result = false;
            try
            {
                if (IsDropped) return (result);

                var cnode = target;
                if (cnode is TreeViewNode)
                {

                }
                else if (TreeFiles.SelectedNodes.Count > 0)
                {
                    cnode = TreeFiles.SelectedNodes[0];
                }
                else return (result);

                //cnode = tvFiles.SelectedNodes[0];
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
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        private async Task<bool> ConvertFileContent()
        {
            bool result = false;

            if (IsDropped) return (result);
            var cnode = target;
            if (cnode is TreeViewNode)
            {

            }
            else if (TreeFiles.SelectedNodes.Count > 0)
            {
                cnode = TreeFiles.SelectedNodes[0];
            }
            else return (result);

            if (!flist.ContainsKey(cnode)) return (result);

            var f = flist[cnode];

            if (f is StorageFile)
            {
                var file = f as StorageFile;
                result = await Utils.ConvertFile(file, CURRENT_SRCENC, CURRENT_DSTENC, CURRENT_CONVERT_ORIGINAL);
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

            ToggleMenuFlyoutItem[] optsSrc = new ToggleMenuFlyoutItem[] {optSrcAuto,
                optSrcAscii,
                optSrc1250, optSrc1251, optSrc1253, optSrc1254, optSrc1255, optSrc1256, optSrc1257, optSrc1258,
                optSrcThai, optSrcRussian,
                optSrcGBK, optSrcBIG5, optSrcJIS, optSrcKorean,
                optSrcUnicode, optSrcUTF8
            };
            foreach (var lang in optsSrc)
            {
                var ENC_NAME = lang.Name.Substring(6);
                var enc = TextCodecs.GetTextEncoder(ENC_NAME);
                ToolTipService.SetToolTip(lang, new ToolTip()
                {
                    Content =
                        $"{"EncodingName".T():-16}: {enc.EncodingName}\n" +
                        $"{"WebName".T():-16}: {enc.WebName}\n" +
                        $"{"CodePage".T():-16}: {enc.CodePage}",
                    Placement = PlacementMode.Right
                });
            }

            ToggleMenuFlyoutItem[] optsDst = new ToggleMenuFlyoutItem[] {optDstAuto,
                optDstAscii,
                optDst1250, optDst1251, optDst1253, optDst1254, optDst1255, optDst1256, optDst1257, optDst1258,
                optDstThai, optDstRussian,
                optDstGBK, optDstBIG5, optDstJIS, optDstKorean,
                optDstUnicode, optDstUTF8
            };
            foreach (var lang in optsDst)
            {
                var ENC_NAME = lang.Name.Substring(6);
                var enc = TextCodecs.GetTextEncoder(ENC_NAME);
                ToolTipService.SetToolTip(lang, new ToolTip()
                {
                    Content =
                        $"{"EncodingName".T():-16}: {enc.EncodingName}\n" +
                        $"{"WebName".T():-16}: {enc.WebName}\n" +
                        $"{"CodePage".T():-16}: {enc.CodePage}",
                    Placement = PlacementMode.Right
                });
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void OptSrc_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] btns = new ToggleMenuFlyoutItem[] { optSrcAuto,
                optSrcAscii,
                optSrc1250, optSrc1251, optSrc1253, optSrc1254, optSrc1255, optSrc1256, optSrc1257, optSrc1258,
                optSrcThai, optSrcRussian,
                optSrcGBK, optSrcBIG5, optSrcJIS, optSrcKorean,
                optSrcUnicode, optSrcUTF8
            };
            foreach (var btn in btns)
            {
                if (sender == btn) btn.IsChecked = true;
                else btn.IsChecked = false;
            }
            var enc = sender as ToggleMenuFlyoutItem;
            var ENC_NAME = enc.Name.Substring(6);
            CURRENT_SRCENC = TextCodecs.GetTextEncoder(ENC_NAME);

            ConvertFrom(TreeFiles, CURRENT_SRCENC);
            if (fcontent != null && fcontent is byte[])
                edSrc.Text = fcontent.ToString(CURRENT_SRCENC);
        }

        private void OptDst_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] btns = new ToggleMenuFlyoutItem[] { optDstAuto,
                optDstAscii,
                optDst1250, optDst1251, optDst1253, optDst1254, optDst1255, optDst1256, optDst1257, optDst1258,
                optDstThai, optDstRussian,
                optDstGBK, optDstBIG5, optDstJIS, optDstKorean,
                optDstUnicode, optDstUTF8
            };
            foreach (var btn in btns)
            {
                if (sender == btn) btn.IsChecked = true;
                else btn.IsChecked = false;
            }
            var enc = sender as ToggleMenuFlyoutItem;
            var ENC_NAME = enc.Name.Substring(6);
            CURRENT_DSTENC = TextCodecs.GetTextEncoder(ENC_NAME);
        }

        private void edSrc_TextChanged(object sender, TextChangedEventArgs e)
        {
            edInfo.Text = $"{"Count".T()}: {edSrc.Text.Length}";
        }

        private void OptWrap_Click(object sender, RoutedEventArgs e)
        {
            if (sender == optWrapText)
            {
                if ((sender as AppBarToggleButton).IsChecked == true)
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
            if (DEEP.Equals("F", StringComparison.CurrentCultureIgnoreCase))
            {
                CURRENT_TREEDEEP = 255;
            }
            else
            {
                CURRENT_TREEDEEP = Convert.ToInt32(DEEP);
            }
            CmdBar.IsOpen = false;
        }

        private async void TreeViewAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ActionRename || sender == TreeNodeActionRename)
            {
                await RenameFile();
            }
            else if (sender == ActionRenameAll)
            {

            }
            else if (sender == optActionRename)
            {
                CURRENT_RENAME_REPLACE = optActionRename.IsChecked;
            }
            else if (sender == ActionConvert || sender == TreeNodeActionConvert)
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
            TreeFiles.SelectedNodes.Clear();
            TreeFiles.SelectedNodes.Add(item);
            if (flist.ContainsKey(item))
            {
                var file = flist[item];
                if (file != null)
                {
                    if (file is StorageFile)
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
                            await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
                        }
                    }
                }
            }
        }

        #region TreeView Node Routines
        private async void TreeFiles_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            if (NODE_FILLING) return;
            if (args.Node.HasUnrealizedChildren)
            {
                NODE_FILLING = !await FillNode(args.Node);
            }
        }

        private void TreeFiles_Collapsed(TreeView sender, TreeViewCollapsedEventArgs args)
        {
            args.Node.HasUnrealizedChildren = true;
        }

        private async void TreeFiles_ItemInvoked(object sender, TappedRoutedEventArgs e)
        {
            NavigationViewItem item = sender as NavigationViewItem;
            if (item.Tag is TreeViewNode)
            {
                TreeViewNode node = item.Tag as TreeViewNode;
                if (flist.ContainsKey(node))
                {
                    var file = flist[node];
                    if (file != null)
                    {
                        if (file is StorageFile)
                        {
                            try
                            {
                                var f = file as StorageFile;
                                if (Utils.text_ext.Contains(f.FileType))
                                {
                                    IBuffer buffer = await FileIO.ReadBufferAsync(f);
                                    DataReader reader = DataReader.FromBuffer(buffer);
                                    byte[] fileContent = new byte[reader.UnconsumedBufferLength];
                                    reader.ReadBytes(fileContent);
                                    fcontent = (byte[])fileContent.Clone();
                                    if (fcontent != null && fcontent is byte[])
                                        edSrc.Text = fcontent.ToString(CURRENT_SRCENC);
                                }
                                else
                                {
                                    edSrc.Text = string.Empty;
                                    fcontent = null;
                                }
                            }
                            catch (Exception ex)
                            {
                                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
                            }
                        }
                    }
                }
            }
        }

        private void TreeFilesNodeContextFlyout_Opened(object sender, object e)
        {
            if (sender is MenuFlyout)
            {
                var ft = (sender as MenuFlyout).Target;
                if (ft.Tag is MyTreeViewNode) target = ft.Tag as MyTreeViewNode;
            }
            CheckFlyoutValid();
        }

        private void TreeFilesNodeContextFlyout_Closed(object sender, object e)
        {
            target = null;
        }

        private async void TreeFilesNodeFlyout_Click(object sender, RoutedEventArgs e)
        {
            if (target is TreeViewNode)
            {
                if (sender == ActionRename || sender == TreeNodeActionRename)
                {
                    await RenameFile();
                }
                else if (sender == ActionConvert || sender == TreeNodeActionConvert)
                {
                    await ConvertFileContent();
                }
                else if (sender == TreeNodeActionClearAll)
                {
                    ClearTree(TreeFiles);
                }
            }
        }
        #endregion

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
                        //flist.Clear();
                        //TreeFiles.RootNodes.Clear();
                        //var ret = await AddTo(tvFiles, folder);
                        FillTree(TreeFiles, folder);
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
                        //flist.Clear();
                        //TreeFiles.RootNodes.Clear();
                        FillTree(TreeFiles, files);

                        //foreach (var file in files)
                        //{
                        //    var ret = await AddTo(tvFiles, file);
                        //}
                        IsDropped = false;
                    }
                    break;
                case "btnClearAll":
                    ClearTree(TreeFiles);
                    break;
                case "btnOptSrc":
                    break;
                case "btnRename":
                    await RenameFile();
                    break;
                case "btnOptDst":
                    break;
                case "btnConvert":
                    await ConvertFileContent();
                    break;
                case "btnShare":
                    if (TreeFiles.SelectedNodes.Count > 0)
                    {
                        var f = TreeFiles.SelectedNodes[0];
                        if (flist.ContainsKey(f))
                        {
                            if (flist[f] is StorageFile)
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
                    foreach (var item in items)
                    {
                        sItems.Add(item);
                    }
                    var ret = await AddTo(TreeFiles, sItems);
                    IsDropped = true;
                }
            }

            //def.Complete();
        }
        #endregion

    }

    public sealed class MyTreeViewNode : TreeViewNode
    {
        public FontIcon Icon { get; set; }
        public ImageSource Source { get; set; }
        public IStorageItem StorageItem { get; set; }
        public MyTreeViewNode Node { get { return (this); } }

        public MyTreeViewNode()
        {

        }

    }

}
