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
	[NodePath("PC/CC/P/VB/MCContent/TabContainer/ProjectSettings/ProjectName/ProjectName")]
	LineEdit _projectName = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/ProjectSettings/ProjectName/CreateFolder")]
	Button _createFolder = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/ProjectSettings/ProjectLocation/ProjectLocation")]
	LineEdit _projectLocation = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/ProjectSettings/ProjectLocation/Browse")]
	Button _browseLocation = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/ProjectSettings/ProjectLocation/ErrorIcon")]
	TextureRect _errorIcon = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/ProjectSettings/ErrorText")]
	Label _errorText = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/ProjectSettings/TemplateProject")]
	OptionButton _templateProject = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/ProjectSettings/Renderers")]
	VBoxContainer _renderers = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/ProjectSettings/Message")]
	Label _message = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/ProjectSettings/GodotVersion")]
	OptionButton _godotVersion = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/ProjectSettings/Renderers/Checkboxes/AdvRenderer/AdvRenderer")]
	CheckBox _advRenderer = null;
	
	[NodePath("PC/CC/P/VB/MCContent/TabContainer/ProjectSettings/Renderers/Checkboxes/AdvRenderer/AdvRendererDesc")]
	Label _advRendererDesc = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/ProjectSettings/Renderers/Checkboxes/SimpleRenderer/SimpleRenderer")]
	CheckBox _simpleRenderer = null;
	
	[NodePath("PC/CC/P/VB/MCContent/TabContainer/ProjectSettings/Renderers/Checkboxes/SimpleRenderer/SimpleRendererDesc")]
	Label _simpleRendererDesc = null;

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/ProjectPlugins/ScrollContainer/List")]
	VBoxContainer _pluginList = null;

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
		GodotVersion gdVers = (GodotVersion)_godotVersion.GetSelectedMetadata();
		int gdVersNum = gdVers.GetVersion();

		NewProject prj = new NewProject {
			ProjectName = _projectName.Text,
			ProjectLocation = _projectLocation.Text,
			GodotId = gdVers.Id,
			GodotVersion = gdVersNum,
			Plugins = new Array<AssetPlugin>(),
			UseAdvRenderer = _advRenderer.Pressed
		};
		if (_templateProject.Selected > 0)
			prj.Template = _templateProject.GetSelectedMetadata() as AssetProject;
		
		foreach(AddonLineEntry ale in _pluginList.GetChildren()) {
			if (ale.Installed) {
				prj.Plugins.Add(ale.GetMeta("asset") as AssetPlugin);
			}
		}

		prj.CreateProject();
		ProjectFile pf = ProjectFile.ReadFromFile(prj.ProjectLocation.PlusFile(gdVersNum <= 2 ? "engine.cfg" : "project.godot").NormalizePath(), gdVersNum);
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
		if (bfdir.IsDirEmpty() && _projectName.Text == "Untitled Project")
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
		GodotVersion versionData = (GodotVersion)_godotVersion.GetSelectedMetadata();
		int version = versionData.GetVersion();
		if (version <= 1)
		{
			_renderers.Visible = false;
			_message.Text = Tr(@"The renderer cannot be changed and addons are not available in v1.x projects.");
		}
		else if (version == 2)
		{
			string tag = versionData.Tag.ToLower();
			_renderers.Visible = false;
			if (tag.StartsWith("2.1") || tag.StartsWith("v2.1"))
			{
				_message.Text = Tr(@"The renderer cannot be changed in v2.x projects.");
			} else
			{
				_message.Text = Tr(@"The renderer cannot be changed in v2.x projects.
									Addons are only available in v2.1+ projects.");
			}
		}
		else if (version >= 3)
		{
			_renderers.Visible = true;
			_advRenderer.Pressed = true;
			_simpleRenderer.Pressed = false;
			if (version == 3)
			{
				_advRenderer.Text = Tr("OpenGL ES 3.0");
				_simpleRenderer.Text = Tr("OpenGL ES 2.0");
				_advRendererDesc.Text = Tr(@"Higher visual quality.
										All features available.
										Incompatible with older hardware.
										Not recommended for web games.");
				_simpleRendererDesc.Text = Tr(@"Lower visual quality.
										Some features not available.
										Works on most hardware.
										Recommended for web games.");
			}
			else if (version >= 4)
			{
				_advRenderer.Text = Tr("Forward+");
				_simpleRenderer.Text = Tr("Mobile");
				_advRendererDesc.Text = Tr(@"Supports desktop platforms only.
									Advanced 3D graphics available.
									Can scale to large complex scenes.
									Slower rendering of simple scenes.");
				_simpleRendererDesc.Text = Tr(@"Supports desktop + mobile platforms.
									Less advanced 3D graphics.
									Less scalable for complex scenes.
									Faster rendering of simple scenes.");
			}
			_message.Text = "The renderer can be changed later, but scenes may need to be adjusted.";
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
			string gdName = CentralStore.Versions[indx].GetDisplayName();
			if (CentralStore.Versions[indx].Id == (string)CentralStore.Settings.DefaultEngine)
			{
				defaultGodot = indx;
				gdName += " (Default)";
			}

			_godotVersion.AddItem(gdName, indx);
			_godotVersion.SetItemMetadata(indx, CentralStore.Versions[indx]);
		}

		if (defaultGodot != -1)
			_godotVersion.Select(defaultGodot);

		_projectName.Text = "Untitled Project";
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

		foreach (AddonLineEntry node in _pluginList.GetChildren()) node.QueueFree();

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
