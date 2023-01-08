using Godot;
using Godot.Sharp.Extras;
using File = System.IO.File;

public class ImportProject : ReferenceRect
{
	[Signal]
	public delegate void update_projects();

#region Node Paths
	[NodePath("PC/CC/P/VB/MCContent/VB/HBoxContainer/LocationValue")]
	LineEdit _locationValue = null;

	[NodePath("PC/CC/P/VB/MCContent/VB/HBoxContainer/LocationBrowse")]
	Button _locationBrowse = null;

	[NodePath("PC/CC/P/VB/MCContent/VB/GodotVersions")]
	OptionButton _godotVersions = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/AddBtn")]
	Button _addBtn = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/CancelBtn")]
	Button _cancelBtn = null;
#endregion

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.OnReady();

		UpdateGodotVersions();
		_locationValue.Text = "";
	}

	private void UpdateGodotVersions()
	{
		_godotVersions.Clear();
		foreach (GodotVersion gdVers in CentralStore.Versions)
		{
			int id = CentralStore.Versions.IndexOf(gdVers);
			_godotVersions.AddItem(gdVers.GetDisplayName(), id);
			_godotVersions.SetItemMetadata(id, gdVers.Id);
		}
	}

	public void ShowDialog(string location = "") {
		if (CentralStore.Versions.Count <= 0)
		{
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"), Tr("You need to add an editor version before you can import a project."));
			return;
		}

		UpdateGodotVersions();
		_locationValue.Text = location;
		Visible = true;
	}

	[SignalHandler("pressed", nameof(_addBtn))]
	void OnAddBtnPressed() {
		if (string.IsNullOrEmpty(_locationValue.Text)) {
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"),
				Tr("You need to select a project file."));
			return;
		}

		if (!File.Exists(_locationValue.Text.NormalizePath())) {
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"),
				Tr("The file doesn't exist."));
			return;
		}

		GodotVersion gdVers = CentralStore.Instance.GetVersion(_godotVersions.GetSelectedMetadata() as string);
		int gdMajorVers = gdVers.GetMajorVersion();
		if ((gdMajorVers <= 2 && !_locationValue.Text.EndsWith("engine.cfg")) || (gdMajorVers >= 3 && !_locationValue.Text.EndsWith("project.godot"))) {
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"),
				string.Format(Tr("{0} is not a valid project file."), _locationValue.Text.GetFile()));
			return;
		}

		ProjectFile pf = ProjectFile.ReadFromFile(_locationValue.Text, gdMajorVers);
		pf.GodotId = gdVers.Id;
		CentralStore.Projects.Add(pf);
		CentralStore.Instance.SaveDatabase();
		EmitSignal("update_projects");
		Visible = false;
	}

	[SignalHandler("pressed", nameof(_cancelBtn))]
	void OnCancelBtnPressed() {
		Visible = false;
	}

	[SignalHandler("pressed", nameof(_locationBrowse))]
	void OnLocationBrowsePressed() {
		int gdMajorVers = CentralStore.Instance.GetVersion(_godotVersions.GetSelectedMetadata() as string).GetMajorVersion();
		if (gdMajorVers <= 2) {
			AppDialogs.ImportFileDialog.Filters = new string[] {"engine.cfg"};
		} else {
			AppDialogs.ImportFileDialog.Filters = new string[] {"project.godot"};
		}
		AppDialogs.ImportFileDialog.CurrentFile = "";
		AppDialogs.ImportFileDialog.CurrentPath = (CentralStore.Settings.ProjectPath + "/").NormalizePath();
		AppDialogs.ImportFileDialog.PopupCentered();
		AppDialogs.ImportFileDialog.Connect("file_selected", this, "OnFileSelected", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.ImportFileDialog.Connect("popup_hide", this, "OnLocationImportHidden", null, (uint)ConnectFlags.Oneshot);
	}

	void OnLocationImportHidden()
	{
		if (AppDialogs.ImportFileDialog.IsConnected("file_selected", this, "OnFileSelected"))
			AppDialogs.ImportFileDialog.Disconnect("file_selected", this, "OnFileSelected");
	}

	void OnFileSelected(string file_path) {
		_locationValue.Text = file_path;
	}
}
