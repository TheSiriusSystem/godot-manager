using Godot;
using Godot.Collections;
using Godot.Sharp.Extras;
using System.Linq;
using DateTime = System.DateTime;
using TimeSpan = System.TimeSpan;
using Environment = System.Environment;

public class GodotLineEntry : HBoxContainer
{
	[Signal]
	public delegate void install_clicked(GodotLineEntry entry);

	[Signal]
	public delegate void uninstall_clicked(GodotLineEntry entry);

	[Signal]
	public delegate void default_selected(GodotLineEntry entry);

	[Signal]
	public delegate void right_clicked(GodotLineEntry entry);

	[Signal]
	public delegate void settings_shared_clicked(GodotLineEntry entry);

	[Signal]
	public delegate void link_settings_clicked(GodotLineEntry entry);

#region Private Node Variables
	[NodePath("Icon")]
	private TextureRect _icon = null;
	[NodePath("vc/VersionTag")]
	private Label _label = null;
	[NodePath("vc/GodotSize")]
	private HBoxContainer _filesize = null;
	[NodePath("vc/GodotSize/Filesize")]
	private Label _size = null;
	[NodePath("SettingsShare")]
	private TextureRect _settingsShare = null;
	[NodePath("Linked")]
	private TextureRect _linked = null;
	[NodePath("Download")]
	private TextureRect _download = null;

	[NodePath("vc/DownloadProgress")]
	private HBoxContainer _downloadProgress = null;
	[NodePath("vc/DownloadProgress/ProgressBar")]
	private ProgressBar _progressBar = null;
	[NodePath("vc/DownloadProgress/Filesize")]
	private Label _fileSize = null;

	[NodePath("vc/ETA")]
	private HBoxContainer _eta = null;
	[NodePath("vc/ETA/HB/EtaRemaining")]
	private Label _etaRemaining = null;
	[NodePath("vc/ETA/HB2/DownloadSpeed")]
	private Label _downloadSpeed = null;

	[NodePath("vc/GodotLocation")]
	private HBoxContainer _loc = null;
	[NodePath("vc/GodotLocation/Location")]
	private Label _location = null;

	[NodePath("DownloadSpeedTimer")]
	private Timer _downloadSpeedTimer = null;
#endregion

#region Private String Variables
	private string sLabel = "Godot x.x.x";
	private string sFilesize = "Unknown";
	private string sLocation = @"E:\Apps\GodotManager\versions\x.x.x";
	private bool bDownloaded = false;
	private bool bSettingsShare = false;
	private bool bSettingsLinked = false;
	private bool bMono = false;
	private GodotVersion gvGodotVersion = null;
	private GithubVersion gvGithubVersion = null;
	private MirrorVersion gvMirrorVersion = null;

	private int iLastByteCount = 0;
	Array<double> adSpeedStack;
	DateTime dtStartTime;
#endregion

#region Public Accessors
	public GodotVersion GodotVersion {
		get => gvGodotVersion;

		set {
			gvGodotVersion = value;
			if (value != null) {
				Mono = value.IsMono;
				Label = value.Tag;
				Location = value.GetExecutablePath();
				File file = new File();
				if (file.Open(Location, File.ModeFlags.Read) == Error.Ok) {
					Filesize = Util.FormatSize(file.GetLen());
					file.Close();
				} else {
					if (value.GithubVersion != null) {
						if (!value.IsMono)
							Filesize = Util.FormatSize(value.GithubVersion.PlatformDownloadSize);
						else
							Filesize = Util.FormatSize(value.GithubVersion.PlatformMonoDownloadSize);
					} else if (value.MirrorVersion != null) {
						Filesize = Util.FormatSize(value.MirrorVersion.PlatformDownloadSize);
					}
				}
				if (_loc != null && _location != null)
					_loc.Visible = true;
			}
		}
	}

	public bool Mono {
		get => bMono;

		set {
			bMono = value;
			GithubVersion = gvGithubVersion;
			MirrorVersion = gvMirrorVersion;
		}
	}

	public GithubVersion GithubVersion {
		get => gvGithubVersion;

		set {
			gvGithubVersion = value;
			if (value == null)
				return;
			if (_loc != null && _location != null)
				_loc.Visible = false;
#if GODOT_WINDOWS || GODOT_UWP
			if (!Mono) {
				Label = value.Name;
				Filesize = Util.FormatSize(!Environment.Is64BitProcess ? value.Standard.Win32_Size : value.Standard.Win64_Size);
			} else {
				Label = value.Name + "-mono";
				Filesize = Util.FormatSize(!Environment.Is64BitProcess ? value.Mono.Win32_Size : value.Mono.Win64_Size);
			}
#elif GODOT_LINUXBSD || GODOT_X11
			if (!Mono) {
				Label = value.Name;
				Filesize = Util.FormatSize(!Environment.Is64BitProcess ? value.Standard.Linux32_Size : value.Standard.Linux64_Size);
			} else {
				Label = value.Name + "-mono";
				Filesize = Util.FormatSize(!Environment.Is64BitProcess ? value.Mono.Linux32_Size : value.Mono.Linux64_Size);
			}
#elif GODOT_MACOS || GODOT_OSX
			if (!Mono) {
				Label = value.Name;
				Filesize = Util.FormatSize(value.Standard.OSX_Size);
			} else {
				Label = value.Name + "-mono";
				Filesize = Util.FormatSize(value.Mono.OSX_Size);
			}
#else
			Label = value.Name;
			Filesize = Util.FormatSize(0.0);
#endif
		}
	}

	public MirrorVersion MirrorVersion {
		get => gvMirrorVersion;

		set {
			gvMirrorVersion = value;
			if (value == null)
				return;
			Label = value.Version;
			if (_loc != null && _location != null)
				_loc.Visible = false;
#if GODOT_WINDOWS || GODOT_UWP
			Filesize = Util.FormatSize(!Environment.Is64BitProcess ? value.Win32_Size : value.Win64_Size);
#elif GODOT_LINUXBSD || GODOT_X11
			Filesize = Util.FormatSize(!Environment.Is64BitProcess ? value.Linux32_Size : value.Linux64_Size);
#elif GODOT_MACOS || GODOT_OSX
			Filesize = Util.FormatSize(!Environment.Is64BitProcess ? value.OSX32_Size : value.OSX64_Size);
#else
			Filesize = Util.FormatSize(0.0);
#endif
		}
	}

	public string Label {
		get => sLabel;
		set {
			sLabel = value;
			if (_label != null)
				_label.Text = $"Godot {value}";
		}
	}

	public string Filesize {
		get => sFilesize;
		set {
			sFilesize = value;
			if (_filesize != null && _size != null)
				_size.Text = value;
		}
	}

	public string Location
	{
		get => sLocation;
		set
		{
			sLocation = value;
			if (_loc != null && _location != null) {
				string path = sLocation.GetBaseDir().NormalizePath();
				_location.Text = path;
				_loc.Visible = true;
			}
		}
	}

	[Export]
	public bool Downloaded {
		get => bDownloaded;

		set {
			bDownloaded = value;
			if (_download != null) {
				ToggleDownloadUninstall(bDownloaded);
			}
		}
	}

	public bool SettingsShared
	{
		get => bSettingsShare;
		set
		{
			bSettingsShare = value;
			if (_settingsShare == null) return;
			_settingsShare.SelfModulate = value ? Colors.YellowGreen : new Color(0.54f,0.54f,0.54f);
		}
	}

	public bool SettingsLinked
	{
		get => bSettingsLinked;
		set
		{
			bSettingsLinked = value;
			if (_linked == null) return;
			_linked.SelfModulate = value ? Colors.Green : new Color(0.54f,0.54f,0.54f);
		}
	}

	public bool IsDownloaded => bDownloaded;

	public int TotalSize { get; set; }
#endregion

	public override void _Ready()
	{
		this.OnReady();

		GithubVersion = gvGithubVersion;
		MirrorVersion = gvMirrorVersion;
		GodotVersion = gvGodotVersion;
		SettingsShared = bSettingsShare;
		SettingsLinked = bSettingsLinked;

		Downloaded = bDownloaded;
		adSpeedStack = new Array<double>();

		if (gvGodotVersion != null) {
			IsGodotV1OrV2(gvGodotVersion.Tag);
		} else if (gvGithubVersion != null) {
			IsGodotV1OrV2(gvGithubVersion.Name);
		} else if (gvMirrorVersion != null) {
			IsGodotV1OrV2(gvMirrorVersion.Version);
		}
	}

	public void ToggleDownloadUninstall(bool value) {
		if (value) {
			_download.Texture = MainWindow._plTextures["UninstallIcon"];
			_download.SelfModulate = new Color("fc9c9c");
		}
		else {
			_download.Texture = MainWindow._plTextures["DownloadIcon"];
			_download.SelfModulate = new Color("7defa7");
		}
	}

	public void ToggleDownloadProgress(bool value) {
		_filesize.Visible = !value;
		_downloadProgress.Visible = value;
		_eta.Visible = value;
	}

	public void StartDownloadStats(int totalSize) {
		dtStartTime = DateTime.Now;
		TotalSize = totalSize;
		_progressBar.MinValue = 0;
		_progressBar.MaxValue = totalSize;
		_fileSize.Text = $"{Util.FormatSize(0)}/{Util.FormatSize(TotalSize)}";
		_downloadSpeedTimer.Start();
	}

	public void StopDownloadStats() {
		_downloadSpeedTimer.Stop();
	}

	public async void ToggleSettingsShared()
	{
		while (_settingsShare == null)
		{
			await this.IdleFrame();
		}

		_settingsShare.Visible = true;
	}

	public async void ToggleSettingsLinked()
	{
		while (_linked == null)
		{
			await this.IdleFrame();
		}

		_linked.Visible = true;
	}

	void IsGodotV1OrV2(string gdVersTag)
	{
		if (gdVersTag[!gdVersTag.ToLower().StartsWith("v") ? 0 : 1].ToString().ToInt() <= 2) {
			_icon.Texture = MainWindow._plTextures["EditorIconV1"];
		}
	}

	[SignalHandler("gui_input")]
	void OnGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton iemb && iemb.Pressed && iemb.ButtonIndex == (int)ButtonList.Right)
		{
			EmitSignal("right_clicked", this);
		}
	}

	[SignalHandler("gui_input", nameof(_download))]
	void OnDownload_GuiInput(InputEvent inputEvent) {
		if (inputEvent is InputEventMouseButton iemb && iemb.Pressed && iemb.ButtonIndex == (int)ButtonList.Left) {
			if (bDownloaded) {
				EmitSignal("uninstall_clicked", this);
			} else {
				if (_download.Texture == MainWindow._plTextures["DownloadIcon"])
					EmitSignal("install_clicked", this);
				else
					EmitSignal("uninstall_clicked", this);
				ToggleDownloadUninstall(_downloadProgress.Visible);
			}
		}
	}

	[SignalHandler("gui_input", nameof(_linked))]
	void OnLinked_GuiInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton iemb && iemb.Pressed && iemb.ButtonIndex == (int)ButtonList.Left)
		{
			EmitSignal("link_settings_clicked", this);
		}
	}

	[SignalHandler("gui_input", nameof(_settingsShare))]
	void OnSettingsShare_GuiInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton iemb && iemb.Pressed && iemb.ButtonIndex == (int)ButtonList.Left)
		{
			EmitSignal("settings_shared_clicked", this);
		}
	}

	[SignalHandler("timeout", nameof(_downloadSpeedTimer))]
	void OnDownloadSpeedTimer_Timeout() {
		Mutex mutex = new Mutex();
		mutex.Lock();
		var lbc = iLastByteCount;
		var tb = _progressBar.Value;
		var speed = tb - lbc;
		adSpeedStack.Add(speed);
		var avgSpeed = adSpeedStack.Sum() / adSpeedStack.Count;
		_downloadSpeed.Text = $"{Util.FormatSize(avgSpeed)}/s";
		TimeSpan elapsedTime = DateTime.Now - dtStartTime;
		_etaRemaining.Text = elapsedTime.ToString("hh':'mm':'ss");
		iLastByteCount = (int)_progressBar.Value;
		mutex.Unlock();
	}

	public void OnChunkReceived(int bytes) {
		_progressBar.Value += bytes;
		_fileSize.Text = $"{Util.FormatSize(_progressBar.Value)}/{Util.FormatSize(TotalSize)}";
	}
}
