using Godot;
using Godot.Collections;
using Godot.Sharp.Extras;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using FPath = System.IO.Path;
using Dir = System.IO.Directory;
using SFile = System.IO.File;
using SearchOption = System.IO.SearchOption;
using DateTime = System.DateTime;

public class ProjectsPanel : Panel
{
#region Node Accessors
	[NodePath("VC/MC/HC/ActionButtons")]
	ActionButtons _actionButtons = null;
	[NodePath("VC/SC")]
	ScrollContainer _scrollContainer = null;
	[NodePath("VC/ProjectSort")]
	MarginContainer _projectSort = null;
	[NodePath("VC/ProjectSort/PC/HeaderButtons/ProjectName")]
	HeaderButton _projectName = null;
	[NodePath("VC/ProjectSort/PC/HeaderButtons/GodotVersion")]
	HeaderButton _godotVersion = null;
	[NodePath("VC/SC/MarginContainer/ProjectList/ListView")]
	VBoxContainer _listView = null;
	[NodePath("VC/SC/MarginContainer/ProjectList/GridView")]
	GridContainer _gridView = null;
	[NodePath("VC/SC/MarginContainer/ProjectList/CategoryView")]
	VBoxContainer _categoryView = null;
	[NodePath("VC/MC/HC/ViewToggleButtons")]
	ViewToggleButtons _viewSelector = null;
#endregion

#region Enumerations
	enum View {
		ListView,
		GridView,
		CategoryView
	}
#endregion

#region Private Variables
	CategoryList clFavorites = null;
	CategoryList clUncategorized = null;

	ProjectLineEntry _currentPLE = null;
	ProjectIconEntry _currentPIE = null;

	View _currentView = View.ListView;
	Dictionary<int, CategoryList> _categoryList;
	ProjectPopup _popupMenu = null;
	Array<ProjectFile> _missingProjects = null;

	private Array<string> Views = null;

	Dictionary<ProjectFile, ProjectLineEntry> pleCache;
	Dictionary<ProjectFile, ProjectIconEntry> pieCache;
	Dictionary<Category, CategoryList> catCache;
	Dictionary<CategoryList, Dictionary<ProjectFile,ProjectLineEntry>> cpleCache;

	bool dragging = false;
	float _topBorder = 0.0f;
	float _bottomBorder = 0.0f;
	float _borderSize = 50.0f;
	float _scrollSpeed = 0.0f;

	Timer _scrollTimer = null;
	Tween _scrollTween = null;
#endregion

	Array<Container> _views;

	public override void _Ready()
	{
		this.OnReady();
		Views = new Array<string> {
			Tr("List View"),
			Tr("Icon View"),
			Tr("Category View")
		};

		_views = new Array<Container>();
		_views.Add(_listView);
		_views.Add(_gridView);
		_views.Add(_categoryView);

		_popupMenu = MainWindow._plScenes["ProjectPopup"].Instance<ProjectPopup>();
		AddChild(_popupMenu);
		_popupMenu.SetAsToplevel(true);

		AppDialogs.ImportProject.Connect("update_projects", this, "PopulateListing");
		AppDialogs.CreateCategory.Connect("update_categories", this, "PopulateListing");
		AppDialogs.RemoveCategory.Connect("update_categories", this, "PopulateListing");
		AppDialogs.EditProject.Connect("project_updated", this, "PopulateListing");
		AppDialogs.CreateProject.Connect("project_created", this, "OnProjectCreated");

		_actionButtons.SetHidden(3);
		_actionButtons.SetHidden(4);
		_categoryList = new Dictionary<int, CategoryList>();
		_missingProjects = new Array<ProjectFile>();

		if (_viewSelector.SelectedView != -1) {
			if (CentralStore.Settings.DefaultView == Tr("Last View Used")) {
				int indx = Views.IndexOf(CentralStore.Settings.LastView);
				_viewSelector.SetView(indx);
				OnViewSelector_Clicked(indx);
			} else {
				int indx = Views.IndexOf(CentralStore.Settings.DefaultView);
				_viewSelector.SetView(indx);
				OnViewSelector_Clicked(indx);
			}
		}

		_topBorder = _scrollContainer.RectGlobalPosition.y + _borderSize;
		_bottomBorder = _scrollContainer.RectSize.y - _borderSize;

		_scrollTimer = new Timer();
		AddChild(_scrollTimer);
		_scrollTimer.Connect("timeout", this, "OnScrollTimer");

		_scrollTween = new Tween();
		AddChild(_scrollTween);

		PopulateListing();
	}

	public override async void _Input(InputEvent inputEvent) {
		if (!Visible) {
			return;
		}

		foreach (Control dialog in AppDialogs.Instance.GetChildren()) {
			if (dialog.Visible) {
				return;
			}
		}

		if (inputEvent is InputEventKey iekEvent) {
			if (Input.IsActionJustPressed("ui_accept"))
			{
				if (_currentPIE != null)
				{
					OnIconEntry_DoubleClicked(_currentPIE);
				}
				else if (_currentPLE != null)
				{
					OnListEntry_DoubleClicked(_currentPLE);
				}
			}
			else if (Input.IsActionJustPressed("remove_project"))
			{
				await OnRemoveKeyPressed();
			}
		} else if (inputEvent is InputEventMouseMotion iemmEvent) {
			if (!dragging)
				return;
			if (iemmEvent.Position.y <= _topBorder)
			{
				_scrollSpeed = Mathf.Clamp(iemmEvent.Position.y - _topBorder, -_borderSize, 0.0f);
				if (_scrollSpeed == -_borderSize)
					_scrollSpeed *= 2;
			}
			else if (iemmEvent.Position.y >= _bottomBorder)
			{
				_scrollSpeed = Mathf.Clamp(iemmEvent.Position.y - _bottomBorder, 0.0f, _borderSize);
				if (_scrollSpeed == _borderSize)
					_scrollSpeed *= 2;
			}
			else if (_scrollSpeed != 0)
			{
				_scrollSpeed = 0;
			}
		}
	}

	[SignalHandler("direction_changed", nameof(_projectName))]
	void OnDirChanged_ProjectName(HeaderButton.SortDirection @dir) {
		_godotVersion.Indeterminate();
		PopulateListing();
	}

	[SignalHandler("direction_changed", nameof(_godotVersion))]
	void OnDirChanged_GodotVersion(HeaderButton.SortDirection @dir) {
		_projectName.Indeterminate();
		PopulateListing();
	}

	void OnScrollTimer() {
		if (!dragging)
			return;
		if (_scrollSpeed == 0)
			return;
		if (_scrollContainer.ScrollVertical == 0 && _scrollSpeed < 0)
			return;
		if (_scrollContainer.ScrollVertical == _scrollContainer.GetVScrollbar().MaxValue && _scrollSpeed > 0)
			return;
		if (_scrollTween.IsActive())
			_scrollTween.StopAll();
		_scrollTween.InterpolateProperty(
			_scrollContainer,
			"scroll_vertical",
			_scrollContainer.ScrollVertical,
			_scrollContainer.ScrollVertical + _scrollSpeed,
			0.24f,
			Tween.TransitionType.Linear,
			Tween.EaseType.Out);
		_scrollTween.Start();
		//_scrollContainer.ScrollVertical += (int)_scrollSpeed;
	}


	public ProjectLineEntry NewPLE(ProjectFile pf) {
		ProjectLineEntry ple = MainWindow._plScenes["ProjectLineEntry"].Instance<ProjectLineEntry>();
		if (_missingProjects.Contains(pf))
			ple.MissingProject = true;
		else if (!ProjectFile.ProjectExists(pf.Location)) {
			_missingProjects.Add(pf);
			ple.MissingProject = true;
		}
		ple.ProjectFile = pf;
		return ple;
	}

	public ProjectIconEntry NewPIE(ProjectFile pf) {
		ProjectIconEntry pie = MainWindow._plScenes["ProjectIconEntry"].Instance<ProjectIconEntry>();
		if (_missingProjects.Contains(pf))
			pie.MissingProject = true;
		else if (!ProjectFile.ProjectExists(pf.Location)) {
			_missingProjects.Add(pf);
			pie.MissingProject = true;
		}
		pie.ProjectFile = pf;
		return pie;
	}

	public CategoryList NewCL(string name) {
		CategoryList clt = MainWindow._plScenes["CategoryList"].Instance<CategoryList>();
		clt.Toggable = true;
		clt.CategoryName = name;
		return clt;
	}

	void ConnectHandlers(Node inode, bool isCategory = false) {
		if (inode is ProjectLineEntry ple) {
			ple.Connect("Clicked", this, "OnListEntry_Clicked");
			ple.Connect("DoubleClicked", this, "OnListEntry_DoubleClicked");
			ple.Connect("RightClicked", this, "OnListEntry_RightClicked");
			ple.Connect("FavoriteUpdated", this, "OnListEntry_FavoriteUpdated");
			if (isCategory) {
				ple.Connect("DragStarted", this, "OnDragStarted");
				ple.Connect("DragEnded", this, "OnDragEnded");
			}
		} else if (inode is ProjectIconEntry pie) {
			pie.Connect("Clicked", this, "OnIconEntry_Clicked");
			pie.Connect("DoubleClicked", this, "OnIconEntry_DoubleClicked");
			pie.Connect("RightClicked", this, "OnIconEntry_RightClicked");
		}
	}

	Array<string> ScanDirectories(Array<string> scanDirs) {
		Array<string> projects = new Array<string>();

		foreach (string pfExt in new string[] {"engine.cfg", "project.godot"}) {
			foreach (string dir in scanDirs) {
				foreach (string proj in Dir.EnumerateFiles(dir, pfExt, SearchOption.AllDirectories)) {
					projects.Add(proj);
				}
			}
		}
		return projects;
	}

	private Array<string> UpdateProjects(Array<string> projs) {
		Array<string> added = new Array<string>();

		foreach (string proj in projs) {
			string pfPath = proj.NormalizePath();
			if (!CentralStore.Instance.HasProject(pfPath))
			{
				bool isOldPrj = pfPath.EndsWith("engine.cfg");
				ProjectFile pf = ProjectFile.ReadFromFile(pfPath, isOldPrj);
				if (pf == null) continue;

				string gvId = "";
				foreach (GodotVersion gdVers in CentralStore.Versions) {
					Array<ushort> versComps = Util.GetVersionComponentsFromString(gdVers.Tag);
					if (isOldPrj) {
						bool isGodot1Prj = SFile.Exists(pfPath.GetBaseDir().PlusFile(".fscache").NormalizePath());
						if ((isGodot1Prj && versComps[0] <= 1) || (!isGodot1Prj && versComps[0] == 2)) {
							gvId = gdVers.Id;
							break;
						}
					} else {
						ProjectConfig pc = new ProjectConfig();
						if (pc.Load(pfPath) == Error.Ok) {
							int cfgVers = pc.GetValue("header", "config_version", "3").ToInt();
							if ((cfgVers == 3 && versComps[0] == 3 && versComps[1] == 0) || (cfgVers == 4 && versComps[0] == 3 && versComps[1] >= 1) || (cfgVers >= 5 && versComps[0] >= 4)) {
								gvId = gdVers.Id;
								break;
							}
						}
					}
				}

				if (!string.IsNullOrEmpty(gvId)) {
					pf.GodotId = gvId;
					CentralStore.Projects.Add(pf);
					added.Add(proj);
				}
			}
		}
		return added;
	}

	public async void ScanForProjects(bool autoscan = false) {
		if (CentralStore.Versions.Count == 0)
		{
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"), Tr("You need to add an editor version before you can scan for projects."));
			return;
		}

		Array<string> projects = new Array<string>();
		Array<string> scanDirs = CentralStore.Settings.ScanDirs.Duplicate();
		int i = 0;

		while (i < scanDirs.Count) {
			if (!Dir.Exists(scanDirs[i]))
				scanDirs.RemoveAt(i);
			else
				i++;
		}

		if (scanDirs.Count == 0) {
			var res = AppDialogs.YesNoDialog.ShowDialog(Tr("Scan Projects"), Tr("There are currently no valid folders to scan. Would you like to add one?"));
			while (!res.IsCompleted)
				await this.IdleFrame();
			
			if (res.Result) {
				AppDialogs.BrowseFolderDialog.CurrentDir = CentralStore.Settings.ProjectPath.NormalizePath();
				AppDialogs.BrowseFolderDialog.PopupCentered();
				AppDialogs.BrowseFolderDialog.Connect("dir_selected", this, "OnScanProjects_DirSelected", null, (uint)ConnectFlags.Oneshot);
				AppDialogs.BrowseFolderDialog.Connect("popup_hide", this, "OnScanProjects_PopupHide", null, (uint)ConnectFlags.Oneshot);
			}
			return;
		}

		Array<string> addedProjs = UpdateProjects(ScanDirectories(scanDirs));
		if (addedProjs.Count == 0)
		{
			if (!autoscan)
				AppDialogs.MessageDialog.ShowMessage(Tr("Scan Projects"), Tr("No new projects found."));
		}
		else
		{
			AppDialogs.MessageDialog.ShowMessage(Tr("Scan Projects"),
				string.Format(Tr("Found {0} project(s)."), addedProjs.Count));
			CentralStore.Instance.SaveDatabase();
			PopulateListing();
		}
	}

	void OnScanProjects_DirSelected(string path) {
		CentralStore.Settings.ScanDirs.Clear();
		CentralStore.Settings.ScanDirs.Add(path);
		CentralStore.Instance.SaveDatabase();
		ScanForProjects();
		PopulateListing();
	}

	void OnScanProjects_PopupHide()
	{
		if (AppDialogs.BrowseFolderDialog.IsConnected("dir_selected", this, "OnScanProjects_DirSelected"))
			AppDialogs.BrowseFolderDialog.Disconnect("dir_selected", this, "OnScanProjects_DirSelected");
	}

	// Optimizing PopulateListing() to utilize Cache of Nodes, adding and removing only as
	// CentralStore changes.  Sorting will happen AFTER all nodes have been generated, and will
	// be added / ordered from the cache to the display controls.
	// (At Least in ListView, Icon and Project no such sorting, but will ensure all updates will be
	// executed at a faster pace, then previous method of Freeing all, and Re-creating. )
	public void PopulateListing()
	{
		// Initialize if not initialized
		if (pleCache == null)
			pleCache = new Dictionary<ProjectFile, ProjectLineEntry>();
		if (pieCache == null)
			pieCache = new Dictionary<ProjectFile, ProjectIconEntry>();
		if (catCache == null)
			catCache = new Dictionary<Category, CategoryList>();
		if (cpleCache == null)
			cpleCache = new Dictionary<CategoryList, Dictionary<ProjectFile, ProjectLineEntry>>();

		ProjectLineEntry ple;
		ProjectIconEntry pie;
		CategoryList clt;

		// Create our Categories
		foreach (Category cat in CentralStore.Categories)
		{
			if (cat == null)
				continue;
			if (catCache.ContainsKey(cat))
				continue;
			clt = NewCL(cat.Name);
			clt.Pinned = CentralStore.Instance.IsCategoryPinned(cat);
			clt.SetMeta("ID", cat.Id);
			clt.Toggled = cat.IsExpanded;
			clt.Pinnable = true;
			_categoryList[cat.Id] = clt;
			catCache[cat] = clt;
			cpleCache[clt] = new Dictionary<ProjectFile, ProjectLineEntry>();
			_categoryView.AddChild(clt);
			clt.Connect("list_toggled", this, "OnCategoryListToggled", new Array { clt });
			clt.Connect("pin_toggled", this, "OnCategoryPinned", new Array() { clt });
			clt.Connect("drag_drop_completed", this, "OnDragDropCompleted");
		}

		if (clFavorites == null)
		{
			clFavorites = NewCL("Favorites");
			clFavorites.SetMeta("ID", -1);
			clFavorites.Toggled = CentralStore.Settings.FavoritesToggled;
			_categoryView.AddChild(clFavorites);
			clFavorites.Connect("list_toggled", this, "OnCategoryListToggled", new Array { clFavorites });
			clFavorites.Connect("drag_drop_completed", this, "OnDragDropCompleted");
			cpleCache[clFavorites] = new Dictionary<ProjectFile, ProjectLineEntry>();
		}

		if (clUncategorized == null)
		{
			clUncategorized = NewCL("Uncategorized");
			clUncategorized.SetMeta("ID", -2);
			clUncategorized.Toggled = CentralStore.Settings.UncategorizedToggled;
			_categoryView.AddChild(clUncategorized);
			clUncategorized.Connect("list_toggled", this, "OnCategoryListToggled", new Array { clUncategorized });
			clUncategorized.Connect("drag_drop_completed", this, "OnDragDropCompleted");
			cpleCache[clUncategorized] = new Dictionary<ProjectFile, ProjectLineEntry>();
		}

		// Create our Project Entries
		foreach (ProjectFile pf in CentralStore.Projects)
		{
			clt = null;
			if (!pleCache.ContainsKey(pf))
			{
				ple = NewPLE(pf);
				pleCache[pf] = ple;
				ConnectHandlers(ple);
			}

			if (!pieCache.ContainsKey(pf))
			{
				pie = NewPIE(pf);
				pieCache[pf] = pie;
				ConnectHandlers(pie);
			}

			if (_categoryList.ContainsKey(pf.CategoryId))
			{
				clt = _categoryList[pf.CategoryId];
			}

			if (clt == null && pf.Favorite)
				clt = clFavorites;

			if (clt == null)
				clt = clUncategorized;

			if (!cpleCache[clt].ContainsKey(pf))
			{
				ple = clt.AddProject(pf);
				cpleCache[clt][pf] = ple;
				ConnectHandlers(ple, true);
			}
		}

		// Clean up of Project Entries
		foreach (ProjectFile pf in pleCache.Keys)
		{
			if (!CentralStore.Projects.Contains(pf))
			{
				pleCache[pf].QueueFree();
				pleCache.Remove(pf);
			}
		}

		foreach (ProjectFile pf in pieCache.Keys)
		{
			if (!CentralStore.Projects.Contains(pf))
			{
				pieCache[pf].QueueFree();
				pieCache.Remove(pf);
			}
		}

		// Cleanup of Categories
		foreach (CategoryList cclt in cpleCache.Keys)
		{
			foreach (ProjectFile pf in cpleCache[cclt].Keys)
			{
				if (!CentralStore.Projects.Contains(pf) || (cclt == clFavorites && !pf.Favorite && cpleCache[cclt].Keys.Contains(pf)))
				{
					cpleCache[cclt][pf].QueueFree();
					cpleCache[cclt].Remove(pf);
				}
			}

			if (cclt != clFavorites && cclt != clUncategorized)
			{
				int CatID = (int)cclt.GetMeta("ID");
				if (!CentralStore.Instance.HasCategoryId(CatID))
				{
					Category cCat = null;
					foreach (Category cat in catCache.Keys)
					{
						if (catCache[cat] == cclt)
							cCat = cat;
					}

					if (cCat != null)
					{
						cpleCache.Remove(cclt);
						catCache.Remove(cCat);
						_categoryList.Remove(CatID);
					}
				}
			}

			if (cclt == clUncategorized)
			{
				foreach (ProjectFile pf in cpleCache[cclt].Keys)
				{
					if (pf.Favorite)
					{
						cpleCache[cclt][pf].QueueFree();
						cpleCache[cclt].Remove(pf);
					}
				}
			}

			cclt.CallDeferred("SortListing");
		}

		PopulateSort();

		if (_missingProjects.Count == 0)
			_actionButtons.SetHidden(6);
		else
			_actionButtons.SetVisible(6);
	}

	public void OnDragDropCompleted(CategoryList source, CategoryList destination, ProjectLineEntry ple) {
		if (cpleCache.ContainsKey(source) && cpleCache.ContainsKey(destination) && cpleCache[source].ContainsKey(ple.ProjectFile) && !cpleCache[destination].ContainsKey(ple.ProjectFile)) {    
			cpleCache[source].Remove(ple.ProjectFile);
			cpleCache[destination][ple.ProjectFile] = ple;

			if (destination == clUncategorized && ple.ProjectFile.Favorite) {
				ple.EmitSignal("FavoriteUpdated", ple);
			}
		}
	}

	public void PopulateSort() {
		foreach (Node node in _listView.GetChildren())
			_listView.RemoveChild(node);

		foreach (Node node in _gridView.GetChildren())
			_gridView.RemoveChild(node);

		foreach (Node node in _categoryView.GetChildren())
			_categoryView.RemoveChild(node);

		foreach (Category cat in CentralStore.Instance.GetPinnedCategories())
			if (cat != null && catCache.ContainsKey(cat))
				_categoryView.AddChild(catCache[cat]);

		foreach (Category cat in CentralStore.Instance.GetUnpinnedCategories())
			if (cat != null && catCache.ContainsKey(cat))
				_categoryView.AddChild(catCache[cat]);

		_categoryView.AddChild(clFavorites);
		_categoryView.AddChild(clUncategorized);

		foreach (IOrderedEnumerable<ProjectFile> apf in SortListing()) {
			foreach (ProjectFile pf in apf)
				_listView.AddChild(pleCache[pf]);
		}

		foreach (IOrderedEnumerable<ProjectFile> apf in SortListing(true)) {
			foreach (ProjectFile pf in apf)
				_gridView.AddChild(pieCache[pf]);
		}
	}

	public void RefreshList() {
		_listView.QueueFreeChildren();
		_gridView.QueueFreeChildren();
		_categoryView.QueueFreeChildren();
		clFavorites = null;
		clUncategorized = null;
		_currentPLE = null;
		_currentPIE = null;
		pleCache = null;
		pieCache = null;
		catCache = null;
		cpleCache = null;
		CallDeferred("PopulateListing");
	}

	public void OnProjectCreated(ProjectFile pf) {
		PopulateListing();
		ExecuteEditorProject(pf);
	}

	private void UpdateListExcept(ProjectLineEntry ple) {
		if (_listView.GetChildren().Contains(ple)) {
			foreach (ProjectLineEntry cple in _listView.GetChildren()) {
				if (cple != ple)
					cple.Color = new Color("002a2e37");
			}
		} else {
			foreach (CategoryList cl in _categoryView.GetChildren()) {
				foreach (ProjectLineEntry cple in cl.List.GetChildren()) {
					if (cple != ple)
						cple.Color = new Color("002a2e37");
				}
			}
		}
	}

	void OnCategoryListToggled(CategoryList clt) {
		int id = (int)clt.GetMeta("ID");
		if (id == -1 || id == -2) {
			if (id == -1)
				CentralStore.Settings.FavoritesToggled = clt.Toggled;
			else
				CentralStore.Settings.UncategorizedToggled = clt.Toggled;
			CentralStore.Instance.SaveDatabase();
			return;
		}
		Category cat = CentralStore.Categories.Where(x => x.Id == id).FirstOrDefault<Category>();
		if (cat == null)
			return;
		
		cat.IsExpanded = clt.Toggled;
		CentralStore.Instance.SaveDatabase();
	}

	void OnCategoryPinned(CategoryList clt)
	{
		int id = (int)clt.GetMeta("ID");
		Category cat = CentralStore.Categories.Where(x => x.Id == id).FirstOrDefault<Category>();
		if (cat == null)
			return;

		if (clt.Pinned)
		{
			CentralStore.Instance.PinCategory(cat);
		}
		else
		{
			CentralStore.Instance.UnpinCategory(cat);
		}

		PopulateSort();
		CentralStore.Instance.SaveDatabase();
	}

	void OnListEntry_Clicked(ProjectLineEntry ple) {
		UpdateListExcept(ple);
		_currentPLE = ple;
	}

	void OnListEntry_DoubleClicked(ProjectLineEntry ple) {
		if (ple.MissingProject)
			return;
		ple.ProjectFile.LastAccessed = DateTime.UtcNow;
		ExecuteEditorProject(ple.ProjectFile);
	}

	void OnListEntry_RightClicked(ProjectLineEntry ple) {
		OnListEntry_Clicked(ple);
		_popupMenu.ProjectLineEntry = ple;
		_popupMenu.ProjectIconEntry = null;
		_popupMenu.Popup_(new Rect2(GetGlobalMousePosition(), _popupMenu.RectSize));
		for (int indx = 0; indx < _popupMenu.GetItemCount(); indx++) {
			if (indx != 4) _popupMenu.SetItemDisabled(indx, ple.MissingProject);
		}
	}

	void OnListEntry_FavoriteUpdated(ProjectLineEntry ple) { 
		PopulateListing();
	}

	void OnDragStarted(ProjectLineEntry ple) {
		dragging = true;
		_scrollTimer.Start(0.25f);
	}

	void OnDragEnded(ProjectLineEntry ple) {
		dragging = false;
		_scrollTimer.Stop();
	}

	private void OnIconEntry_Clicked(ProjectIconEntry pie) {
		UpdateIconsExcept(pie);
		_currentPIE = pie;
	}

	private void OnIconEntry_DoubleClicked(ProjectIconEntry pie)
	{
		if (pie.MissingProject)
			return;
		pie.ProjectFile.LastAccessed = DateTime.UtcNow;
		ExecuteEditorProject(pie.ProjectFile);
	}

	void OnIconEntry_RightClicked(ProjectIconEntry pie) {
		OnIconEntry_Clicked(pie);
		_popupMenu.ProjectLineEntry = null;
		_popupMenu.ProjectIconEntry = pie;
		_popupMenu.Popup_(new Rect2(GetGlobalMousePosition(), _popupMenu.RectSize));
		for (int indx = 0; indx < _popupMenu.GetItemCount(); indx++) {
			if (indx != 4) _popupMenu.SetItemDisabled(indx, pie.MissingProject);
		}
	}

	public async void _IdPressed(int id) {
		ProjectFile pf;
		if (_popupMenu.ProjectLineEntry != null) {
			pf = _popupMenu.ProjectLineEntry.ProjectFile;
			_currentPLE = _popupMenu.ProjectLineEntry;
		} else {
			pf = _popupMenu.ProjectIconEntry.ProjectFile;
			_currentPIE = _popupMenu.ProjectIconEntry;
		}
		switch (id) {
			case 0:     // Open Project
				ExecuteEditorProject(pf);
				break;
			case 1:     // Run Project
				ExecuteProject(pf);
				break;
			case 3:     // Edit Project File
				AppDialogs.EditProject.Connect("project_updated", this, "OnProjectUpdated", new Array {pf}, (uint)ConnectFlags.Oneshot);
				AppDialogs.EditProject.Connect("hide", this, "OnHide_EditProject", null, (uint)ConnectFlags.Oneshot);
				AppDialogs.EditProject.ShowDialog(pf);
				break;
			case 4:     // Remove Project
				await RemoveProject(pf);
				break;
			case 6:     // Show Project Files
				OS.ShellOpen("file://" + pf.Location.GetBaseDir());
				break;
			case 7:     // Show Project Data Folder
				string folder = GetProjectDataFolder(pf);
				if (Dir.Exists(folder))
					OS.ShellOpen("file://" + folder);
				else
					AppDialogs.MessageDialog.ShowMessage(Tr("Show Data Folder"), 
						string.Format(Tr("The data folder \"{0}\" doesn't exist."), folder));
				break;
		}
	}

	private void OnProjectUpdated(ProjectFile pf) {
		var ple = pleCache.Where( x => x.Key == pf ).Select( x => x.Value ).FirstOrDefault<ProjectLineEntry>();
		if (ple != null) {
			ple.ProjectFile = pf;
		}

		foreach (CategoryList cat in cpleCache.Keys) {
			ple = cpleCache[cat].Where( x => x.Key == pf).Select( x => x.Value ).FirstOrDefault<ProjectLineEntry>();
			if (ple != null)
				ple.ProjectFile = pf;
		}

		var pie = pieCache.Where( x => x.Key == pf ).Select( x => x.Value ).FirstOrDefault<ProjectIconEntry>();
		if (pie != null)
			pie.ProjectFile = pf;
	}

	private void OnHide_EditProject() {
		if (AppDialogs.EditProject.IsConnected("project_updated", this, "OnProjectUpdated"))
			AppDialogs.EditProject.Disconnect("project_updated", this, "OnProjectUpdated");

		if (AppDialogs.EditProject.IsConnected("hide", this, "OnHide_EditProject"))
			AppDialogs.EditProject.Disconnect("hide", this, "OnHide_EditProject");
	}

	private void RemoveMissingProjects() {
		foreach (ProjectFile missing in _missingProjects) {
			CentralStore.Projects.Remove(missing);
		}
		CentralStore.Instance.SaveDatabase();
		_missingProjects.Clear();
		PopulateListing();
	}

	private string GetProjectDataFolder(ProjectFile pf)
	{
		ProjectConfig pc = new ProjectConfig();
		pc.Load(pf.Location);
		string folder = "";
		if (pc.HasSectionKey("application", "config/use_custom_user_dir"))
		{
			if (pc.GetValue("application", "config/use_custom_user_dir") == "true") {
				folder = OS.GetDataDir().Join(pc.GetValue("application", "config/custom_user_dir_name"));
			} else {
				folder = OS.GetDataDir().Join("Godot", "app_userdata", pf.Name);
			}
		}
		else
		{
			folder = OS.GetDataDir().Join("Godot", "app_userdata", pf.Name);
		}
		return folder.NormalizePath();
	}

	private void StartSharedSettings(GodotVersion gv)
	{
		if (!string.IsNullOrEmpty(gv.SharedSettings))
		{
			GodotVersion ssgv = CentralStore.Instance.FindVersion(gv.SharedSettings);
			if (ssgv == null)
			{
				gv.SharedSettings = "";
				CentralStore.Instance.SaveDatabase();
			}
			else
			{
				ushort gmv = Util.GetVersionComponentsFromString(gv.Tag)[0];
				string fromPath = ssgv.Location.Join("editor_data");
				string toPath = gv.Location.Join("editor_data");
				Array<string> copies = new Array<string>();
				string es = "editor_settings";
				if (gmv <= 1) {
					es = es + ".xml";
				} else if (gmv == 2) {
					es = es + ".tres";
					copies.Add(fromPath.Join("text_editor_themes"));
				} else {
					es = es + $"-{(gmv == 3 ? "3" : "4")}.tres";
					copies.Add(fromPath.Join("feature_profiles"));
					copies.Add(fromPath.Join("script_templates"));
					copies.Add(fromPath.Join("text_editor_themes"));
				}
				copies.Add(fromPath.Join(es));
				foreach (string path in copies)
					CopyRecursive(path, toPath);
			}
		}
	}

	private void ExecuteProject(ProjectFile pf)
	{
		GodotVersion gv = CentralStore.Instance.FindVersion(pf.GodotId);
		if (gv == null)
		{
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"), Tr("The editor version associated with this project was not found."));
			return;
		}

		if (!SFile.Exists(gv.GetExecutablePath().GetOSDir()))
		{
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"), string.Format(Tr("The executable path for {0} doesn't exist."), gv.GetDisplayName()));
			return;
		}

		StartSharedSettings(gv);

		ProcessStartInfo psi = new ProcessStartInfo();
		psi.FileName = gv.GetExecutablePath().GetOSDir();
		psi.WorkingDirectory = pf.Location.GetBaseDir().GetOSDir().NormalizePath();
		psi.UseShellExecute = !CentralStore.Settings.NoConsole;
		psi.CreateNoWindow = CentralStore.Settings.NoConsole;

		Process proc = Process.Start(psi);
	}

	private void UpdateIconsExcept(ProjectIconEntry pie) {
		foreach (ProjectIconEntry cpie in _gridView.GetChildren()) {
			if (cpie != pie)
				cpie.Color = new Color("002a2e37");
		}
	}

	private void ExecuteEditorProject(ProjectFile pf)
	{
		GodotVersion gv = CentralStore.Instance.FindVersion(pf.GodotId);
		if (gv == null)
		{
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"), Tr("The editor version associated with this project was not found."));
			return;
		}

		if (!SFile.Exists(gv.GetExecutablePath().GetOSDir()))
		{
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"), string.Format(Tr("The executable path for {0} doesn't exist."), gv.GetDisplayName()));
			return;
		}

		StartSharedSettings(gv);

		ProcessStartInfo psi = new ProcessStartInfo();
		psi.FileName = gv.GetExecutablePath().GetOSDir();
		psi.Arguments = "-e";
		psi.WorkingDirectory = pf.Location.GetBaseDir().GetOSDir().NormalizePath();
		psi.UseShellExecute = !CentralStore.Settings.NoConsole;
		psi.CreateNoWindow = CentralStore.Settings.NoConsole;
		Process.Start(psi);
	}

	void CopyRecursive(string fromPath, string toPath)
	{
		if (!Dir.Exists(toPath))
			Dir.CreateDirectory(toPath);

		var files = new Array<string>();
		if (Dir.Exists(fromPath))
			files = GodotInstaller.RecurseDirectory(fromPath);
		else
			files.Add(fromPath);

		foreach (var file in files)
		{
			var newFile = file.Replace(fromPath, toPath);
			if (newFile == toPath)
				newFile = toPath.Join(FPath.GetFileName(file));
			if (Dir.Exists(file) && !Dir.Exists(newFile))
			{
				Dir.CreateDirectory(newFile);
			}
			else if (SFile.Exists(file))
			{
				if (SFile.Exists(newFile))
					SFile.Delete(newFile);
				SFile.Copy(file, newFile);
			}
		}
	}

	Array<string> BuildSharedSettingsFiles(GodotVersion gv, string fromPath)
	{
		Array<string> files = new Array<string>();

		ushort gmv = Util.GetVersionComponentsFromString(gv.Tag)[0];
		string es = "editor_settings";
		if (gmv <= 1) {
			es = es + ".xml";
		} else if (gmv == 2) {
			es = es + ".tres";
			files.Add(fromPath.Join("text_editor_themes"));
		} else {
			es = es + $"-{(gmv == 3 ? "3" : "4")}.tres";
			files.Add(fromPath.Join("feature_profiles"));
			files.Add(fromPath.Join("script_templates"));
			files.Add(fromPath.Join("text_editor_themes"));
		}
		files.Add(fromPath.Join(es));
		return files;
	}

	[SignalHandler("clicked", nameof(_actionButtons))]
	async void OnActionButtons_Clicked(int index) {
		switch (index) {
			case 0: // New Project File
				AppDialogs.CreateProject.ShowDialog();
				break;
			case 1: // Import Project File
				AppDialogs.ImportProject.ShowDialog();
				break;
			case 2: // Scan Project Folder
				ScanForProjects();
				break;
			case 3: // Add Category
				AppDialogs.CreateCategory.ShowDialog();
				break;
			case 4: // Remove Category
				AppDialogs.RemoveCategory.ShowDialog();
				break;
			case 5: // Remove Project
				await OnRemoveKeyPressed();
				break;
			case 6: // Remove Missing Projects
				var res = AppDialogs.YesNoDialog.ShowDialog(Tr("Please Confirm..."), 
					Tr("Remove all missing projects from the list?"),
					Tr("Remove All"),
					Tr("Cancel"));
				await res;
				if (res.Result)
					RemoveMissingProjects();
				break;
			case 7: // Refresh List
				RefreshList();
				break;
		}
	}

	private async Task OnRemoveKeyPressed()
	{
		ProjectFile pf = null;
		if (_currentView == View.GridView)
		{
			if (_currentPIE != null)
				pf = _currentPIE.ProjectFile;
		}
		else
		{
			if (_currentPLE != null)
				pf = _currentPLE.ProjectFile;
		}
		
		if (pf == null)
			return;
		
		await RemoveProject(pf);
	}

	private void RemoveProjectListing(ProjectFile pf, bool deleteFiles = false)
	{
		_currentPIE = null;
		_currentPLE = null;
		if (deleteFiles)
			RemoveFolders(pf.Location.GetBaseDir());
		if (_missingProjects.Contains(pf)) {
			_missingProjects.Remove(pf);
			if (_missingProjects.Count == 0)
				_actionButtons.SetHidden(6);
		}
		CentralStore.Projects.Remove(pf);
		CentralStore.Instance.SaveDatabase();
		PopulateListing();
	}

	private async Task RemoveProject(ProjectFile pf)
	{
		if (!_missingProjects.Contains(pf)) {
			var task = AppDialogs.YesNoCancelDialog.ShowDialog(Tr("Please Confirm..."),
					string.Format(Tr("Remove {0} from the list?"), pf.Name),
					Tr("Project and Files"), Tr("Just Project"));
			while (!task.IsCompleted)
				await this.IdleFrame();
			switch (task.Result)
			{
				case YesNoCancelDialog.ActionResult.FirstAction:
					RemoveProjectListing(pf, true);
					break;
				case YesNoCancelDialog.ActionResult.SecondAction:
					RemoveProjectListing(pf, false);
					break;
				case YesNoCancelDialog.ActionResult.CancelAction:
					break;
			}
		} else {
			RemoveProjectListing(pf, false);
		}
	}

	void RemoveFolders(string path) {
		Directory dir = new Directory();
		if (dir.Open(path) == Error.Ok) {
			dir.ListDirBegin(true, false);
			var filename = dir.GetNext();
			while (!string.IsNullOrEmpty(filename)) {
				if (dir.CurrentIsDir()) {
					RemoveFolders(path.PlusFile(filename).NormalizePath());
				}
				dir.Remove(filename);
				filename = dir.GetNext();
			}
			dir.ListDirEnd();
		}
		if (dir.Open(path.GetBaseDir()) == Error.Ok) {
			dir.Remove(path.GetFile());
		}
	}

	[SignalHandler("Clicked", nameof(_viewSelector))]
	void OnViewSelector_Clicked(int page) {
		for (int i = 0; i < _views.Count; i++) {
			_views[i].Visible = (i == page);
		}
		if (page == 2) {
			_actionButtons.SetVisible(3);
			_actionButtons.SetVisible(4);
		} else {
			_actionButtons.SetHidden(3);
			_actionButtons.SetHidden(4);
		}
		if (page == 0) {
			_projectSort.Visible = true;
		} else {
			_projectSort.Visible = false;
		}
		_currentView = (View)page;
		CentralStore.Settings.LastView = Views[page];
	}

	public System.Collections.ArrayList SortListing(bool @default = false) {
		IOrderedEnumerable<ProjectFile> fav;
		IOrderedEnumerable<ProjectFile> non_fav;

		// Default Behavior
		if (_projectName.Direction == HeaderButton.SortDirection.Indeterminate &&
			_godotVersion.Direction == HeaderButton.SortDirection.Indeterminate ||
			@default) {
			fav = CentralStore.Projects.Where(pf => pf.Favorite)
						.OrderByDescending(pf => pf.LastAccessed);

			non_fav = CentralStore.Projects.Where(pf => !pf.Favorite)
						.OrderByDescending(pf => pf.LastAccessed);
		// Sort by Project Name
		} else if (_projectName.Direction != HeaderButton.SortDirection.Indeterminate &&
					_godotVersion.Direction == HeaderButton.SortDirection.Indeterminate) {
			if (_projectName.Direction == HeaderButton.SortDirection.Up) {
				fav = CentralStore.Projects.OrderByDescending(pf => pf.Name);
				non_fav = null;
			} else {
				fav = CentralStore.Projects.OrderBy(pf => pf.Name);
				non_fav = null;
			}
		// Sort by Godot Version
		} else {
			if (_godotVersion.Direction == HeaderButton.SortDirection.Up) {
				fav = CentralStore.Projects.OrderBy(pf => CentralStore.Instance.GetVersion(pf.GodotId).Tag);
				non_fav = null;
			} else {
				fav = CentralStore.Projects.OrderByDescending(pf => CentralStore.Instance.GetVersion(pf.GodotId).Tag);
				non_fav = null;
			}
		}

		if (non_fav == null)
			return new System.Collections.ArrayList() { fav };
		else
			return new System.Collections.ArrayList() { fav, non_fav };
	}
}   
