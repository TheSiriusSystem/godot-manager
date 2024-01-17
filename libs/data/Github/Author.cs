using Godot;
using Newtonsoft.Json;

namespace Github {
	[JsonObject(MemberSerialization.OptIn)]
	public class Author : Object {
		[JsonProperty] public string Login;
	}
}