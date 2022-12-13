using System.Threading.Tasks;
using Godot;
using Godot.Sharp.Extras;

public class EditCustomGodot : ReferenceRect
{

#region Signals
	[Signal]
	public delegate void edited_custom_godot();
#endregion

#region Node Paths
	[NodePath("PC/CC/P/VB/MCContent/VB/Tag")]
	LineEdit _Tag = null;

	[NodePath("PC/CC/P/VB/MCContent/VB/FilePath/Location")]
	LineEdit _Location = null;

	[NodePath("PC/CC/P/VB/MCContent/VB/FilePath/Browse")]
	Button _Browse = null;

	[NodePath("PC/CC/P/VB/MCContent/VB/MonoEnabled")]
	CheckBox _MonoEnabled = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/EditBtn")]
	Button _EditBtn = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/CancelBtn")]
	Button _CancelBtn = null;
#endregion

    private GodotLineEntry _currentGLE = null;

	public override void _Ready()
	{
		this.OnReady();
	}

#region Public Functions
	public void ShowDialog(GodotLineEntry gle) {
        _currentGLE = gle;
		_Tag.Text = _currentGLE.GodotVersion.Tag;
		_Location.Text = _currentGLE.GodotVersion.GetExecutablePath();
		_MonoEnabled.Pressed = _currentGLE.GodotVersion.IsMono;
		Visible = true;
	}
#endregion

#region Events
	[SignalHandler("pressed", nameof(_Browse))]
	void OnBrowsePressed() {
		AppDialogs.BrowseGodotDialog.Connect("file_selected", this, "OnFileSelected", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseGodotDialog.Connect("popup_hide", this, "OnBrowseDialogHidden", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseGodotDialog.PopupCentered();
	}

	void OnFileSelected(string file) {
		_Location.Text = file;
	}

	void OnBrowseDialogHidden() {
		if (AppDialogs.BrowseFolderDialog.IsConnected("file_selected", this, "OnFileSelected"))
			AppDialogs.BrowseGodotDialog.Disconnect("file_selected", this, "OnFileSelected");
	}

	[SignalHandler("pressed", nameof(_EditBtn))]
	async Task OnEditPressed() {
		if (_Tag.Text == "" || _Location.Text == "") {
			AppDialogs.MessageDialog.ShowMessage(Tr("Edit Editor Version"),
			Tr("You need to provide a name and a location for this editor version."));
			return;
		}

        bool isProblematicName = true;
		for (int indx = 1; indx <= 4; indx++) {
			string versNum = indx.ToString();
			if (_Tag.Text.StartsWith(versNum) || _Tag.Text.StartsWith("v" + versNum)) {
				isProblematicName = false;
				break;
			}
		}
		if (isProblematicName) {
			bool res = await AppDialogs.YesNoDialog.ShowDialog(Tr("Add Editor Version"), Tr("This tag may cause problems with version detection when creating a project. Are you sure you want to continue?"));
			if (!res) return;
		}

        foreach (GodotVersion version in CentralStore.Versions) {
            if (version.Id == _currentGLE.GodotVersion.Id) {
                version.Tag = _Tag.Text;
                version.Location = _Location.Text.GetBaseDir();
                version.ExecutableName = _Location.Text.GetFile();
                version.IsMono = _MonoEnabled.Pressed;
            }
        }
		CentralStore.Instance.SaveDatabase();
        _currentGLE = null;
		Visible = false;
		EmitSignal("edited_custom_godot");
	}

	[SignalHandler("pressed", nameof(_CancelBtn))]
	void OnCancelPressed() {
        _currentGLE = null;
		Visible = false;
	}
#endregion
}
