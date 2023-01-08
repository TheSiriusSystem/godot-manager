using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Godot.Sharp.Extras;
using Guid = System.Guid;
using TimeSpan = System.TimeSpan;
using DateTime = System.DateTime;
using FileStream = System.IO.FileStream;
using FileMode = System.IO.FileMode;
using Directory = System.IO.Directory;
using SFile = System.IO.File;
using System.IO.Compression;

public class AssetLibPanel : Panel
{
#region Nodes Path
    #region Search Switcher
    [NodePath("VC/MC/HC/PC/HC/Addons")]
    Button _addonsBtn = null;

    [NodePath("VC/MC/HC/PC/HC/Templates")]
    Button _templatesBtn = null;

    [NodePath("VC/MC/HC/PC/HC/Manage")]
    Button _manageBtn = null;
    #endregion

    #region Search Container
    [NodePath("VC/SearchContainer")]
    VBoxContainer _searchContainer = null;

    #region Search Fields
    [NodePath("VC/SearchContainer/HC/SearchField")]
    LineEdit _searchField = null;

    [NodePath("VC/SearchContainer/HC2/SortBy")]
    OptionButton _sortBy = null;

    [NodePath("VC/SearchContainer/HC2/Category")]
    OptionButton _category = null;

    [NodePath("VC/SearchContainer/HC2/GodotVersion")]
    OptionButton _godotVersion = null;

    [NodePath("VC/SearchContainer/HC2/VisitWebsite")]
    Button _visitWebsite = null;

    [NodePath("VC/SearchContainer/HC2/Support")]
    Button _support = null;

    [NodePath("VC/SearchContainer/HC2/Support/SupportPopup")]
    PopupMenu _supportPopup = null;
    #endregion

    #region Paginated Listings for Addons and Templates
    [NodePath("VC/SearchContainer/plAddons")]
    PaginatedListing _plAddons = null;

    [NodePath("VC/SearchContainer/plTemplates")]
    PaginatedListing _plTemplates = null;
    #endregion
#endregion

    #region Manage Container
    [NodePath("VC/ManageContainer")]
    VBoxContainer _manageContainer = null;

    [NodePath("VC/ManageContainer/HC/PC/HC/Addons")]
    Button _mAddonsBtn = null;

    [NodePath("VC/ManageContainer/HC/PC/HC/Templates")]
    Button _mTemplateBtn = null;

    [NodePath("VC/ManageContainer/HC2/SearchField")]
    LineEdit _mSearchField = null;

    [NodePath("VC/ManageContainer/HC2/Import")]
    Button _import = null;

    [NodePath("VC/ManageContainer/plmAddons")]
    PaginatedListing _plmAddons = null;

    [NodePath("VC/ManageContainer/plmTemplates")]
    PaginatedListing _plmTemplates = null;
    #endregion
#endregion

#region Private Variables
    const string url = "https://godotengine.org/asset-library/api/";
    readonly Dictionary<string, string> localLicenses = new Dictionary<string, string>()
    {
        {"BSD Zero Clause License", "0BSD"},
        {"Academic Free License (\"AFL\") v. 3.0", "AFL-3.0"},
        {"GNU AFFERO GENERAL PUBLIC LICENSE", "AGPLv3"},
        {"Apache License", "Apache-2.0"},
        {"The Artistic License 2.0", "Artistic-2.0"},
        {"BSD 2-Clause License", "BSD-2-Clause"},
        {"The Clear BSD License", "BSD-3-Clause-Clear"},
        {"BSD 3-Clause License", "BSD-3-Clause"},
        {"BSD 4-Clause License", "BSD-4-Clause"},
        {"Boost Software License - Version 1.0 - August 17th, 2003", "BSL-1.0"},
        {"Attribution 4.0 International", "CC-BY-4.0"},
        {"Attribution-ShareAlike 4.0 International", "CC-BY-SA-4.0"},
        {"Creative Commons Legal Code", "CC0"},
        {"CONTRAT DE LICENCE DE LOGICIEL LIBRE CeCILL", "CECILL-2.1"},
        {"CERN Open Hardware Licence Version 2 - Permissive", "CERN-OHL-P-2.0"},
        {"CERN Open Hardware Licence Version 2 - Strongly Reciprocal", "CERN-OHL-S-2.0"},
        {"CERN Open Hardware Licence Version 2 - Weakly Reciprocal", "CERN-OHL-W-2.0"},
        {"Educational Community License", "ECL-2.0"},
        {"Eclipse Public License - v 1.0", "EPL-1.0"},
        {"Eclipse Public License - v 2.0", "EPL-2.0"},
        {"Licensed under the EUPL V.1.1", "EUPL-1.1"},
        {"EUROPEAN UNION PUBLIC LICENCE v. 1.2", "EUPL-1.2"},
        {"GNU Free Documentation License", "GFDL-1.3"},
        {"the Free Software Foundation; either version 2 of the License, or", "GPLv2"},
        {"\"This License\" refers to version 3 of the GNU General Public License.", "GPLv3"},
        {"ISC License", "ISC"},
        {"as the successor of the GNU Library Public License, version 2, hence", "LGPLv2.1"},
        {"As used herein, \"this License\" refers to version 3 of the GNU Lesser", "LGPLv3"},
        {"The LaTeX Project Public License", "LPPL-1.3c"},
        {"MIT No Attribution", "MIT-0"},
        {"MIT License", "MIT"},
        {"Mozilla Public License Version 2.0", "MPL-2.0"},
        {"Microsoft Public License (Ms-PL)", "MS-PL"},
        {"Microsoft Reciprocal License (Ms-RL)", "MS-RL"},
        {"You may obtain a copy of Mulan PSL v2 at:", "MulanPSL-2.0"},
        {"University of Illinois/NCSA Open Source License", "NCSA"},
        {"ODC Open Database License (ODbL)", "ODbL-1.0"},
        {"SIL OPEN FONT LICENSE Version 1.1 - 26 February 2007", "OFL-1.1"},
        {"Open Software License (\"OSL\") v. 3.0", "OSL-3.0"},
        {"PostgreSQL License", "PostgreSQL"},
        {"This is free and unencumbered software released into the public domain.", "Unlicense"},
        {"The Universal Permissive License (UPL), Version 1.0", "UPL-1.0"},
        {"VIM LICENSE", "Vim"},
        {"DO WHAT THE FUCK YOU WANT TO PUBLIC LICENSE", "WTFPL"},
        {"zlib License", "Zlib"}
    };
    int _plaCurrentPage = 0;
    int _pltCurrentPage = 0;
    int _plmCurrentPage = 0;
    string lastSearch = "";
    DateTime lastConfigureRequest;
    DateTime lastSearchRequest;
    TimeSpan defaultWaitSearch = TimeSpan.FromMinutes(5);
    TimeSpan defaultWaitConfigure = TimeSpan.FromHours(2);
#endregion

    public override void _Ready()
    {
        this.OnReady();
        lastConfigureRequest = DateTime.Now - TimeSpan.FromHours(3);
        lastSearchRequest = DateTime.Now - TimeSpan.FromMinutes(6);
        GetParent<TabContainer>().Connect("tab_changed", this, "OnPageChanged");
    }

    [SignalHandler("pressed", nameof(_import))]
    async void OnImportPressed()
    {
        AppDialogs.ImportFileDialog.Connect("popup_hide", this, "OnImportClosed", null, (uint)ConnectFlags.Oneshot);
        var result = await AppDialogs.YesNoCancelDialog.ShowDialog(Tr("Import Asset"),
            Tr("What type of asset would you like to import?"),
            Tr("Addon"), Tr("Template"));
        if (result == YesNoCancelDialog.ActionResult.FirstAction) {
            AppDialogs.ImportFileDialog.Filters = new string[] {"plugin.cfg", "*.zip"};
            AppDialogs.ImportFileDialog.Connect("file_selected", this, "OnPluginImport", null, (uint)ConnectFlags.Oneshot);
        } else if (result == YesNoCancelDialog.ActionResult.SecondAction) {
            AppDialogs.ImportFileDialog.Filters = new string[] {"engine.cfg", "project.godot", "*.zip"};
            AppDialogs.ImportFileDialog.Connect("file_selected", this, "OnTemplateImport", null, (uint)ConnectFlags.Oneshot);
        } else {
            return;
        }
        AppDialogs.ImportFileDialog.CurrentFile = "";
        AppDialogs.ImportFileDialog.CurrentPath = (CentralStore.Settings.ProjectPath + "/").NormalizePath();
        AppDialogs.ImportFileDialog.PopupCentered();
    }

    void OnImportClosed() {
        if (AppDialogs.ImportFileDialog.IsConnected("file_selected", this, "OnTemplateImport"))
            AppDialogs.ImportFileDialog.Disconnect("file_selected", this, "OnTemplateImport");

        if (AppDialogs.ImportFileDialog.IsConnected("file_selected", this, "OnPluginImport"))
            AppDialogs.ImportFileDialog.Disconnect("file_selected", this, "OnPluginImport");
    }

    void OnPluginImport(string filepath) {
        if (filepath.EndsWith("plugin.cfg")) {
            PluginDirectoryImport(filepath);
        } else if (filepath.EndsWith(".zip")) {
            AssetZipImport(filepath, true);
        } else {
            AppDialogs.MessageDialog.ShowMessage(Tr("Error"), 
                string.Format(Tr("{0} is not a valid plugin file."), filepath.GetFile()));
        }
    }

    void OnTemplateImport(string filepath)
    {
        if (filepath.EndsWith("engine.cfg") || filepath.EndsWith("project.godot")) {
            TemplateDirectoryImport(filepath);
        } else if (filepath.EndsWith(".zip")) {
            AssetZipImport(filepath, false);
        } else {
            AppDialogs.MessageDialog.ShowMessage(Tr("Error"),
                string.Format(Tr("{0} is not a valid project file."), filepath.GetFile()));
        }
    }

    [SignalHandler("id_pressed", nameof(_supportPopup))]
    async void OnSupportPopup_IdPressed(int id)
    {
        _supportPopup.SetItemChecked(id, !_supportPopup.IsItemChecked(id));
        await UpdatePaginatedListing(_addonsBtn.Pressed ? _plAddons : _plTemplates);
    }

    [SignalHandler("pressed", nameof(_visitWebsite))]
    void OnVisitWebsitePressed() {
        OS.ShellOpen("https://godotengine.org/asset-library");
    }

    [SignalHandler("pressed", nameof(_support))]
    void OnSupportPressed() {
        _supportPopup.Popup_(new Rect2(_support.RectGlobalPosition + new Vector2(0,_support.RectSize.y), _supportPopup.RectSize));
    }

    [SignalHandler("text_entered", nameof(_searchField))]
    async void OnSearchField_TextEntered(string text) {
        await UpdatePaginatedListing(_addonsBtn.Pressed ? _plAddons : _plTemplates);
    }

    [SignalHandler("item_selected", nameof(_category))]
    async void OnCategorySelected(int index) {
        _plaCurrentPage = 0;
        _pltCurrentPage = 0;
        await UpdatePaginatedListing(_addonsBtn.Pressed ? _plAddons : _plTemplates);
    }

    [SignalHandler("item_selected", nameof(_sortBy))]
    async void OnSortBySelected(int index) {
        _plaCurrentPage = 0;
        _pltCurrentPage = 0;
        await UpdatePaginatedListing(_addonsBtn.Pressed ? _plAddons : _plTemplates);
    }

    [SignalHandler("item_selected", nameof(_godotVersion))]
    async void OnGodotVersionSelected(int index)
    {
        _plaCurrentPage = 0;
        _pltCurrentPage = 0;
        await UpdatePaginatedListing(_addonsBtn.Pressed ? _plAddons : _plTemplates);
    }

    [SignalHandler("page_changed", nameof(_plAddons))]
    async void OnPLAPageChanged(int page) {
        _plaCurrentPage = page;
        await UpdatePaginatedListing(_plAddons);
    }

    [SignalHandler("page_changed", nameof(_plTemplates))]
    async void OnPLTPageChanged(int page) {
        _pltCurrentPage = page;
        await UpdatePaginatedListing(_plTemplates);
    }

    [SignalHandler("pressed", nameof(_addonsBtn))]
    async void OnAddonsPressed() {
        _templatesBtn.Pressed = false;
        _manageBtn.Pressed = false;
        _plTemplates.Visible = false;
        _plAddons.Visible = true;
        _searchContainer.Visible = true;
        _manageContainer.Visible = false;
        await Configure(false);
        await UpdatePaginatedListing(_plAddons);
    }

    [SignalHandler("pressed", nameof(_templatesBtn))]
    async void OnTemplatesPressed() {
        _addonsBtn.Pressed = false;
        _manageBtn.Pressed = false;
        _plTemplates.Visible = true;
        _plAddons.Visible = false;
        _searchContainer.Visible = true;
        _manageContainer.Visible = false;
        await Configure(true);
        await UpdatePaginatedListing(_plTemplates);
    }

    [SignalHandler("pressed", nameof(_manageBtn))]
    async void OnManagePressed() {
        _addonsBtn.Pressed = false;
        _templatesBtn.Pressed = false;
        _plTemplates.Visible = false;
        _plAddons.Visible = false;
        _searchContainer.Visible = false;
        _manageContainer.Visible = true;
        if (_mTemplateBtn.Pressed)
            await UpdatePaginatedListing(_plmTemplates);
        else
            await UpdatePaginatedListing(_plmAddons);
    }

    [SignalHandler("pressed", nameof(_mAddonsBtn))]
    async void OnManageAddonPressed() {
        _plmAddons.Visible = true;
        _mAddonsBtn.Pressed = true;
        _plmTemplates.Visible = false;
        _mTemplateBtn.Pressed = false;
        await UpdatePaginatedListing(_plmAddons);
    }

    [SignalHandler("pressed", nameof(_mTemplateBtn))]
    async void OnManageTemplatePressed() {
        _plmAddons.Visible = false;
        _mAddonsBtn.Pressed = false;
        _plmTemplates.Visible = true;
        _mTemplateBtn.Pressed = true;
        await UpdatePaginatedListing(_plmTemplates);
    }

    async void OnPageChanged(int page) {
        if (GetParent<TabContainer>().GetCurrentTabControl() == this)
		{
            if ((DateTime.Now - lastConfigureRequest) >= defaultWaitConfigure) {
                await Configure(_templatesBtn.Pressed);
                if (_category.GetItemCount() == 1)
                    return;
            }

            if ((DateTime.Now - lastSearchRequest) >= defaultWaitSearch) {
                await UpdatePaginatedListing(_addonsBtn.Pressed ? _plAddons : _plTemplates);
            }
		}
	}

    void FinalizeAssetInfo(ref AssetLib.Asset asset, string filepath) {
        string baseDir = filepath.GetBaseDir();
		asset.AssetId = $"local-{Guid.NewGuid().ToString()}";
        asset.Version = "-1";
        asset.Cost = "None";
        asset.BrowseUrl = $"file://{baseDir}";
        asset.IssuesUrl = "";
        asset.ModifyDate = DateTime.UtcNow.ToString();
        asset.DownloadUrl = $"file://{filepath}";
        asset.Previews = new Array<AssetLib.Preview>();
        if (!filepath.EndsWith(".zip")) {
            File file = new File();
            foreach (string ext in new string[] {"", ".txt", ".md", ".rst"}) {
                if (file.Open(baseDir.Join("LICENSE" + ext), File.ModeFlags.Read) == Error.Ok) {
                    foreach (string key in localLicenses.Keys) {
                        if (file.GetAsText().Find(key) != -1) {
                            asset.Cost = localLicenses[key];
                            break;
                        }
                    }
                    break;
                }
            }
            file.Close();
        }
	}

    AssetLib.Asset CreateAssetDirectory(string filepath, bool is_plugin) {
        AssetLib.Asset asset = new AssetLib.Asset();
        if (is_plugin) {
            ConfigFile cfg = new ConfigFile();
            cfg.Load(filepath);
            asset.Type = "addon";
            asset.Title = cfg.GetValue("plugin", "name", filepath.GetBaseDir().BaseName()) as string;
            asset.Author = cfg.GetValue("plugin", "author", "Unknown") as string;
            asset.VersionString = cfg.GetValue("plugin", "version", "Unknown") as string;
            asset.Category = "Misc";
            asset.GodotVersion = "2.1+";
            asset.Description = cfg.GetValue("plugin", "description") as string;
            asset.IconUrl = MainWindow._plTextures["DefaultIconV3"].ResourcePath;
        } else {
            ProjectConfig pc = new ProjectConfig(filepath);
			bool isOldPrj = filepath.EndsWith("engine.cfg");
            pc.Load();
            asset.Type = "project";
            asset.Title = pc.GetValue("application", isOldPrj ? "name" : "config/name");
            asset.Author = "Unknown";
            asset.VersionString = "Unknown";
            asset.Category = "Projects";
            asset.GodotVersion = isOldPrj ? "1.x-2.x" : "3.x+";
            asset.Description = isOldPrj ? "" : pc.GetValue("application", "config/description");
            asset.IconUrl = "zip+" + pc.GetValue("application", isOldPrj ? "icon" : "config/icon");
        }
        FinalizeAssetInfo(ref asset, filepath);
        return asset;
    }

    AssetLib.Asset CreateAssetZip(string filepath, bool is_plugin) {
        AssetLib.Asset asset = new AssetLib.Asset();
        if (is_plugin) {
            ConfigFile cfg = new ConfigFile();
            bool found = false;

            using (ZipArchive za = ZipFile.OpenRead(filepath)) {
                foreach (ZipArchiveEntry zae in za.Entries) {
                    if (zae.FullName.EndsWith("plugin.cfg")) {
                        found = true;
                        cfg.Parse(zae.ReadFile());
                        break;
                    }
                }
            }

            if (!found)
                return null;

            asset.Type = "addon";
            asset.Title = cfg.GetValue("plugin", "name", filepath.GetFile().BaseName()) as string;
            asset.Author = cfg.GetValue("plugin", "author", "Unknown") as string;
            asset.VersionString = cfg.GetValue("plugin", "version", "Unknown") as string;
            asset.Category = "Misc";
            asset.GodotVersion = "2.1+";
            asset.Description = cfg.GetValue("plugin", "description") as string;
            asset.IconUrl = MainWindow._plTextures["DefaultIconV3"].ResourcePath;
        } else {
            ProjectConfig pc = new ProjectConfig();
            bool found = false;
			bool isOldPrj = false;

            using (ZipArchive za = ZipFile.OpenRead(filepath)) {
                foreach (ZipArchiveEntry zae in za.Entries) {
                    if (!zae.FullName.EndsWith("/") && (zae.Name == "engine.cfg" || zae.Name == "project.godot") && pc.LoadBuffer(zae.ReadFile()) == Error.Ok) {
                        found = true;
						isOldPrj = (zae.Name == "engine.cfg");
                        break;
                    }
                }
            }

            if (!found)
                return null;

			asset.Type = "project";
            asset.Title = pc.GetValue("application", isOldPrj ? "name" : "config/name");
            asset.Author = "Unknown";
            asset.VersionString = "Unknown";
            asset.Category = "Projects";
            asset.GodotVersion = isOldPrj ? "1.x-2.x" : "3.x+";
            asset.Description = isOldPrj ? "" : pc.GetValue("application", "config/description");
            asset.IconUrl = "zip+" + pc.GetValue("application", isOldPrj ? "icon" : "config/icon");
        }
        FinalizeAssetInfo(ref asset, filepath);
        return asset;
    }

    async void PluginDirectoryImport(string filepath) {
        string pluginPath = filepath.GetBaseDir().NormalizePath();
        string pluginName = pluginPath.GetFile();
        string zipName = $"addon-local-{pluginName}.zip".NormalizeFileName();
        string zipFile = $"{CentralStore.Settings.CachePath}/downloads/{zipName}";
        using (var fh = new FileStream(zipFile, FileMode.Create)) {
            using (var afh = new ZipArchive(fh, ZipArchiveMode.Create)) {
                foreach (string entry in Directory.EnumerateFileSystemEntries(pluginPath, "*", System.IO.SearchOption.AllDirectories)) {
                    if (entry == "." || entry == "..")
                        continue;
                    if (Directory.Exists(entry))
                        continue;

                    var zipPath = $"addons/{pluginName}".Join(entry.Substring(pluginPath.Length+1));
                    afh.CreateEntryFromFile(entry, zipPath);
                }
            }
        }
        AssetPlugin plgn = new AssetPlugin();
        plgn.Asset = CreateAssetDirectory(filepath, true);
        plgn.Location = zipFile;
        AppDialogs.AddonInstaller.ShowDialog(plgn);
        while (AppDialogs.AddonInstaller.Visible)
            await this.IdleFrame();
        CentralStore.Plugins.Add(plgn);
        CentralStore.Instance.SaveDatabase();
        UpdateAssetListing();
        AppDialogs.MessageDialog.ShowMessage(Tr("Asset Added"), 
			string.Format(Tr("{0} has been imported. You can find it in the Downloaded Addons tab."), pluginName.BaseName()));
    }

    void TemplateDirectoryImport(string filepath) {
        string templatePath = filepath.GetBaseDir().NormalizePath();
        string templateName = templatePath.GetFile();
        string zipName = $"project-local-{templateName}.zip".NormalizeFileName();
        string zipFile = $"{CentralStore.Settings.CachePath}/downloads/{zipName}";
        using (var fh = new FileStream(zipFile, FileMode.Create)) {
            using (var afh = new ZipArchive(fh, ZipArchiveMode.Create)) {
                foreach (string entry in Directory.EnumerateFileSystemEntries(templatePath, "*", System.IO.SearchOption.AllDirectories)) {
                    GD.Print(entry.GetFile());
                    if (entry == "." || entry == "..")
                        continue;
                    if (entry.EndsWith(".import"))
                        continue;
                    if (Directory.Exists(entry))
                        continue;

                    var zipPath = entry.Substring(templatePath.Length+1);
                    afh.CreateEntryFromFile(entry, "zip/" + zipPath);
                }
            }
        }
        AssetProject prj = new AssetProject();
        prj.Asset = CreateAssetDirectory(filepath, false);
        prj.Location = zipFile;
        CentralStore.Templates.Add(prj);
        CentralStore.Instance.SaveDatabase();
        UpdateAssetListing();
        AppDialogs.MessageDialog.ShowMessage(Tr("Asset Added"), 
			string.Format(Tr("{0} has been imported. You can find it in the Downloaded Templates tab."), templateName.BaseName()));
    }

    async void AssetZipImport(string filepath, bool is_plugin) {
        string zipFile = filepath.NormalizePath();
        string zipName = $"{(is_plugin ? "addon" : "project")}-local-{zipFile.GetFile().BaseName()}.zip".NormalizeFileName();
        string newZipFile = $"{CentralStore.Settings.CachePath}/downloads/{zipName}";
        SFile.Copy(zipFile, newZipFile);
        AssetLib.Asset asset = CreateAssetZip(filepath, is_plugin);
        if (is_plugin) {
            AssetPlugin plgn = new AssetPlugin();
            plgn.Asset = asset;
            plgn.Location = newZipFile;
            AppDialogs.AddonInstaller.ShowDialog(plgn);
            while (AppDialogs.AddonInstaller.Visible)
                await this.IdleFrame();
            CentralStore.Plugins.Add(plgn);
        } else {
            AssetProject prj = new AssetProject();
            prj.Asset = asset;
            prj.Location = newZipFile;
            CentralStore.Templates.Add(prj);
        }
        CentralStore.Instance.SaveDatabase();
        UpdateAssetListing();
        AppDialogs.MessageDialog.ShowMessage(Tr("Asset Added"), 
			string.Format(Tr("{0} has been imported. You can find it in the Downloaded {1} tab."), asset.Title, is_plugin ? "Addons" : "Templates"));
    }

	private async Task Configure(bool projectsOnly)
	{
		AppDialogs.BusyDialog.UpdateHeader(Tr("Fetching Assets"));
		AppDialogs.BusyDialog.UpdateByline(Tr("Fetching asset information from Godot Asset Library..."));
		AppDialogs.BusyDialog.ShowDialog();

		AssetLib.AssetLib.Instance.Connect("chunk_received", this, "OnChunkReceived");
		var task = AssetLib.AssetLib.Instance.Configure(url,projectsOnly);
		while (!task.IsCompleted)
		{
			await this.IdleFrame();
		}

		AssetLib.AssetLib.Instance.Disconnect("chunk_received", this, "OnChunkReceived");

		_category.Clear();
        _category.AddItem("Any", 0);
		AssetLib.ConfigureResult configureResult = task.Result;

        if (configureResult == null) {
            PaginatedListing pl = _addonsBtn.Pressed ? _plAddons : _plTemplates;
            pl.ClearResults();
            AppDialogs.BusyDialog.HideDialog();
            AppDialogs.MessageDialog.ShowMessage(Tr("Error"), Tr("Unable to connect to Godot Asset Library."));
            return;
        }

		foreach (AssetLib.CategoryResult category in configureResult.Categories)
		{
			_category.AddItem(category.Name, category.Id.ToInt());
		}
        lastConfigureRequest = DateTime.Now;
	}

    private string[] GetSupport() {
        Array<string> support = new Array<string>();
        if (_supportPopup.IsItemChecked(0))
            support.Add("official");
        if (_supportPopup.IsItemChecked(1))
            support.Add("community");
        if (_supportPopup.IsItemChecked(2))
            support.Add("testing");
        string[] asupport = new string[support.Count];
        foreach (string t in support)
            asupport[support.IndexOf(t)] = t;
        return asupport;
    }

    public string GetGodotVersion()
    {
        return _godotVersion.GetItemText(_godotVersion.Selected).ToLower();
    }

	private async Task UpdatePaginatedListing(PaginatedListing pl)
	{
        GetTree().Root.GetNode<MainWindow>("MainWindow").RemoveMissingAssets();
        if (pl == _plmAddons || pl == _plmTemplates) {
            pl.ClearResults();
            if (pl == _plmAddons)
                pl.UpdateAddons();
            else
                pl.UpdateTemplates();
        } else {
            AppDialogs.BusyDialog.UpdateHeader(Tr("Fetching Assets"));
            AppDialogs.BusyDialog.UpdateByline(Tr("Processing search results..."));
            AppDialogs.BusyDialog.ShowDialog();

            bool projectsOnly = (pl == _plTemplates);
            int sortBy = _sortBy.Selected;
            int categoryId = _category.GetSelectedId();
            string filter = _searchField.Text;

            Task<AssetLib.QueryResult> stask = AssetLib.AssetLib.Instance.Search(url, 
                projectsOnly ? _pltCurrentPage : _plaCurrentPage,
                    GetGodotVersion(), projectsOnly, sortBy, GetSupport(), categoryId, filter);
            while (!stask.IsCompleted)
                await this.IdleFrame();

            if (stask.Result == null) {
                pl.ClearResults();
                AppDialogs.BusyDialog.HideDialog();
                AppDialogs.MessageDialog.ShowMessage(Tr("Error"), Tr("Unable to connect to Godot Asset Library."));
                return;
            }

            pl.UpdateResults(stask.Result);
            AppDialogs.BusyDialog.HideDialog();
            lastSearchRequest = DateTime.Now;
        }
	}

    public async void UpdateAssetListing()
    {
        if (_mAddonsBtn.Pressed)
            await UpdatePaginatedListing(_plmAddons);
        else
            await UpdatePaginatedListing(_plmTemplates);
    }
}
