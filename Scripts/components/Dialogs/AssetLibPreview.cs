using Godot;
using Godot.Collections;
using Godot.Sharp.Extras;
using Uri = System.Uri;
using File = System.IO.File;

public class AssetLibPreview : ReferenceRect
{
#region Signals
	[Signal]
	public delegate void installed_addon(bool update);

	[Signal]
	public delegate void uninstalled_addon();

	[Signal]
	public delegate void preview_closed();
#endregion

#region Node Paths
	[NodePath("PC/CC/P/VB/MC/TitleBarBG/HB/Title")]
	Label _DialogTitle = null;
	
	[NodePath("PC/CC/P/VB/MCContent/HSC/InfoPanel/HBoxContainer/Icon")]
	TextureRect _Icon = null;

	[NodePath("PC/CC/P/VB/MCContent/HSC/InfoPanel/HBoxContainer/PluginInfo/Category")]
	Label _Category = null;

	[NodePath("PC/CC/P/VB/MCContent/HSC/InfoPanel/HBoxContainer/PluginInfo/Author")]
	Label _Author = null;

	[NodePath("PC/CC/P/VB/MCContent/HSC/InfoPanel/HBoxContainer/PluginInfo/License")]
	Label _License = null;

	[NodePath("PC/CC/P/VB/MCContent/HSC/InfoPanel/HBoxContainer/PluginInfo/Version")]
	Label _Version = null;

	[NodePath("PC/CC/P/VB/MCContent/HSC/InfoPanel/HBoxContainer/PluginInfo/GodotVersion")]
	private Label _GodotVersion = null;

	[NodePath("PC/CC/P/VB/MCContent/HSC/InfoPanel/PC/SC/Description")]
	RichTextLabel _Description = null;

	[NodePath("PC/CC/P/VB/MCContent/HSC/ScreenShots/Preview")]
	TextureRect _Preview = null;

	[NodePath("PC/CC/P/VB/MCContent/HSC/ScreenShots/Preview/PlayButton")]
	TextureRect _PlayButton = null;

	[NodePath("PC/CC/P/VB/MCContent/HSC/ScreenShots/Preview/MissingThumbnails")]
	TextureRect _MissingThumbnails = null;

	[NodePath("PC/CC/P/VB/MCContent/HSC/ScreenShots/PC/SC/Thumbnails")]
	HBoxContainer _Thumbnails = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/DownloadBtn")]
	Button _Download = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/Uninstall")]
	Button _Uninstall = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/Sep3")]
	Control _Sep3 = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/CloseBtn")]
	Button _Close = null;
#endregion

#region Private Variables
	private string sIconPath;
	private DownloadQueue dlq = null;
	private ImageDownloader dldIcon = null;
	private Array<ImageDownloader> dldPreviews = null;
	private AssetLib.Asset _asset;

	private Array<string> Templates = new Array<string> {"Templates", "Projects", "Demos"};
#endregion

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		dlq = new DownloadQueue();
		AddChild(dlq);
		this.OnReady();
		dldPreviews = new Array<ImageDownloader>();
	}

	[SignalHandler("meta_clicked", nameof(_Description))]
	void OnDescription_MetaClicked(object meta) {
		OS.ShellOpen((string)meta);
	}

	[SignalHandler("pressed", nameof(_Close))]
	void OnClosePressed() {
		Visible = false;
	}
	
	[SignalHandler("pressed", nameof(_Download))]
	async void OnDownloadPressed() {
		// LOGIC: If asset is an Addon, after download is complete, popup Installer Reference creator
		// to allow user to select what files to be installed when selecting this addon.
		// If asset is a Template/Project/Demo, this will be added to templates for creating New Projects from.
		// Two new central repositories needed for Addons / Projects when saving to the user's computer.

		AppDialogs.DownloadAddon.Asset = _asset;
		AppDialogs.DownloadAddon.LoadInformation();
		AppDialogs.DownloadAddon.Connect("download_completed", this, "OnDownloadAddonCompleted", null, (uint)ConnectFlags.Oneshot);
		AppDialogs.DownloadAddon.Connect("download_failed", this, "OnDownloadAddonFailed", null, (uint)ConnectFlags.Oneshot);
		await AppDialogs.DownloadAddon.StartDownload();
	}

	async void OnDownloadAddonCompleted(AssetLib.Asset asset, AssetProject ap, AssetPlugin apl) {
		AppDialogs.DownloadAddon.Disconnect("download_failed", this, "OnDownloadAddonCompleted");
		if (apl != null) {
			AppDialogs.AddonInstaller.ShowDialog(apl);
		}
		while (AppDialogs.AddonInstaller.Visible)
			await this.IdleFrame();
		EmitSignal("installed_addon", (_Download.Text != "Download"));
		Visible = false;
		AppDialogs.MessageDialog.ShowMessage(Tr("Asset Added"), 
			string.Format(Tr("{0} has finished downloading. You can find it in the Downloaded {1} tab."), asset.Title, apl != null ? "Addons" : "Templates"));
	}

	void OnDownloadAddonFailed(string errDesc = "") {
		if (AppDialogs.DownloadAddon.IsConnected("download_completed", this, "OnDownloadAddonCompleted"))
			AppDialogs.DownloadAddon.Disconnect("download_completed", this, "OnDownloadAddonCompleted");
		AppDialogs.DownloadAddon.Visible = false;
		if (!string.IsNullOrEmpty(errDesc))
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"), Tr(errDesc));
	}

	[SignalHandler("hide")]
	void OnHide_AssetLibPreview() {
		EmitSignal("preview_closed");
	}

	public void ShowDialog(AssetLib.Asset asset) {
		_asset = asset;
		_DialogTitle.Text = asset.Title;
		_Category.Text = asset.Category;
		_Author.Text = asset.Author;
		_License.Text = asset.Cost;
		_Version.Text = asset.VersionString;
		_GodotVersion.Text = asset.GodotVersion;
		if (!asset.AssetId.StartsWith("local-")) {
			_Description.BbcodeText = $"[table=1][cell][color=lime][url={$"https://godotengine.org/asset-library/asset/{asset.AssetId}"}]" +
			Tr("Asset Page") + $"[/url][/color][/cell][cell][color=aqua][url={asset.BrowseUrl}]" + 
			Tr("Repository") + $"[/url][/color][/cell][cell][color=aqua][url={asset.IssuesUrl}]" +
			Tr("Issues") + $"[/url][/color][/cell][/table]\n{asset.Description.Replace("\r", "")}";
			if (string.IsNullOrEmpty(asset.IconUrl)) {
				sIconPath = MainWindow._plTextures["MissingIcon"].ResourcePath;
			} else {
				Uri uri = new Uri(asset.IconUrl);
				sIconPath = $"{CentralStore.Settings.CachePath}/images/{asset.AssetId}{uri.AbsolutePath.GetExtension()}";
			}
		} else {
			_Description.BbcodeText = $"[table=1][cell][color=aqua][url={asset.BrowseUrl}]" +
			Tr("Repository") + $"[/url][/color][/cell][/table]\n{asset.Description.Replace("\r", "")}";
			sIconPath = MainWindow._plTextures["DefaultIconV3"].ResourcePath;
		}
		_Description.ScrollToLine(0);
		
		if (!File.Exists(sIconPath.GetOSDir().NormalizePath())) {
			dldIcon = new ImageDownloader(asset.IconUrl, sIconPath);
			dlq.Push(dldIcon);
		} else {
			if (sIconPath.EndsWith(".gif")) {
				GifTexture gif = new GifTexture(sIconPath);
				_Icon.Texture = gif;
			} else {
				Texture icon = Util.LoadImage(sIconPath);
				if (icon == null)
					_Icon.Texture = MainWindow._plTextures["MissingIcon"];
				else
					_Icon.Texture = icon;
			}
		}
		_Preview.Texture = MainWindow._plTextures["WaitThumbnail"];
		_MissingThumbnails.Visible = false;
		_PlayButton.Visible = false;
		
		foreach (TextureRect rect in _Thumbnails.GetChildren()) {
			_Thumbnails.RemoveChild(rect);
			rect.QueueFree();
		}

		for (int i = 0; i < asset.Previews.Count; i++) {
			TextureRect preview = new TextureRect();
			preview.RectMinSize = new Vector2(120,120);
			preview.Texture = MainWindow._plTextures["WaitThumbnail"];
			preview.Expand = true;
			preview.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
			preview.Connect("gui_input", this, "OnGuiInput_Preview", new Array { preview });
			_Thumbnails.AddChild(preview);
			Uri tnUri = new Uri(asset.Previews[i].Thumbnail);
			string iconPath = $"{CentralStore.Settings.CachePath}/images/{asset.AssetId}-{i}-{asset.Previews[i].PreviewId}{tnUri.AbsolutePath.GetExtension()}";
			if (!File.Exists(iconPath.GetOSDir().NormalizePath())) {
				ImageDownloader dld = new ImageDownloader(asset.Previews[i].Thumbnail, iconPath);
				dldPreviews.Add(dld);
				preview.SetMeta("iconPath", iconPath);
				dlq.Push(dld);
			} else {
				dldPreviews.Add(null);
				if (iconPath.EndsWith(".gif")) {
					GifTexture gif = new GifTexture(iconPath);
					preview.Texture = gif;
				} else {
					Texture icon = Util.LoadImage(iconPath);
					if (icon != null)
						preview.Texture = icon;
				}
			}

			preview.SetMeta("url",asset.Previews[i].Link);
		}
		
		if (_Thumbnails.GetChildCount() > 0 && _Thumbnails.GetChild(0) is TextureRect fp && fp.Texture != null && fp.Texture.ResourcePath != MainWindow._plTextures["WaitThumbnail"].ResourcePath) {
			UpdatePreview(fp);
		} else {
			_Preview.Texture = null;
			_MissingThumbnails.Visible = true;
		}

		if (CentralStore.Instance.HasPluginId(asset.AssetId) || CentralStore.Instance.HasTemplateId(asset.AssetId)) {
			_Uninstall.Visible = true;
			_Sep3.Visible = true;
			_Download.Visible = true;
			AssetLib.Asset assetInfo;
			if (Templates.IndexOf(asset.Category) == -1) {
				assetInfo = CentralStore.Instance.GetPluginId(asset.AssetId).Asset;
			} else {
				assetInfo = CentralStore.Instance.GetTemplateId(asset.AssetId).Asset;
			}
			if (assetInfo.VersionString != asset.VersionString ||
				assetInfo.Version != asset.Version ||
				assetInfo.ModifyDate != asset.ModifyDate) {
				_Download.Disabled = false;
			} else {
				_Download.Disabled = true;
			}
			_Download.Text = Tr("Update");
		} else {
			_Uninstall.Visible = false;
			_Sep3.Visible = false;
			_Download.Visible = true;
			_Download.Disabled = false;
			_Download.Text = Tr("Download");
		}

		dlq.StartDownload();
		Visible = true;
	}

	[SignalHandler("pressed", nameof(_Uninstall))]
	async void OnPressed_Uninstall() {
		bool res = await AppDialogs.YesNoDialog.ShowDialog(Tr("Please Confirm..."),
					string.Format(Tr("Remove {0} from the list?\nYour project folders' contents won't be modified."), _asset.Title),
					Tr("Remove"), Tr("Cancel"));
		if (!res) return;

		if (CentralStore.Instance.HasPluginId(_asset.AssetId)) {
			// Handle Addon Uninstall
			AssetPlugin plg = CentralStore.Instance.GetPluginId(_asset.AssetId);
			string plgPath = plg.Location.NormalizePath();
			if (File.Exists(plgPath))
				File.Delete(plgPath);
			CentralStore.Plugins.Remove(plg);
		} else if (CentralStore.Instance.HasTemplateId(_asset.AssetId)) {
			// Handle Template Uninstall
			AssetProject prj = CentralStore.Instance.GetTemplateId(_asset.AssetId);
			string prjPath = prj.Location.NormalizePath();
			if (File.Exists(prjPath))
				File.Delete(prjPath);
			CentralStore.Templates.Remove(prj);
		}
		EmitSignal("uninstalled_addon");
		Visible = false;
	}

	[SignalHandler("gui_input", nameof(_PlayButton))]
	void OnGuiInput_PlayButton(InputEvent inputEvent) {
		if (inputEvent is InputEventMouseButton iembEvent && iembEvent.Pressed && iembEvent.ButtonIndex == (int)ButtonList.Left) {
			string url = _Preview.GetMeta("url") as string;
			OS.ShellOpen(url);
		}
	}

	void OnGuiInput_Preview(InputEvent inputEvent, TextureRect rect) {
		if (inputEvent is InputEventMouseButton iembEvent && iembEvent.Pressed && iembEvent.ButtonIndex == (int)ButtonList.Left) {
			UpdatePreview(rect);
		}
	}

	private void UpdatePreview(TextureRect rect)
	{
		_Preview.Texture = rect.Texture;
		_Preview.SetMeta("url", rect.GetMeta("url"));
		Uri tnUri = new Uri(rect.GetMeta("url") as string);
		_MissingThumbnails.Visible = false;
		if (tnUri.Host.IndexOf("youtube.com") != -1)
		{
			_PlayButton.Visible = true;
		}
		else
		{
			_PlayButton.Visible = false;
		}
	}

	[SignalHandler("download_completed", nameof(dlq))]
	void OnImageDownloaded(ImageDownloader dld) {
		if (dld == dldIcon) {
			if (sIconPath.EndsWith(".gif")) {
				GifTexture gif = new GifTexture(sIconPath);
				_Icon.Texture = gif;
			} else {
				Texture icon = Util.LoadImage(sIconPath);
				if (icon != null)
					_Icon.Texture = icon;
			}
		} else {
			if (dldPreviews.Contains(dld))
			{
				int indx = dldPreviews.IndexOf(dld);
				UpdateThumbnail(indx);
				if (indx == 0) {
					UpdatePreview(_Thumbnails.GetChild(indx) as TextureRect);
				}

				dldPreviews[indx] = null;
			}
		}
	}

	[SignalHandler("queue_finished", nameof(dlq))]
	void OnQueueFinished() {
		dldPreviews.Clear();
	}

	private void UpdateThumbnail(int indx)
	{
		TextureRect preview = _Thumbnails.GetChild(indx) as TextureRect;
		if (!preview.HasMeta("iconPath"))
			return;
		object iconMeta = preview.GetMeta("iconPath");
		if (iconMeta == null)
			return;
		string iconPath = iconMeta as string;
		if (File.Exists(iconPath.GetOSDir().NormalizePath()))
		{
			if (iconPath.EndsWith(".gif")) {
				GifTexture gif = new GifTexture(iconPath);
				preview.Texture = gif;
			} else {
				Texture icon = Util.LoadImage(iconPath);
				if (icon != null)
					preview.Texture = icon;
			}
		}
	}
}
