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

    public void UpdateConfig(int totalPages) {
        iTotalPages = totalPages;
        foreach (Button btn in abPages)
            btn.QueueFree();
        abPages.Clear();
        for (int i = 0; i < totalPages; i++) {
            Button btn = new Button();
            btn.Text = $"{i+1}";
            btn.RectMinSize = new Vector2(25,0);
            btn.Connect("pressed", this, "PageChanged", new Array { i });
            _pageCount.AddChild(btn);
            abPages.Add(btn);
            if (i > 9) {
                btn.Visible = false;
            }
        }
        if (totalPages > 0) {
            iCurrentPage = 0;
            (_pageCount.GetChild(0) as Button).Disabled = true;
            CheckPage();
        }
    }

    public void CheckPage() {
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
        int from = iCurrentPage - 5;
        if (from < 0)
            from = 0;
        
        int to = from + 9;
        if (to > iTotalPages)
            to = iTotalPages;
        
        for (int i = 0; i < iTotalPages; i++) {
            if (i >= from && i <= to) {
                abPages[i].Visible = true;
            } else {
                abPages[i].Visible = false;
            }
        }
    }

    public void StepPage(int page) {
        (_pageCount.GetChild(iCurrentPage) as Button).Disabled = false;
        if (page == 0)
            iCurrentPage = 0;
        else if (page == -2)
            iCurrentPage = iTotalPages - 1;
        else
            iCurrentPage += page;
        (_pageCount.GetChild(iCurrentPage) as Button).Disabled = true;
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
            (_pageCount.GetChild(iCurrentPage) as Button).Disabled = false;
        
        iCurrentPage = page;
        if (_pageCount.GetChildCount() > 0 && _pageCount.GetChildCount() > iCurrentPage)
            (_pageCount.GetChild(iCurrentPage) as Button).Disabled = true;
    }

    public void PageChanged(int page) {
        if (_pageCount.GetChildCount() > 0 && _pageCount.GetChildCount() > iCurrentPage)
            (_pageCount.GetChild(iCurrentPage) as Button).Disabled = false;
        
        iCurrentPage = page;
        if (_pageCount.GetChildCount() > 0 && _pageCount.GetChildCount() > iCurrentPage)
            (_pageCount.GetChild(iCurrentPage) as Button).Disabled = true;
        
        CheckPage();
        EmitSignal("page_changed", page);
    }
}
