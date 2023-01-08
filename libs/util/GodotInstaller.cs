using Godot;
using Godot.Collections;
using Guid = System.Guid;
using Uri = System.Uri;
using FPath = System.IO.Path;
using SFile = System.IO.File;
using SDirectory = System.IO.Directory;
using System.IO.Compression;
using System.Threading.Tasks;

public class GodotInstaller : Object {

	[Signal] public delegate void chunk_received(int size);
	[Signal] public delegate void download_completed(GodotInstaller self);
	[Signal] public delegate void download_failed(GodotInstaller self, HTTPClient.Status error);

	GDCSHTTPClient _client = null;
	GodotVersion _version = null;
	public bool _cancelled = false;

	public GodotVersion GodotVersion {
		get {
			return _version;
		}
	}

	public int DownloadSize {
		get {
			if (_version.GithubVersion == null)
				return _version.MirrorVersion.PlatformDownloadSize;
			else
				return _version.IsMono ? 
						_version.GithubVersion.PlatformMonoDownloadSize : 
						_version.GithubVersion.PlatformDownloadSize;
		}
	}

	public GodotInstaller(GodotVersion version) {
		_version = version;
		_client = new GDCSHTTPClient();
		_client.Connect("chunk_received", this, "OnChunkReceived");
	}

	public static GodotInstaller FromGithub(GithubVersion gh, bool is_mono = false) {
		string gvTag = gh.Name + (!is_mono ? "" : "-mono");
		string fName = $"editor-{gvTag}.zip".NormalizeFileName();
		GodotVersion gv = new GodotVersion() {
			Id = Guid.NewGuid().ToString(),
			Tag = gvTag,
			Url = is_mono ? gh.PlatformMonoDownloadURL : gh.PlatformDownloadURL,
			Location = $"{CentralStore.Settings.EnginePath}/{gvTag.NormalizeFileName()}",
			CacheLocation = $"{CentralStore.Settings.CachePath}/downloads/{fName}",
			GithubVersion = gh,
			IsMono = is_mono
		};

		GodotInstaller installer = new GodotInstaller(gv);
		return installer;
	}

	public static GodotInstaller FromMirror(MirrorVersion mv, bool is_mono = false) {
		string fName = $"editor-{mv.Version}.zip".NormalizeFileName();
		GodotVersion gv = new GodotVersion() {
			Id = Guid.NewGuid().ToString(),
			Tag = mv.Version,
			Url = mv.PlatformDownloadURL,
			Location = $"{CentralStore.Settings.EnginePath}/{mv.Version.NormalizeFileName()}",
			CacheLocation = $"{CentralStore.Settings.CachePath}/downloads/{fName}",
			MirrorVersion = mv,
			IsMono = is_mono
		};

		GodotInstaller installer = new GodotInstaller(gv);
		return installer;
	}

	public static GodotInstaller FromVersion(GodotVersion vers) {
		return new GodotInstaller(vers);
	}

	void OnChunkReceived(int size) {
		EmitSignal("chunk_received", size);
	}

	public async Task<HTTPResponse> FollowRedirect(string url = "") {
		Uri dlUri;
		if (string.IsNullOrEmpty(url))
			dlUri = new Uri(_version.Url);
		else
			dlUri = new Uri(url);
		
		var res = await _client.StartClient(dlUri.Host, dlUri.Port, dlUri.Scheme == "https");

		if (res != HTTPClient.Status.Connected) {
			EmitSignal("download_failed", this, res);
			return null;
		}

		var resp = _client.MakeRequest(dlUri.PathAndQuery, true);

		while (!resp.IsCompleted)
			await this.IdleFrame();

		if (resp.Result.ResponseCode == 302) {
			return await FollowRedirect((string)resp.Result.Headers["Location"]);
		}

		return resp.Result;
	}

	public async Task Download() {
		Uri dlUri = new Uri(_version.Url);
		var resp = FollowRedirect();

		while (!_cancelled && !resp.IsCompleted)
			await this.IdleFrame();
		
		if (_cancelled || resp.Result == null) {
			EmitSignal("download_failed", this, HTTPClient.Status.Requesting);
			return;
		}

		Mutex mutex = new Mutex();
		mutex.Lock();
		File file = new File();
		if (file.Open(_version.CacheLocation, File.ModeFlags.Write) != Error.Ok) {
			EmitSignal("download_failed", this, HTTPClient.Status.Body);
			return;
		}

		file.StoreBuffer(resp.Result.BodyRaw);
		file.Close();
		mutex.Unlock();
		EmitSignal("download_completed", this);
	}

	public void Install() {
		Uninstall(false);
#if GODOT_WINDOWS || GODOT_UWP || GODOT_LINUXBSD || GODOT_X11
		if (!_version.IsMono) {
			ZipFile.ExtractToDirectory(_version.CacheLocation, _version.Location);
		} else {
			Util.ExtractFromZipFolderTo(_version.CacheLocation, _version.Location);
		}
#else
		ZipFile.ExtractToDirectory(_version.CacheLocation, _version.Location);
#endif

		Array<string> fileList = new Array<string>();
		using (ZipArchive za = ZipFile.OpenRead(_version.CacheLocation.NormalizePath())) {
			foreach (ZipArchiveEntry zae in za.Entries) {
				fileList.Add(zae.Name);
			}
		}

#if GODOT_WINDOWS || GODOT_UWP
		foreach (string fname in fileList) {
			if (fname.EndsWith(".exe")) {
				_version.ExecutableName = fname;
				break;
			}
		}
#elif GODOT_LINUXBSD || GODOT_X11
		foreach (string fname in fileList) {
			if (!System.Environment.Is64BitProcess) {
				if (fname.EndsWith(".x86") || fname.EndsWith(".32")) {
					_version.ExecutableName = fname;
					break;
				}
			} else {
				if (fname.EndsWith(".x86_64") || fname.EndsWith(".64")) {
					_version.ExecutableName = fname;
					break;
				}
			}
		}
		Util.Chmod(_version.GetExecutablePath(), 0755);
#elif GODOT_MACOS || GODOT_OSX
		_version.ExecutableName = "Godot";
		Util.Chmod(_version.GetExecutablePath(), 0755);
#endif
		if (CentralStore.Settings.SelfContainedEditors && _version.GetMajorVersion() >= 2) {
			File fh = new File();
			if (fh.Open($"{_version.Location}/._sc_".GetOSDir().NormalizePath(), File.ModeFlags.Write) == Error.Ok) {
				fh.StoreString("");
				fh.Close();
			}
		}
	}

	public static Array<string> RecurseDirectory(string path) {
		Array<string> files = new Array<string>();
		foreach (string dir in SDirectory.EnumerateDirectories(path)) {
			foreach (string file in RecurseDirectory(FPath.Combine(path,dir).NormalizePath())) {
				files.Add(file);
			}
			files.Add(FPath.Combine(path,dir).NormalizePath());
		}

		foreach (string file in SDirectory.EnumerateFiles(path)) {
			files.Add(file.NormalizePath());
		}

		files.Add(path.NormalizePath());

		return files;
	}

	public void Uninstall(bool deleteCache = true) {
		if (SDirectory.Exists(_version.Location)) {
			foreach (string file in RecurseDirectory(_version.Location)) {
				if (SDirectory.Exists(file))
					SDirectory.Delete(file);
				else if (SFile.Exists(file))
					SFile.Delete(file);
			}
		}
		if (deleteCache && SFile.Exists(_version.CacheLocation)) {
			SFile.Delete(_version.CacheLocation);
		}
	}
}
