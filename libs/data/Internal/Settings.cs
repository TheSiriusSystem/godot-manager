using Godot;
using Godot.Collections;
using Newtonsoft.Json;
using DateTime = System.DateTime;

[JsonObject(MemberSerialization.OptIn)]
public class Settings : Object {
	[JsonProperty] public string ProjectPath;
	[JsonProperty] public string EnginePath;
	[JsonProperty] public string CachePath;
	[JsonProperty] public string LastView;
	[JsonProperty] public string DefaultView;
	[JsonProperty] public bool UseSystemTitlebar;
	[JsonProperty] public bool UseLastMirror;
	[JsonProperty] public bool NoConsole;
	[JsonProperty] public bool SelfContainedEditors;
	[JsonProperty] public bool EnableAutoScan;
	[JsonProperty] public bool FavoritesToggled;
	[JsonProperty] public bool UncategorizedToggled;
	[JsonProperty] public Array<string> ScanDirs;
	[JsonProperty] public Array<string> SettingsShare;

	[JsonProperty] public int LastEngineMirror;

	public bool FirstTimeRun = false;

	public Settings() {
		ProjectPath = OS.GetSystemDir(OS.SystemDir.Documents).Join("Godot Projects").NormalizePath();
		EnginePath = Util.GetUserFolder("versions");
		CachePath = Util.GetUserFolder("cache");
		LastView = Tr("List View");
		DefaultView = Tr("List View");
		SelfContainedEditors = true;
		EnableAutoScan = false;
		FavoritesToggled = false;
		UncategorizedToggled = false;
		NoConsole = true;
		UseSystemTitlebar = false;
		UseLastMirror = false;
		ScanDirs = new Array<string>();
		LastEngineMirror = 0;
		SettingsShare = new Array<string>();
	}

	public void SetupDefaultValues() {
		FirstTimeRun = true;

		// Scan Directories (Default Project path added)
		ScanDirs.Add(ProjectPath);
	}
}