using Godot;
using Newtonsoft.Json;
using System.Linq;
using Environment = System.Environment;

[JsonObject(MemberSerialization.OptIn)]
public class GithubVersion : Object
{
	[JsonProperty] public string Name;	// Github.Release.Name
	[JsonProperty] public VersionUrls Standard; // See VersionUrls
	[JsonProperty] public VersionUrls Mono; // See VersionUrls

	public string PlatformDownloadURL {
		get {
#if GODOT_WINDOWS || GODOT_UWP
			return !Environment.Is64BitProcess ? Standard.Win32 : Standard.Win64;
#elif GODOT_LINUXBSD || GODOT_X11
			return !Environment.Is64BitProcess ? Standard.Linux32 : Standard.Linux64;
#elif GODOT_MACOS || GODOT_OSX
			return Standard.OSX;
#else
			return "";
#endif
		}
	}

	public string PlatformMonoDownloadURL {
		get {
#if GODOT_WINDOWS || GODOT_UWP
			return !Environment.Is64BitProcess ? Mono.Win32 : Mono.Win64;
#elif GODOT_LINUXBSD || GODOT_X11
			return !Environment.Is64BitProcess ? Mono.Linux32 : Mono.Linux64;
#elif GODOT_MACOS || GODOT_OSX
			return Mono.OSX;
#else
			return "";
#endif
		}
	}

	public int PlatformDownloadSize {
		get {
#if GODOT_WINDOWS || GODOT_UWP
			return !Environment.Is64BitProcess ? Standard.Win32_Size : Standard.Win64_Size;
#elif GODOT_LINUXBSD || GODOT_X11
			return !Environment.Is64BitProcess ? Standard.Linux32_Size : Standard.Linux64_Size;
#elif GODOT_MACOS || GODOT_OSX
			return Standard.OSX_Size;
#else
			return -1;
#endif
		}
	}

	public int PlatformMonoDownloadSize {
		get {
#if GODOT_WINDOWS || GODOT_UWP
			return !Environment.Is64BitProcess ? Mono.Win32_Size : Mono.Win64_Size;
#elif GODOT_LINUXBSD || GODOT_X11
			return !Environment.Is64BitProcess ? Mono.Linux32_Size : Mono.Linux64_Size;
#elif GODOT_MACOS || GODOT_OSX
			return Mono.OSX_Size;
#else
			return -1;
#endif
		}
	}


	void GatherUrls(Github.Release release) {
		string[] fields = new string[] {
			"Win32", "Win64", "Linux32", "Linux64", "OSX",
			"Templates", "Headless", "Server"
		};
		string[] standard_match = new string[] {
			"win32", "win64", "x11.32", "x11.64", "osx", 
			"export_templates.tpz", "linux_headless.64", "linux_server.64"
		};
		string[] standard_not_match = new string[] {
			"mono_win32", "mono_win64", "JavaDaHutForYou", "JavaDaHutForYou", "mono_osx",
			"mono_export_templates.tpz", "JavaDaHutForYou", "JavaDaHutForYou"
		};
		string[] mono_match = new string[] {
			"mono_win32", "mono_win64", "mono_x11_32", "mono_x11_64", "mono_osx",
			"mono_export_templates.tpz", "mono_linux_headless_64", "mono_linux_server_64"
		};

		VersionUrls standard = new VersionUrls();
		VersionUrls mono = new VersionUrls();
		for (int i = 0; i < standard_match.Length; i++) {
			var t = from asset in release.Assets
					where asset.Name.FindN(standard_match[i]) != -1 && asset.Name.FindN(standard_not_match[i]) == -1
					select asset;
			if (t.FirstOrDefault() is Github.Asset ghAsset) {
				standard[fields[i]] = ghAsset.BrowserDownloadUrl;
				standard[$"{fields[i]}_Size"] = ghAsset.Size;
			}
			t = from asset in release.Assets
				where asset.Name.FindN(mono_match[i]) != -1
				select asset;
			if (t.FirstOrDefault() is Github.Asset mghAsset) {
				mono[fields[i]] = mghAsset.BrowserDownloadUrl;
				mono[$"{fields[i]}_Size"] = mghAsset.Size;
			}
		}
		Standard = standard;
		Mono = mono;
	}


	public static GithubVersion FromAPI(Github.Release release) {
		GithubVersion api = new GithubVersion();
		api.Name = release.Name;
		foreach (string str in new string[] {"_stable", "-stable"}) {
			api.Name = api.Name.ReplaceN(str, "");
		}
		api.GatherUrls(release);
		return api;
	}
}