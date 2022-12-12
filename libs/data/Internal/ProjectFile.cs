using Godot;
using Godot.Collections;
using Newtonsoft.Json;
using DateTime = System.DateTime;

[JsonObject(MemberSerialization.OptIn)]
public class ProjectFile : Godot.Object {
	[JsonProperty] public string Icon;
	[JsonProperty] public string Name;
	[JsonProperty] public string Description;
	[JsonProperty] public string Location;
	[JsonProperty] public string GodotId;
	[JsonProperty] public int CategoryId;
	[JsonProperty] public bool Favorite;
	[JsonProperty] public DateTime LastAccessed;
	[JsonProperty] public Array<string> Assets;

	public static ProjectFile ReadFromFile(string filePath, int compatLevel = 3) {
		ProjectFile projectFile = null;
		ProjectConfig project = new ProjectConfig();
		var ret = project.Load(filePath);
		if (ret == Error.Ok) {
			if (!project.HasSection("application")) {
				AppDialogs.MessageDialog.ShowMessage("Failed to Load Project", "Section 'application' does not exist in the project file.");
				return projectFile;
			}
			if (compatLevel <= 2) {
				if (!project.HasSectionKey("application", "name")) {
					AppDialogs.MessageDialog.ShowMessage("Failed to Load Project", "Key 'name' does not exist in the project file.");
				}

				projectFile = new ProjectFile();
				projectFile.Name = project.GetValue("application", "name");
				projectFile.Description = projectFile.Tr("No Description");
				projectFile.Location = filePath.NormalizePath();
				projectFile.Icon = project.GetValue("application", "icon", "res://icon.png");
				return projectFile;
			} else if (compatLevel >= 3) {
				if (!project.HasSection("header")) {
					AppDialogs.MessageDialog.ShowMessage("Failed to Load Project", "Section 'header' does not exist in the project file.");
					return projectFile;
				}
				if (!project.HasSectionKey("header", "config_version")) {
					AppDialogs.MessageDialog.ShowMessage("Failed to Load Project", "Key 'config_version' does not exist in the project file.");
					return projectFile;
				}
				if (!project.HasSectionKey("application", "config/name")) {
					AppDialogs.MessageDialog.ShowMessage("Failed to Load Project", "Key 'config/name' does not exist in the project file.");
					return projectFile;
				}

				int configVersion = project.GetValue("header", "config_version").ToInt();
				if (configVersion >= 3) {
					projectFile = new ProjectFile();
					projectFile.Name = project.GetValue("application", "config/name");
					projectFile.Description = project.GetValue("application", "config/description", projectFile.Tr("No Description"));
					projectFile.Location = filePath.NormalizePath();
					projectFile.Icon = project.GetValue("application", "config/icon", "res://icon.png");
				} else {
					AppDialogs.MessageDialog.ShowMessage("Failed to Load Project", $"{filePath}: Project Version does not match version 3+.");
				}
			}
		} else {
			AppDialogs.MessageDialog.ShowMessage("Failed to Load Project", $"{filePath}: {ret}");
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
			if (pf.GetValue("header","config_version") == "4" || pf.GetValue("header","config_version") == "5") {
				this.Name = pf.GetValue("application", "config/name");
				this.Description = pf.GetValue("application", "config/description", Tr("No Description"));
				this.Icon = pf.GetValue("application","config/icon", "res://icon.png");
			}
		}
	}

	public void WriteUpdatedData() {
		ProjectConfig pf = new ProjectConfig();
		var ret = pf.Load(Location);
		if (ret == Error.Ok) {
			pf.SetValue("application", "config/name", $"\"{this.Name}\"");
			pf.SetValue("application", "config/description", $"\"{this.Description}\"");
			pf.SetValue("application", "config/icon", $"\"{this.Icon}\"");
			pf.Save(Location);
		}
	}
}