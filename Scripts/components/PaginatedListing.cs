using Godot;
using Godot.Sharp.Extras;
using Uri = System.Uri;
using File = System.IO.File;
using System.IO.Compression;

public class PaginatedListing : ScrollContainer
{
#region Signals
    [Signal]
    public delegate void page_changed(int page);
#endregion

#region Node Paths
    [NodePath("VBoxContainer/TopPageCount")]
    PaginationNav _topPageCount = null;

    [NodePath("VBoxContainer/Listing")]
    GridContainer _listing = null;

    [NodePath("VBoxContainer/BottomPageCount")]
    PaginationNav _bottomPageCount = null;
#endregion

#region Private Variables
    AssetLib.QueryResult alqrLastResult = null;
    DownloadQueue dlq = null;
#endregion

    public override void _Ready()
    {
        dlq = new DownloadQueue();
        dlq.Name = "DownloadQueue";
        AddChild(dlq);
        this.OnReady();
    }

    public void ClearResults() {
        foreach (AssetLibEntry ale in _listing.GetChildren()) {
            ale.QueueFree();
        }
        alqrLastResult = null;
        _topPageCount.UpdateConfig(0);
        _bottomPageCount.UpdateConfig(0);
        return;
    }

    public void UpdateAddons() {
        foreach (AssetLibEntry ale in _listing.GetChildren()) {
            ale.QueueFree();
        }

        _topPageCount.Visible = false;
        _bottomPageCount.Visible = false;

        foreach (AssetPlugin plgn in CentralStore.Plugins) {
            AssetLibEntry ale = MainWindow._plScenes["AssetLibEntry"].Instance<AssetLibEntry>();
            ale.Title = plgn.Asset.Title;
            ale.Category = plgn.Asset.Category;
            ale.Author = plgn.Asset.Author;
            ale.License = plgn.Asset.Cost;
            ale.AssetId = plgn.Asset.AssetId;
            ale.UpdateAvailable = false;
            ale.Downloaded = false;
            _listing.AddChild(ale);
            if (string.IsNullOrEmpty(plgn.Asset.IconUrl)) {
                ale.Icon = MainWindow._plTextures["DefaultIconV3"];
            } else {
                if (plgn.Asset.IconUrl.StartsWith("res://")) {
                    ale.Icon = Util.LoadImage(plgn.Asset.IconUrl);
                } else {
                    Uri uri = new Uri(plgn.Asset.IconUrl);
                    string iconPath = $"{CentralStore.Settings.CachePath}/images/{plgn.Asset.AssetId}{uri.AbsolutePath.GetExtension()}";
                    ale.SetMeta("iconPath", iconPath);
                    if (!File.Exists(iconPath.GetOSDir().NormalizePath())) {
                        // Implement Image Downloader through Download Queue
                        ImageDownloader dld = new ImageDownloader(plgn.Asset.IconUrl, iconPath);
                        dlq.Push(dld);
                        ale.SetMeta("dld", dld);
                    } else {
                        Texture icon = Util.LoadImage(iconPath);
                        if (icon == null)
                            ale.Icon = MainWindow._plTextures["MissingIcon"];
                        else
                            ale.Icon = icon;
                    }
                }
            }
        }
        dlq.StartDownload();
    }

    string FindFile(ZipArchive za, string file) {
        foreach (var entry in za.Entries) {
            if (entry.FullName.Contains(file))
                return entry.FullName;
        }
        return "";
    }

    public void UpdateTemplates() {
        foreach (AssetLibEntry ale in _listing.GetChildren()) {
            ale.QueueFree();
        }

        _topPageCount.Visible = false;
        _bottomPageCount.Visible = false;

        foreach (AssetProject prj in CentralStore.Templates) {
            AssetLibEntry ale = MainWindow._plScenes["AssetLibEntry"].Instance<AssetLibEntry>();
            ale.Title = prj.Asset.Title;
            ale.Category = prj.Asset.Category;
            ale.Author = prj.Asset.Author;
            ale.License = prj.Asset.Cost;
            ale.AssetId = prj.Asset.AssetId;
            ale.UpdateAvailable = false;
            ale.Downloaded = false;
            _listing.AddChild(ale);
            if (string.IsNullOrEmpty(prj.Asset.IconUrl)) {
                ale.Icon = MainWindow._plTextures["MissingIcon"];
            } else {
                string iconPath;
                if (prj.Asset.IconUrl.StartsWith("zip+")) {
                    iconPath = $"{CentralStore.Settings.CachePath}/images/{prj.Asset.AssetId}{prj.Asset.IconUrl.GetExtension()}";
                    string zipPath = prj.Asset.IconUrl.Substring("zip+res://".Length);
                    if (!File.Exists(iconPath)) {
                        using (ZipArchive za = ZipFile.Open(prj.Location,ZipArchiveMode.Read)) {
                            zipPath = FindFile(za, zipPath);
                            if (!string.IsNullOrEmpty(zipPath)) {
                                ZipArchiveEntry zae = za.GetEntry(FindFile(za, zipPath));
                                byte[] buffer = zae.ReadBuffer();
                                var fh = new Godot.File();
                                if (fh.Open(iconPath,Godot.File.ModeFlags.Write) == Error.Ok) {
                                    fh.StoreBuffer(buffer);
                                    fh.Close();
                                }
                            }
                        }
                    }
                } else {
                    Uri uri = new Uri(prj.Asset.IconUrl);
                    iconPath = $"{CentralStore.Settings.CachePath}/images/{prj.Asset.AssetId}{uri.AbsolutePath.GetExtension()}";
                    ale.SetMeta("iconPath", iconPath);
                    if (!File.Exists(iconPath.GetOSDir().NormalizePath())) {
                        // Implement Image Downloader through Download Queue
                        ImageDownloader dld = new ImageDownloader(prj.Asset.IconUrl, iconPath);
                        dlq.Push(dld);
                        ale.SetMeta("dld", dld);
                    }
                }
                if (File.Exists(iconPath.GetOSDir().NormalizePath())) {
                    Texture icon = Util.LoadImage(iconPath);
                    if (icon == null)
                        ale.Icon = MainWindow._plTextures["MissingIcon"];
                    else
                        ale.Icon = icon;
                }
            }
        }
        dlq.StartDownload();
    }

    public void UpdateResults(AssetLib.QueryResult result) {
        foreach (AssetLibEntry ale in _listing.GetChildren()) {
            ale.QueueFree();
        }
        alqrLastResult = result;
        _topPageCount.UpdateConfig(result.Pages);
        _bottomPageCount.UpdateConfig(result.Pages);
        _topPageCount.SetPage(result.Page);
        _bottomPageCount.SetPage(result.Page);
        ScrollVertical = 0;
        foreach (AssetLib.AssetResult asset in result.Result) {
            AssetLibEntry ale = MainWindow._plScenes["AssetLibEntry"].Instance<AssetLibEntry>();
            ale.Title = asset.Title;
            ale.Category = asset.Category;
            ale.Author = asset.Author;
            ale.License = asset.Cost;
            ale.AssetId = asset.AssetId;
            if (CentralStore.Instance.HasPluginId(asset.AssetId)) {
                AssetPlugin plgn = CentralStore.Instance.GetPluginId(ale.AssetId);
                if (plgn != null) {
                    if (plgn.Asset.VersionString != asset.VersionString ||
                        plgn.Asset.Version != asset.Version ||
                        plgn.Asset.ModifyDate != asset.ModifyDate)
                        ale.UpdateAvailable = true;
                    else
                        ale.Downloaded = true;
                }
            } else if (CentralStore.Instance.HasTemplateId(asset.AssetId)) {
                AssetProject prj = CentralStore.Instance.GetTemplateId(ale.AssetId);
                if (prj != null) {
                    if (prj.Asset.VersionString != asset.VersionString ||
                        prj.Asset.Version != asset.Version ||
                        prj.Asset.ModifyDate != asset.ModifyDate)
                        ale.UpdateAvailable = true;
                    else
                        ale.Downloaded = true;
                }
            }
            _listing.AddChild(ale);
            Uri uri = new Uri(asset.IconUrl);
            string iconPath = $"{CentralStore.Settings.CachePath}/images/{asset.AssetId}{uri.AbsolutePath.GetExtension()}";
            ale.SetMeta("iconPath", iconPath);
            if (!File.Exists(iconPath.GetOSDir().NormalizePath())) {
                // Implement Image Downloader through Download Queue
                ImageDownloader dld = new ImageDownloader(asset.IconUrl, iconPath);
                dlq.Push(dld);
                ale.SetMeta("dld", dld);
            } else {
                Texture icon = Util.LoadImage(iconPath);
                if (icon == null)
                    ale.Icon = MainWindow._plTextures["MissingIcon"];
                else
                    ale.Icon = icon;
            }
        }
        dlq.StartDownload();
    }

    [SignalHandler("download_completed", nameof(dlq))]
    void OnImageDownloaded(ImageDownloader dld) {
        foreach (AssetLibEntry ale in _listing.GetChildren()) {
            if (ale.HasMeta("dld") && (ale.GetMeta("dld") as ImageDownloader) == dld) {
                ale.RemoveMeta("dld");
                string iconPath = ale.GetMeta("iconPath") as string;
                if (File.Exists(iconPath.GetOSDir().NormalizePath())) {
                    Texture icon = Util.LoadImage(iconPath);
                    if (icon == null)
                        icon = MainWindow._plTextures["MissingIcon"];
                    ale.Icon = icon;
                } else {
                    ale.Icon = MainWindow._plTextures["MissingIcon"];
                }
                return;
            }
        }
    }

    [SignalHandler("page_changed", nameof(_topPageCount))]
    [SignalHandler("page_changed", nameof(_bottomPageCount))]
    void OnPageChanged(int page) {
        if (alqrLastResult != null && page != alqrLastResult.Page) {
            EmitSignal("page_changed", page);
        }
    }
}
