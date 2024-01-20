using Godot;
using Godot.Collections;
using Path = System.IO.Path;
using DirectoryInfo = System.IO.DirectoryInfo;
using DirectoryNotFoundException = System.IO.DirectoryNotFoundException;
using FileNotFoundException = System.IO.FileNotFoundException;
using FileInfo = System.IO.FileInfo;
using Dir = System.IO.Directory;
using SFile = System.IO.File;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO.Compression;

public static class Util
{
	public static string GetResourceBase(this string path, string file) {
		return Path.Combine(path.GetBaseDir(), file.Replace("res://", "")).Replace(@"\","/");
	}

	public static string GetProjectRoot(this string path, string target) {
		return target.Replace(path,"res:/").Replace(@"\", "/");
	}

	public static string GetOSDir(this string path) {
		return ProjectSettings.GlobalizePath(path);
	}

	public static string GetExtension(this string path) {
		return Path.GetExtension(path);
	}

	public static Array<ushort> GetVersionComponentsFromString(string text) {
		Array<ushort> versionComponents = new Array<ushort> { 0, 0, 0 };
		Match resMatch = Regex.Match(text, @"^(?:\D+)?(?<Major>\d+)(?:\.(?<Minor>\d+))?(?:\.(?<Patch>\d+))?(?:.+?)?$");
		if (resMatch.Success) {
			ushort.TryParse(resMatch.Groups["Major"].Value, out ushort result);
			versionComponents[0] = result;
			ushort.TryParse(resMatch.Groups["Minor"].Value, out result);
			versionComponents[1] = result;
			ushort.TryParse(resMatch.Groups["Patch"].Value, out result);
			versionComponents[2] = result;
		}
		return versionComponents;
	}

	static string[] ByteSizes = new string[] {"B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "RB", "QB"};

	public static string FormatSize(double bytes) {
		double len = bytes;
		int order = 0;
		while (len >= 1024 && order < ByteSizes.Length - 1) {
			order++;
			len = len / 1024;
		}
		return string.Format("{0:F} {1}", len, ByteSizes[order]);
	}

	public static string NormalizePath(this string path) {
		if (path.StartsWith("res://") || path.StartsWith("user://"))
			return Path.GetFullPath(path.GetOSDir());
		else
			return Path.GetFullPath(path);
	}

	public static string NormalizeFileName(this string text) {
		foreach (char invalidChar in Path.GetInvalidFileNameChars()) {
			text = text.Replace(invalidChar, '\0');
		}
		return text;
	}

	public static string Join(this string path, params string[] addTo) {
		if (path.EndsWith("/"))
			path = path.Substr(0,path.Length-1);
		
		foreach (string part in addTo) {
			path += "/" + part;
		}
		return path;
	}

	public static string Join(this string[] parts, string separator) => string.Join(separator, parts);
	public static string GetParentFolder(this string path) => path.GetBaseDir().GetBaseDir();
	public static bool IsDirEmpty(this string path) => !Dir.Exists(path) || !Dir.EnumerateFileSystemEntries(path).Any();

	public static string GetUserFolder(params string[] parts)
	{
		string path = "user://";
		if (OS.HasFeature("standalone"))
		{
			string exePath = OS.GetExecutablePath().GetBaseDir().NormalizePath();
			if (SFile.Exists(exePath.Join("._sc_")) || SFile.Exists(exePath.Join("_sc_")))
				path = exePath.Join("data_" + ProjectSettings.GetSetting("application/config/name"));
		}
		return parts.Length == 0 ? path : path.Join(parts);
	}

	public static string GetDatabaseFile()
	{	
		string path = GetUserFolder("central_store.json");
		if (OS.HasFeature("standalone"))
		{
			string exePath = OS.GetExecutablePath().GetBaseDir().NormalizePath();
			if ((SFile.Exists(exePath.Join("._sc_")) || SFile.Exists(exePath.Join("_sc_"))) && !Dir.Exists(path.GetBaseDir()))
				Dir.CreateDirectory(path.GetBaseDir().NormalizePath());
		}
		return path;
	}

	public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive = false) {
		var dir = new DirectoryInfo(sourceDir);

		if (!dir.Exists)
			throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
		
		DirectoryInfo[] dirs = dir.GetDirectories();

		Dir.CreateDirectory(destinationDir);

		foreach (FileInfo file in dir.GetFiles()) {
			string targetFilePath = Path.Combine(destinationDir, file.Name);
			file.CopyTo(targetFilePath);
		}

		if (recursive) {
			foreach (DirectoryInfo subDir in dirs) {
				string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
				CopyDirectory(subDir.FullName, newDestinationDir, true);
			}
		}
	}

	public static void CopyTo(string srcFile, string destFile) {
		FileInfo file = new FileInfo(srcFile);
		
		if (!file.Exists)
			throw new FileNotFoundException($"Source file not found: {file.FullName}");
		
		file.CopyTo(destFile);
	}

	public static void ExtractFromZipFolderTo(string sourceArchiveFileName, string destinationDirectoryName) {
		using (ZipArchive za = ZipFile.OpenRead(sourceArchiveFileName.NormalizePath())) {
			foreach (ZipArchiveEntry zae in za.Entries) {
				int pp = zae.FullName.Find("/") + 1;
				string path = destinationDirectoryName.PlusFile(zae.FullName.Substr(pp, zae.FullName.Length)).NormalizePath();
				if (zae.FullName.EndsWith("/")) {
					// Is folder, we need to ensure to make the folder in the destination.
					Dir.CreateDirectory(path);
				} else {
					zae.ExtractToFile(path);
				}
			}
		}
	}

	public static SignalAwaiter IdleFrame(this Godot.Object obj) {
		return obj.ToSignal(Engine.GetMainLoop(), "idle_frame");
	}

	public static ImageTexture LoadImage(string path) {
		var image = new Image();
		
		if (path.StartsWith("res://"))
		{
			StreamTexture tex = GD.Load<StreamTexture>(path);
			image = tex.GetData();
		} else {
			if (!SFile.Exists(path.GetOSDir().NormalizePath()))
				return null;

			if (SixLabors.ImageSharp.Image.DetectFormat(path.GetOSDir().NormalizePath()) == null)
				return null;

			Error err = image.Load(path);
			if (err != Error.Ok)
				return null;
		}
		var texture = new ImageTexture();
		texture.CreateFromImage(image);
		return texture;
	}

#if GODOT_LINUXBSD || GODOT_X11 || GODOT_MACOS || GODOT_OSX
	public static bool Chmod(string path, int perms) {
		string chmod_cmd = "";
		Array output = new Array();
		int exit_code = OS.Execute("which", new string[] { "chmod" }, true, output);
		if (exit_code == 0)
			chmod_cmd = (output[0] as string).StripEdges();

		if (string.IsNullOrEmpty(chmod_cmd))
			return false;

		exit_code = OS.Execute(chmod_cmd, new string[] { perms.ToString(), path.GetOSDir() }, true);
		if (exit_code != 0) 
			return false;

		return true;
	}
#endif

	public static string ReadFile(this ZipArchiveEntry zae) {
		byte[] buffer = new byte[zae.Length];
		using (var fh = zae.Open()) {
			fh.Read(buffer, 0, (int)zae.Length);
		}
		return buffer.GetStringFromUTF8();
	}

	public static byte[] ReadBuffer(this ZipArchiveEntry zae) {
		byte[] buffer = new byte[zae.Length];
		using (var fh = zae.Open()) {
			fh.Read(buffer, 0, (int)zae.Length);
		}
		return buffer;
	}
}
