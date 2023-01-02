using Godot;
using Godot.Sharp.Extras;
using Godot.Collections;
using Guid = System.Guid;
using SFile = System.IO.File;
using System.Text.RegularExpressions;

public class EditProject : ReferenceRect
{
#region Signals
	[Signal]
	public delegate void project_updated();
#endregion

#region Node Paths
#region General Tab
[NodePath("PC/CC/P/VB/MCContent/VC/HC/ProjectIcon")]
TextureRect _Icon = null;

[NodePath("PC/CC/P/VB/MCContent/VC/HC/MC/VC/ProjectName")]
LineEdit _ProjectName = null;

[NodePath("PC/CC/P/VB/MCContent/VC/GodotVersion")]
OptionButton _GodotVersion = null;

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

#region Resources
[Resource("res://components/AddonLineEntry.tscn")] private PackedScene ALineEntry = null;
[Resource("res://Assets/Icons/default_project_icon_v3.png")] private Texture DefaultIcon = null;
[Resource("res://Assets/Icons/missing_icon.svg")] private Texture MissingIcon = null;
#endregion

#region Private Variables
	ProjectFile _pf = null;
	EPData _data;
	Regex _gitTag = new Regex("-[A-Za-z0-9]{40,}");
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
		var texture = Util.LoadImage(ProjectFile.Location.GetResourceBase(IconPath));
		if (texture == null)
			_Icon.Texture = MissingIcon;
		else
			_Icon.Texture = texture;
		_ProjectName.Text = ProjectName;
		_ProjectDescription.Text = Description;
		_GodotVersion.Clear();
		foreach(GodotVersion gdver in CentralStore.Versions) {
			_GodotVersion.AddItem(gdver.GetDisplayName());
			_GodotVersion.SetItemMetadata(_GodotVersion.GetItemCount()-1, gdver.Id);
			if (ProjectFile.GodotId == gdver.Id)
				_GodotVersion.Selected = _GodotVersion.GetItemCount()-1;
		}

		if (ProjectFile.Assets == null)
			ProjectFile.Assets = new Array<string>();

		Visible = true;
	}
#endregion

#region Event Handlers
	[SignalHandler("pressed", nameof(_SaveBtn))]
	void OnSaveBtnPressed() {
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
		if (path == "")
			return;
		string pfPath = ProjectFile.Location.GetBaseDir().Replace(@"\", "/");
		string fPath;
		if (path.StartsWith(pfPath)) {
			fPath = path;
		} else {
			fPath = pfPath.PlusFile(path.GetFile());
			if (SFile.Exists(fPath)) {
				string backupPath = fPath.BaseName() + "_" + Guid.NewGuid().ToString() + fPath.GetExtension();
				SFile.Move(fPath, backupPath);
				AppDialogs.MessageDialog.ShowMessage(Tr("Previous Icon Renamed"), Tr($"A icon of the same name was found in your project's root. It has been renamed to \"{backupPath.GetFile().RStrip(".png")}\"."));
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
	void OnProjectDescriptionTextChanged() {
		Description = _ProjectDescription.Text;
	}
#endregion

}
