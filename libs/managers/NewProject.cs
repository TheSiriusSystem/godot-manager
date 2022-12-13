using Godot;
using Godot.Collections;
using System.IO.Compression;
using Directory = System.IO.Directory;
using StreamWriter = System.IO.StreamWriter;

public class NewProject : Object {
	public string ProjectName;
	public string ProjectLocation;
	public AssetProject Template;
	public string GodotId;
	public int GodotVersion;
	public Array<AssetPlugin> Plugins;
	public bool UseAdvRenderer;

	public bool CreateProject() {
		string cfgName = GodotVersion <= 2 ? "engine.cfg" : "project.godot";

		if (Template == null)
		{
			// Need to create the Project File ourselves.
			CreateProjectFile();
			if (!Directory.Exists(ProjectLocation.PlusFile(".builds").NormalizePath()))
				Directory.CreateDirectory(ProjectLocation.PlusFile(".builds"));
			CreateDefaultEnvironment();
			CopyIcon();
			ExtractPlugins();
		} else {
			// Project file should be provided in the Template.
			ExtractTemplate();
			ProjectConfig pf = new ProjectConfig();
			pf.Load(ProjectLocation.PlusFile(cfgName).NormalizePath());
			pf.SetValue("application", GodotVersion <= 2 ? "name" : "config/name", $"\"{ProjectName}\"");

			// Need way to compile Assets before Enabling Plugins
			// if (Plugins.Count > 0)
			// 	SetupPlugins(pf);

			pf.Save(ProjectLocation.PlusFile(cfgName));
			ExtractPlugins();
		}
		
		return true;
	}

	private void ExtractTemplate()
	{
		using (ZipArchive za = ZipFile.OpenRead(ProjectSettings.GlobalizePath(Template.Location))) {
			foreach(ZipArchiveEntry zae in za.Entries) {
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
		string gdVersTag = CentralStore.Instance.GetVersion(GodotId).Tag.ToLower();
		if ((GodotVersion == 2 && !gdVersTag.StartsWith("2.0") && !gdVersTag.StartsWith("v2.0")) || GodotVersion >= 3) {
			if (!Directory.Exists(ProjectLocation.PlusFile("addons").NormalizePath()))
				Directory.CreateDirectory(ProjectLocation.PlusFile("addons"));

			foreach(AssetPlugin plgn in Plugins) {
				PluginInstaller installer = new PluginInstaller(plgn);
				installer.Install(ProjectLocation);
			}
		}
	}

	private void CopyIcon()
	{
		Texture image = null;
		if (GodotVersion <= 2) {
			image = GD.Load<Texture>("res://Assets/Icons/default_project_icon_v1.png");
		} else if (GodotVersion == 3) {
			image = GD.Load<Texture>("res://Assets/Icons/default_project_icon_v3.png");
		} else if (GodotVersion >= 4) {
			image = GD.Load<Texture>("res://Assets/Icons/default_project_icon_v4.png");
		}
		image.GetData().SavePng(ProjectLocation.PlusFile("icon.png").NormalizePath());
	}

	private void CreateDefaultEnvironment()
	{
		if (GodotVersion >= 3) {
			using (StreamWriter writer = new StreamWriter(ProjectLocation.PlusFile("default_env.tres").NormalizePath())) {
				writer.WriteLine("[gd_resource type=\"Environment\" load_steps=2 format=2]");
				writer.WriteLine("");
				writer.WriteLine("[sub_resource type=\"ProceduralSky\" id=1]");
				writer.WriteLine("");
				writer.WriteLine("[resource]");
				writer.WriteLine("background_mode = 2");
				writer.WriteLine("background_sky = SubResource(1)");
			}
		}
	}

	private void CreateProjectFile()
	{
		ProjectConfig pf = new ProjectConfig();
		if (GodotVersion <= 2) {
			pf.SetValue("application", "name", $"\"{ProjectName}\"");
			pf.SetValue("application", "icon", "\"res://icon.png\"");
		} else {
			int cfgVer = 3;
			string gdVersTag = CentralStore.Instance.GetVersion(GodotId).Tag.ToLower();
			if (!gdVersTag.StartsWith("3.0") && !gdVersTag.StartsWith("v3.0")) {
				cfgVer = 4;
			} else if (GodotVersion >= 4)
			{
				cfgVer = 5;
			}

			pf.SetValue("header", "config_version", cfgVer.ToString());
			pf.SetValue("application", "config/name", $"\"{ProjectName}\"");
			pf.SetValue("application", "config/icon", "\"res://icon.png\"");
			if (cfgVer == 4) {
				if (!UseAdvRenderer)
				{
					pf.SetValue("rendering", "quality/driver/driver_name", "\"GLES2\"");
				}
				if (CentralStore.Instance.FindVersion(GodotId).IsMono)
				{
					pf.SetValue("mono", "debugger_agent/wait_timeout", "7000");
				}
			} else if (cfgVer >= 5) {
				pf.SetValue("application", "config/features", "PackedStringArray(\"4.0\", \"Vulkan Clustered\")");
				if (!UseAdvRenderer)
				{
					pf.SetValue("rendering", "renderer/rendering_method", "\"mobile\"");
				}
			}
			pf.SetValue("rendering", "environment/default_environment", "\"res://default_env.tres\"");
		}
		pf.Save(ProjectLocation.PlusFile(GodotVersion <= 2 ? "engine.cfg" : "project.godot").NormalizePath());
	}
}