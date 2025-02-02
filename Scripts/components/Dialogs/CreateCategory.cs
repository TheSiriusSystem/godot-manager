using Godot;
using Godot.Sharp.Extras;


public class CreateCategory : ReferenceRect
{
#region Signals
	[Signal]
	public delegate void update_categories();
#endregion

#region Node Paths
	[NodePath("PC/CC/P/VB/MCContent/VB/CategoryName")]
	LineEdit _categoryName = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/CreateBtn")]
	Button _createBtn = null;

	[NodePath("PC/CC/P/VB/MCButtons/HB/CancelBtn")]
	Button _cancelBtn = null;
#endregion

#region Private Variables
#endregion
	public override void _Ready()
	{
		this.OnReady();
	}

	[SignalHandler("pressed", nameof(_createBtn))]
	void OnPressedCreateBtn() {
		if (string.IsNullOrEmpty(_categoryName.Text)) {
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"), Tr("You need to provide a name for this category."));
			return;
		}
		if (CentralStore.Instance.HasCategory(_categoryName.Text)) {
			AppDialogs.MessageDialog.ShowMessage(Tr("Error"), Tr("There is already a category of the same name."));
			return;
		}
		Category c = new Category();
		c.Name = _categoryName.Text;
		int id = CentralStore.Categories.Count;
		while (CentralStore.Instance.HasCategoryId(id)) {
			id++;
		}
		c.Id = id;
		CentralStore.Categories.Add(c);
		CentralStore.Instance.SaveDatabase();
		EmitSignal("update_categories");
		Visible = false;
	}

	[SignalHandler("pressed", nameof(_cancelBtn))]
	void OnPressedCancelBtn() {
		Visible = false;
	}

	public void ShowDialog() {
		_categoryName.Text = "";
		Visible = true;
	}
}
