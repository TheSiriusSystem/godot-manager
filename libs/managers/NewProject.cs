using Godot;
using Godot.Collections;
using System.IO.Compression;
using Directory = System.IO.Directory;
using StreamWriter = System.IO.StreamWriter;

public class NewProject : Object {
	public string ProjectName;
	public string ProjectLocation;
	public int VersionControlSystem;
	public AssetProject Template;
	public string GodotId;
	public int GodotMajorVersion;
	public int GodotMinorVersion;
	public Array<AssetPlugin> Plugins;
	public Dictionary ModifiedKeys;

	public bool CreateProject() {
		string pfName = GodotMajorVersion <= 2 ? "engine.cfg" : "project.godot";

		if (Template == null)
		{
			// Need to create the Project File ourselves.
			CreateProjectFile();
			CreateVersionControlMetadata();
			CreateDefaultEnvironment();
			CopyIcon();
			ExtractPlugins();
		} else {
			// Project file should be provided in the Template.
			ExtractTemplate();
			ProjectConfig pf = new ProjectConfig();
			pf.Load(ProjectLocation.PlusFile(pfName).NormalizePath());
			pf.SetValue("application", GodotMajorVersion <= 2 ? "name" : "config/name", $"\"{ProjectName}\"");

			// Need way to compile Assets before Enabling Plugins
			// if (Plugins.Count > 0)
			// 	SetupPlugins(pf);

			pf.Save(ProjectLocation.PlusFile(pfName));
			ExtractPlugins();
		}
		
		return true;
	}

	private void ExtractTemplate()
	{
		using (ZipArchive za = ZipFile.OpenRead(ProjectSettings.GlobalizePath(Template.Location))) {
			foreach (ZipArchiveEntry zae in za.Entries) {
				int pp = zae.FullName.Find("/") + 1;
				string path = zae.FullName.Substr(pp, zae.FullName.Length);
				if (zae.FullName.EndsWith("/")) {
					// Is folder, we need to ensure to make the folder in the Project Location.
					Directory.CreateDirectory(ProjectLocation.PlusFile(path));
				} else {
					zae.ExtractToFile(ProjectLocation.PlusFile(path));
				}
			}
		}
	}

	private void ExtractPlugins()
	{
		if ((GodotMajorVersion == 2 && GodotMinorVersion >= 1) || GodotMajorVersion >= 3) {
			string addonsPath = ProjectLocation.PlusFile("addons").NormalizePath();
			if (!Directory.Exists(addonsPath))
				Directory.CreateDirectory(addonsPath);

			foreach (AssetPlugin plgn in Plugins) {
				PluginInstaller installer = new PluginInstaller(plgn);
				installer.Install(ProjectLocation);
			}
		}
	}

	private void CopyIcon()
	{
		Texture image = MainWindow._plTextures["DefaultIconV3"];
		if (GodotMajorVersion <= 2) {
			image = MainWindow._plTextures["DefaultIconV1"];
		} else if (GodotMajorVersion >= 4) {
			image = MainWindow._plTextures["DefaultIconV4"];
		}
		image.GetData().SavePng(ProjectLocation.PlusFile("icon.png").NormalizePath());
	}

	private void CreateDefaultEnvironment()
	{
		if (GodotMajorVersion == 3) {
			using (StreamWriter writer = new StreamWriter(ProjectLocation.PlusFile("default_env.tres").NormalizePath())) {
				writer.WriteLine("[gd_resource type=\"Environment\" load_steps=2 format=2]");
				if (GodotMinorVersion >= 5) writer.WriteLine("");
				writer.WriteLine("[sub_resource type=\"ProceduralSky\" id=1]");
				if (GodotMinorVersion >= 5) writer.WriteLine("");
				writer.WriteLine("[resource]");
				writer.WriteLine("background_mode = 2");
				writer.WriteLine("background_sky = SubResource( 1 )");
				writer.WriteLine("");
			}
		}
	}

	private void CreateProjectFile()
	{
		ProjectConfig pf = new ProjectConfig();
		if (GodotMajorVersion <= 2) {
			pf.SetValue("application", "name", $"\"{ProjectName}\"");
			pf.SetValue("application", "icon", "\"res://icon.png\"");
		} else {
			string cfgVers = "3";
			if (GodotMajorVersion == 3 && GodotMinorVersion >= 1) {
				cfgVers = "4";
			} else if (GodotMajorVersion >= 4) {
				cfgVers = "5";
			}

			pf.SetValue("header", "config_version", cfgVers);
			pf.SetValue("application", "config/name", $"\"{ProjectName}\"");
			pf.SetValue("application", "config/icon", "\"res://icon.png\"");
			if (GodotMajorVersion == 3) {
				pf.SetValue("rendering", "environment/default_environment", "\"res://default_env.tres\"");
				if (GodotMinorVersion >= 3) {
					pf.SetValue("physics", "common/enable_pause_aware_picking", "true");
				}
				if (GodotMinorVersion >= 5) {
					pf.SetValue("gui", "common/drop_mouse_on_gui_input_disabled", "true");
				}
			}
			foreach (string section in ModifiedKeys.Keys) {
				Dictionary sectionDict = ModifiedKeys[section] as Dictionary;
				foreach (string key in sectionDict.Keys) {
					pf.SetValue(section, key, sectionDict[key] as string);
				}
			}
		}
		pf.Save(ProjectLocation.PlusFile(GodotMajorVersion <= 2 ? "engine.cfg" : "project.godot").NormalizePath());
	}

	private void CreateVersionControlMetadata()
	{
		switch (VersionControlSystem) {
			case 1: // Git
				using (StreamWriter writer = new StreamWriter(ProjectLocation.PlusFile(".gitattributes").NormalizePath())) {
					writer.WriteLine("# Auto detect text files and perform LF normalization");
					writer.WriteLine("* text=auto eol=lf");
				}
				using (StreamWriter writer = new StreamWriter(ProjectLocation.PlusFile(".gitignore").NormalizePath())) {
					writer.WriteLine(".DS_Store");
					writer.WriteLine("");
					writer.WriteLine("# Godot 1 specific ignores");
					writer.WriteLine(".fscache");
					writer.WriteLine("");
					writer.WriteLine("# Godot 4+ specific ignores");
					writer.WriteLine(".godot/");
					writer.WriteLine("");
					writer.WriteLine("# Godot-specific ignores");
					writer.WriteLine(".import/");
					writer.WriteLine("export.cfg");
					writer.WriteLine("export_presets.cfg");
					writer.WriteLine("");
					writer.WriteLine("# Imported translations (automatically generated from CSV files)");
					writer.WriteLine("*.translation");
					writer.WriteLine("");
					writer.WriteLine("# Mono-specific ignores");
					writer.WriteLine(".mono/");
					writer.WriteLine("data_*/");
					writer.WriteLine("mono_crash.*.json");
				}
				break;
		}
	}
}