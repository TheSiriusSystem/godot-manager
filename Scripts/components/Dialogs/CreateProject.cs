using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Godot.Sharp.Extras;
using Guid = System.Guid;
using Directory = System.IO.Directory;

public class CreateProject : ReferenceRect
{

#region Signals
	[Signal]
	public delegate void project_created(ProjectFile projFile);
#endregion

#region Node Paths
	[NodePath("PC/CC/P/VB/MCContent/TabContainer")]
	TabContainer _tabs = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/Settings/ProjectName/ProjectName")]
	LineEdit _projectName = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/Settings/ProjectName/CreateFolder")]
	Button _createFolder = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/Settings/ProjectLocation/ProjectLocation")]
	LineEdit _projectLocation = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/Settings/ProjectLocation/Browse")]
	Button _browseLocation = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/Settings/ProjectLocation/ErrorIcon")]
	TextureRect _errorIcon = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/Settings/ErrorText")]
	Label _errorText = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/Settings/TemplateProject")]
	OptionButton _templateProject = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/Settings/Renderers")]
	VBoxContainer _renderers = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/Settings/Renderers/HB/Options")]
	VBoxContainer _rendererOptions = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/Settings/GodotVersion")]
	OptionButton _godotVersion = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/Settings/Renderers/HB/Description")]
	Label _rendererDesc = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/Addons/ScrollContainer")]
	ScrollContainer _sc = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/Addons/ScrollContainer/List")]
	VBoxContainer _pluginList = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/Addons/ErrorText")]
	Label _pluginErrorText = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/CreateBtn")]
	Button _createBtn = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/CancelBtn")]
	Button _cancelBtn = null;
#endregion

#region Resources
	Texture StatusError = GD.Load<Texture>("res://Assets/Icons/icon_status_error.svg");
	Texture StatusSuccess = GD.Load<Texture>("res://Assets/Icons/icon_status_success.svg");
	Texture StatusWarning = GD.Load<Texture>("res://Assets/Icons/icon_status_warning.svg");
#endregion

#region Assets
	[Resource("res://components/AddonLineEntry.tscn")] private PackedScene ALineEntry = null;
	[Resource("res://Assets/Icons/default_project_icon_v3.png")] private Texture DefaultIcon = null;
#endregion

#region Variables
#endregion

#region Helper Functions
	public enum DirError {
		OK,
		ERROR,
		WARNING
	}

	public void ShowMessage(string msg, DirError err) {
		_errorText.Text = msg;
		switch(err) {
			case DirError.OK:
				_errorIcon.Texture = StatusSuccess;
				_createBtn.Disabled = false;
				break;
			case DirError.WARNING:
				_errorIcon.Texture = StatusWarning;
				_createBtn.Disabled = false;
				break;
			case DirError.ERROR:
				_errorIcon.Texture = StatusError;
				_createBtn.Disabled = true;
				break;
		}
	}
#endregion

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.OnReady();
		ShowMessage("",DirError.OK);
	}

	[SignalHandler("pressed", nameof(_createBtn))]
	void OnCreatePressed() {
		GodotVersion gdVers = _godotVersion.GetSelectedMetadata() as GodotVersion;
		int gdMajorVers = gdVers.GetMajorVersion();
		Dictionary modifiedKeys = new Dictionary();

		if (_renderers.Visible) {
			IEnumerable<RendererOption> options = _rendererOptions.GetChildren().OfType<RendererOption>();
			foreach (RendererOption option in options) {
				if (option.Pressed) {
					modifiedKeys = option.currentMeta["keys"] as Dictionary;
					break;
				}
			}
		}

		NewProject prj = new NewProject {
			ProjectName = _projectName.Text,
			ProjectLocation = _projectLocation.Text,
			GodotId = gdVers.Id,
			GodotMajorVersion = gdMajorVers,
			GodotMinorVersion = gdVers.GetMinorVersion(),
			Plugins = new Array<AssetPlugin>(),
			ModifiedKeys = modifiedKeys.Duplicate()
		};
		if (_templateProject.Selected > 0)
			prj.Template = _templateProject.GetSelectedMetadata() as AssetProject;

		foreach(AddonLineEntry ale in _pluginList.GetChildren()) {
			if (ale.Installed) {
				prj.Plugins.Add(ale.GetMeta("asset") as AssetPlugin);
			}
		}

		prj.CreateProject();
		ProjectFile pf = ProjectFile.ReadFromFile(prj.ProjectLocation.PlusFile(gdMajorVers <= 2 ? "engine.cfg" : "project.godot").NormalizePath(), gdMajorVers);
		pf.GodotId = prj.GodotId;
		pf.Assets = new Array<string>();

		foreach(AssetPlugin plugin in prj.Plugins)
			pf.Assets.Add(plugin.Asset.AssetId);

		CentralStore.Projects.Add(pf);
		CentralStore.Instance.SaveDatabase();
		EmitSignal("project_created", pf);
		Visible = false;
	}

	[SignalHandler("pressed", nameof(_createFolder))]
	void OnCreateFolderPressed() {
		string path = _projectLocation.Text;
		string newDir = path.Join(_projectName.Text).NormalizePath();
		Directory.CreateDirectory(newDir);
		_projectLocation.Text = newDir;
		TestPath(newDir);
	}

	[SignalHandler("pressed", nameof(_browseLocation))]
	void OnBrowseLocationPressed() {
		AppDialogs.BrowseFolderDialog.CurrentFile = "";
		AppDialogs.BrowseFolderDialog.CurrentPath = (CentralStore.Settings.ProjectPath + "/").NormalizePath();
		AppDialogs.BrowseFolderDialog.PopupCentered(new Vector2(510, 390));
		AppDialogs.BrowseFolderDialog.Connect("dir_selected", this, "OnDirSelected", null, (int)ConnectFlags.Oneshot);
		AppDialogs.BrowseFolderDialog.Connect("popup_hide", this, "OnDirSelected_PopupHidden", null, (int)ConnectFlags.Oneshot);
	}

	void OnDirSelected(string bfdir) {
		bfdir = bfdir.NormalizePath();
		_projectLocation.Text = bfdir;
		AppDialogs.BrowseFolderDialog.Visible = false;
		AppDialogs.BrowseFolderDialog.Disconnect("dir_selected", this, "OnDirSelected");
		TestPath(bfdir);
		if (bfdir.IsDirEmpty() && _projectName.Text == "New Game Project")
			_projectName.Text = bfdir.GetFile().Capitalize();
	}

	void OnDirSelected_PopupHidden()
	{
		if (AppDialogs.BrowseFolderDialog.IsConnected("dir_selected", this, "OnDirSelected"))
			AppDialogs.BrowseFolderDialog.Disconnect("dir_selected", this, "OnDirSelected");
	}

	[SignalHandler("pressed", nameof(_cancelBtn))]
	void OnCancelPressed() {
		Visible = false;
	}

	[SignalHandler("item_selected", nameof(_godotVersion))]
	void OnVersionSelected(int index) {
		GodotVersion gdVers = _godotVersion.GetSelectedMetadata() as GodotVersion;
		int gdMajorVers = gdVers.GetMajorVersion();
		if (gdMajorVers <= 2 || (gdMajorVers == 3 && gdVers.GetMinorVersion() == 0))
		{
			_renderers.Visible = false;
		}
		else
		{
			_renderers.Visible = true;
			IEnumerable<RendererOption> options = _rendererOptions.GetChildren().OfType<RendererOption>();
			foreach (RendererOption option in options)
			{
				if (option.Pressed)
					option.EmitSignal("toggled", true);
				option.Pressed = (option.Name == "1");
				foreach (Dictionary meta in option.metadata) {
					if (gdMajorVers == (int)meta["version"]) {
						option.Visible = true;
						option.Text = (string)meta["name"];
						break;
					} else {
						option.Visible = false;
					}
				}
			}
		}
		PopulatePlugins();
	}

	private void PopulatePlugins() {
		foreach (AddonLineEntry node in _pluginList.GetChildren()) node.QueueFree();

		GodotVersion gdVers = _godotVersion.GetSelectedMetadata() as GodotVersion;
		int gdMajorVers = gdVers.GetMajorVersion();
		if (gdMajorVers <= 1 || (gdMajorVers == 2 && gdVers.GetMinorVersion() == 0)) {
			_sc.Visible = false;
			_pluginErrorText.Visible = true;
		} else {
			_sc.Visible = true;

			foreach (AssetPlugin plgn in CentralStore.Plugins)
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
				_pluginList.AddChild(ale);
			}

			_pluginErrorText.Visible = false;
		}
	}

	public void ShowDialog() {
		GodotVersion defaultEngine;
		if (CentralStore.Settings.DefaultEngine != string.Empty && CentralStore.Settings.DefaultEngine != Guid.Empty.ToString())
		{
			defaultEngine = CentralStore.Instance.GetVersion(CentralStore.Settings.DefaultEngine);
		}
		else if (CentralStore.Versions.Count > 0)
		{
			defaultEngine = CentralStore.Instance.GetVersion(CentralStore.Versions[0].Id);
		}
		else
		{
			AppDialogs.MessageDialog.ShowMessage(Tr("No Editor Versions Found"), Tr("You need to add an editor version before you can create a project."));
			return;
		}

		_godotVersion.Clear();
		int defaultGodot = -1;
		for (int indx = 0; indx < CentralStore.Versions.Count; indx++)
		{
			if (CentralStore.Versions[indx].Id == (string)CentralStore.Settings.DefaultEngine)
			{
				defaultGodot = indx;
			}

			_godotVersion.AddItem(CentralStore.Versions[indx].GetDisplayName(), indx);
			_godotVersion.SetItemMetadata(indx, CentralStore.Versions[indx]);
		}

		if (defaultGodot != -1)
			_godotVersion.Select(defaultGodot);

		_tabs.CurrentTab = 0;
		_projectName.Text = "New Game Project";
		_projectLocation.Text = CentralStore.Settings.ProjectPath;
		TestPath(CentralStore.Settings.ProjectPath);
		OnVersionSelected(_godotVersion.Selected);
		_templateProject.Clear();
		_templateProject.AddItem("None");
		foreach(AssetProject tmpl in CentralStore.Templates) {
			string gdName = tmpl.Asset.Title;
			_templateProject.AddItem(gdName);
			_templateProject.SetItemMetadata(CentralStore.Templates.IndexOf(tmpl)+1, tmpl);
		}
		PopulatePlugins();

		Visible = true;
	}

	[SignalHandler("text_changed", nameof(_projectLocation))]
	void OnProjectLocation_TextChanged(string new_text) {
		TestPath(new_text);
	}

	private void TestPath(string path) {
		if (!Directory.Exists(path)) {
			ShowMessage(Tr("The path specified doesn't exist."), DirError.ERROR);
			return;
		}

		if (!path.IsDirEmpty()) {
			ShowMessage(Tr("Please choose an empty folder."), DirError.ERROR);
		} else {
			ShowMessage("", DirError.OK);
		}
	}
}
