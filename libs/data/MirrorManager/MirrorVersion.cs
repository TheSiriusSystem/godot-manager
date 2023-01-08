using Godot;
using Godot.Collections;
using FPath = System.IO.Path;
using Newtonsoft.Json;
using Environment = System.Environment;

[JsonObject(MemberSerialization.OptIn)]
public class MirrorVersion : Object
{
	[JsonProperty] public int Id { get; set; } = 0;
	[JsonProperty] public int MirrorId { get; set; } = 0;

	[JsonProperty] public string Version { get; set; } = "";
	[JsonProperty] public string BaseLocation { get; set; } = "";

	[JsonProperty] public string OSX32 { get; set; } = "";
	[JsonProperty] public string OSX64 { get; set; } = "";
	[JsonProperty] public string Win32 { get; set; } = "";
	[JsonProperty] public string Win64 { get; set; } = "";
	[JsonProperty] public string Linux32 { get; set; } = "";
	[JsonProperty] public string Linux64 { get; set; } = "";
	[JsonProperty] public string Source { get; set; } = "";

	[JsonProperty] public int OSX32_Size { get; set; } = 0;
	[JsonProperty] public int OSX64_Size { get; set; } = 0;
	[JsonProperty] public int Win32_Size { get; set; } = 0;
	[JsonProperty] public int Win64_Size { get; set; } = 0;
	[JsonProperty] public int Linux32_Size { get; set; } = 0;
	[JsonProperty] public int Linux64_Size { get; set; } = 0;
	[JsonProperty] public int Source_Size { get; set; } = 0;

	[JsonProperty] public Array<string> Tags { get; set; } = new Array<string>();

	public string PlatformDownloadURL {
		get {
#if GODOT_WINDOWS || GODOT_UWP
			return FPath.Combine(BaseLocation, !Environment.Is64BitProcess ? Win32 : Win64);
#elif GODOT_LINUXBSD || GODOT_X11
			return FPath.Combine(BaseLocation, !Environment.Is64BitProcess ? Linux32 : Linux64);
#elif GODOT_MACOS || GODOT_OSX
			return FPath.Combine(BaseLocation, !Environment.Is64BitProcess ? OSX32 : OSX64);
#else
			return "";
			#endif
		}
	}

	public string PlatformZipFile {
		get {
#if GODOT_WINDOWS || GODOT_UWP
			return !Environment.Is64BitProcess ? Win32 : Win64;
#elif GODOT_LINUXBSD || GODOT_X11
			return !Environment.Is64BitProcess ? Linux32 : Linux64;
#elif GODOT_MACOS || GODOT_OSX
			return !Environment.Is64BitProcess ? OSX32 : OSX64;
#else
			return "";
#endif
		}
	}

	public int PlatformDownloadSize
	{
		get {
#if GODOT_WINDOWS || GODOT_UWP
			return !Environment.Is64BitProcess ? Win32_Size : Win64_Size;
#elif GODOT_LINUXBSD || GODOT_X11
			return !Environment.Is64BitProcess ? Linux32_Size : Linux64_Size;
#elif GODOT_MACOS || GODOT_OSX
			return !Environment.Is64BitProcess ? OSX32_Size : OSX64_Size;
#else
			return -1;
#endif
		}
	}
}