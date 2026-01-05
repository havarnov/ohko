using System;
using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI;

namespace Ohko.Editor;

public partial class FramesUserControl : UserControl
{
    public FramesViewModel ViewModel => (FramesViewModel?)DataContext ?? throw new InvalidOperationException();

    public FramesUserControl()
    {
        InitializeComponent();
        ScrollViewer.PointerWheelChanged += OnPointerWheelChanged;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        scrollViewer.Offset = new Vector(
            scrollViewer.Offset.X - e.Delta.Y * 30,
            scrollViewer.Offset.Y);

        e.Handled = true;
    }
}

public class FramesViewModel : ViewModelBase
{
    public EditorModel EditorModel { get; }

    public ObservableCollection<FrameButton> Frames { get; } = [];

    public ReactiveCommand<FrameButton, Unit> FrameButtonClickedCommand { get; }

    public FramesViewModel(EditorModel editorModel)
    {
        EditorModel = editorModel;

        FrameButtonClickedCommand = ReactiveCommand.Create<FrameButton>(button =>
        {
            EditorModel.SelectedFrameIdx = button.FrameIndex;
        });

        this.WhenAnyValue(vm => vm.EditorModel.FrameCount)
            .Subscribe(frameCount =>
            {
                Frames.Clear();
                for (int i = 0; i < frameCount; i++)
                {
                    Frames.Add(new FrameButton { FrameIndex = i, });
                }
            });
    }
}

public class FrameButton : ViewModelBase
{
    public int FrameIndex { get; init; }
}