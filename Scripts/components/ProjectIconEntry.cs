using Godot;
using Godot.Sharp.Extras;

public class ProjectIconEntry : ColorRect
{
#region Signals
    [Signal]
    public delegate void Clicked(ProjectLineEntry self);
    [Signal]
    public delegate void DoubleClicked(ProjectLineEntry self);
    [Signal]
    public delegate void RightClicked(ProjectLineEntry self);
    [Signal]
    public delegate void RightDoubleClicked(ProjectLineEntry self);
#endregion

#region Private Node Variables
    [NodePath("cc/vc/ProjectIcon")]
    private TextureRect _icon = null;
    [NodePath("cc/vc/ProjectName")]
    private Label _projectName = null;
    [NodePath("cc/vc/ProjectLocation")]
    private Label _projectLocation = null;
    [NodePath("cc/vc/GodotVersion")]
    private Label _godotVersion = null;
#endregion

#region Private Variables
    private string sIcon;
    private string sProjectName;
    private string sProjectLocation;
    private string sGodotVersion;
    //private int iGodotVersion;
    private ProjectFile pfProjectFile;
#endregion

#region Public Accessors
    public bool MissingProject { get; set; } = false;

    public string Icon {
        get {
            if (_icon != null)
                return _icon.Texture.ResourcePath;
            else
                return sIcon;
        }

        set {
            sIcon = value;
            if (_icon != null)
                if (MissingProject)
                    _icon.Texture = MainWindow._plTextures["MissingIcon"];
                else {
                    if (System.IO.File.Exists(value)) {
                        var texture = Util.LoadImage(value);
                        if (texture == null)
                            _icon.Texture = MainWindow._plTextures["MissingIcon"];
                        else
                            _icon.Texture = texture;
                    } else {
                        _icon.Texture = MainWindow._plTextures["MissingIcon"];
                    }
                }
        }
    }

    public string ProjectName {
        get {
            if (_projectName != null)
                return _projectName.Text;
            else
                return sProjectName;
        }

        set {
            sProjectName = value;
            if (_projectName != null) {
                Vector2 size = MainWindow._plFonts["DroidSansBold16"].GetStringSize(value);
                if (size.x > 280) {
                    _projectName.Align = Label.AlignEnum.Left;
                } else {
                    _projectName.Align = Label.AlignEnum.Center;
                }
                _projectName.Text = value;
            }
        }
    }

    public string Location {
        get {
            if (_projectLocation != null)
                return _projectLocation.Text;
            else
                return sProjectLocation;
        }

        set {
            sProjectLocation = value;
            if (_projectLocation != null)
                _projectLocation.Text = value.GetBaseDir().NormalizePath();
        }
    }

    public ProjectFile ProjectFile {
        get {
            return pfProjectFile;
        }

        set {
            pfProjectFile = value;
            ProjectName = value.Name + (!MissingProject ? "" : " - Missing Project");
            Icon = value.Location.GetResourceBase(value.Icon);
            Location = value.Location;
            GodotId = value.GodotId;
        }
    }

    public string GodotId {
        get {
            return sGodotVersion;
        }

        set {
            sGodotVersion = value;
            GodotVersion gv = CentralStore.Instance.FindVersion(value);
            if (_godotVersion != null) {
                if (gv != null) {
                    _godotVersion.Text = gv.GetDisplayName();
                    Vector2 size = MainWindow._plFonts["DroidSans14"].GetStringSize(gv.GetDisplayName());
                    if (size.x > 280) {
                        _godotVersion.Align = Label.AlignEnum.Left;
                    } else {
                        _godotVersion.Align = Label.AlignEnum.Center;
                    }
                } else {
                    _godotVersion.Text = Tr("Unknown");
                    _godotVersion.Align = Label.AlignEnum.Center;
                }
            }
        }
    }
#endregion

    public override void _Ready() {
        this.OnReady();

        Icon = sIcon;
        ProjectName = sProjectName;
        Location = sProjectLocation;
        GodotId = sGodotVersion;
        Modulate = new Color(Modulate.r, Modulate.g, Modulate.b, !MissingProject ? 1.0f : 0.5f);
    }

    [SignalHandler("gui_input")]
    void OnGuiInput(InputEvent inputEvent) {
        if (!(inputEvent is InputEventMouseButton))
            return;
        var iemb = inputEvent as InputEventMouseButton;
        if (!iemb.Pressed)
            return;

        if (iemb.ButtonIndex == (int)ButtonList.Left) {
            if (iemb.Doubleclick)
                EmitSignal("DoubleClicked", this);
            else {
                SelfModulate = new Color("ffffffff");
                EmitSignal("Clicked", this);
            }
        } else if (iemb.ButtonIndex == (int)ButtonList.Right) {
            if (iemb.Doubleclick)
                EmitSignal("RightDoubleClicked", this);
            else {
                SelfModulate = new Color("ffffffff");
                EmitSignal("RightClicked", this);
            }
        }
    }
}
