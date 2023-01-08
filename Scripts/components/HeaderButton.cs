using Godot;
using Godot.Sharp.Extras;

[Tool]
public class HeaderButton : PanelContainer
{
    public enum SortDirection {
        Indeterminate,
        Up,
        Down
    }

    [Signal]
    public delegate void direction_changed(SortDirection button);

    [Export]
    public string Title {
        get {
            if (_headerTitle != null)
                return _headerTitle.Text;
            else
                return _title;
        }

        set {
            _title = value;
            if (_headerTitle != null)
                _headerTitle.Text = value;
        }
    }

    [Export(PropertyHint.Enum,"Direction to Sort")]
    public SortDirection Direction {
        get {
            if (_dirIcon != null) {
                if (_dirIcon.Texture == MainWindow._plTextures["DropDown"])
                    return (_dirIcon.FlipV ? SortDirection.Up : SortDirection.Down);
                else
                    return SortDirection.Indeterminate;
            } else {
                return _direction;
            }
        }

        set {
            _direction = value;
            if (_dirIcon != null) {
                if (value == SortDirection.Indeterminate)
                    _dirIcon.Texture = MainWindow._plTextures["Minus"];
                else {
                    _dirIcon.Texture = MainWindow._plTextures["DropDown"];
                    _dirIcon.FlipV = (value == SortDirection.Up);
                }
            }
        }
    }

    [NodePath("HC/Label")]
    Label _headerTitle = null;

    [NodePath("HC/DirIcon")]
    TextureRect _dirIcon = null;

    private string _title;
    private SortDirection _direction;

    public override void _Ready()
    {
        this.OnReady();
        Title = _title;
        Direction = _direction;
    }

    public void Indeterminate() {
        Direction = SortDirection.Indeterminate;
    }

    [SignalHandler("gui_input")]
    void OnGuiInput_Header(InputEvent @event) {
        if (@event is InputEventMouseButton @iemb && @iemb.ButtonIndex == (int)ButtonList.Left) {
            if (@iemb.Doubleclick) {
                Direction = SortDirection.Indeterminate;
                EmitSignal("direction_changed", Direction);
            } else if (@iemb.Pressed) {
                Direction = (Direction == SortDirection.Down) ? SortDirection.Up : SortDirection.Down;
                EmitSignal("direction_changed", Direction);
            }
        }
    }
}
