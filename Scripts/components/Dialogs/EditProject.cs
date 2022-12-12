using Godot;
using Godot.Sharp.Extras;
using Godot.Collections;
using System.Linq;
using Directory = System.IO.Directory;
using SFile = System.IO.File;
using System.IO.Compression;
using System.Text.RegularExpressions;

public class EditProject : ReferenceRect
{
#region Signals
	[Signal]
	public delegate void project_updated();
#endregion

#region Node Paths
[NodePath("PC/CC/P/VB/MCContent/TC")] private TabContainer _TabContainer = null;

#region General Tab
[NodePath("PC/CC/P/VB/MCContent/TC/General/VC/HC/ProjectIcon")]
TextureRect _Icon = null;

[NodePath("PC/CC/P/VB/MCContent/TC/General/VC/HC/MC/VC/ProjectName")]
LineEdit _ProjectName = null;

[NodePath("PC/CC/P/VB/MCContent/TC/General/VC/GodotVersion")]
OptionButton _GodotVersion = null;

[NodePath("PC/CC/P/VB/MCContent/TC/General/VC/ProjectDescription")]
TextEdit _ProjectDescription = null;
#endregion

#region Plugins Tab
[NodePath("PC/CC/P/VB/MCContent/TC/Addons/Project Plugins/ScrollContainer/MC/List")]
VBoxContainer _PluginList = null;
#endregion

#region Dialog Controls
[NodePath("PC/CC/P/VB/MCButtons/HB/SaveBtn")]
Button _SaveBtn = null;

[NodePath("PC/CC/P/VB/MCButtons/HB/CancelBtn")]
Button _CancelBtn = null;
#endregion
#endregion

#region Resources
[Resource("res://components/AddonLineEntry.tscn")] private PackedScene ALineEntry = null;
[Resource("res://Assets/Icons/default_project_icon.png")] private Texture DefaultIcon = null;
#endregion

#region Private Variables
	ProjectFile _pf = null;
	bool _isDirty = false;
	EPData _data;
	Regex _gitTag = new Regex("-[A-Za-z0-9]{40,}");
#endregion

#region Internal Structure
	struct EPData {
		public string IconPath;
		public string ProjectName;
		public string GodotVersion;
		public string Description;
		public Array<string> QueueAddons;
	}
#endregion

#region Public Variables
	public string IconPath {
		get => _data.IconPath;
		set => _data.IconPath = value;
	}

	public string ProjectName {
		get => _data.ProjectName;
		set => _data.ProjectName = value;
	}

	public string GodotVersion { 
		get => _data.GodotVersion;
		set => _data.GodotVersion = value;
	}

	public string Description {
		get => _data.Description;
		set => _data.Description = value;
	}

	public ProjectFile ProjectFile {
		get => _pf;
		set {
			_pf = value;
			IconPath = _pf.Icon;
			ProjectName = _pf.Name;
			GodotVersion = _pf.GodotVersion;
			Description = _pf.Description;
		}
	}
#endregion

	public override void _Ready()
	{
		this.OnReady();
		_data = new EPData();
		_data.QueueAddons = new Array<string>();
	}

#region Public Functions
	public void ShowDialog(ProjectFile pf) {
		foreach(AddonLineEntry node in _PluginList.GetChildren()) {
			node.QueueFree();
		}

		foreach(AssetPlugin plgn in CentralStore.Plugins)
		{
			string imgLoc =
				$"{CentralStore.Settings.CachePath}/images/{plgn.Asset.AssetId}{plgn.Asset.IconUrl.GetExtension()}"
					.NormalizePath();
			AddonLineEntry ale = ALineEntry.Instance<AddonLineEntry>();
			
			ale.Icon = Util.LoadImage(imgLoc);
			if (ale.Icon == null) ale.Icon = DefaultIcon;
			
			ale.Title = plgn.Asset.Title;
			ale.Version = plgn.Asset.VersionString;
			ale.SetMeta("asset", plgn);
			_PluginList.AddChild(ale);
			ale.Connect("install_clicked", this, "OnToggledPlugin");
		}

		ProjectFile = pf;
		PopulateData();
		Visible = true;
		_PluginList.GetParent().GetParent<ScrollContainer>().ScrollVertical = 0;
		_TabContainer.CurrentTab = 0;
	}
#endregion

#region Private Functions
	void PopulateData() {
		_Icon.Texture = Util.LoadImage(ProjectFile.Location.GetResourceBase(IconPath));
		_ProjectName.Text = ProjectName;
		_ProjectDescription.Text = Description;
		_GodotVersion.Clear();
		foreach(GodotVersion gdver in CentralStore.Versions) {
			string gdName = gdver.GetDisplayName();
			if (gdver.Id == CentralStore.Settings.DefaultEngine)
				gdName += Tr(" (Default)");
			_GodotVersion.AddItem(gdName);
			_GodotVersion.SetItemMetadata(_GodotVersion.GetItemCount()-1, gdver.Id);
			if (ProjectFile.GodotVersion == gdver.Id)
				_GodotVersion.Selected = _GodotVersion.GetItemCount()-1;
		}

		foreach(AddonLineEntry ale in _PluginList.GetChildren())
		{
			ale.Installed = false;
		}

		if (ProjectFile.Assets == null)
			ProjectFile.Assets = new Array<string>();
		
		foreach(string assetId in ProjectFile.Assets) {
			foreach(AddonLineEntry ale in _PluginList.GetChildren()) {
				AssetPlugin plugin = (AssetPlugin)ale.GetMeta("asset");
				if (plugin.Asset.AssetId == assetId)
				{
					ale.Installed = true;
				}
			}
		}
		
		_isDirty = false;
		_SaveBtn.Disabled = true;
	}

	void UpdatePlugins() {
		Array<AssetPlugin> plugins = new Array<AssetPlugin>();
		Array<AssetPlugin> install = new Array<AssetPlugin>();
		Array<AssetPlugin> remove = new Array<AssetPlugin>();

		foreach(AddonLineEntry ale in _PluginList.GetChildren()) {
			if (ale.Installed)
				plugins.Add((AssetPlugin)ale.GetMeta("asset"));
		}

		var res = from asset in plugins
				where !ProjectFile.Assets.Contains(asset.Asset.AssetId)
				select asset;

		foreach(AssetPlugin asset in res.AsEnumerable<AssetPlugin>())
			install.Add(asset);
		
		foreach(string assetId in ProjectFile.Assets) {
			var ares = from asset in plugins
						where asset.Asset.AssetId == assetId
						select asset;
			if (ares.FirstOrDefault() == null)
				remove.Add(CentralStore.Instance.GetPluginId(assetId));
		}

		foreach(AssetPlugin plugin in remove) {
			PluginInstaller installer = new PluginInstaller(plugin);
			installer.Uninstall(ProjectFile.Location.GetBaseDir().NormalizePath());
			ProjectFile.Assets.Remove(plugin.Asset.AssetId);
		}

		foreach(AssetPlugin plugin in install) {
			PluginInstaller installer = new PluginInstaller(plugin);
			installer.Install(ProjectFile.Location.GetBaseDir().NormalizePath());
			ProjectFile.Assets.Add(plugin.Asset.AssetId);
		}
		
		CentralStore.Instance.SaveDatabase();
	}
#endregion

#region Event Handlers
	void OnToggledPlugin(bool toggle) {
		_isDirty = true;
		_SaveBtn.Disabled = false;
	}

	[SignalHandler("pressed", nameof(_SaveBtn))]
	void OnSaveBtnPressed() {
		ProjectFile.Name = ProjectName;
		ProjectFile.Description = Description;
		ProjectFile.Icon = IconPath;
		ProjectFile.GodotVersion = GodotVersion;
		ProjectFile.WriteUpdatedData();
		UpdatePlugins();
		CentralStore.Instance.SaveDatabase();
		EmitSignal("project_updated");
		Visible = false;
	}

	[SignalHandler("pressed", nameof(_CancelBtn))]
	async void OnCancelBtnPressed() {
		if (_isDirty) {
			var res = AppDialogs.YesNoDialog.ShowDialog(Tr("Edit Project"), Tr("You have unsaved changes, do you wish to stop editing?"));
			await res;
			if (res.Result) {
				Visible = false;
			}
		} else {
			Visible = false;
		}
	}

	[SignalHandler("gui_input", nameof(_Icon))]
	void OnIconGuiInput(InputEvent inputEvent) {
		if (inputEvent is InputEventMouseButton iemb) {
			if (iemb.Pressed && iemb.ButtonIndex == (int)ButtonList.Left)
			{
				AppDialogs.ImageFileDialog.Connect("file_selected", this, "OnFileSelected", null,
					(int)ConnectFlags.Oneshot);
				AppDialogs.ImageFileDialog.Connect("popup_hide", this, "OnFilePopupHide", null,
					(uint)ConnectFlags.Oneshot);
				AppDialogs.ImageFileDialog.CurrentDir = ProjectFile.Location.GetBaseDir();
				AppDialogs.ImageFileDialog.PopupCentered();
			}
		}
	}

	async void OnFileSelected(string path) {
		if (path == "")
			return;
		var pfpath = ProjectFile.Location.GetBaseDir().Replace(@"\", "/");
		if (path.StartsWith(pfpath))
			IconPath = pfpath.GetProjectRoot(path);
		else {
			var ret = AppDialogs.YesNoDialog.ShowDialog(Tr("Icon Selection"),
				Tr("This file is outside your project structure, do you want to copy it to the root of your project?"));
			await ret;
			if (ret.Result) {
				SFile.Copy(path, pfpath.PlusFile(path.GetFile()));
				IconPath = pfpath.GetProjectRoot(path);
			} else {
				AppDialogs.MessageDialog.ShowMessage(Tr("Icon Selection"),
					Tr("Icon not copied, unable to use icon for Project."));
				AppDialogs.ImageFileDialog.Visible = false;
				return;
			}
		}
		_Icon.Texture = Util.LoadImage(path);
		AppDialogs.ImageFileDialog.Visible = false;
		_isDirty = true;
		_SaveBtn.Disabled = false;
	}

	void OnFilePopupHide() {
		AppDialogs.ImageFileDialog.Disconnect("file_selected", this, "OnFileSelected");
	}

	[SignalHandler("text_changed", nameof(_ProjectName))]
	void OnProjectNameTextChanged(string text) {
		ProjectName = text;
		_isDirty = true;
		_SaveBtn.Disabled = false;
	}

	[SignalHandler("item_selected", nameof(_GodotVersion))]
	void OnGodotVersionItemSelected(int index) {
		GodotVersion = _GodotVersion.GetItemMetadata(index) as string;
		_isDirty = true;
		_SaveBtn.Disabled = false;
	}

	[SignalHandler("text_changed", nameof(_ProjectDescription))]
	void OnProjectDescriptionTextChanged() {
		Description = _ProjectDescription.Text;
		_isDirty = true;
		_SaveBtn.Disabled = false;
	}
#endregion

}
