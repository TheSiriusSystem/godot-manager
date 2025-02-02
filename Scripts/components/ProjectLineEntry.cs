using Godot;
using Godot.Collections;
using Godot.Sharp.Extras;

public class ProjectLineEntry : ColorRect
{
#region Signals
	[Signal]
	public delegate void Clicked(ProjectLineEntry self);
	[Signal]
	public delegate void DoubleClicked(ProjectLineEntry self);
	[Signal]
	public delegate void RightClicked(ProjectLineEntry self);
	[Signal]
	public delegate void FavoriteUpdated(ProjectLineEntry self);
	[Signal]
	public delegate void DragStarted(ProjectLineEntry self);
	[Signal]
	public delegate void DragEnded(ProjectLineEntry self);
#endregion

#region Private Node Variables
	[NodePath("hc/ProjectIcon")]
	private TextureRect _icon = null;
	[NodePath("hc/vc/ProjectName")]
	private Label _name = null;
	[NodePath("hc/vc/ProjectDesc")]
	private Label _desc = null;
	[NodePath("hc/vc/ProjectLocation")]
	private Label _location = null;
	[NodePath("hc/GodotVersion")]
	private Label _version = null;
	[NodePath("hc/HeartIcon")]
	public HeartIcon HeartIcon = null;
#endregion

#region Private Variables
	private string sIcon = MainWindow._plTextures["MissingIcon"].ResourcePath;
	private string sName = "Project Name";
	private string sDesc = "Project Description";
	private string sLocation = "/home/eumario/Projects/Godot/ProjectName";
	private string sGodotVersion = "";
	private ProjectFile pfProjectFile = null;
#endregion

#region Public Accessors
	public bool MissingProject { get; set; } = false;

	public ProjectFile ProjectFile {
		get {
			return pfProjectFile;
		}

		set {
			pfProjectFile = value;
			Name = value.Name + (!MissingProject ? "" : " - Missing Project");
			Description = value.Description;
			Icon = value.Location.GetResourceBase(value.Icon);
			Location = value.Location;
			GodotId = value.GodotId;
			if (HeartIcon != null) {
				HeartIcon.SetCheck(value.Favorite);
			}
		}
	}

	public string Icon {
		get {
			if (_icon != null)
				return _icon.Texture.ResourcePath;
			else
				return sIcon;
		}

		set {
			sIcon = value;
			if (_icon != null) {
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
	}

	new public string Name {
		get {
			if (_name != null)
				return _name.Text;
			else
				return sName;
		}
		set {
			sName = value;
			if (_name != null) {
				_name.Text = value;
				_name.HintTooltip = _name.Text;
			}
		}
	}

	public string Description {
		get {
			return sDesc;
		}
		set {
			sDesc = value;
			if (_desc != null) {
				if (string.IsNullOrEmpty(sDesc))
					_desc.Text = Tr("No Description");
				else
					_desc.Text = value;
				_desc.HintTooltip = _desc.Text;
			}
		}
	}

	public string Location {
		get {
			return sLocation;
		}
		set {
			sLocation = value;
			if (_location != null) {
				_location.Text = value.GetBaseDir().NormalizePath();
				_location.HintTooltip = _location.Text;
			}
		}
	}

	public string GodotId {
		get {
			return sGodotVersion;
		}
		set {
			sGodotVersion = value;
			if (_version != null) {
				GodotVersion gv = CentralStore.Instance.FindVersion(value);
				if (gv != null) {
					_version.Text = gv.GetDisplayName();
					_version.HintTooltip = Tr("Using ") + _version.Text;
					_version.MouseFilter = MouseFilterEnum.Pass;
				} else {
					_version.Text = Tr("Unknown");
					_version.HintTooltip = "";
					_version.MouseFilter = MouseFilterEnum.Ignore;
				}
			}
		}
	}
#endregion

	public override void _Ready()
	{
		this.OnReady();

		Icon = sIcon;
		Name = sName;
		Description = sDesc;
		Location = sLocation;
		GodotId = sGodotVersion;
		HeartIcon.SetCheck(ProjectFile.Favorite);
		Modulate = new Color(Modulate.r, Modulate.g, Modulate.b, !MissingProject ? 1.0f : 0.5f);
	}

	[SignalHandler("clicked", nameof(HeartIcon))]
	void OnHeartClicked() {
		ProjectFile.Favorite = HeartIcon.IsChecked();
		CentralStore.Instance.SaveDatabase();
		EmitSignal("FavoriteUpdated", this);
	}

	[SignalHandler("mouse_entered")]
	void OnMouseEntered() {
		if (!Color.IsEqualApprox(new Color("31ffffff")))
			Color = new Color("2a2e37");
	}

	[SignalHandler("mouse_exited")]
	void OnMouseExited() {
		if (!Color.IsEqualApprox(new Color("31ffffff")))
			Color = new Color("002a2e37");
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
				Color = new Color("31ffffff");
				EmitSignal("Clicked", this);
			}
		} else if (iemb.ButtonIndex == (int)ButtonList.Right) {
			Color = new Color("31ffffff");
			EmitSignal("RightClicked", this);
		}
	}

	// Test Drag and Drop
	public override bool CanDropData(Vector2 position, object data)
	{
		return GetParent().GetParent<CategoryList>().CanDropData(position, data);
	}

	public override void DropData(Vector2 position, object data)
	{
		GetParent().GetParent<CategoryList>().DropData(position, data);
	}

	public override object GetDragData(Vector2 position) {
		if (!(GetParent().GetParent() is CategoryList))
			return null;
		Dictionary data = new Dictionary();
		data["source"] = this;
		data["parent"] = this.GetParent().GetParent();
		var preview = MainWindow._plScenes["ProjectLineEntry"].Instance<ProjectLineEntry>();
		var notifier = new VisibilityNotifier2D();
		preview.AddChild(notifier);
		notifier.Connect("screen_entered", this, "OnDragStart");
		notifier.Connect("screen_exited", this, "OnDragEnded");
		preview.ProjectFile = ProjectFile;
		SetDragPreview(preview);
		data["preview"] = preview;
		return data;
	}

	void OnDragStart() {
		Input.MouseMode = Input.MouseModeEnum.Confined;
		EmitSignal("DragStarted", this);
	}

	void OnDragEnded() {
		Input.MouseMode = Input.MouseModeEnum.Visible;
		EmitSignal("DragEnded", this);
	}
}
