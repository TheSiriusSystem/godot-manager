using Godot;
using Godot.Collections;
using Newtonsoft.Json;

namespace Github {

	[JsonObject(MemberSerialization.OptIn)]
	public class Release : Object {
		[JsonProperty] public string Name;
		[JsonProperty] public Author Author;
		[JsonProperty] public Array<Asset> Assets;

		public static Release FromJson(string data) {
			return JsonConvert.DeserializeObject<Release>(data,DefaultSettings.defaultJsonSettings);
		}
	}
}