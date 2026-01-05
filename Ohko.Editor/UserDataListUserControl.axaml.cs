using System;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;

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

    public ICommand CreateNewUserDataCommand { get; }

    public UserDataListViewModel(EditorModel editorModel)
    {
        EditorModel = editorModel;
        CreateNewUserDataCommand = ReactiveCommand.Create(CreateNewUserData);
    }

    private void CreateNewUserData()
    {
        EditorModel.AddUserModel(new UserDataModel(Guid.NewGuid(), []));
    }
}