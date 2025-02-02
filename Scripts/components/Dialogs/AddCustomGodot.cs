using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Godot.Sharp.Extras;
using Guid = System.Guid;
using SFile = System.IO.File;

public class AddCustomGodot : ReferenceRect
{

#region Signals
	[Signal]
	public delegate void added_custom_godot();
#endregion

#region Node Paths
	[NodePath("PC/CC/P/VB/MCContent/VB/Tag")]
	LineEdit _Tag = null;

	[NodePath("PC/CC/P/VB/MCContent/VB/FilePath/Location")]
	LineEdit _Location = null;

	[NodePath("PC/CC/P/VB/MCContent/VB/FilePath/Browse")]
	Button _Browse = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/AddBtn")]
	Button _AddBtn = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/CancelBtn")]
	Button _CancelBtn = null;
#endregion

	public override void _Ready()
	{
		this.OnReady();
	}

#region Public Functions
	public void ShowDialog() {
		_Tag.Text = "";
		OnTagChanged("");
		_Location.Text = "";
		Visible = true;
	}
#endregion

#region Events
	[SignalHandler("text_changed", nameof(_Tag))]
	void OnTagChanged(string text) {
		Array<ushort> gdvercomps = Util.GetVersionComponentsFromString(text);
		_Tag.HintTooltip = $"Detected Version {gdvercomps[0]}.{gdvercomps[1]}.{gdvercomps[2]}";
	}

	[SignalHandler("pressed", nameof(_Browse))]
	void OnBrowsePressed() {
		AppDialogs.BrowseGodotDialog.Connect("file_selected", this, "OnFileSelected", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseGodotDialog.Connect("popup_hide", this, "OnBrowseDialogHidden", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseGodotDialog.CurrentFile = "";
		AppDialogs.BrowseGodotDialog.CurrentPath = (CentralStore.Settings.EnginePath + "/").NormalizePath();
		AppDialogs.BrowseGodotDialog.PopupCentered();
	}

	void OnFileSelected(string file) {
		_Tag.Text = file.GetFile();
		foreach (string str in new string[] {"godot_v", "_console", "_win32", "_win64", "_stable", "-stable", file.GetExtension()}) {
			_Tag.Text = _Tag.Text.ReplaceN(str, "");
		}
		_Tag.Text = _Tag.Text.ReplaceN("_mono", "-mono");
		OnTagChanged(_Tag.Text);
		_Location.Text = file;
	}

	void OnBrowseDialogHidden() {
		if (AppDialogs.BrowseGodotDialog.IsConnected("file_selected", this, "OnFileSelected"))
			AppDialogs.BrowseGodotDialog.Disconnect("file_selected", this, "OnFileSelected");
	}

	[SignalHandler("pressed", nameof(_AddBtn))]
	async Task OnAddPressed() {
		if (string.IsNullOrEmpty(_Tag.Text) || string.IsNullOrEmpty(_Location.Text)) {
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
		string fileName = _Location.Text.GetFile();
		foreach (string extension in MainWindow._customGDExtensions) {
			if (_Location.Text.GetExtension() == extension) {
				isExtensionValid = true;
				break;
			}
		}
		if (!isExtensionValid) {
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"),
			string.Format(Tr("{0} is not a valid executable file."), fileName));
			return;
		}

		foreach (GodotVersion gdver in CentralStore.Versions) {
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

		bool isProblematicName = true;
		for (int indx = 1; indx <= 4; indx++) {
			string versNum = indx.ToString();
			if (_Tag.Text.StartsWith(versNum) || _Tag.Text.StartsWith("v" + versNum)) {
				isProblematicName = false;
				break;
			}
		}
		if (isProblematicName) {
			bool res = await AppDialogs.YesNoDialog.ShowDialog(Tr("Please Confirm..."), Tr("This tag may cause problems with version detection when creating a project."), Tr("Add"), Tr("Cancel"));
			if (!res) return;
		}

		GodotVersion gv = new GodotVersion();
		gv.Id = Guid.NewGuid().ToString();
		gv.Tag = _Tag.Text;
		gv.Location = _Location.Text.GetBaseDir();
#if GODOT_MACOS || GODOT_OSX
		gv.ExecutableName = !gv.Tag.ToLower().Contains("mono") ? "Godot" : "Godot_mono";
#else
		gv.ExecutableName = _Location.Text.GetFile();
#endif
		CentralStore.Versions.Add(gv);
		CentralStore.Instance.SaveDatabase();
		Visible = false;
		EmitSignal("added_custom_godot");
	}

	[SignalHandler("pressed", nameof(_CancelBtn))]
	void OnCancelPressed() {
		Visible = false;
	}
#endregion
}
