using System.Diagnostics.CodeAnalysis;
using Godot;
using Godot.Collections;
using Godot.Sharp.Extras;
using Directory = System.IO.Directory;

[SuppressMessage("ReSharper", "CheckNamespace")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
public class FirstRunWizard : ReferenceRect
{

	#region Signals

	[Signal]
	public delegate void wizard_completed();

	#endregion

	#region Node Paths

	// Main Wizard Controls
	[NodePath] private Button PrevStep = null;
	[NodePath] private Button Cancel = null;
	[NodePath] private Button NextStep = null;

	// Wizard Control
	[NodePath] private TabContainer Wizard = null;

	// STEPS
	// Step 2 (All Platforms) - Setup Settings
	[NodePath] private LineEdit EngineLoc = null;
	[NodePath] private Button EngineBrowse = null;
	[NodePath] private Button EngineDefault = null;

	[NodePath] private LineEdit CacheLoc = null;
	[NodePath] private Button CacheBrowse = null;
	[NodePath] private Button CacheDefault = null;

	[NodePath] private LineEdit ProjectLoc = null;
	[NodePath] private Button ProjectBrowse = null;
	[NodePath] private Button ProjectDefault = null;

	[NodePath] private CheckBox TitleBar = null;

	// Step 3 (All Platforms) - Install Godot Engines
	[NodePath] private GodotPanel GodotPanel = null;

	#endregion

	private bool loaded_engines = false;

	private Array<string> OriginalSettings = null;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.OnReady();

		OriginalSettings = new Array<string>()
		{
			CentralStore.Settings.EnginePath.GetOSDir().NormalizePath(),
			CentralStore.Settings.CachePath.GetOSDir().NormalizePath(),
			CentralStore.Settings.ProjectPath.GetOSDir().NormalizePath()
		};
		EngineLoc.Text = OriginalSettings[0];
		CacheLoc.Text = OriginalSettings[1];
		ProjectLoc.Text = OriginalSettings[2];
		TitleBar.Pressed = false;

		Wizard.CurrentTab = 0;
		PrevStep.Disabled = true;
	}

	public void ShowDialog() => Visible = true;
	public void HideDialog() => Visible = false;

	string GetEngineDefaultPath() => Util.GetUserFolder("versions").GetOSDir().NormalizePath();
	string GetCacheDefaultPath() => Util.GetUserFolder("cache").GetOSDir().NormalizePath();

	string GetProjectDefaultPath() => OS.GetSystemDir(OS.SystemDir.Documents).Join("Godot Projects").NormalizePath();

	// Default Buttons Handlers
	[SignalHandler("pressed", nameof(EngineDefault))]
	void OnPressed_EngineDefault()
	{
		OriginalSettings[0] = EngineLoc.Text;
		EngineLoc.Text = GetEngineDefaultPath();
		CentralStore.Settings.EnginePath = GetEngineDefaultPath();
	}

	[SignalHandler("pressed", nameof(CacheDefault))]
	void OnPressed_CacheDefault()
	{
		OriginalSettings[1] = CacheLoc.Text;
		CacheLoc.Text = GetCacheDefaultPath();
		CentralStore.Settings.CachePath = CacheLoc.Text;
	}

	[SignalHandler("pressed", nameof(ProjectDefault))]
	void OnPressed_ProjectDefault()
	{
		OriginalSettings[2] = ProjectLoc.Text;
		ProjectLoc.Text = GetProjectDefaultPath();
		CentralStore.Settings.ProjectPath = ProjectLoc.Text;
	}

	// Browse Buttons Handlers
	[SignalHandler("pressed", nameof(EngineBrowse))]
	void OnPressed_EngineBrowse()
	{
		AppDialogs.BrowseFolderDialog.CurrentDir = EngineLoc.Text;
		AppDialogs.BrowseFolderDialog.Connect("dir_selected", this, "OnDirSelected_EngineBrowse", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseFolderDialog.Connect("popup_hide", this, "OnPopupHide", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseFolderDialog.PopupCentered();
	}

	[SignalHandler("pressed", nameof(CacheBrowse))]
	void OnPressed_CacheBrowse()
	{
		AppDialogs.BrowseFolderDialog.CurrentDir = CacheLoc.Text;
		AppDialogs.BrowseFolderDialog.Connect("dir_selected", this, "OnDirSelected_CacheBrowse", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseFolderDialog.Connect("popup_hide", this, "OnPopupHide", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseFolderDialog.PopupCentered();
	}

	[SignalHandler("pressed", nameof(ProjectBrowse))]
	void OnPressed_ProjectBrowse()
	{
		string path = ProjectLoc.Text.NormalizePath();
		AppDialogs.BrowseFolderDialog.CurrentDir = Directory.Exists(path) ? path : OS.GetSystemDir(OS.SystemDir.Documents).NormalizePath();
		AppDialogs.BrowseFolderDialog.Connect("dir_selected", this, "OnDirSelected_ProjectBrowse", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseFolderDialog.Connect("popup_hide", this, "OnPopupHide", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.BrowseFolderDialog.PopupCentered();
	}

	// Setting Handlers
	void OnDirSelected_EngineBrowse(string dir)
	{
		OriginalSettings[0] = EngineLoc.Text;
		EngineLoc.Text = dir.GetOSDir().NormalizePath();
		CentralStore.Settings.EnginePath = EngineLoc.Text;
		EnsureDirectoryExists(EngineLoc.Text);
	}

	void OnDirSelected_CacheBrowse(string dir)
	{
		OriginalSettings[1] = CacheLoc.Text;
		CacheLoc.Text = dir.GetOSDir().NormalizePath();
		CentralStore.Settings.CachePath = CacheLoc.Text;
		EnsureDirectoryExists(EngineLoc.Text);
	}

	void OnDirSelected_ProjectBrowse(string dir)
	{
		OriginalSettings[2] = ProjectLoc.Text;
		ProjectLoc.Text = dir.GetOSDir().NormalizePath();
		CentralStore.Settings.ProjectPath = ProjectLoc.Text;
		EnsureDirectoryExists(EngineLoc.Text);
	}

	[SignalHandler("toggled", nameof(TitleBar))]
	void OnToggled_TitleBar(bool buttonPressed)
	{
		CentralStore.Settings.UseSystemTitlebar = buttonPressed;
		GetTree().Root.GetNode<MainWindow>("MainWindow").UpdateWindow();
	}

	void OnPopupHide()
	{
		if (AppDialogs.BrowseFolderDialog.IsConnected("dir_selected", this, "OnDirSelected_EngineBrowse"))
			AppDialogs.BrowseFolderDialog.Disconnect("dir_selected", this, "OnDirSelected_EngineBrowse");

		if (AppDialogs.BrowseFolderDialog.IsConnected("dir_selected", this, "OnDirSelected_CacheBrowse"))
			AppDialogs.BrowseFolderDialog.Disconnect("dir_selected", this, "OnDirSelected_CacheBrowse");

		if (AppDialogs.BrowseFolderDialog.IsConnected("dir_selected", this, "OnDirSelected_ProjectBrowse"))
			AppDialogs.BrowseFolderDialog.Disconnect("dir_selected", this, "OnDirSelected_ProjectBrowse");
	}

	// Navigation buttons Handlers
	[SignalHandler("pressed", nameof(PrevStep))]
	void OnPressed_PrevStep()
	{
		if (Wizard.CurrentTab > 0)
		{
			Wizard.CurrentTab--;
			UpdateControls();
		}
	}

	[SignalHandler("pressed", nameof(NextStep))]
	async void OnPressed_NextStep()
	{
		int tabCount = Wizard.GetTabCount();
		if (Wizard.CurrentTab >= tabCount - 1)
		{
			CentralStore.Settings.ScanDirs = new Array<string>() { ProjectLoc.Text };
			CentralStore.Instance.SaveDatabase();
			EmitSignal("wizard_completed");
			Visible = false;
		}
		else
		{
			if (Wizard.CurrentTab < tabCount)
			{
				Wizard.CurrentTab++;
				UpdateControls();
				if (Wizard.CurrentTab == 2 && !loaded_engines)
				{
					CentralStore.GHVersions.Clear();
					CentralStore.TFVersions.Clear();
					await GodotPanel.GatherReleases();
					await GodotPanel.PopulateList(0);
					loaded_engines = true;
				}
			}
		}
	}

	[SignalHandler("pressed", nameof(Cancel))]
	async void OnPressed_Cancel()
	{
		bool res = await AppDialogs.YesNoDialog.ShowDialog(Tr("Please Confirm..."),
			Tr("Are you sure you want to exit the First Run Wizard? Any changes you have made to the settings will be lost."),
			Tr("Exit"),
			Tr("Continue"));
		if (res)
		{
			// System.IO.Compression.FileSystem
			CentralStore.Settings.EnginePath = OriginalSettings[0];
			CentralStore.Settings.CachePath = OriginalSettings[1];
			CentralStore.Settings.ProjectPath = OriginalSettings[2];
			CentralStore.Settings.ScanDirs = new Array<string>() { OriginalSettings[2] };
			CentralStore.Settings.UseSystemTitlebar = false;
			CentralStore.Instance.SaveDatabase();
			GetTree().Root.GetNode<MainWindow>("MainWindow").UpdateWindow();
			HideDialog();
		}
	}

	void EnsureDirectoryExists(string path)
	{
		if (!Directory.Exists(path))
			Directory.CreateDirectory(path);
	}

	void UpdateControls()
	{
		PrevStep.Disabled = (Wizard.CurrentTab <= 1);
		NextStep.Text = Wizard.CurrentTab < Wizard.GetTabCount() - 1 ? "Next" : "Finish";
	}
}
