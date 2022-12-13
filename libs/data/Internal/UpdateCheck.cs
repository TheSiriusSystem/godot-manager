using Godot;
using Newtonsoft.Json;
using DateTime = System.DateTime;

[JsonObject(MemberSerialization.OptIn)]
public class UpdateCheck : Object
{
	[JsonProperty] public DateTime LastCheck { get; set; }
}