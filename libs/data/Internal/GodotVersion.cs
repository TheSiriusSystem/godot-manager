using Godot;
using Newtonsoft.Json;
using Guid = System.Guid;

[JsonObject(MemberSerialization.OptIn)]
public class GodotVersion : Object {
	[JsonProperty] public string Id; // This will be an UUID
	[JsonProperty] public string Tag; // This will be used to display to the user
	[JsonProperty] public bool IsMono; // This is used to determine if the file downloaded is Mono
	[JsonProperty] public string Location; // Location of where Godot is
	[JsonProperty] public string ExecutableName; // Name of the Final Executable
	[JsonProperty] public string CacheLocation; // Location of where the cache file is.
	[JsonProperty] public string Url;	// URL downloaded from (Will match Location for Custom)
	[JsonProperty] public GithubVersion GithubVersion;
	[JsonProperty] public MirrorVersion MirrorVersion;
	[JsonProperty] public string SharedSettings;

	public GodotVersion() {
		Id = Guid.Empty.ToString();
		Tag = "";
		Location = "";
		Url = "";
		SharedSettings = "";
	}

	public string GetDisplayName() {
		return $"Godot {Tag}";
	}

	public string GetExecutablePath() {
		string exePath = "";
#if GODOT_MACOS || GODOT_OSX
		exePath = Location.Join(!IsMono ? "Godot.app" : "Godot_mono.app", "Contents", "MacOS", ExecutableName);
#else
		exePath = Location.Join(ExecutableName);
#endif
		return exePath.NormalizePath();
	}
}