using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Godot.Sharp.Extras;
using System.Linq;
using System.Threading.Tasks;
using Uri = System.Uri;
using Guid = System.Guid;
using DateTime = System.DateTime;
using TimeSpan = System.TimeSpan;
using Mirrors;

[Tool]
public class GodotPanel : Panel
{
	#region Nodes
	// [NodePath("VB/MC/HC/UseMono")]
	// CheckBox UseMono = null;

	[NodePath("VB/MC/HC/PC/TagSelection")]
	MenuButton TagSelection = null;

	[NodePath("VB/MC/HC/ActionButtons")]
	ActionButtons ActionButtons = null;

	[NodePath("VB/MC/HC/DownloadSource")]
	OptionButton DownloadSource = null;

	[NodePath("VB/SC/GodotList/Install")]
	CategoryList Installed = null;

	[NodePath("VB/SC/GodotList/Download")]
	CategoryList Downloading = null;

	[NodePath("VB/SC/GodotList/Available")]
	CategoryList Available = null;
	#endregion

	private EnginePopup _enginePopup = null;

	[Export] private bool InWizard = false;

	private List<string> NoHideWizard = new List<string>()
	{
		"ActionButtons", "Spacer2", "PC", "Spacer3", "Label2", "DownloadSource"
	};

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		this.OnReady();
		if (InWizard)
		{
			var node = GetNode<HBoxContainer>("VB/MC/HC");
			foreach (Control child in node.GetChildren())
			{
				if (NoHideWizard.Contains(child.Name))
					continue;
				child.Visible = false;
			}
			Installed.Visible = false;
		}
		else
		{
			GetParent<TabContainer>().Connect("tab_changed", this, "OnPageChanged");
			AppDialogs.AddCustomGodot.Connect("added_custom_godot", this, "PopulateList");
			AppDialogs.EditCustomGodot.Connect("edited_custom_godot", this, "PopulateList");
		}
		_enginePopup = MainWindow._plScenes["EnginePopup"].Instance<EnginePopup>();
		_enginePopup.Name = "EngineContextMenu";
		AddChild(_enginePopup);

		DownloadSource.Clear();
		DownloadSource.AddItem("GitHub");

		if (CentralStore.Mirrors.Count == 0) {
			var res = MirrorManager.Instance.GetMirrors();
			while (!res.IsCompleted)
				await this.IdleFrame();

			foreach (MirrorSite site in res.Result) {
				var cres = from csite in CentralStore.Mirrors
						where csite.Id == site.Id
						select csite;
				if (cres.FirstOrDefault<MirrorSite>() == null) {
					CentralStore.Mirrors.Add(site);
					CentralStore.MRVersions[site.Id] = new Array<MirrorVersion>();
				}
			}
		}

		foreach (MirrorSite site in CentralStore.Mirrors)
			DownloadSource.AddItem(site.Name, site.Id);

		TagSelection.GetPopup().HideOnCheckableItemSelection = false;
		TagSelection.GetPopup().Connect("id_pressed", this, "OnIdPressed_TagSelection");
		OnlyMono();
		AppDialogs.ManageCustomDownloads.Connect("update_list", this, "PopulateList");
	}

	async void OnIdPressed_TagSelection(int id) {
		if (id == 0)
			TagSelection.GetPopup().SetItemChecked(id, !IsMono());
		else {
			for (int i = 2; i < TagSelection.GetPopup().GetItemCount(); i++) {
				TagSelection.GetPopup().SetItemChecked(i, (i == id));
			}
		}
		await PopulateList();
	}

	bool IsMono() {
		return TagSelection.GetPopup().IsItemChecked(0);
	}

	bool IsStable() {
		return TagSelection.GetPopup().IsItemChecked(2);
	}

	bool IsAlpha() {
		return TagSelection.GetPopup().IsItemChecked(3);
	}

	bool IsBeta() {
		return TagSelection.GetPopup().IsItemChecked(4);
	}

	bool IsRC() {
		return TagSelection.GetPopup().IsItemChecked(5);
	}

	void OnlyMono() {
		if (DownloadSource.Selected == 0)
		{
			for (int i = 2; i < TagSelection.GetPopup().GetItemCount(); i++) {
				TagSelection.GetPopup().SetItemDisabled(i, true);
			}
		}
	}

	void AllTags() {
		if (DownloadSource.Selected > 0)
		{
			for (int i = 2; i < TagSelection.GetPopup().GetItemCount(); i++) {
				TagSelection.GetPopup().SetItemDisabled(i, false);
			}
		}
	}

	[SignalHandler("item_selected", nameof(DownloadSource))]
	async void OnItemSelected_DownloadSource(int index)
	{
		if (index == 0) {
			OnlyMono();
			if (CentralStore.GHVersions.Count == 0) {
				var t = GatherReleases();
				while (!t.IsCompleted) {
					await this.IdleFrame();
				}
			}
		} else {
			AllTags();
			int id = DownloadSource.GetSelectedId();
			if (CentralStore.MRVersions[id].Count == 0) {
				await GatherReleases();
			}
		}

		if (CentralStore.Settings.UseLastMirror)
			CentralStore.Settings.LastEngineMirror = index;
		await PopulateList();
	}

	[SignalHandler("clicked", nameof(ActionButtons))]
	async void OnActionClicked(int index) {
		switch (index) {
			case 0:     // Add Custom Godot
				AppDialogs.AddCustomGodot.ShowDialog();
				break;
			case 1:     // Manage Custom Godot Downloads
				AppDialogs.ManageCustomDownloads.ShowDialog();
				break;
			case 2:     // Refresh List
				var t = GatherReleases();
				while (!t.IsCompleted) {
					await this.IdleFrame();
				}
				await PopulateList();
				break;
		}
	}

	async void OnPageChanged(int page) {
		if (GetParent<TabContainer>().GetCurrentTabControl() != this) return;

		if (CentralStore.Settings.UseLastMirror)
		{
			DownloadSource.Selected = CentralStore.Settings.LastEngineMirror;
			if (CentralStore.Settings.LastEngineMirror == 0)
				OnlyMono();
			else
				AllTags();
		}

		if (DownloadSource.Selected == 0) {
			if (CentralStore.GHVersions.Count == 0) {
				var t = GatherReleases();
				while (!t.IsCompleted) {
					await this.IdleFrame();
				}
			}
		} else {
			MirrorSite site = CentralStore.Mirrors[DownloadSource.Selected - 1];
			if (CentralStore.MRVersions[site.Id].Count == 0) {
				var t = GatherReleases();
				while (!t.IsCompleted)
					await this.IdleFrame();
			}
		}
		await PopulateList();
	}

	async void OnDownloadCompleted(GodotInstaller installer, GodotLineEntry gle) {
		Downloading.List.RemoveChild(gle);
		if (Downloading.List.GetChildCount() == 0)
			Downloading.Visible = false;
		
		gle.StopDownloadStats();
		installer.Install();

		CentralStore.Versions.Add(installer.GodotVersion);

		CentralStore.Instance.SaveDatabase();
		gle.Downloaded = true;
		gle.ToggleDownloadProgress(false);

		await PopulateList();
	}

	async void OnDownloadFailed(GodotInstaller installer, HTTPClient.Status status, GodotLineEntry gle) {
		Downloading.List.RemoveChild(gle);
		if (Downloading.List.GetChildCount() == 0)
			Downloading.Visible = false;
		
		Available.List.AddChild(gle);
		gle.ToggleDownloadProgress(false);
		gle.Downloaded = false;

		string errDesc = "";
		Uri dl = new Uri(installer.GodotVersion.Url);
		switch (status) {
			case HTTPClient.Status.CantConnect:
				errDesc = string.Format(Tr("Unable to connect to server {0}."), dl.Host);
				break;
			case HTTPClient.Status.CantResolve:
				errDesc = string.Format(Tr("Unable to resolve server {0}."), dl.Host);
				break;
			case HTTPClient.Status.ConnectionError:
				errDesc = string.Format(Tr($"Unable to connect to server {0}:{1}."), dl.Host, dl.Port);
				break;
			case HTTPClient.Status.Requesting:
				errDesc = string.Format(Tr("Request to server {0} failed to produce a result."), dl.Host);
				break;
			case HTTPClient.Status.SslHandshakeError:
				errDesc = string.Format(Tr("SSL certificate/communication failed with {0}."), dl.Host);
				break;
			case HTTPClient.Status.Body:
				errDesc = string.Format(Tr("Unable to save cached file to disk at \"{0}\"."),installer.GodotVersion.CacheLocation);
				break;
			default:
				errDesc = Tr("An unknown error has ocurred.");
				break;
		}

		bool res = await AppDialogs.YesNoDialog.ShowDialog(Tr("Download Engine Failed"), errDesc, Tr("Retry"), Tr("Cancel"));
		if (res)
			await OnInstallClicked(gle);
	}

	async void RemoveGodot(GodotLineEntry gle, bool deleteFiles) {
		if (deleteFiles) {
			GodotInstaller.FromVersion(gle.GodotVersion).Uninstall();
		}
		foreach (ProjectFile pf in CentralStore.Projects) {
			if (pf.GodotId == gle.GodotVersion.Id) {
				pf.GodotId = Guid.Empty.ToString();
			}
		}
		CentralStore.Versions.Remove(gle.GodotVersion);
		CentralStore.Instance.SaveDatabase();
		await PopulateList();
	}

	async Task OnInstallClicked(GodotLineEntry gle) {
		Available.List.RemoveChild(gle);
		Downloading.List.AddChild(gle);
		Downloading.Visible = true;
		gle.ToggleDownloadProgress(true);

		GodotInstaller installer = null;

		if (gle.GithubVersion == null && gle.MirrorVersion == null)
			installer = GodotInstaller.FromCustomEngineDownload(gle.CustomEngine);
		else if (gle.GithubVersion == null && gle.CustomEngine == null)
			installer = GodotInstaller.FromMirror(gle.MirrorVersion, IsMono());
		else
			installer = GodotInstaller.FromGithub(gle.GithubVersion, IsMono());

		installer.Connect("chunk_received", gle, "OnChunkReceived");
		installer.Connect("download_completed", this, "OnDownloadCompleted", new Array { gle });
		installer.Connect("download_failed", this, "OnDownloadFailed", new Array { gle });

		gle.ToggleDownloadProgress(true);

		gle.StartDownloadStats(installer.DownloadSize);

		await installer.Download();
	}

	async void OnUninstallClicked(GodotLineEntry gle) {
		var task = AppDialogs.YesNoCancelDialog.ShowDialog(Tr("Please Confirm..."),
				string.Format(Tr("Remove {0} from the list?"), gle.GodotVersion.GetDisplayName()),
				Tr("Editor and Files"), Tr("Just Editor"));
		while (!task.IsCompleted)
			await this.IdleFrame();
		switch (task.Result)
		{
			case YesNoCancelDialog.ActionResult.FirstAction:
				RemoveGodot(gle, true);
				break;
			case YesNoCancelDialog.ActionResult.SecondAction:
				RemoveGodot(gle, false);
				break;
			case YesNoCancelDialog.ActionResult.CancelAction:
				break;
		}
	}

	void OnSettingsSharedClicked(GodotLineEntry gle)
	{
		gle.SettingsShared = !gle.SettingsShared;
		if (gle.SettingsShared)
		{
			CentralStore.Settings.SettingsShare.Add(gle.GodotVersion.Id);
		}
		else
		{
			foreach (GodotLineEntry dlGLE in Installed.List.GetChildren<GodotLineEntry>())
			{
				if (dlGLE.GodotVersion.SharedSettings == gle.GodotVersion.Id || dlGLE.GodotVersion.SharedSettings == gle.GodotVersion.Tag)
				{
					dlGLE.GodotVersion.SharedSettings = string.Empty;
					dlGLE.SettingsLinked = false;
				}
			}
			CentralStore.Settings.SettingsShare.Remove(gle.GodotVersion.Id);
		}
		CentralStore.Instance.SaveDatabase();
	}

	void OnLinkSettingsClicked(GodotLineEntry gle)
	{
		if (gle.SettingsLinked)
		{
			gle.GodotVersion.SharedSettings = string.Empty;
			gle.SettingsLinked = false;
			CentralStore.Instance.SaveDatabase();
			return;
		}
		var list = new Godot.Collections.Dictionary<string, string>();
		foreach (var id in CentralStore.Settings.SettingsShare)
		{
			var gv = CentralStore.Instance.FindVersion(id);
			if (gv != null && gv != gle.GodotVersion)
				list[gv.Tag] = id;
		}

		if (list.Count <= 0)
		{
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"),
			Tr("There are no editor versions to link the settings to."));
			return;
		}

		AppDialogs.ListSelectDialog.Connect("option_selected", this, "OnOptionSelected_LinkSettings", new Array() { gle }, (uint)ConnectFlags.Oneshot);
		AppDialogs.ListSelectDialog.Connect("option_cancelled", this, "OnOptionCancelled_LinkSettings", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.ListSelectDialog.ShowDialog(Tr("Link Settings"), Tr("Select an editor version to link the settings to:"), list);
	}

	void OnOptionSelected_LinkSettings(string id, GodotLineEntry gle)
	{
		OnOptionCancelled_LinkSettings();
		gle.GodotVersion.SharedSettings = id;
		gle.SettingsLinked = true;
		CentralStore.Instance.SaveDatabase();
	}

	void OnOptionCancelled_LinkSettings()
	{
		if (AppDialogs.ListSelectDialog.IsConnected("option_selected", this, "OnOptionSelected_LinkSettings"))
			AppDialogs.ListSelectDialog.Disconnect("option_selected", this, "OnOptionSelected_LinkSettings");

		if (AppDialogs.ListSelectDialog.IsConnected("option_cancelled", this, "OnOptionCancelled_LinkSettings"))
			AppDialogs.ListSelectDialog.Disconnect("option_cancelled", this, "OnOptionCancelled_LinkSettings");
	}

	public async Task PopulateList() {
		foreach (Node child in Installed.List.GetChildren())
			child.QueueFree();
		foreach (Node child in Available.List.GetChildren())
			child.QueueFree();

		await this.IdleFrame();

		foreach (GodotVersion gdv in CentralStore.Versions) {
			GodotLineEntry gle = MainWindow._plScenes["GodotLineEntry"].Instance<GodotLineEntry>();
			gle.GodotVersion = gdv;
			gle.GithubVersion = gdv.GithubVersion;
			gle.MirrorVersion = gdv.MirrorVersion;
			gle.Mono = gdv.IsMono;
			gle.Downloaded = true;
			if (CentralStore.Settings.SelfContainedEditors)
			{
				gle.ToggleSettingsShared();
				gle.ToggleSettingsLinked();
			}

			gle.SettingsShared = CentralStore.Settings.SettingsShare.Contains(gdv.Id);
			gle.SettingsLinked = CentralStore.Settings.SettingsShare.Contains(gdv.SharedSettings);
			Installed.List.AddChild(gle);
			gle.Connect("uninstall_clicked", this, "OnUninstallClicked");
			gle.Connect("right_clicked", this, "OnRightClicked_Installed");
			gle.Connect("settings_shared_clicked", this, "OnSettingsSharedClicked");
			gle.Connect("link_settings_clicked", this, "OnLinkSettingsClicked");
		}
		
		// Handle CustomEngineDownload first, before official mirrors
		foreach (CustomEngineDownload ced in CentralStore.CustomEngines)
		{
			GodotLineEntry gle = MainWindow._plScenes["GodotLineEntry"].Instance<GodotLineEntry>();
			gle.CustomEngine = ced;
			Available.List.AddChild(gle);
			gle.Connect("install_clicked", this, "OnInstallClicked");
			gle.Connect("right_clicked", this, "OnRightClicked_Installable");
		}

		if (DownloadSource.Selected == 0) {
			// Handle Github
			foreach (GithubVersion gv in CentralStore.GHVersions) {
				GodotLineEntry gle = MainWindow._plScenes["GodotLineEntry"].Instance<GodotLineEntry>();
				gle.GithubVersion = gv;
				gle.Mono = IsMono();
				Available.List.AddChild(gle);
				gle.Connect("install_clicked", this, "OnInstallClicked");
				gle.Connect("right_clicked", this, "OnRightClicked_Installable");
			}
		} else {
			// Handle Mirror
			foreach (MirrorVersion mv in CentralStore.MRVersions[DownloadSource.GetSelectedId()].Reverse()) {
				GodotLineEntry gle = MainWindow._plScenes["GodotLineEntry"].Instance<GodotLineEntry>();
				gle.MirrorVersion = mv;
				gle.Mono = IsMono();
				Available.List.AddChild(gle);
				gle.Connect("install_clicked", this, "OnInstallClicked");
				gle.Connect("right_clicked", this, "OnRightClicked_Installable");
			}
		}

		UpdateVisibility();
	}

	void OnRightClicked_Installable(GodotLineEntry gle)
	{
		_enginePopup.GodotLineEntry = gle;
		_enginePopup.SetItemText(0, "Download");
		for (int indx = 1; indx < _enginePopup.GetItemCount(); indx++) {
			_enginePopup.SetItemDisabled(indx, true);
		}
		_enginePopup.Popup_(new Rect2(GetGlobalMousePosition(), _enginePopup.RectSize));
	}

	void OnRightClicked_Installed(GodotLineEntry gle)
	{
		_enginePopup.GodotLineEntry = gle;
		_enginePopup.SetItemText(0, "Remove");
		for (int indx = 1; indx < _enginePopup.GetItemCount(); indx++) {
			_enginePopup.SetItemDisabled(indx, false);
		}
		_enginePopup.Popup_(new Rect2(GetGlobalMousePosition(), _enginePopup.RectSize));
	}

	public void _IdPressed(int id)
	{
		switch (id)
		{
			case 0:
				if (_enginePopup.GodotLineEntry.Downloaded)
				{
					// Remove
					OnUninstallClicked(_enginePopup.GodotLineEntry);
				}
				else
				{
					// Download
					OnInstallClicked(_enginePopup.GodotLineEntry);
				}
				break;
			case 1:
				AppDialogs.EditCustomGodot.ShowDialog(_enginePopup.GodotLineEntry);
				break;
			case 2:
				OS.Clipboard = _enginePopup.GodotLineEntry.GodotVersion.GetExecutablePath();
				break;
			case 4:
				_enginePopup.GodotLineEntry.EmitSignal("settings_shared_clicked", _enginePopup.GodotLineEntry);
				break;
			case 5:
				_enginePopup.GodotLineEntry.EmitSignal("link_settings_clicked", _enginePopup.GodotLineEntry);
				break;
			case 7:
				OS.ShellOpen("file://" + _enginePopup.GodotLineEntry.GodotVersion.GetExecutablePath().GetBaseDir());
				break;
		}
	}

	private void UpdateVisibility() {
		Array<string> gdName = new Array<string>();
		foreach (GodotLineEntry igle in Installed.List.GetChildren()) {
			gdName.Add(igle.Label);
		}

		foreach (GodotLineEntry agle in Available.List.GetChildren()) {
			agle.Visible = false;
			// Is in Installed List?
			if (gdName.IndexOf(agle.Label) != -1)
				continue;

			// Does it not have MirrorVersion?
			if (agle.MirrorVersion == null) {
				agle.Visible = true;
				continue;
			}

			Array<string> tags = new Array<string>();

			if (IsAlpha()) tags.Add("alpha");
			if (IsBeta()) tags.Add("beta");
			if (IsRC()) tags.Add("rc");
			if (IsMono()) tags.Add("mono");

			agle.Visible = Enumerable.SequenceEqual(agle.MirrorVersion.Tags, tags);
		}
	}

	private int downloadedBytes = 0;
	void OnChunkReceived(int bytes) {
		downloadedBytes += bytes;
	}

	public async Task GatherGithubReleases() {
		AppDialogs.BusyDialog.UpdateHeader(Tr("Fetching Editor Downloads"));
		AppDialogs.BusyDialog.UpdateByline(Tr("Fetching release information from GitHub..."));
		AppDialogs.BusyDialog.ShowDialog();
		downloadedBytes = 0;
		Github.Github.Instance.Connect("chunk_received", this, "OnChunkReceived");
		var task = Github.Github.Instance.GetAllReleases();
		while (!task.IsCompleted) {
			await this.IdleFrame();
		}

		Github.Github.Instance.Disconnect("chunk_received", this, "OnChunkReceived");

		foreach (Github.Release release in task.Result) {
			GithubVersion gv = GithubVersion.FromAPI(release);
			if (gv.PlatformDownloadSize > 0) CentralStore.GHVersions.Add(gv);
		}
		CentralStore.Instance.SaveDatabase();

		AppDialogs.BusyDialog.HideDialog();
	}

	public async Task GatherMirrorReleases() {
		int id = DownloadSource.GetSelectedId();
		MirrorSite mirror = CentralStore.Mirrors.Where(x => x.Id == id).FirstOrDefault<MirrorSite>();
		if (mirror == null)
			return;

		AppDialogs.BusyDialog.UpdateHeader("Fetching Editor Downloads");
		AppDialogs.BusyDialog.UpdateByline(Tr(string.Format(Tr("Fetching release information from {0}..."), mirror.Name)));
		AppDialogs.BusyDialog.ShowDialog();
		downloadedBytes = 0;
		Mirrors.MirrorManager.Instance.Connect("chunk_received", this, "OnChunkReceived");
		var task = Mirrors.MirrorManager.Instance.GetEngineLinks(id);

		while (!task.IsCompleted)
			await this.IdleFrame();

		Mirrors.MirrorManager.Instance.Disconnect("chunk_received", this, "OnChunkReceived");

		foreach (MirrorVersion version in task.Result) {
			CentralStore.MRVersions[id].Add(version);
		}
		CentralStore.Instance.SaveDatabase();

		AppDialogs.BusyDialog.HideDialog();
	}

	public async Task GatherReleases() {
		if (DownloadSource.Selected == 0)
			await GatherGithubReleases();
		else
			await GatherMirrorReleases();
	}
}
