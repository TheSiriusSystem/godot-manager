using Godot;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class AssetProject : Object {
	[JsonProperty] public AssetLib.Asset Asset;
	[JsonProperty] public string Location;
}