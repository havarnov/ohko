using Avalonia.Controls;

namespace Ohko.Editor;

public partial class UserDataListUserControl : UserControl
{
    public UserDataListUserControl()
    {
        InitializeComponent();
    }
}

public class UserDataListViewModel
{
    public EditorModel EditorModel { get; }

    public UserDataListViewModel(EditorModel editorModel)
    {
        EditorModel = editorModel;
    }
}