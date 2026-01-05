using System;
using System.Threading.Tasks;
using System.Windows.Input;
using AsepriteDotNet.IO;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;

namespace Ohko.Editor;

public partial class EditorUserControl : UserControl
{
    private EditorViewModel ViewModel => (EditorViewModel?)DataContext ?? throw new InvalidOperationException();

    public EditorUserControl()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        // FrameIndexSelector.DataContext = new FramesViewModel(ViewModel.EditorModel);
        UserDataList.DataContext = new UserDataListViewModel(ViewModel.EditorModel);
        UserData.DataContext = new UserDataViewModel(ViewModel.EditorModel);
    }
}

public class EditorViewModel : ViewModelBase
{
    private readonly UserControl _control;
    public EditorModel EditorModel { get; }
    public ICommand SelectFileCommand { get; }

    public EditorViewModel(UserControl control, EditorModel editorModel)
    {
        EditorModel = editorModel;
        _control = control;
        SelectFileCommand = ReactiveCommand.CreateFromTask(SelectFile);

    }

    private async Task SelectFile()
    {
        try
        {
            // Get top level from the current control. Alternatively, you can use Window reference instead.
            var topLevel = TopLevel.GetTopLevel(_control);
            if (topLevel is null)
            {
                return;
            }

            // Start async operation to open the dialog.
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Open Aseprite File",
                    AllowMultiple = false,
                    FileTypeFilter = [new FilePickerFileType("aseprite"), new FilePickerFileType("ase")]
                });

            if (files.Count != 1)
            {
                return;
            }

            var file = AsepriteFileLoader.FromStream(files[0].Name, await files[0].OpenReadAsync());
            EditorModel.AsepriteFile = file;
        }
        catch (Exception e)
        {
            // Nothing can be done :(
            Console.WriteLine(e);
        }
    }
}