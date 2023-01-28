using System.Collections.Generic;
using System.Linq;
using System.IO.Compression;
using Godot;
using Godot.Collections;
using Godot.Sharp.Extras;
using File = System.IO.File;
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

	[NodePath("PC/CC/P/VB/MCContent/TabContainer/Settings/VersionControlMetadata/Option")]
	OptionButton _vcMetadata = null;

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
		switch (err) {
			case DirError.OK:
				_errorIcon.Texture = MainWindow._plTextures["StatusSuccess"];
				_createBtn.Disabled = false;
				break;
			case DirError.WARNING:
				_errorIcon.Texture = MainWindow._plTextures["StatusWarning"];
				_errorText.SelfModulate = new Color("ffdd65");
				_createBtn.Disabled = false;
				break;
			case DirError.ERROR:
				_errorIcon.Texture = MainWindow._plTextures["StatusError"];
				_errorText.SelfModulate = new Color("ff4848");
				_createBtn.Disabled = true;
				break;
		}
		_createBtn.MouseDefaultCursorShape = !_createBtn.Disabled ? CursorShape.PointingHand : CursorShape.Arrow;
	}
#endregion

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.OnReady();
		ShowMessage("",DirError.OK);
	}

	[SignalHandler("pressed", nameof(_createBtn))]
	async void OnCreatePressed() {
		GodotVersion gdVers = _godotVersion.GetSelectedMetadata() as GodotVersion;
		int gdMajorVers = gdVers.GetMajorVersion();
		string pfName = gdMajorVers <= 2 ? "engine.cfg" : "project.godot";

		AssetProject template = _templateProject.Selected <= 0 ? null : _templateProject.GetSelectedMetadata() as AssetProject;
		if (template != null) {
			string templatePath = template.Location.NormalizePath();
			if (!File.Exists(templatePath)) {
				AppDialogs.MessageDialog.ShowMessage(Tr("Error"), Tr("The template project archive was not found."));
				return;
			}

			bool foundPF = false;
			using (ZipArchive za = ZipFile.OpenRead(templatePath)) {
				foreach (ZipArchiveEntry zae in za.Entries) {
					if (!zae.FullName.EndsWith("/")) {
						int pp = zae.FullName.Find("/") + 1;
						string fpath = zae.FullName.Substr(pp, zae.FullName.Length);
						if (zae.Name == pfName && _projectLocation.Text.PlusFile(fpath).NormalizePath() == _projectLocation.Text.PlusFile(fpath.GetFile()).NormalizePath()) {
							foundPF = true;
							break;
						}
					}
				}
			}
			if (!foundPF) {
				AppDialogs.MessageDialog.ShowMessage(Tr("Error"), string.Format(Tr("{0} was not found in the template project archive's root."), pfName));
				return;
			}
		}

		if (!_projectLocation.Text.IsDirEmpty())
		{
			bool res = await AppDialogs.YesNoDialog.ShowDialog(Tr("Warning"),
				Tr("You are about to create a Godot project in a non-empty folder. The entire contents of this folder will be imported as project resources!\nAre you sure you wish to continue?"));
			if (!res) return;
		}

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
			VersionControlSystem = _vcMetadata.Selected,
			Template = template,
			GodotId = gdVers.Id,
			GodotMajorVersion = gdMajorVers,
			GodotMinorVersion = gdVers.GetMinorVersion(),
			Plugins = new Array<AssetPlugin>(),
			ModifiedKeys = modifiedKeys.Duplicate(true)
		};

		foreach (AddonLineEntry ale in _pluginList.GetChildren()) {
			if (ale.Installed) {
				prj.Plugins.Add(ale.GetMeta("asset") as AssetPlugin);
			}
		}

		prj.CreateProject();
		ProjectFile pf = ProjectFile.ReadFromFile(prj.ProjectLocation.PlusFile(pfName).NormalizePath(), (pfName == "engine.cfg"));
		pf.GodotId = prj.GodotId;
		pf.Assets = new Array<string>();

		foreach (AssetPlugin plugin in prj.Plugins)
			pf.Assets.Add(plugin.Asset.AssetId);

		CentralStore.Projects.Add(pf);
		CentralStore.Instance.SaveDatabase();
		EmitSignal("project_created", pf);
		Visible = false;
	}

	[SignalHandler("pressed", nameof(_createFolder))]
	void OnCreateFolderPressed() {
		try {
			string path = _projectLocation.Text;
			string newDir = path.Join(_projectName.Text).NormalizePath();
			Directory.CreateDirectory(newDir);
			_projectLocation.Text = newDir;
			TestPath(newDir);
		} catch (System.Exception ex) {
			AppDialogs.MessageDialog.ShowMessage(Tr("Mono Error"), Tr(!string.IsNullOrEmpty(ex.Message) ? ex.Message : "A unknown error has occurred."));
		}
	}

	[SignalHandler("pressed", nameof(_browseLocation))]
	void OnBrowseLocationPressed() {
		AppDialogs.BrowseFolderDialog.CurrentDir = CentralStore.Settings.ProjectPath.NormalizePath();
		AppDialogs.BrowseFolderDialog.PopupCentered();
		AppDialogs.BrowseFolderDialog.Connect("dir_selected", this, "OnDirSelected", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseFolderDialog.Connect("popup_hide", this, "OnDirSelected_PopupHidden", null, (uint)ConnectFlags.Oneshot);
	}

	void OnDirSelected(string bfdir) {
		bfdir = bfdir.NormalizePath();
		_projectLocation.Text = bfdir;
		AppDialogs.BrowseFolderDialog.Visible = false;
		TestPath(bfdir);
		if (bfdir.IsDirEmpty() && (string.IsNullOrEmpty(_projectName.Text) || _projectName.Text == "New Game Project"))
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
				AddonLineEntry ale = MainWindow._plScenes["AddonLineEntry"].Instance<AddonLineEntry>();

				ale.Icon = Util.LoadImage(imgLoc);
				if (ale.Icon == null) ale.Icon = MainWindow._plTextures["DefaultIconV3"];

				ale.Title = plgn.Asset.Title;
				ale.Version = plgn.Asset.VersionString;
				ale.SetMeta("asset", plgn);
				_pluginList.AddChild(ale);
			}

			_pluginErrorText.Visible = false;
		}
	}

	public void ShowDialog() {
		_godotVersion.Clear();
		for (int indx = 0; indx < CentralStore.Versions.Count; indx++) {
			_godotVersion.AddItem(CentralStore.Versions[indx].GetDisplayName(), indx);
			_godotVersion.SetItemMetadata(indx, CentralStore.Versions[indx]);
		}
		if (_godotVersion.GetItemCount() == 0) {
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"), Tr("You need to add an editor version before you can create a project."));
			return;
		}

		_tabs.CurrentTab = 0;
		_projectName.Text = "New Game Project";
		_projectLocation.Text = CentralStore.Settings.ProjectPath;
		TestPath(CentralStore.Settings.ProjectPath);
		_templateProject.Clear();
		_templateProject.AddItem("None");
		foreach (AssetProject tmpl in CentralStore.Templates) {
			string gdName = tmpl.Asset.Title;
			_templateProject.AddItem(gdName);
			_templateProject.SetItemMetadata(CentralStore.Templates.IndexOf(tmpl)+1, tmpl);
		}
		_vcMetadata.Selected = 0;
		OnVersionSelected(_godotVersion.Selected);

		Visible = true;
	}

	[SignalHandler("text_changed", nameof(_projectName))]
	private void TestName(string name) {
		if (!TestPath(_projectLocation.Text)) {
			return;
		}

		TestPath(_projectLocation.Text);
		if (string.IsNullOrEmpty(name)) {
			ShowMessage(Tr("It would be a good idea to name your project."), DirError.WARNING);
		}
	}

	[SignalHandler("text_changed", nameof(_projectLocation))]
	private bool TestPath(string path) {
		if (!Directory.Exists(path)) {
			ShowMessage(Tr("The path specified doesn't exist."), DirError.ERROR);
			return false;
		}



		if (!path.IsDirEmpty()) {
			ShowMessage(Tr("The selected path is not empty. Choosing an empty folder is highly recommended."), DirError.WARNING);
		} else {
			ShowMessage("", DirError.OK);
		}
		return true;
	}
}
