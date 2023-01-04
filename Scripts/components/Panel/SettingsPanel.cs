using Godot;
using Godot.Sharp.Extras;
using Godot.Collections;
using ActionStack = System.Collections.Generic.Stack<System.Action>;
using Uri = System.Uri;
using System.Text.RegularExpressions;

public class SettingsPanel : Panel
{
	#region Node Paths
	
	#region Page Buttons
	[NodePath("VB/Header/HC/PC/HC/General")]
	Button _generalBtn = null;

	[NodePath("VB/Header/HC/PC/HC/Projects")]
	Button _projectsBtn = null;

	[NodePath("VB/Header/HC/PC/HC/About")]
	Button _aboutBtn = null;

	[NodePath("VB/Header/HC/PC/HC/Contributions")]
	Button _contributionBtn = null;

	[NodePath("VB/Header/HC/PC/HC/Licenses")]
	Button _licensesBtn = null;
	#endregion

	#region Main Controls
	[NodePath("VB/MC/TC")]
	TabContainer _pages = null;

	[NodePath("VB/Header/HC/ActionButtons")]
	ActionButtons _actionButtons = null;
	#endregion

	#region General Page
	[NodePath("VB/MC/TC/General/GC/HBIL/GodotInstallLocation")]
	LineEdit _godotInstallLocation = null;

	[NodePath("VB/MC/TC/General/GC/HBIL/Browse")]
	Button _godotBrowseButton = null;

	[NodePath("VB/MC/TC/General/GC/HBCL/CacheInstallLocation")]
	LineEdit _cacheInstallLocation = null;

	[NodePath("VB/MC/TC/General/GC/HBCL/Browse")]
	Button _cacheBrowseButton = null;

	[NodePath("VB/MC/TC/General/GC/ProjectView")]
	OptionButton _defaultProjectView = null;

	[NodePath("VB/MC/TC/General/GC/TitleBar")]
	private CheckBox _useSystemTitlebar = null;

	[NodePath("VB/MC/TC/General/GC/UseLastMirror")]
	CheckBox _useLastMirror = null;

	[NodePath("VB/MC/TC/General/GC/HBLO/NoConsole")]
	CheckBox _noConsole = null;

	[NodePath("VB/MC/TC/General/GC/HBLO/EditorProfiles")]
	CheckBox _editorProfiles = null;

	[NodePath("VB/MC/TC/General/GC/MirrorTabs/Asset Library")]
	ItemListWithButtons _assetMirror = null;
	#endregion

	#region Projects Page
	[NodePath("VB/MC/TC/Projects/GC/HBPL/DefaultProjectLocation")]
	LineEdit _defaultProjectLocation = null;

	[NodePath("VB/MC/TC/Projects/GC/HBPL/BrowseProjectLocation")]
	Button _browseProjectLocation = null;

	[NodePath("VB/MC/TC/Projects/GC/AutoScanProjects")]
	CheckBox _autoScanProjects = null;

	[NodePath("VB/MC/TC/Projects/GC/DirectoryScan")]
	ItemListWithButtons _directoryScan = null;
	#endregion

	#region About Page
	[NodePath("VB/MC/TC/About/MC/VB/HBHeader/VB/VersionInfo")]
	Label _versionInfo = null;
	[NodePath("VB/MC/TC/About/MC/VB/HBHeader/VB/EmailWebsite")]
	RichTextLabel _emailWebsite = null;

	[NodePath("VB/MC/TC/About/MC/VB/MC/BuiltWith")]
	RichTextLabel _builtWith = null;

	[NodePath("VB/MC/TC/About/MC/VB/CenterContainer/HB/VB/BuyMe")]
	TextureRect _buyMe = null;

	[NodePath("VB/MC/TC/About/MC/VB/CenterContainer/HB/VB/ItchIO")]
	TextureRect _itchIo = null;

	[NodePath("VB/MC/TC/About/MC/VB/CenterContainer/HB/VB2/Github")]
	TextureRect _github = null;

	[NodePath("VB/MC/TC/About/MC/VB/CenterContainer/HB/VB2/Discord")]
	TextureRect _discord = null;
	#endregion

	#region Contributions Page
	[NodePath("VB/MC/TC/Contributions/Contributors")]
	RichTextLabel _contributors = null;
	#endregion
	
	#region Licenses Page
	[NodePath("VB/MC/TC/Licenses")]
	TabContainer _licenses = null;

	[NodePath("VB/MC/TC/Licenses/MIT License")]
	RichTextLabel _mitLicense = null;

	[NodePath("VB/MC/TC/Licenses/Apache License")]
	RichTextLabel _apacheLicense = null;
	#endregion

	#endregion

	#region Version String for About
	#endregion

	#region Private Variables
	Array<string> _views;
	// In Hours
	double[] _dCheckInterval = new double[] {
		1,      // 1 Hour
		12,     // 12 hours
		24,     // 1 day
		168,    // 1 week
		336,    // Bi-Weekly
		720     // Monthly (30 Days)
	};
	bool bPInternal = false;
	ActionStack _undoActions;

	Regex IsNumeric = new Regex(@"\d+");
	#endregion

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.OnReady();

		_builtWith.BbcodeText = BuildVersionInfo(); //VERSION_INFORMATION;
		_undoActions = new ActionStack();
		_views = new Array<string>();

		for (int i = 0; i < _defaultProjectView.GetItemCount(); i++) {
			_views.Add(_defaultProjectView.GetItemText(i));
		};

		// Load up our Settings to be displayed to end user
		LoadSettings();
		updateActionButtons();

		GetParent<TabContainer>().Connect("tab_changed", this, "OnPageChanged");
		_versionInfo.Text = $"Version {VERSION.GodotManager}-{VERSION.Channel}";
	}

	void updateActionButtons() {
		_actionButtons.Visible = _undoActions.Count > 0;
	}

	string BuildVersionInfo() {
		return "[table=3][cell][color=green]" + Tr("Project Name") +
		"[/color][/cell][cell][color=green]" + Tr("Version") +
		"[/color]     [/cell][cell][color=green]Website[/color][/cell]" +
		"[cell][color=aqua]Godot Engine (Mono Edition)      [/color][/cell][cell][color=white]v" +
		Engine.GetVersionInfo()["string"] +
		"     [/color][/cell][cell][color=yellow][url]https://godotengine.org[/url][/color][/cell]" +
		"[cell][color=aqua]GodotSharpExtras[/color][/cell][cell][color=white]v" +
		VERSION.GodotSharpExtras +
		"[/color][/cell][cell][color=yellow][url]https://github.com/eumario/GodotSharpExtras[/url][/color][/cell]" +
		"[cell][color=aqua]NewtonSoft JSON[/color][/cell][cell][color=white]v" + 
		VERSION.NewtonsoftJSON +
		"[/color][/cell][cell][color=yellow][url]https://www.newtonsoft.com/json[/url][/color][/cell]" +
		"[cell][color=aqua]SixLabors ImageSharp[/color][/cell][cell][color=white]v" +
		VERSION.ImageSharp +
		"[/color][/cell][cell][color=yellow][url]https://sixlabors.com/products/imagesharp/[/url][/color][/cell]" +
		"[cell][color=aqua]System.IO.Compression[/color][/cell][cell][color=white]v" +
		VERSION.SystemIOCompression +
		"[/color][/cell][cell][color=yellow][url]https://www.nuget.org/packages/System.IO.Compression/[/url][/color][/cell][/table]";
	}

#region Internal Functions for use in the Settings Page
	void LoadSettings() {
		// General Page
		bPInternal = true;
		_godotInstallLocation.Text = CentralStore.Settings.EnginePath.GetOSDir().NormalizePath();
		_cacheInstallLocation.Text = CentralStore.Settings.CachePath.GetOSDir().NormalizePath();
		_defaultProjectView.Select(_views.IndexOf(CentralStore.Settings.DefaultView));
		_useSystemTitlebar.Pressed = CentralStore.Settings.UseSystemTitlebar;
		_useLastMirror.Pressed = CentralStore.Settings.UseLastMirror;
		_editorProfiles.Pressed = CentralStore.Settings.SelfContainedEditors;
		_noConsole.Pressed = CentralStore.Settings.NoConsole;

		_assetMirror.Clear();
		foreach (string meta in _assetMirror.GetMetaList())
			_assetMirror.RemoveMeta(meta);
		
		foreach (Dictionary<string, string> mirror in CentralStore.Settings.AssetMirrors) {
			_assetMirror.AddItem(mirror["name"]);
			_assetMirror.SetMeta(mirror["name"], mirror["url"]);
		}
		
		// _godotMirror.Clear();
		// foreach (string meta in _godotMirror.GetMetaList())
		// 	_godotMirror.RemoveMeta(meta);
		
		// foreach (Dictionary<string, string> mirror in CentralStore.Settings.EngineMirrors) {
		// 	_godotMirror.AddItem(mirror["name"]);
		// 	_godotMirror.SetMeta(mirror["name"], mirror["url"]);
		// }

		// Project Page
		_defaultProjectLocation.Text = CentralStore.Settings.ProjectPath.NormalizePath();
		_directoryScan.Clear();
		foreach (string dir in CentralStore.Settings.ScanDirs) {
			_directoryScan.AddItem(dir.NormalizePath());
		}
		bPInternal = false;
	}

	void UpdateSettings() {
		GetTree().Root.GetNode<MainWindow>("MainWindow").EnsureDirStructure();
		CentralStore.Settings.EnginePath = _godotInstallLocation.Text.GetOSDir().NormalizePath();
		CentralStore.Settings.CachePath = _cacheInstallLocation.Text.GetOSDir().NormalizePath();
		CentralStore.Settings.DefaultView = _defaultProjectView.GetItemText(_defaultProjectView.Selected);
		CentralStore.Settings.UseSystemTitlebar = _useSystemTitlebar.Pressed;
		CentralStore.Settings.UseLastMirror = _useLastMirror.Pressed;
		CentralStore.Settings.SelfContainedEditors = _editorProfiles.Pressed;
		GetTree().Root.GetNode<MainWindow>("MainWindow").UpdateWindow();

		foreach (GodotVersion version in CentralStore.Versions) {
			if (_editorProfiles.Pressed) {
				File fh = new File();
				fh.Open($"{version.Location}/._sc_".GetOSDir().NormalizePath(), File.ModeFlags.Write);
				fh.StoreString(" ");
				fh.Close();
			} else {
				Directory dh = new Directory();
				dh.Open($"{version.Location}".GetOSDir().NormalizePath());
				dh.Remove($"{version.Location}/._sc_".GetOSDir().NormalizePath());
			}
		}

		CentralStore.Settings.NoConsole = _noConsole.Pressed;
		CentralStore.Settings.AssetMirrors.Clear();
		for (int i = 0; i < _assetMirror.GetItemCount(); i++) {
			Dictionary<string, string> data = new Dictionary<string, string>();
			data["name"] = _assetMirror.GetItemText(i);
			data["url"] = (string)_assetMirror.GetMeta(data["name"]);
			CentralStore.Settings.AssetMirrors.Add(data);
		}
		
		CentralStore.Settings.ProjectPath = _defaultProjectLocation.Text.GetOSDir().NormalizePath();
		CentralStore.Settings.ScanDirs.Clear();
		for (int i = 0; i < _directoryScan.GetItemCount(); i++) {
			CentralStore.Settings.ScanDirs.Add(_directoryScan.GetItemText(i));
		}
		CentralStore.Instance.SaveDatabase();
		_undoActions.Clear();
		updateActionButtons(); 
	}
#endregion

#region Event Handlers for Notebook
	async void OnPageChanged(int page) {
		if (GetParent<TabContainer>().GetCurrentTabControl() == this) {
			LoadSettings();
		} else {
			if (_undoActions.Count > 0) {
				var res = AppDialogs.YesNoDialog.ShowDialog(Tr("Unsaved Settings"), 
					Tr("You have unsaved settings, do you wish to save your settings?"));
				await res;
				if (res.Result)
					UpdateSettings();
				else {
					bPInternal = true;
					while (_undoActions.Count > 0) {
						_undoActions.Pop().Invoke();
					}
					updateActionButtons();
					bPInternal = false;
				}
			}
		}
	}

	[SignalHandler("pressed", nameof(_generalBtn))]
	void OnGeneralPressed() {
		_generalBtn.Pressed = true;
		_projectsBtn.Pressed = false;
		_aboutBtn.Pressed = false;
		_contributionBtn.Pressed = false;
		_licensesBtn.Pressed = false;
		_pages.CurrentTab = 0;
	}

	[SignalHandler("pressed", nameof(_projectsBtn))]
	void OnProjectsPressed() {
		_generalBtn.Pressed = false;
		_projectsBtn.Pressed = true;
		_aboutBtn.Pressed = false;
		_contributionBtn.Pressed = false;
		_licensesBtn.Pressed = false;
		_pages.CurrentTab = 1;
	}

	[SignalHandler("pressed", nameof(_aboutBtn))]
	void OnAboutPressed() {
		_generalBtn.Pressed = false;
		_projectsBtn.Pressed = false;
		_aboutBtn.Pressed = true;
		_contributionBtn.Pressed = false;
		_licensesBtn.Pressed = false;
		_pages.CurrentTab = 2;
	}

	[SignalHandler("pressed", nameof(_contributionBtn))]
	void OnContributionPressed() {
		_generalBtn.Pressed = false;
		_projectsBtn.Pressed = false;
		_aboutBtn.Pressed = false;
		_contributionBtn.Pressed = true;
		_licensesBtn.Pressed = false;
		_pages.CurrentTab = 3;
	}

	[SignalHandler("pressed", nameof(_licensesBtn))]
	void OnLicensesPressed() {
		_generalBtn.Pressed = false;
		_projectsBtn.Pressed = false;
		_aboutBtn.Pressed = false;
		_contributionBtn.Pressed = false;
		_licensesBtn.Pressed = true;
		_pages.CurrentTab = 4;
	}
#endregion

#region Event Handlers for Action Buttons
	[SignalHandler("clicked", nameof(_actionButtons))]
	void OnActionButtonsClicked(int index) {
		switch (index) {
			case 0:
				UpdateSettings();
				updateActionButtons();
				break;
			case 1:
				bPInternal = true;
				while (_undoActions.Count > 0) {
					_undoActions.Pop().Invoke();
				}
				updateActionButtons();
				bPInternal = false;
				break;
		}
	}
#endregion

#region Event Handlers for General Page
	[SignalHandler("text_changed", nameof(_godotInstallLocation))]
	void OnGodotInstallLocation() {
		string oldVal = CentralStore.Settings.EnginePath;
		if (!bPInternal) {
			_undoActions.Push(() => {
				CentralStore.Settings.EnginePath = oldVal;
				_godotInstallLocation.Text = oldVal.GetOSDir().NormalizePath();
			});
			updateActionButtons();
		}
		CentralStore.Settings.EnginePath = _godotInstallLocation.Text;
	}

	[SignalHandler("pressed", nameof(_godotBrowseButton))]
	void OnGodotBrowse() {
		AppDialogs.BrowseFolderDialog.Connect("dir_selected", this, "OnBrowseGodot_DirSelected", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseFolderDialog.Connect("popup_hide", this, "OnPopupHide", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseFolderDialog.CurrentDir = "";
		AppDialogs.BrowseFolderDialog.PopupCentered();
	}

	void OnBrowseGodot_DirSelected(string dir_name) {
		_godotInstallLocation.Text = dir_name.GetOSDir().NormalizePath();
		OnGodotInstallLocation();
	}

	void OnPopupHide()
	{
		if (AppDialogs.BrowseFolderDialog.IsConnected("dir_selected", this, "OnBrowseGodot_DirSelected"))
			AppDialogs.BrowseFolderDialog.Disconnect("dir_selected", this, "OnBrowseGodot_DirSelected");

		if (AppDialogs.BrowseFolderDialog.IsConnected("dir_selected", this, "OnBrowseCache_DirSelected"))
			AppDialogs.BrowseFolderDialog.Disconnect("dir_selected", this, "OnBrowseCache_DirSelected");

		if (AppDialogs.BrowseFolderDialog.IsConnected("dir_selected", this, "OnBrowseProjectLocation_DirSelected"))
			AppDialogs.BrowseFolderDialog.Disconnect("dir_selected", this, "OnBrowseProjectLocation_DirSelected");

		if (AppDialogs.BrowseFolderDialog.IsConnected("dir_selected", this, "OnDirScan_DirSelected"))
			AppDialogs.BrowseFolderDialog.Disconnect("dir_selected", this, "OnDirScan_DirSelected");

		if (AppDialogs.BrowseFolderDialog.IsConnected("dir_selected", this, "OnEditDirScan_DirSelected"))
			AppDialogs.BrowseFolderDialog.Disconnect("dir_selected", this, "OnEditDirScan_DirSelected");
	}

	[SignalHandler("text_changed", nameof(_cacheInstallLocation))]
	void OnCacheInstallLocation() {
		string oldVal = CentralStore.Settings.CachePath;
		if (!bPInternal) {
			_undoActions.Push(() => {
				_cacheInstallLocation.Text = oldVal.GetOSDir().NormalizePath();
			});
			updateActionButtons();
		}
		_cacheInstallLocation.Text = _cacheInstallLocation.Text.GetOSDir().NormalizePath();
	}

	[SignalHandler("pressed", nameof(_cacheBrowseButton))]
	void OnBrowseCacheLocation() {
		AppDialogs.BrowseFolderDialog.Connect("dir_selected", this, "OnBrowseCache_DirSelected", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseFolderDialog.Connect("popup_hide", this, "OnPopupHide", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseFolderDialog.CurrentDir = "";
		AppDialogs.BrowseFolderDialog.PopupCentered();
	}

	void OnBrowseCache_DirSelected(string dir_name) {
		_cacheInstallLocation.Text = dir_name.GetOSDir().NormalizePath();
		OnCacheInstallLocation();
	}

	[SignalHandler("item_selected", nameof(_defaultProjectView))]
	void OnDefaultProjectView(int index) {
		string oldVal = CentralStore.Settings.DefaultView;
		if (!bPInternal) {
			_undoActions.Push(() => {
				CentralStore.Settings.DefaultView = oldVal;
				_defaultProjectView.Select(_views.IndexOf(oldVal));
			});
			updateActionButtons();
		}
		CentralStore.Settings.DefaultView = _defaultProjectView.GetItemText(index);
	}

	[SignalHandler("toggled", nameof(_noConsole))]
	void OnNoConsole(bool toggle) {
		bool oldVal = CentralStore.Settings.NoConsole;
		if (!bPInternal) {
			_undoActions.Push(() => {
				CentralStore.Settings.NoConsole = oldVal;
				_noConsole.Pressed = oldVal;
			});
			updateActionButtons();
		}
		CentralStore.Settings.NoConsole = toggle;
	}

	[SignalHandler("toggled", nameof(_editorProfiles))]
	void OnEditorProfiles(bool toggle) {
		bool oldVal = CentralStore.Settings.SelfContainedEditors;
		if (!bPInternal) {
			_undoActions.Push(() => {
				CentralStore.Settings.SelfContainedEditors = oldVal;
				_editorProfiles.Pressed = oldVal;
			});
			updateActionButtons();
		}
		CentralStore.Settings.SelfContainedEditors = toggle;
	}

	#region Asset Mirror Actions
	[SignalHandler("add_requested", nameof(_assetMirror))]
	void OnAssetMirror_Add() {
		AppDialogs.AddonMirror.Connect("asset_add_mirror", this, "OnAssetAddMirror");
		AppDialogs.AddonMirror.ShowDialog();
	}

	void OnAssetAddMirror(string protocol, string domainName, string pathTo) {
		string url = $"{protocol}://{domainName}{pathTo}";

		int indx = _assetMirror.GetItemCount();
		_undoActions.Push(() => {
			_assetMirror.RemoveItem(indx);
			_assetMirror.RemoveMeta(domainName);
		});
		updateActionButtons();

		_assetMirror.AddItem(domainName);
		_assetMirror.SetMeta(domainName, url);
		
		AppDialogs.AddonMirror.Disconnect("asset_add_mirror", this, "OnAssetAddMirror");
	}

	[SignalHandler("edit_requested", nameof(_assetMirror))]
	void OnAssetMirror_Edit() {
		int indx = _assetMirror.GetSelected();
		if (indx == -1)
			return;

		var url = (string)_assetMirror.GetMeta(_assetMirror.GetItemText(indx));
		var uri = new Uri(url);

		AppDialogs.AddonMirror.Connect("asset_add_mirror", this, "OnAssetEditMirror");
		AppDialogs.AddonMirror.ShowDialog(uri.Scheme, uri.Host, uri.AbsolutePath, true);
	}

	void OnAssetEditMirror(string protocol, string domainName, string pathTo) {
		string url = $"{protocol}://{domainName}{pathTo}";
		int indx = _assetMirror.GetSelected();

		var oldName = _assetMirror.GetItemText(indx);
		var oldUrl = (string)_assetMirror.GetMeta(oldName);
		
		if (oldName != domainName)
			_assetMirror.RemoveMeta(oldName);
		
		_assetMirror.SetItemText(indx, domainName);
		_assetMirror.SetMeta(domainName, url);

		_undoActions.Push(() => {
			if (oldName != domainName)
				_assetMirror.RemoveMeta(domainName);
			
			_assetMirror.SetMeta(oldName, oldUrl);
			_assetMirror.SetItemText(indx, oldName);
		});
		updateActionButtons();

		AppDialogs.AddonMirror.Disconnect("asset_add_mirror", this, "OnAssetEditMirror");
	}

	[SignalHandler("remove_requested", nameof(_assetMirror))]
	void OnAssetMirror_Remove() {
		int indx = _assetMirror.GetSelected();
		if (indx == -1)
			return;
		
		var oldName = _assetMirror.GetItemText(indx);
		var oldUrl = _assetMirror.GetMeta(oldName);

		_undoActions.Push(() => {
			var nindx = _assetMirror.GetItemCount();
			_assetMirror.AddItem(oldName);
			_assetMirror.SetMeta(oldName, oldUrl);
			_assetMirror.MoveItem(nindx, indx);
		});
		updateActionButtons();

		_assetMirror.RemoveItem(indx);
		_assetMirror.RemoveMeta(oldName);
	}
	#endregion

	// #region Godot Mirror Actions
	// [SignalHandler("add_requested", nameof(_godotMirror))]
	// void OnGodotMirror_Add() {

	// }

	// [SignalHandler("edit_requested", nameof(_godotMirror))]
	// void OnGodotMirror_Edit() {

	// }

	// [SignalHandler("remove_requested", nameof(_godotMirror))]
	// void OnGodotMirror_Remove() {

	// }
	// #endregion
#endregion

#region Event Handlers for Projects Page
	[SignalHandler("text_changed", nameof(_defaultProjectLocation))]
	void OnDefaultProjectLocation() {
		string oldVal = CentralStore.Settings.ProjectPath;
		if (!bPInternal) {
			_undoActions.Push(() => {
				CentralStore.Settings.ProjectPath = oldVal;
				_defaultProjectLocation.Text = oldVal.GetOSDir().NormalizePath();
			});
			updateActionButtons();
		}
		CentralStore.Settings.ProjectPath = _defaultProjectLocation.Text;
	}

	[SignalHandler("pressed", nameof(_browseProjectLocation))]
	void OnBrowseProjectLocation_Pressed() {
		AppDialogs.BrowseFolderDialog.CurrentDir = _defaultProjectLocation.Text.NormalizePath();
		AppDialogs.BrowseFolderDialog.PopupCentered();
		AppDialogs.BrowseFolderDialog.Connect("dir_selected", this, "OnBrowseProjectLocation_DirSelected", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseFolderDialog.Connect("popup_hide", this, "OnPopupHide", null, (uint)ConnectFlags.Oneshot);
	}

	void OnBrowseProjectLocation_DirSelected(string path) {
		_defaultProjectLocation.Text = path.NormalizePath();
		AppDialogs.BrowseFolderDialog.Visible = false;
		OnDefaultProjectLocation();
	}

	[SignalHandler("toggled", nameof(_autoScanProjects))]
	void OnAutoScanProjects(bool toggle)
	{
		bool oldVal = CentralStore.Settings.EnableAutoScan;
		if (!bPInternal) {
			_undoActions.Push(() => {
				CentralStore.Settings.EnableAutoScan = oldVal;
				_autoScanProjects.Pressed = oldVal;
			});
			updateActionButtons();
		}
		CentralStore.Settings.EnableAutoScan = toggle;
	}

	[SignalHandler("toggled", nameof(_useSystemTitlebar))]
	void OnUseSystemTitlebar(bool toggle) {
		bool oldVal = CentralStore.Settings.UseSystemTitlebar;
		if (!bPInternal) {
			_undoActions.Push(() => {
				CentralStore.Settings.UseSystemTitlebar = oldVal;
				_useSystemTitlebar.Pressed = oldVal;
			});
			updateActionButtons();
		}
		CentralStore.Settings.UseSystemTitlebar = toggle;
	}

	[SignalHandler("toggled", nameof(_useLastMirror))]
	void OnUseLastMirror(bool toggle)
	{
		bool oldVal = CentralStore.Settings.UseLastMirror;
		if (!bPInternal)
		{
			_undoActions.Push(() =>
			{
				CentralStore.Settings.UseLastMirror = oldVal;
				_useLastMirror.Pressed = oldVal;
			});
			updateActionButtons();
		}

		CentralStore.Settings.UseLastMirror = toggle;
	}

	#region Directory Scan List Actions
	[SignalHandler("add_requested", nameof(_directoryScan))]
	void OnDirScan_AddRequest() {
		AppDialogs.BrowseFolderDialog.CurrentDir = _defaultProjectLocation.Text.NormalizePath();
		AppDialogs.BrowseFolderDialog.PopupCentered();
		AppDialogs.BrowseFolderDialog.Connect("dir_selected", this, "OnDirScan_DirSelected", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseFolderDialog.Connect("popup_hide", this, "OnPopupHide", null, (uint)ConnectFlags.Oneshot);
	}

	void OnDirScan_DirSelected(string path) {
		int index = _directoryScan.GetItemCount();
		_directoryScan.AddItem(path.NormalizePath());
		if (!bPInternal) {
			_undoActions.Push(() => _directoryScan.RemoveItem(index));
			updateActionButtons();
		}
		AppDialogs.BrowseFolderDialog.Visible = false;
	}

	[SignalHandler("edit_requested", nameof(_directoryScan))]
	void OnDirScan_EditRequest() {
		int index = _directoryScan.GetSelected();
		if (index == -1)
			return;
		AppDialogs.BrowseFolderDialog.CurrentDir = _directoryScan.GetItemText(index).NormalizePath();
		AppDialogs.BrowseFolderDialog.Connect("dir_selected", this, "OnEditDirScan_DirSelected", new Array() {index}, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseFolderDialog.Connect("popup_hide", this, "OnPopupHide", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseFolderDialog.PopupCentered();
	}

	void OnEditDirScan_DirSelected(string path, int index) {
		string oldVal = _directoryScan.GetItemText(index);
		_undoActions.Push(() => _directoryScan.SetItemText(index, oldVal));
		updateActionButtons();
		_directoryScan.SetItemText(index, path);
	}

	[SignalHandler("remove_requested", nameof(_directoryScan))]
	void OnDirScan_RemoveRequest() {
		int indx = _directoryScan.GetSelected();
		if (indx == -1)
			return;
		string oldVal = _directoryScan.GetItemText(indx);
		if (!bPInternal) {
			_undoActions.Push(() => {
				int nidx = _directoryScan.GetItemCount();
				_directoryScan.AddItem(oldVal);
				_directoryScan.MoveItem(nidx, indx);
			});
			updateActionButtons();
		}
		_directoryScan.RemoveItem(indx);
	}
	#endregion

#endregion

#region Event Handler for About Page
	[SignalHandler("meta_clicked", nameof(_emailWebsite))]
	[SignalHandler("meta_clicked", nameof(_builtWith))]
	[SignalHandler("meta_clicked", nameof(_mitLicense))]
	[SignalHandler("meta_clicked", nameof(_apacheLicense))]
	[SignalHandler("meta_clicked", nameof(_contributors))]
	void OnMetaClicked(object meta) {
		OS.ShellOpen((string)meta);
	}

	[SignalHandler("gui_input", nameof(_buyMe))]
	void OnBuyMe_GuiInput(InputEvent inputEvent) {
		if (inputEvent is InputEventMouseButton iembEvent) {
			if (iembEvent.Pressed && iembEvent.ButtonIndex == (int)ButtonList.Left) {
				OS.ShellOpen("https://www.buymeacoffee.com/eumario");
			}
		}
	}

	[SignalHandler("gui_input", nameof(_itchIo))]
	void OnItchIo_GuiInput(InputEvent inputEvent) {
		if (inputEvent is InputEventMouseButton iembEvent) {
			if (iembEvent.Pressed && iembEvent.ButtonIndex == (int)ButtonList.Left) {
				OS.ShellOpen("https://eumario.itch.io/godot-manager");
			}
		}
	}

	[SignalHandler("gui_input", nameof(_github))]
	void OnGithub_GuiInput(InputEvent inputEvent) {
		if (inputEvent is InputEventMouseButton iembEvent) {
			if (iembEvent.Pressed && iembEvent.ButtonIndex == (int)ButtonList.Left) {
				OS.ShellOpen("https://github.com/eumario/godot-manager");
			}
		}
	}

	[SignalHandler("gui_input", nameof(_discord))]
	void OnDiscord_GuiInput(InputEvent inputEvent) {
		if (inputEvent is InputEventMouseButton iembEvent) {
			if (iembEvent.Pressed && iembEvent.ButtonIndex == (int)ButtonList.Left) {
				OS.ShellOpen("https://discord.gg/ESkwAMN2Tt");
			}
		}
	}
#endregion

}
