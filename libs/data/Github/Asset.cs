using Godot;
using Newtonsoft.Json;

namespace Github {
	[JsonObject(MemberSerialization.OptIn)]
	public class Asset : Object {
		[JsonProperty] public string Url;
		[JsonProperty] public string BrowserDownloadUrl;
		[JsonProperty] public int Id;
		[JsonProperty] public string Name;
		[JsonProperty] public string Label;
		[JsonProperty] public int Size;
		[JsonProperty] public int DownloadCount;

		public static Asset FromJson(string data) {
			return JsonConvert.DeserializeObject<Asset>(data,DefaultSettings.defaultJsonSettings);
		}
	}
}