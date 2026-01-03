using AsepriteDotNet.Aseprite;
using Avalonia.Controls;

namespace Ohko.Editor;

public partial class FileEditorControl : UserControl
{
    public FileEditorControl()
    {
        InitializeComponent();
    }

    public AsepriteFile? File { get; set; }
}