using Godot;
using Godot.Collections;
using File = System.IO.File;
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
		if (Template == null)
		{
			// Need to create the Project File ourselves.
			CreateDefaultEnvironment();
			CopyIcon();
		} else {
			// Project file should be provided in the Template.
			Util.ExtractFromZipFolderTo(Template.Location, ProjectLocation);
		}
		SetupProjectConfig();
		CreateVersionControlMetadata();
		ExtractPlugins();
		return true;
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

	private void SetupProjectConfig()
	{
		ProjectConfig pf = new ProjectConfig();
		string pfPath = ProjectLocation.PlusFile(GodotMajorVersion <= 2 ? "engine.cfg" : "project.godot").NormalizePath();

		if (Template != null) pf.Load(pfPath);
		if (GodotMajorVersion <= 2) {
			pf.SetValue("application", "name", $"\"{ProjectName}\"");
			if (Template == null) pf.SetValue("application", "icon", "\"res://icon.png\"");
		} else {
			string cfgVers = "3";
			if (GodotMajorVersion == 3 && GodotMinorVersion >= 1) {
				cfgVers = "4";
			} else if (GodotMajorVersion >= 4) {
				cfgVers = "5";
			}

			pf.SetValue("header", "config_version", cfgVers);
			pf.SetValue("application", "config/name", $"\"{ProjectName}\"");
			if (Template == null) pf.SetValue("application", "config/icon", "\"res://icon.png\"");
			if (GodotMajorVersion == 3) {
				if (Template == null) pf.SetValue("rendering", "environment/default_environment", "\"res://default_env.tres\"");
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
		pf.Save(pfPath);
	}

	private void CreateVersionControlMetadata()
	{
		string gitattributesPath = ProjectLocation.PlusFile(".gitattributes").NormalizePath();
		string gitignorePath = ProjectLocation.PlusFile(".gitignore").NormalizePath();

		switch (VersionControlSystem) {
			case 0: // None
				if (File.Exists(gitattributesPath))
					File.Delete(gitattributesPath);
				if (File.Exists(gitignorePath))
					File.Delete(gitignorePath);
				break;
			case 1: // Git
				using (StreamWriter writer = new StreamWriter(gitattributesPath)) {
					writer.WriteLine("# Auto detect text files and perform LF normalization");
					writer.WriteLine("* text=auto eol=lf");
				}
				using (StreamWriter writer = new StreamWriter(gitignorePath)) {
					writer.WriteLine("# Godot-specific ignores");
					writer.WriteLine(".import/");
					writer.WriteLine(".godot/");
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
					writer.WriteLine("");
					writer.WriteLine("# Visual Studio 2015/2017 cache/options directory");
					writer.WriteLine(".vs/");
					writer.WriteLine("");
					writer.WriteLine("# Visual Studio 2017 auto generated files");
					writer.WriteLine(@"Generated\ Files/");
					writer.WriteLine("");
					writer.WriteLine("# Visual Studio Code");
					writer.WriteLine(".vscode/");
					writer.WriteLine("*.code-workspace");
					writer.WriteLine(".history/");
					writer.WriteLine("");
					writer.WriteLine("# Windows");
					writer.WriteLine("Thumbs.db");
					writer.WriteLine("Thumbs.db:encryptable");
					writer.WriteLine("ehthumbs.db");
					writer.WriteLine("ehthumbs_vista.db");
					writer.WriteLine("");
					writer.WriteLine("# Linux");
					writer.WriteLine("*~");
					writer.WriteLine(".directory");
					writer.WriteLine("");
					writer.WriteLine("# macOS");
					writer.WriteLine(".DS_Store");
					writer.WriteLine("__MACOSX");
				}
				break;
		}
	}
}