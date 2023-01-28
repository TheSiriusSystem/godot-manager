using Godot;
using Godot.Collections;
using Newtonsoft.Json;
using DateTime = System.DateTime;

[JsonObject(MemberSerialization.OptIn)]
public class ProjectFile : Object {
	[JsonProperty] public string Icon;
	[JsonProperty] public string Name;
	[JsonProperty] public string Description;
	[JsonProperty] public string Location;
	[JsonProperty] public string GodotId;
	[JsonProperty] public int CategoryId;
	[JsonProperty] public bool Favorite;
	[JsonProperty] public DateTime LastAccessed;
	[JsonProperty] public Array<string> Assets;

	public static ProjectFile ReadFromFile(string filePath, bool isOldPrj) {
		ProjectFile projectFile = null;
		ProjectConfig project = new ProjectConfig();
		var ret = project.Load(filePath);
		if (ret == Error.Ok) {
			if (!project.HasSection("application")) {
				GD.PrintErr($"Section \"application\" doesn't exist in \"{filePath}\".");
				return projectFile;
			}

			filePath = filePath.NormalizePath();
			if (isOldPrj) {
				if (!project.HasSectionKey("application", "name")) {
					GD.PrintErr($"Key \"name\" doesn't exist in \"{filePath}\".");
					return projectFile;
				}

				projectFile = new ProjectFile();
				projectFile.Name = project.GetValue("application", "name");
				projectFile.Description = "";
				projectFile.Location = filePath;
				projectFile.Icon = project.GetValue("application", "icon", "res://icon.png");
			} else {
				if (!project.HasSection("header")) {
					GD.PrintErr($"Section \"header\" doesn't exist in \"{filePath}\".");
					return projectFile;
				}
				if (!project.HasSectionKey("header", "config_version")) {
					GD.PrintErr($"Key \"config_version\" doesn't exist in \"{filePath}\".");
					return projectFile;
				}
				if (!project.HasSectionKey("application", "config/name")) {
					GD.PrintErr($"Key \"config/name\" doesn't exist in \"{filePath}\".");
					return projectFile;
				}

				projectFile = new ProjectFile();
				projectFile.Name = project.GetValue("application", "config/name");
				projectFile.Description = project.GetValue("application", "config/description", "");
				projectFile.Location = filePath;
				projectFile.Icon = project.GetValue("application", "config/icon", "res://icon.png");
			}
		} else {
			GD.PrintErr($"Failed to load \"{filePath.GetFile()}\". Error Code: {ret}");
		}
		return projectFile;
	}

	public static bool ProjectExists(string filePath) {
		bool ret = false;

		var path = filePath.GetBaseDir();
		var dir = new Directory();
		ret = dir.DirExists(path);
		if (ret) {
			ret = dir.FileExists(filePath);
		}

		return ret;
	}

	public bool HasPlugin(string id) {
		return Assets.Contains(id);
	}

	public ProjectFile() {
		Icon = "";
		Name = "";
		Description = "";
		Location = "";
		GodotId = "";
		CategoryId = -1;
		Favorite = false;
		LastAccessed = DateTime.UtcNow;
	}

	public void UpdateData() {
		ProjectConfig pf = new ProjectConfig();
		var ret = pf.Load(Location);
		if (ret == Error.Ok) {
			if (!pf.HasSection("header")) {
				this.Name = pf.GetValue("application", "name");
				this.Description = "";
				this.Icon = pf.GetValue("application", "icon");
			} else if (pf.GetValue("header", "config_version").ToInt() >= 3) {
				this.Name = pf.GetValue("application", "config/name");
				this.Description = pf.GetValue("application", "config/description", "");
				this.Icon = pf.GetValue("application", "config/icon", "res://icon.png");
			}
		}
	}

	public void WriteUpdatedData() {
		ProjectConfig pf = new ProjectConfig();
		var ret = pf.Load(Location);
		if (ret == Error.Ok) {
			if (CentralStore.Instance.GetVersion(GodotId).GetMajorVersion() <= 2) {
				pf.SetValue("application", "name", $"\"{this.Name}\"");
				pf.SetValue("application", "icon", $"\"{this.Icon}\"");
			} else {
				pf.SetValue("application", "config/name", $"\"{this.Name}\"");
				pf.SetValue("application", "config/description", $"\"{this.Description}\"");
				pf.SetValue("application", "config/icon", $"\"{this.Icon}\"");
			}
			pf.Save(Location);
		}
	}
}