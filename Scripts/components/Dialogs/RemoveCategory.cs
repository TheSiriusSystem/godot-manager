using Godot;
using Godot.Sharp.Extras;


public class RemoveCategory : ReferenceRect
{
#region Signals
	[Signal]
	public delegate void update_categories();
#endregion

#region Node Paths
	[NodePath("PC/CC/P/VB/MCContent/VB/Categories")]
	ItemList _categoryList = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/RemoveBtn")]
	Button _removeBtn = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/CancelBtn")]
	Button _cancelBtn = null;
#endregion

#region Private Variables
	string selectedItem;
#endregion

	public override void _Ready()
	{
		this.OnReady();
		selectedItem = "";
	}

	[SignalHandler("item_selected", nameof(_categoryList))]
	void OnItemSelected(int index) {
		selectedItem = _categoryList.GetItemText(index);
	}

	[SignalHandler("pressed", nameof(_removeBtn))]
	async void OnPressedRemoveBtn() {
		if (string.IsNullOrEmpty(selectedItem)) {
			AppDialogs.MessageDialog.ShowMessage(Tr("Remove Category"), Tr("You must select a category before it can be removed."));
			return;
		}

		if (!CentralStore.Instance.HasCategory(selectedItem)) {
			AppDialogs.MessageDialog.ShowMessage(Tr("Remove Category"),
				string.Format(Tr("The selected category \"{0}\" doesn't exist."), selectedItem));
			return;
		}
		Category cat = CentralStore.Instance.GetCategoryByName(selectedItem);
		var res = AppDialogs.YesNoDialog.ShowDialog(Tr("Please Confirm..."),
				string.Format(Tr("You are about to remove category \"{0}\".\nAll projects in this category will be moved to the \"Uncategorized\" category."), selectedItem), Tr("Remove"), Tr("Cancel"));
		while (!res.IsCompleted) {
			await this.IdleFrame();
		}
		if (res.Result) {
			foreach (ProjectFile prj in CentralStore.Projects) {
				if (prj.CategoryId == cat.Id)
					prj.CategoryId = -1;
			}
			CentralStore.Categories.Remove(cat);
			CentralStore.Instance.SaveDatabase();
			EmitSignal("update_categories");
			Visible = false;
		}
	}

	[SignalHandler("pressed", nameof(_cancelBtn))]
	void OnPressedCancelBtn() {
		Visible = false;
	}

	public void ShowDialog() {
		_categoryList.Clear();
		foreach (Category cat in CentralStore.Categories) {
			_categoryList.AddItem(cat.Name);
		}
		if (_categoryList.Items.Count == 0) {
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"),
			Tr("There are no categories to remove."));
			return;
		}
		Visible = true;
	}
}
