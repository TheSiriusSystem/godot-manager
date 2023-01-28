using Godot;
using Godot.Collections;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class CentralStoreData : Object {
	[JsonProperty] public Settings Settings;
	[JsonProperty] public Array<ProjectFile> Projects;
	[JsonProperty] public Array<GodotVersion> Versions;
	[JsonProperty] public Array<GithubVersion> GHVersions;
	[JsonProperty] public Array<MirrorVersion> TFVersions;
	[JsonProperty] public Array<Category> Categories;
	[JsonProperty] public Array<int> PinnedCategories;
	[JsonProperty] public Array<AssetPlugin> Plugins;
	[JsonProperty] public Array<AssetProject> Templates;
	[JsonProperty] public int TotalNewsPages;

	public CentralStoreData() {
		Settings = new Settings();
		Projects = new Array<ProjectFile>();
		Versions = new Array<GodotVersion>();
		GHVersions = new Array<GithubVersion>();
		TFVersions = new Array<MirrorVersion>();
		Categories = new Array<Category>();
		PinnedCategories = new Array<int>();
		Plugins = new Array<AssetPlugin>();
		Templates = new Array<AssetProject>();
	}
}