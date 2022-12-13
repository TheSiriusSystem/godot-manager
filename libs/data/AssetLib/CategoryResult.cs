using Godot;
using Newtonsoft.Json;

namespace AssetLib {
	[JsonObject(MemberSerialization.OptIn)]
	public class CategoryResult : Object {
		[JsonProperty] public string Id;
		[JsonProperty] public string Name;
		[JsonProperty] public string Type;
	}
}
