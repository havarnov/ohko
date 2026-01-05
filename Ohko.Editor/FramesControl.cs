using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Ohko.Editor
{
    public class FramesControl : Control
    {
        // --- Styled properties ---
        public static readonly StyledProperty<int> FrameCountProperty =
            AvaloniaProperty.Register<FramesControl, int>(nameof(FrameCount), 0);

        public static readonly StyledProperty<ICollection<int>> CurrentlySelectedFramesProperty =
            AvaloniaProperty.Register<FramesControl, ICollection<int>>(nameof(CurrentlySelectedFrames), []);

        public static readonly StyledProperty<IEnumerable<(string Name, int Start, int Length)>> TagsProperty =
            AvaloniaProperty.Register<FramesControl, IEnumerable<(string Name, int Start, int Length)>>(nameof(Tags), []);

        public static readonly StyledProperty<int?> SelectedFrameProperty =
            AvaloniaProperty.Register<FramesControl, int?>(nameof(SelectedFrame));

        // --- Properties ---
        public int FrameCount
        {
            get => GetValue(FrameCountProperty);
            set => SetValue(FrameCountProperty, value);
        }

        public ICollection<int> CurrentlySelectedFrames
        {
            get => GetValue(CurrentlySelectedFramesProperty);
            set => SetValue(CurrentlySelectedFramesProperty, value);
        }

        public IEnumerable<(string Name, int Start, int Length)> Tags
        {
            get => GetValue(TagsProperty);
            set => SetValue(TagsProperty, value);
        }

        public int? SelectedFrame
        {
            get => GetValue(SelectedFrameProperty);
            set => SetValue(SelectedFrameProperty, value);
        }

        // --- Layout constants ---
        private const double CellWidth = 30;
        private const double CellHeight = 30;
        private const double TagHeight = 10;
        private const double CheckboxHeight = 20;
        private const double CheckboxSize = 12;

        public FramesControl()
        {
            AffectsRender<FramesControl>(FrameCountProperty, CurrentlySelectedFramesProperty, TagsProperty, SelectedFrameProperty);
            AffectsMeasure<FramesControl>(FrameCountProperty, CurrentlySelectedFramesProperty, TagsProperty, SelectedFrameProperty);

            ClipToBounds = true;
        }

        // --- Layout ---
        protected override Size MeasureOverride(Size availableSize)
        {
            double width = FrameCount * CellWidth;
            double height = CellHeight + TagHeight + CheckboxHeight;
            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize) => finalSize;

        // --- Rendering ---
        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (FrameCount == 0) return;

            // Precompute checkbox Y position
            double checkboxY = CellHeight + TagHeight + (CheckboxHeight - CheckboxSize) / 2;

            // --- Draw frames ---
            for (int i = 0; i < FrameCount; i++)
            {
                double x = i * CellWidth;
                double y = 0;

                // Cell rectangle
                var rect = new Rect(x, y, CellWidth, CellHeight);
                context.DrawRectangle(Brushes.LightGray, new Pen(Brushes.Black), rect);

                // Frame number centered
                var text = new FormattedText(
                    i.ToString(),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    12,
                    Brushes.Black
                );

                double textX = x + (CellWidth - text.Width) / 2;
                double textY = y + (CellHeight - text.Height) / 2;
                context.DrawText(text, new Point(textX, textY));
            }

            // --- Draw tags ---
            foreach (var tag in Tags)
            {
                double tagX = tag.Start * CellWidth;
                double tagWidth = tag.Length * CellWidth;
                var tagRect = new Rect(tagX, CellHeight, tagWidth, TagHeight);

                context.DrawRectangle(Brushes.LightBlue, new Pen(Brushes.Blue), tagRect);

                // Tag name centered
                var tagText = new FormattedText(
                    tag.Name,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    10,
                    Brushes.Black
                );

                double textX = tagX + (tagWidth - tagText.Width) / 2;
                double textY = CellHeight + (TagHeight - tagText.Height) / 2;
                context.DrawText(tagText, new Point(textX, textY));
            }

            // --- Draw checkboxes ---
            foreach (var frame in Enumerable.Range(0, FrameCount))
            {
                double x = frame * CellWidth + (CellWidth - CheckboxSize) / 2;
                bool isSelected = CurrentlySelectedFrames.Contains(frame);

                var rect = new Rect(x, checkboxY, CheckboxSize, CheckboxSize);
                context.DrawEllipse(isSelected ? Brushes.LightGreen : Brushes.LightGray,
                    new Pen(Brushes.Black),
                    rect);
            }

            // --- Draw highlight for SelectedFrame ---
            if (SelectedFrame.HasValue && SelectedFrame.Value >= 0 && SelectedFrame.Value < FrameCount)
            {
                double highlightX = SelectedFrame.Value * CellWidth;
                var highlightRect = new Rect(highlightX, 0, CellWidth, CellHeight);
                context.DrawRectangle(null, new Pen(Brushes.Red, 2), highlightRect);
            }
        }

        // --- Input handling ---
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            var point = e.GetPosition(this);
            int frameIndex = (int)(point.X / CellWidth);

            if (frameIndex < 0 || frameIndex >= FrameCount) return;

            // Compute checkbox rect
            double checkboxY = CellHeight + TagHeight + (CheckboxHeight - CheckboxSize) / 2;
            var checkboxRect = new Rect(frameIndex * CellWidth + (CellWidth - CheckboxSize) / 2, checkboxY, CheckboxSize, CheckboxSize);

            if (checkboxRect.Contains(point))
            {
                if (!CurrentlySelectedFrames.Remove(frameIndex))
                {
                    CurrentlySelectedFrames.Add(frameIndex);
                }

                InvalidateVisual();
            }
            else if (point.Y <= CellHeight + TagHeight)
            {
                // select frame via SelectedFrame
                SelectedFrame = frameIndex;
                InvalidateVisual();
            }
        }
    }
}
