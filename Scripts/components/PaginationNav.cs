using Godot;
using Godot.Sharp.Extras;
using Godot.Collections;

public class PaginationNav : CenterContainer
{
#region Signals
    [Signal]
    public delegate void page_changed(int page);
#endregion

#region Node Paths
    [NodePath("Controls/FirstPage")]
    Button _firstPage = null;

    [NodePath("Controls/PrevPage")]
    Button _prevPage = null;

    [NodePath("Controls/PageCount")]
    HBoxContainer _pageCount = null;

    [NodePath("Controls/NextPage")]
    Button _nextPage = null;

    [NodePath("Controls/LastPage")]
    Button _lastPage = null;
#endregion

#region Private Variables
    private int iTotalPages = 0;
    private int iCurrentPage = 0;
    private Array<Button> abPages = new Array<Button>();
#endregion

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.OnReady();
        _firstPage.Connect("pressed", this, "StepPage", new Array { 0 });
        _prevPage.Connect("pressed", this, "StepPage", new Array { -1 });
        _nextPage.Connect("pressed", this, "StepPage", new Array { 1 });
        _lastPage.Connect("pressed", this, "StepPage", new Array { -2 });
    }

    void ToggleButton(int btnIndex, bool enabled) {
        Button btn = _pageCount.GetChild(btnIndex) as Button;
        btn.Disabled = !enabled;
        btn.MouseDefaultCursorShape = enabled ? CursorShape.PointingHand : CursorShape.Arrow;
    }

    public void UpdateConfig(int totalPages) {
        iTotalPages = totalPages;
        foreach (Button btn in abPages)
            btn.QueueFree();
        abPages.Clear();
        for (int i = 0; i < totalPages; i++) {
            Button btn = new Button();
            btn.Text = $"{i + 1}";
            btn.RectMinSize = new Vector2(25, 0);
            btn.MouseDefaultCursorShape = CursorShape.PointingHand;
            btn.Connect("pressed", this, "PageChanged", new Array { i });
            _pageCount.AddChild(btn);
            abPages.Add(btn);
            if (i == iCurrentPage) {
                btn.Disabled = true;
            }
            if (i > 9) {
                btn.Visible = false;
            }
        }
        CheckPage();
    }

    public void CheckPage() {
        if (iTotalPages > 1) {
            Visible = true;
            if (iCurrentPage == 0) {
                _firstPage.Disabled = true;
                _prevPage.Disabled = true;
            } else {
                _firstPage.Disabled = false;
                _prevPage.Disabled = false;
            }
            if (iCurrentPage + 1 == iTotalPages) {
                _lastPage.Disabled = true;
                _nextPage.Disabled = true;
            } else {
                _lastPage.Disabled = false;
                _nextPage.Disabled = false;
            }
        } else {
            Visible = false;
            _firstPage.Disabled = true;
            _prevPage.Disabled = true;
            _lastPage.Disabled = true;
            _nextPage.Disabled = true;
        }
        _firstPage.MouseDefaultCursorShape = !_firstPage.Disabled ? CursorShape.PointingHand : CursorShape.Arrow;
        _prevPage.MouseDefaultCursorShape = !_prevPage.Disabled ? CursorShape.PointingHand : CursorShape.Arrow;
        _lastPage.MouseDefaultCursorShape = !_lastPage.Disabled ? CursorShape.PointingHand : CursorShape.Arrow;
        _nextPage.MouseDefaultCursorShape = !_nextPage.Disabled ? CursorShape.PointingHand : CursorShape.Arrow;

        int from = Mathf.Max(iCurrentPage - 5, 0);
        int to = Mathf.Min(from + 9, iTotalPages);

        for (int i = 0; i < iTotalPages; i++) {
            if (i >= from && i <= to) {
                abPages[i].Visible = true;
            } else {
                abPages[i].Visible = false;
            }
        }
    }

    public void StepPage(int page) {
        ToggleButton(iCurrentPage, true);
        if (page == 0)
            iCurrentPage = 0;
        else if (page == -2)
            iCurrentPage = iTotalPages - 1;
        else
            iCurrentPage += page;
        ToggleButton(iCurrentPage, false);
        CheckPage();
        EmitSignal("page_changed", iCurrentPage);
    }

    public void SetPage(int page) {
        if (page > iTotalPages)
            return;
        
        if (page < 0)
            return;

        CheckPage();

        if (_pageCount.GetChildCount() > 0 && _pageCount.GetChildCount() > iCurrentPage)
            ToggleButton(iCurrentPage, true);

        iCurrentPage = page;
        if (_pageCount.GetChildCount() > 0 && _pageCount.GetChildCount() > iCurrentPage)
            ToggleButton(iCurrentPage, false);
    }

    public void PageChanged(int page) {
        if (_pageCount.GetChildCount() > 0 && _pageCount.GetChildCount() > iCurrentPage)
            ToggleButton(iCurrentPage, true);
        
        iCurrentPage = page;
        if (_pageCount.GetChildCount() > 0 && _pageCount.GetChildCount() > iCurrentPage)
            ToggleButton(iCurrentPage, false);
        
        CheckPage();
        EmitSignal("page_changed", page);
    }
}
