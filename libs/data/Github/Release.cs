using Godot;
using Godot.Collections;
using Newtonsoft.Json;

namespace Github {
	[JsonObject(MemberSerialization.OptIn)]
	public class Release : Object {
		[JsonProperty] public string Name;
		[JsonProperty] public string TagName;
		[JsonProperty] public Array<Asset> Assets;
	}
}