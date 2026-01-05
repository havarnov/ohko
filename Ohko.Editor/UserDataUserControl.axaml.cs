using Avalonia.Controls;

namespace Ohko.Editor;

public partial class UserDataUserControl : UserControl
{
    public UserDataUserControl()
    {
        InitializeComponent();
    }
}

public class UserDataViewModel(EditorModel editorModel) : ViewModelBase
{
    public string? UserData { get; set; }
}