using Godot;
using Godot.Sharp.Extras;
using Godot.Collections;
using File = System.IO.File;
using Directory = System.IO.Directory;

public class MainWindow : Control
{
	[NodePath("bg/Shell/Sidebar")] ColorRect _sidebar = null;
	Array<PageButton> _buttons;
	[NodePath("bg/Shell/VC/TabContainer")] TabContainer _notebook = null;
	[NodePath("bg/Shell/VC/TabContainer/Projects")] ProjectsPanel _projectsPanel = null;

#region Public Variables
	// CustomGodot Dialog Filter
	public static string[] _customGDExtensions = new string[] {".exe", ".x86", ".x86_64", ".32", ".64", ".app"};

	// Preload Resources
	public static readonly Dictionary<string, PackedScene> _plScenes = new Dictionary<string, PackedScene>()
	{
		{"AddonLineEntry", GD.Load<PackedScene>("res://components/AddonLineEntry.tscn")},
		{"AssetLibEntry", GD.Load<PackedScene>("res://components/AssetLibEntry.tscn")},
		{"CategoryList", GD.Load<PackedScene>("res://components/CategoryList.tscn")},
		{"EnginePopup", GD.Load<PackedScene>("res://components/EnginePopup.tscn")},
		{"GodotLineEntry", GD.Load<PackedScene>("res://components/GodotLineEntry.tscn")},
		{"ProjectIconEntry", GD.Load<PackedScene>("res://components/ProjectIconEntry.tscn")},
		{"ProjectLineEntry", GD.Load<PackedScene>("res://components/ProjectLineEntry.tscn")},
		{"ProjectPopup", GD.Load<PackedScene>("res://components/ProjectPopup.tscn")},
		{"NewsItem", GD.Load<PackedScene>("res://components/NewsItem.tscn")}
	};

	public static readonly Dictionary<string, Texture> _plTextures = new Dictionary<string, Texture>()
	{
		{"AddIcon", GD.Load<Texture>("res://Assets/Icons/icon_add.svg")},
		{"DefaultIconV1", GD.Load<Texture>("res://Assets/Icons/default_project_icon_v1.png")},
		{"DefaultIconV3", GD.Load<Texture>("res://Assets/Icons/default_project_icon_v3.png")},
		{"DefaultIconV4", GD.Load<Texture>("res://Assets/Icons/default_project_icon_v4.png")},
		{"DownloadIcon", GD.Load<Texture>("res://Assets/Icons/download.svg")},
		{"DropDown", GD.Load<Texture>("res://Assets/Icons/drop_down1.svg")},
		{"EditorIconV1", GD.Load<Texture>("res://Assets/Icons/editor_icon_v1.svg")},
		{"FT_Image", GD.Load<Texture>("res://Assets/Icons/icon_ft_image.svg")},
		{"FT_Audio", GD.Load<Texture>("res://Assets/Icons/icon_ft_audio.svg")},
		{"FT_PackedScene", GD.Load<Texture>("res://Assets/Icons/icon_ft_packed_scene.svg")},
		{"FT_Shader", GD.Load<Texture>("res://Assets/Icons/icon_ft_shader.svg")},
		{"FT_GDScript", GD.Load<Texture>("res://Assets/Icons/icon_ft_gdscript.svg")},
		{"FT_CSharp", GD.Load<Texture>("res://Assets/Icons/icon_ft_csharp.svg")},
		{"FT_VisualScript", GD.Load<Texture>("res://Assets/Icons/icon_ft_visualscript.svg")},
		{"FT_Resource", GD.Load<Texture>("res://Assets/Icons/icon_ft_resource.svg")},
		{"FT_AtlasTexture", GD.Load<Texture>("res://Assets/Icons/icon_ft_atlas_texture.svg")},
		{"FT_Mesh", GD.Load<Texture>("res://Assets/Icons/icon_ft_mesh.svg")},
		{"FT_Text", GD.Load<Texture>("res://Assets/Icons/icon_ft_text.svg")},
		{"FT_Font", GD.Load<Texture>("res://Assets/Icons/icon_ft_font.svg")},
		{"FT_Object", GD.Load<Texture>("res://Assets/Icons/icon_ft_object.svg")},
		{"FT_File", GD.Load<Texture>("res://Assets/Icons/icon_ft_file.svg")},
		{"FT_Folder", GD.Load<Texture>("res://Assets/Icons/icon_ft_folder.svg")},
		{"Minus", GD.Load<Texture>("res://Assets/Icons/minus.svg")},
		{"MissingIcon", GD.Load<Texture>("res://Assets/Icons/missing_icon.svg")},	
		{"StatusError", GD.Load<Texture>("res://Assets/Icons/icon_status_error.svg")},
		{"StatusSuccess", GD.Load<Texture>("res://Assets/Icons/icon_status_success.svg")},
		{"StatusWarning", GD.Load<Texture>("res://Assets/Icons/icon_status_warning.svg")},
		{"UninstallIcon", GD.Load<Texture>("res://Assets/Icons/uninstall.svg")},
		{"WaitThumbnail", GD.Load<Texture>("res://Assets/Icons/icon_thumbnail_wait.svg")},
		{"X", GD.Load<Texture>("res://Assets/Icons/x.svg")}
	};

	public static readonly Dictionary<string, Font> _plFonts = new Dictionary<string, Font>()
	{
		{"DroidSans14", GD.Load<Font>("res://Resources/Fonts/droid-regular-14.tres")},
		{"DroidSansBold16", GD.Load<Font>("res://Resources/Fonts/droid-bold-16.tres")}
	};
#endregion

	public MainWindow() {
		if (!CentralStore.Instance.LoadDatabase()) {
			CentralStore.Settings.SetupDefaultValues();
			CentralStore.Instance.SaveDatabase();
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.OnReady();

		EnsureDirStructure();

		_buttons = new Array<PageButton>();
		foreach (var pb in GetTree().GetNodesInGroup("page_buttons")) {
			if (pb is PageButton) {
				_buttons.Add(pb as PageButton);
			}
		}
		foreach (var pb in _buttons) {
			int i = _buttons.IndexOf(pb);
			if (i == _notebook.CurrentTab)
				pb.Activate();
			else
				pb.Deactivate();
			pb.Connect("Clicked", this, "OnPageButton_Clicked");
		}
		AppDialogs dlgs = AppDialogs.Instance;
		dlgs.SetAnchorsAndMarginsPreset(LayoutPreset.Wide);
		dlgs.Name = "AppDialogs";
		AddChild(dlgs);

#if GODOT_WINDOWS || GODOT_UWP
        _customGDExtensions = new string[] {".exe"};
#elif GODOT_LINUXBSD || GODOT_X11
		_customGDExtensions = new string[] {".x86", ".x86_64", ".32", ".64"};
#elif GODOT_MACOS || GODOT_OSX
		_customGDExtensions = new string[] {".app"};
#endif

		var res = CleanupCarriageReturns();

		OS.WindowBorderless = !CentralStore.Settings.UseSystemTitlebar;
		GetTree().Root.GetNode<Titlebar>("MainWindow/bg/Shell/VC/TitleBar").Visible = !CentralStore.Settings.UseSystemTitlebar;
		GetTree().Root.GetNode<Control>("MainWindow/bg/Shell/VC/VisibleSpacer").Visible = CentralStore.Settings.UseSystemTitlebar;

		if (CentralStore.Settings.FirstTimeRun)
		{
			AppDialogs.FirstRunWizard.ShowDialog();
		}

		if (CentralStore.Settings.EnableAutoScan)
		{
			_projectsPanel.ScanForProjects(true);
		}

		RemoveMissingAssets();
	}

	private Array<int> CleanupCarriageReturns(Node node = null)
	{
		Array<int> count = new Array<int>() { 0, 0 };
		if (node == null) {
			node = GetTree().Root;
		}

		foreach (Node cnode in node.GetChildren()) {
			if (cnode.GetChildCount() > 0) {
				var res = CleanupCarriageReturns(cnode);
				count[0] += res[0];
				count[1] += res[1];
			}
			if (cnode is Label || cnode is TextEdit || cnode is LineEdit) {
				var data = (string)cnode.Get("text");
				if (data.Contains("\r")) {
					cnode.Set("text", data.Replace("\r",""));
					count[0] += 1;
				}
			}
			if (cnode is RichTextLabel) {
				var data = (string)cnode.Get("bbcode_text");
				if (data.Contains("\r")) {
					cnode.Set("bbcode_text", data.Replace("\r", ""));
					count[0] += 1;
				}
			}
			count[1] = count[1] + 1;
		}
		return count;
	}

	public void EnsureDirStructure() {
		string[] paths = new string[] {
			CentralStore.Settings.CachePath.GetOSDir(),
			CentralStore.Settings.CachePath.Join("downloads").GetOSDir(),
			CentralStore.Settings.CachePath.Join("images").GetOSDir(),
			CentralStore.Settings.EnginePath.GetOSDir()
		};

		foreach (string path in paths) {
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
		}
	}

	public void RemoveMissingAssets() {
		int missingAssets = 0;
        for (int i = 0; i < CentralStore.Plugins.Count; i++) {
            if (CentralStore.Plugins[i] == null || !File.Exists(CentralStore.Plugins[i].Location.NormalizePath())) {
                CentralStore.Plugins.Remove(CentralStore.Plugins[i]);
                missingAssets++;
            }
        }
        for (int i = 0; i < CentralStore.Templates.Count; i++) {
            if (CentralStore.Templates[i] == null || !File.Exists(CentralStore.Templates[i].Location.NormalizePath())) {
                CentralStore.Templates.Remove(CentralStore.Templates[i]);
                missingAssets++;
            }
        }
        if (missingAssets > 0)
            CentralStore.Instance.SaveDatabase();
	}

	public void UpdateWindow() {
		OS.WindowBorderless = !CentralStore.Settings.UseSystemTitlebar;
		GetNode<Titlebar>("bg/Shell/VC/TitleBar").Visible = !CentralStore.Settings.UseSystemTitlebar;
		GetNode<Control>("bg/Shell/VC/VisibleSpacer").Visible = CentralStore.Settings.UseSystemTitlebar;
	}

	void OnPageButton_Clicked(PageButton pb) {
		_notebook.CurrentTab = _buttons.IndexOf(pb);
	}

	[SignalHandler("tree_exiting")]
	void OnExitingTree() {
		CentralStore.Instance.SaveDatabase();
	}
}
