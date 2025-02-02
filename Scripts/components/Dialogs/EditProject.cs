using Godot;
using Godot.Sharp.Extras;
using Godot.Collections;
using Guid = System.Guid;
using SFile = System.IO.File;

public class EditProject : ReferenceRect
{
#region Signals
	[Signal]
	public delegate void project_updated();
#endregion

#region Node Paths
#region General Tab
	[NodePath("PC/CC/P")]
	Panel _Panel = null;

	[NodePath("PC/CC/P/VB/MCContent/VC/HC/ProjectIcon")]
	TextureRect _Icon = null;

	[NodePath("PC/CC/P/VB/MCContent/VC/HC/MC/VC/ProjectName")]
	LineEdit _ProjectName = null;

	[NodePath("PC/CC/P/VB/MCContent/VC/GodotVersion")]
	OptionButton _GodotVersion = null;

	[NodePath("PC/CC/P/VB/MCContent/VC/ProjectDescriptionHeader")]
	Label _ProjectDescriptionHeader = null;

	[NodePath("PC/CC/P/VB/MCContent/VC/ProjectDescription")]
	TextEdit _ProjectDescription = null;
#endregion

#region Dialog Controls
	[NodePath("PC/CC/P/VB/MCButtons/HB/SaveBtn")]
	Button _SaveBtn = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/CancelBtn")]
	Button _CancelBtn = null;
#endregion
#endregion

#region Private Variables
	ProjectFile _pf = null;
	EPData _data;
#endregion

#region Internal Structure
	struct EPData {
		public string IconPath;
		public string ProjectName;
		public string GodotId;
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

	public string GodotId { 
		get => _data.GodotId;
		set => _data.GodotId = value;
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
			GodotId = _pf.GodotId;
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
		ProjectFile = pf;
		bool isOldPrj = pf.Location.EndsWith("engine.cfg");

		_GodotVersion.Clear();
		foreach (GodotVersion gdver in CentralStore.Versions) {
			ushort gdmv = Util.GetVersionComponentsFromString(gdver.Tag)[0];
			if ((gdmv <= 2 && pf.Location.EndsWith("engine.cfg")) || (gdmv >= 3 && pf.Location.EndsWith("project.godot"))) {
				_GodotVersion.AddItem(gdver.GetDisplayName());
				_GodotVersion.SetItemMetadata(_GodotVersion.GetItemCount() - 1, gdver.Id);
				if (ProjectFile.GodotId == gdver.Id)
					_GodotVersion.Selected = _GodotVersion.GetItemCount() - 1;
				OnGodotVersionItemSelected(_GodotVersion.Selected);
			}
		}
		if (_GodotVersion.GetItemCount() == 0) {
			EmitSignal("hide");
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"), Tr("You need to add a Godot " + (isOldPrj ? "1.x-2.x" : "3.x+") + " instance before you can edit this project."));
			return;
		}

		var texture = Util.LoadImage(ProjectFile.Location.GetResourceBase(IconPath));
		_Panel.RectMinSize = new Vector2(_Panel.RectMinSize.x, isOldPrj ? 210 : 410);
		if (texture == null)
			_Icon.Texture = MainWindow._plTextures["MissingIcon"];
		else
			_Icon.Texture = texture;
		_ProjectName.Text = ProjectName;
		_ProjectDescriptionHeader.Visible = !isOldPrj;
		_ProjectDescription.Visible = !isOldPrj;
		if (!isOldPrj)
			_ProjectDescription.Text = Description;

		if (ProjectFile.Assets == null)
			ProjectFile.Assets = new Array<string>();

		Visible = true;
	}
#endregion

#region Event Handlers
	[SignalHandler("pressed", nameof(_SaveBtn))]
	void OnSaveBtnPressed() {
		ushort gdmv = Util.GetVersionComponentsFromString(CentralStore.Instance.GetVersion(GodotId).Tag)[0];
		if ((gdmv <= 2 && _pf.Location.EndsWith("project.godot")) || (gdmv >= 3 && _pf.Location.EndsWith("engine.cfg")))
		{
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"), Tr("The project cannot be associated with this editor version."));
			return;
		}

		ProjectFile.Name = ProjectName;
		ProjectFile.Description = Description;
		ProjectFile.Icon = IconPath;
		ProjectFile.GodotId = GodotId;
		ProjectFile.WriteUpdatedData();
		CentralStore.Instance.SaveDatabase();
		EmitSignal("project_updated");
		Visible = false;
	}

	[SignalHandler("pressed", nameof(_CancelBtn))]
	void OnCancelBtnPressed() {
		Visible = false;
	}

	[SignalHandler("gui_input", nameof(_Icon))]
	void OnIconGuiInput(InputEvent inputEvent) {
		if (inputEvent is InputEventMouseButton iemb && iemb.Pressed && iemb.ButtonIndex == (int)ButtonList.Left) {
			AppDialogs.ImageFileDialog.Connect("file_selected", this, "OnFileSelected", null, (uint)ConnectFlags.Oneshot);
			AppDialogs.ImageFileDialog.Connect("popup_hide", this, "OnFilePopupHide", null, (uint)ConnectFlags.Oneshot);
			AppDialogs.ImageFileDialog.CurrentFile = "";
			AppDialogs.ImageFileDialog.CurrentPath = (ProjectFile.Location.GetBaseDir() + "/").NormalizePath();
			AppDialogs.ImageFileDialog.PopupCentered();
		}
	}

	void OnFileSelected(string path) {
		if (string.IsNullOrEmpty(path))
			return;
		string pfPath = ProjectFile.Location.GetBaseDir().Replace(@"\", "/");
		string fPath;
		if (path.StartsWith(pfPath)) {
			fPath = path;
		} else {
			fPath = pfPath.PlusFile(path.GetFile()).NormalizePath();
			if (SFile.Exists(fPath)) {
				string backupPath = fPath.BaseName() + "_" + Guid.NewGuid().ToString() + fPath.GetExtension();
				SFile.Move(fPath, backupPath);
				AppDialogs.MessageDialog.ShowMessage(Tr("Previous Icon Renamed"), Tr($"A icon of the same name was found in your project's root. It has been renamed to {backupPath.GetFile().BaseName()}."));
			}
			SFile.Copy(path, fPath);
		}
		IconPath = pfPath.GetProjectRoot(fPath);
		_Icon.Texture = Util.LoadImage(fPath);
		AppDialogs.ImageFileDialog.Visible = false;
	}

	void OnFilePopupHide() {
		if (AppDialogs.ImageFileDialog.IsConnected("file_selected", this, "OnFileSelected"))
			AppDialogs.ImageFileDialog.Disconnect("file_selected", this, "OnFileSelected");
	}

	[SignalHandler("text_changed", nameof(_ProjectName))]
	void OnProjectNameTextChanged(string text) {
		ProjectName = text;
	}

	[SignalHandler("item_selected", nameof(_GodotVersion))]
	void OnGodotVersionItemSelected(int index) {
		GodotId = _GodotVersion.GetItemMetadata(index) as string;
	}

	[SignalHandler("text_changed", nameof(_ProjectDescription))]
	void OnProjectDescriptionTextChanged(string text) {
		Description = text;
	}
#endregion

}
