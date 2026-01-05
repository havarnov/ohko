using Avalonia.Controls;
using ReactiveUI;

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
    public EditorModel EditorModel { get; } = editorModel;
}