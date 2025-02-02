using Godot;
using Godot.Collections;

public class AppDialogs : Control
{
#region Node Paths
    public FirstRunWizard FirstRunWizard_ = null;
    public AddCustomGodot AddCustomGodot_ = null;
    public EditCustomGodot EditCustomGodot_ = null;
    public BusyDialog BusyDialog_ = null;
    public YesNoDialog YesNoDialog_ = null;
    public YesNoCancelDialog YesNoCancelDialog_ = null;
    public ImportProject ImportProject_ = null;
    public MessageDialog MessageDialog_ = null;
    public FileDialog ImageFileDialog_ = null;
    public FileDialog ImportFileDialog_ = null;
    public FileDialog BrowseFolderDialog_ = null;
    public FileDialog BrowseGodotDialog_ = null;
    public CreateProject CreateProject_ = null;
    public EditProject EditProject_ = null;
    public CreateCategory CreateCategory_ = null;
    public RemoveCategory RemoveCategory_ = null;
    public AssetLibPreview AssetLibPreview_ = null;
    public DownloadAddon DownloadAddon_ = null;
    public AddonInstaller AddonInstaller_ = null;
    public FileConflictDialog FileConflictDialog_ = null;
    public ListSelectDialog ListSelectDialog_ = null;
#endregion

#region Singleton Variables to access in program
    public static FirstRunWizard FirstRunWizard => Instance.FirstRunWizard_;
    public static AddCustomGodot AddCustomGodot => Instance.AddCustomGodot_;
    public static EditCustomGodot EditCustomGodot => Instance.EditCustomGodot_;
    public static BusyDialog BusyDialog => Instance.BusyDialog_;
    public static YesNoDialog YesNoDialog => Instance.YesNoDialog_;
    public static YesNoCancelDialog YesNoCancelDialog => Instance.YesNoCancelDialog_;
    public static ImportProject ImportProject => Instance.ImportProject_;
    public static MessageDialog MessageDialog => Instance.MessageDialog_;
    public static FileDialog ImageFileDialog => Instance.ImageFileDialog_;
    public static FileDialog ImportFileDialog => Instance.ImportFileDialog_;
    public static FileDialog BrowseFolderDialog => Instance.BrowseFolderDialog_;
    public static FileDialog BrowseGodotDialog => Instance.BrowseGodotDialog_;
    public static CreateProject CreateProject => Instance.CreateProject_;
    public static EditProject EditProject => Instance.EditProject_;
    public static CreateCategory CreateCategory => Instance.CreateCategory_;
    public static RemoveCategory RemoveCategory => Instance.RemoveCategory_;
    public static AssetLibPreview AssetLibPreview => Instance.AssetLibPreview_;
    public static DownloadAddon DownloadAddon => Instance.DownloadAddon_;
    public static AddonInstaller AddonInstaller => Instance.AddonInstaller_;
    public static FileConflictDialog FileConflictDialog => Instance.FileConflictDialog_;
    public static ListSelectDialog ListSelectDialog => Instance.ListSelectDialog_;

    #endregion

    private static AppDialogs _instance = null;

    public static AppDialogs Instance {
        get {
            if (_instance == null)
                _instance = new AppDialogs();
            
            return _instance;
        }
    }

    private Array<ReferenceRect> dialogs;

    protected AppDialogs() {

        // Initialize Dialogs
        FirstRunWizard_ = GD.Load<PackedScene>("res://components/Dialogs/FirstRunWizard.tscn").Instance<FirstRunWizard>();
        AddCustomGodot_ = GD.Load<PackedScene>("res://components/Dialogs/AddCustomGodot.tscn").Instance<AddCustomGodot>();
        EditCustomGodot_ = GD.Load<PackedScene>("res://components/Dialogs/EditCustomGodot.tscn").Instance<EditCustomGodot>();
        BusyDialog_ = GD.Load<PackedScene>("res://components/Dialogs/BusyDialog.tscn").Instance<BusyDialog>();
        YesNoDialog_ = GD.Load<PackedScene>("res://components/Dialogs/YesNoDialog.tscn").Instance<YesNoDialog>();
        YesNoCancelDialog_ = GD.Load<PackedScene>("res://components/Dialogs/YesNoCancelDialog.tscn").Instance<YesNoCancelDialog>();
        ImportProject_ = GD.Load<PackedScene>("res://components/Dialogs/ImportProject.tscn").Instance<ImportProject>();
        MessageDialog_ = GD.Load<PackedScene>("res://components/Dialogs/MessageDialog.tscn").Instance<MessageDialog>();
        CreateProject_ = GD.Load<PackedScene>("res://components/Dialogs/CreateProject.tscn").Instance<CreateProject>();
        EditProject_ = GD.Load<PackedScene>("res://components/Dialogs/EditProject.tscn").Instance<EditProject>();
        CreateCategory_ = GD.Load<PackedScene>("res://components/Dialogs/CreateCategory.tscn").Instance<CreateCategory>();
        RemoveCategory_ = GD.Load<PackedScene>("res://components/Dialogs/RemoveCategory.tscn").Instance<RemoveCategory>();
        AssetLibPreview_ = GD.Load<PackedScene>("res://components/Dialogs/AssetLibPreview.tscn").Instance<AssetLibPreview>();
        DownloadAddon_ = GD.Load<PackedScene>("res://components/Dialogs/DownloadAddon.tscn").Instance<DownloadAddon>();
        AddonInstaller_ = GD.Load<PackedScene>("res://components/Dialogs/AddonInstaller.tscn").Instance<AddonInstaller>();
        FileConflictDialog_ = GD.Load<PackedScene>("res://components/Dialogs/FileConflictDialog.tscn").Instance<FileConflictDialog>();
        ListSelectDialog_ = GD.Load<PackedScene>("res://components/Dialogs/ListSelectDialog.tscn").Instance<ListSelectDialog>();

        ImageFileDialog_ = new FileDialog();
        ImageFileDialog_.Name = "ImageFileDialog";
        ImageFileDialog_.Mode = FileDialog.ModeEnum.OpenFile;
        ImageFileDialog_.Access = FileDialog.AccessEnum.Filesystem;
        ImageFileDialog_.WindowTitle = Tr("Open a File");
        ImageFileDialog_.Filters = new string[] {"*.png", "*.webp", "*.svg"};
        ImageFileDialog_.RectMinSize = new Vector2(510, 390);
        ImageFileDialog_.Theme = GD.Load<Theme>("res://Resources/DefaultTheme.tres");

        // Internal File Dialog
        ImportFileDialog_ = new FileDialog();
        ImportFileDialog_.Name = "ImportFileDialog";
        ImportFileDialog_.Mode = FileDialog.ModeEnum.OpenFile;
        ImportFileDialog_.Access = FileDialog.AccessEnum.Filesystem;
        ImportFileDialog_.WindowTitle = Tr("Open a File");
        ImportFileDialog_.Filters = new string[] {"engine.cfg", "project.godot"};
        ImportFileDialog_.RectMinSize = new Vector2(510, 390);
        ImportFileDialog_.Theme = GD.Load<Theme>("res://Resources/DefaultTheme.tres");

        // Internal Browse Folder Dialog
        BrowseFolderDialog_ = new FileDialog();
        BrowseFolderDialog_.Name = "BrowseFolderDialog";
        BrowseFolderDialog_.Mode = FileDialog.ModeEnum.OpenDir;
        BrowseFolderDialog_.Access = FileDialog.AccessEnum.Filesystem;
        BrowseFolderDialog_.WindowTitle = Tr("Open a Folder");
        BrowseFolderDialog_.RectMinSize = new Vector2(510, 390);
        BrowseFolderDialog_.Theme = GD.Load<Theme>("res://Resources/DefaultTheme.tres");

        // Internal Browse Godot Dialog
        BrowseGodotDialog_ = new FileDialog();
        BrowseGodotDialog_.Name = "BrowseGodotDialog";
        BrowseGodotDialog_.Mode = FileDialog.ModeEnum.OpenFile;
        BrowseGodotDialog_.Access = FileDialog.AccessEnum.Filesystem;
        BrowseGodotDialog_.WindowTitle = Tr("Open a File");
#if GODOT_WINDOWS || GODOT_UWP
        BrowseGodotDialog_.Filters = new string[] {"*.exe"};
#elif GODOT_LINUXBSD || GODOT_X11
        BrowseGodotDialog_.Filters = new string[] {"*.x86", "*.x86_64", "*.32", "*.64"};
#elif GODOT_MACOS || GODOT_OSX
        BrowseGodotDialog_.Filters = new string[] {"*.app"};
#else
        BrowseGodotDialog_.Filters = new string[] {"*.exe", "*.x86", "*.x86_64", "*.32", "*.64", "*.app"};
#endif
        BrowseGodotDialog_.RectMinSize = new Vector2(510, 390);
        BrowseGodotDialog_.Theme = GD.Load<Theme>("res://Resources/DefaultTheme.tres");

        dialogs = new Array<ReferenceRect> {    // Hierarchy of Dialogs in window, for proper displaying
            FirstRunWizard_,                    // First Run Wizard Helper
            AddCustomGodot_,                    // Add Custom Godot
            EditCustomGodot_,                   // Edit Custom Godot
            CreateProject_, ImportProject_,     // Create Project / Import Project
            EditProject_,                       // Edit Project
            AssetLibPreview_, DownloadAddon_,   // Asset Library Preview / Download Addon/Project
            CreateCategory_,                    // Create a Category
            RemoveCategory_,                    // Remove a Category
            AddonInstaller_,                    // Installer Dialog for Addon/Plugins
            FileConflictDialog_,                // File Conflict Dialog
            YesNoDialog_,                       // Yes No Prompt
            YesNoCancelDialog_,                 // Yes, No, Cancel Prompt
            BusyDialog_,                        // Busy Dialog
            MessageDialog_,                     // Message Dialog
            ListSelectDialog_,                  // Dialog for Selecting a Specific Option from a List.
        };

        MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    public override void _EnterTree() {
        // Setup Full Rect for dialogs:
        foreach (ReferenceRect dlg in dialogs) {
            dlg.SetAnchorsAndMarginsPreset(LayoutPreset.Wide);
            dlg.Visible = false;
            AddChild(dlg);
        }
        AddChild(ImageFileDialog_);
        AddChild(ImportFileDialog_);
        AddChild(BrowseFolderDialog_);
        AddChild(BrowseGodotDialog_);
    }
}
