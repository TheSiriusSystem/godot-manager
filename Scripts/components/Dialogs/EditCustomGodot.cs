using System.Threading.Tasks;
using Godot;
using Godot.Sharp.Extras;
using SFile = System.IO.File;

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

	[NodePath("PC/CC/P/VB/MCButtons/HB/EditBtn")]
	Button _EditBtn = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/CancelBtn")]
	Button _CancelBtn = null;
#endregion

    GodotLineEntry _currentGLE = null;

	public override void _Ready()
	{
		this.OnReady();
	}

#region Public Functions
	public void ShowDialog(GodotLineEntry gle) {
        _currentGLE = gle;
		_Tag.Text = _currentGLE.GodotVersion.Tag;
		_Location.Text = _currentGLE.GodotVersion.GetExecutablePath();
		Visible = true;
	}
#endregion

#region Events
	[SignalHandler("pressed", nameof(_Browse))]
	void OnBrowsePressed() {
		AppDialogs.BrowseGodotDialog.Connect("file_selected", this, "OnFileSelected", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseGodotDialog.Connect("popup_hide", this, "OnBrowseDialogHidden", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseGodotDialog.CurrentFile = "";
		AppDialogs.BrowseGodotDialog.CurrentPath = (CentralStore.Settings.EnginePath + "/").NormalizePath();
		AppDialogs.BrowseGodotDialog.PopupCentered();
	}

	void OnFileSelected(string file) {
		_Location.Text = file;
	}

	void OnBrowseDialogHidden() {
		if (AppDialogs.BrowseGodotDialog.IsConnected("file_selected", this, "OnFileSelected"))
			AppDialogs.BrowseGodotDialog.Disconnect("file_selected", this, "OnFileSelected");
	}

	[SignalHandler("pressed", nameof(_EditBtn))]
	async Task OnEditPressed() {
		if (_Tag.Text == "" || _Location.Text == "") {
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"),
			Tr("You need to provide a tag and a location for this editor version."));
			return;
		}

		if (!SFile.Exists(_Location.Text.NormalizePath())) {
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"),
			Tr("The file doesn't exist."));
			return;
		}

		bool isExtensionValid = false;
		foreach (string extension in MainWindow._customGDExtensions) {
			if (_Location.Text.GetExtension() == extension) {
				isExtensionValid = true;
				break;
			}
		}
		if (!isExtensionValid) {
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"),
			Tr("The file's extension is invalid."));
			return;
		}

		foreach (GodotVersion gdver in CentralStore.Versions) {
			if (gdver != _currentGLE.GodotVersion) {
				if (gdver.ExecutableName == _Location.Text.GetFile()) {
					AppDialogs.MessageDialog.ShowMessage(Tr("Error"),
					Tr(string.Format("This editor version is already added as {0}.", gdver.GetDisplayName())));
					return;
				} else if (gdver.Tag == _Tag.Text) {
					AppDialogs.MessageDialog.ShowMessage(Tr("Error"),
					Tr(string.Format("This tag is already used by {0}.", gdver.GetDisplayName())));
					return;
				}
			}
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
			bool res = await AppDialogs.YesNoDialog.ShowDialog(Tr("Please Confirm..."), Tr("This tag may cause problems with version detection when creating a project."), Tr("Edit"), Tr("Cancel"));
			if (!res) return;
		}

        foreach (GodotVersion version in CentralStore.Versions) {
            if (version.Id == _currentGLE.GodotVersion.Id) {
                version.Tag = _Tag.Text;
                version.Location = _Location.Text.GetBaseDir();
#if GODOT_MACOS || GODOT_OSX
		version.ExecutableName = !version.Tag.ToLower().Contains("mono") ? "Godot" : "Godot_mono";
#else
		version.ExecutableName = _Location.Text.GetFile();
#endif
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
