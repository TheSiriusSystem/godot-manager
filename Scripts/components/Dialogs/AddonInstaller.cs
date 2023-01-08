using Godot;
using Godot.Collections;
using Godot.Sharp.Extras;
using SFile = System.IO.File;

public class AddonInstaller : ReferenceRect
{
#region Node Paths
	[NodePath("PC/CC/P/VB/MCContent/VB/DetailLabel")]
	Label _detailLabel = null;

	[NodePath("PC/CC/P/VB/MCContent/VB/SC/VBoxContainer/AddonTree")]
	Tree _addonTree = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/OkButton")]
	Button _okButton = null;
#endregion

#region Private Variables
	private readonly string[] imageExts = new string[] {".bmp",".dds",".exr",".hdr",".jpg",".jpeg",".png",".svg",".tga",".webp"};
	private PluginInstaller _installer;
	private TreeItem _root;
	private bool _updating = false;
	private Dictionary<string, TreeItem> _statusMap;
#endregion

#region Icon Registry
	Dictionary<string, Texture> IconRegistry = null;

	Array<string> IgnoreFiles = null;
#endregion

	void AddRegistry(string[] exts, Texture icon) {
		foreach (string ext in exts)
			IconRegistry.Add(ext, icon);
	}

	void InitRegistry() {
		IconRegistry = new Dictionary<string, Texture>();
		// Image Formats
		AddRegistry(imageExts, MainWindow._plTextures["FT_Image"]);
		// Audio Formats
		AddRegistry(new string[] {".wav",".mp3",".ogg"}, MainWindow._plTextures["FT_Audio"]);
		// Packed Scene Formats
		AddRegistry(new string[] {".xml",".scn",".tscn",".escn",".dae",".gltf",".glb"}, MainWindow._plTextures["FT_PackedScene"]);
		// Shader Formats
		AddRegistry(new string[] {".gdshader",".shader"}, MainWindow._plTextures["FT_Shader"]);
		// Script Formats
		AddRegistry(new string[] {".gd"}, MainWindow._plTextures["FT_GDScript"]);
		AddRegistry(new string[] {".cs"}, MainWindow._plTextures["FT_CSharp"]);
		AddRegistry(new string[] {".vs"}, MainWindow._plTextures["FT_VisualScript"]);
		// Atlas Texture Format
		AddRegistry(new string[] {".atlastex"}, MainWindow._plTextures["FT_AtlasTexture"]);
		// Mesh Texture Format
		AddRegistry(new string[] {".obj"}, MainWindow._plTextures["FT_Mesh"]);
		// Text File Formats
		AddRegistry(new string[] {".txt",".md",".rst",".json",".yml",".yaml",".toml",".cfg",".ini"}, MainWindow._plTextures["FT_Text"]);
		// Font Formats
		AddRegistry(new string[] {".ttf",".otf",".woff",".fnt"}, MainWindow._plTextures["FT_Font"]);
		// No Extension
		AddRegistry(new string[] {"::noext::"}, MainWindow._plTextures["FT_Object"]);
		// Unknown Extension
		AddRegistry(new string[] {"::unknown::"}, MainWindow._plTextures["FT_File"]);
		AddRegistry(new string[] {"::folder::"}, MainWindow._plTextures["FT_Folder"]);
	}

	void InitIgnoreFiles() {
		IgnoreFiles = new Array<string>();
		foreach (string ext in imageExts) {
			IgnoreFiles.Add($"res://icon{ext}");
			IgnoreFiles.Add($"res://icon{ext}.flags");
			IgnoreFiles.Add($"res://icon{ext}.import");
		}
		IgnoreFiles.Add("res://engine.cfg");
		IgnoreFiles.Add("res://project.godot");
		IgnoreFiles.Add("res://default_env.tres");
		IgnoreFiles.Add("res://.fscache");
		IgnoreFiles.Add("res://.gitignore");
		IgnoreFiles.Add("res://.gitattributes");
		foreach (string fileName in new string[] {"README", "READ_ME", "LICENSE", "LICENCE", "CODE_OF_CONDUCT", "CODEOF_CONDUCT", "CODE_OFCONDUCT", "CODEOFCONDUCT", "CONTRIBUTE", "CONTRIBUTED", "CONTRIBUTING", "CONTRIBUTOR", "CONTRIBUTORS", "SECURITY", "CREDITS", "TODO", "TODO_LIST", "CHANGES", "CHANGELOG", "CHANGE_LOG", "UPDATELOG", "UPDATE_LOG"}) {
			IgnoreFiles.Add($"res://{fileName}");
			IgnoreFiles.Add($"res://{fileName}.txt");
			IgnoreFiles.Add($"res://{fileName}.md");
			IgnoreFiles.Add($"res://{fileName}.rst");
		}
	}

	public override void _Ready()
	{
		this.OnReady();
		InitRegistry();
		InitIgnoreFiles();
		_statusMap = new Dictionary<string, TreeItem>();
	}

	public void ShowDialog(AssetPlugin asset) {
		_installer = new PluginInstaller(asset);
		_detailLabel.Text = string.Format(Tr("Contents of asset \"{0}\"\nSelect the files to install during project creation:"), asset.Asset.Title);
		PopulateTree();
		Visible = true;
	}

	void UpdateSubitems(TreeItem item, bool check, bool first = false) {
		// Code "Copied" from Godot editor_asset_installer.cpp
		item.SetChecked(0, check);

		if (item.GetChildren() != null) {
			UpdateSubitems(item.GetChildren(), check);
		}

		if (!first && item.GetNext() != null) {
			UpdateSubitems(item.GetNext(), check);
		}
	}

	void UncheckParent(TreeItem item) {
		// Code "Copied" from Godot editor_asset_installer.cpp
		if (item == null)
			return;
		
		bool any_checked = false;
		TreeItem citem = item.GetChildren();
		while (citem != null) {
			if (citem.IsChecked(0)) {
				any_checked = true;
				break;
			}
			citem = citem.GetNext();
		}

		if (!any_checked) {
			item.SetChecked(0,false);
			UncheckParent(item.GetParent());
		}
	}

	[SignalHandler("pressed", nameof(_okButton))]
	void OnPressed_OkButton() {
		Array<string> installFiles = new Array<string>();

		foreach (string key in _statusMap.Keys) {
			if (_statusMap[key] != null && _statusMap[key].IsChecked(0)) {
				installFiles.Add(key);
			}
		}

		int selectedFiles = 0;
		foreach (TreeItem value in _statusMap.Values) {
			if (value != null && value.IsChecked(0)) {
				selectedFiles++;
			}
		}
		if (selectedFiles <= 0) {
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"), Tr("You need to select an addon file."));
			return;
		}

		_installer.AssetPlugin.InstallFiles = installFiles;
		CentralStore.Instance.SaveDatabase();
		Visible = false;
	}

	[SignalHandler("item_edited", nameof(_addonTree))]
	void OnItemEdited() {
		// Code "Copied" from Godot editor_asset_installer.cpp
		if (_updating)
			return;
		
		TreeItem item = _addonTree.GetEdited();
		if (item == null)
			return;

		_updating = true;

		string path = item.GetMetadata(0) as string;

		if (path == "" || item == _root) {
			UpdateSubitems(item, item.IsChecked(0), true);
		}

		if (item.IsChecked(0)) {
			while (item != null) {
				item.SetChecked(0, true);
				item = item.GetParent();
			}
		} else {
			UncheckParent(item.GetParent());
		}
		_updating = false;
	}


	void PopulateTree() {
		if (!SFile.Exists(_installer.AssetPlugin.Location)) {
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"), Tr("The addon archive doesn't exist."));
			return;
		}

		// Original code inspired by editor_asset_installer.cpp
		_updating = true;
		_addonTree.Clear();
		_root = _addonTree.CreateItem(null, -1);
		_root.SetCellMode(0,TreeItem.TreeCellMode.Check);
		_root.SetChecked(0,true);
		_root.SetText(0,"res://");
		_root.SetIcon(0,IconRegistry["::folder::"]);
		_root.SetEditable(0,true);
		Dictionary<string, TreeItem> folders = new Dictionary<string, TreeItem>();

		int indx = -1;
		Array<string> _zipContents = _installer.GetZipContents();
		foreach (string entry in _installer.GetFileList()) {
			string path = entry;
			bool isdir = false;
			indx++;

			if (path.IndexOf("/") > 0)
				path = path.Substr(path.IndexOf("/")+1,path.Length);

			if (path.EndsWith("/")) {
				path = path.Substr(0,path.Length-1);
				isdir = true;
			}

			if (string.IsNullOrEmpty(path))
				continue;

			int pp = path.FindLast("/");

			TreeItem parent;
			if (pp == -1) {
				parent = _root;
			} else {
				string ppath = path.Substr(0,pp);
				if (folders.ContainsKey(ppath))
					parent = folders[ppath];
				else
					parent = _root;
			}

			TreeItem ti = _addonTree.CreateItem(parent);
			ti.SetCellMode(0, TreeItem.TreeCellMode.Check);
			ti.SetChecked(0,true);
			ti.SetEditable(0,true);
			if (isdir) {
				folders[path] = ti;
				ti.SetText(0, path.GetFile() + "/");
				ti.SetIcon(0, IconRegistry["::folder::"]);
				ti.SetMetadata(0,"");
			} else {
				string file = path.GetFile();
				string ext = file.GetExtension().ToLower();
				if (IconRegistry.ContainsKey(ext)) {
					ti.SetIcon(0, IconRegistry[ext]);
				} else if (ext == "") {
					ti.SetIcon(0, IconRegistry["::noext::"]);
				} else {
					ti.SetIcon(0, IconRegistry["::unknown::"]);
				}
				ti.SetText(0,file);

				ti.SetMetadata(0,"res://".Join(path));
				
				if (IgnoreFiles.Contains("res://".Join(path))) {
					ti.SetChecked(0,false);
					ti.SetCustomColor(0,new Color(1,0,0));
				}
			}

			_statusMap[_zipContents[indx]] = ti;
		}
		_updating = false;
	}
}
