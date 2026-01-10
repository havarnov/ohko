using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Ohko.Editor;

public sealed class ZoomGridCanvas : Control
{
    public static readonly StyledProperty<EditorModel> EditorModelProperty =
        AvaloniaProperty.Register<ZoomGridCanvas, EditorModel>(nameof(EditorModel));

    public static readonly StyledProperty<UserDataModel> UserDataModelProperty =
        AvaloniaProperty.Register<ZoomGridCanvas, UserDataModel>(nameof(UserDataModel));

    public static readonly StyledProperty<Bitmap?> ImageProperty =
        AvaloniaProperty.Register<ZoomGridCanvas, Bitmap?>(nameof(Image));

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<ZoomGridCanvas, double>(nameof(Zoom), 10);

    public static readonly StyledProperty<IEnumerable<RectangleModel>> RectanglesProperty =
        AvaloniaProperty.Register<ZoomGridCanvas, IEnumerable<RectangleModel>>(nameof(Rectangles), []);

    public static readonly StyledProperty<int> GridStepProperty =
        AvaloniaProperty.Register<ZoomGridCanvas, int>(nameof(GridStep), 1);

    public static readonly StyledProperty<double> MinZoomProperty =
        AvaloniaProperty.Register<ZoomGridCanvas, double>(nameof(MinZoom), 10);

    public static readonly StyledProperty<double> MaxZoomProperty =
        AvaloniaProperty.Register<ZoomGridCanvas, double>(nameof(MaxZoom), 80);

    public double MinZoom
    {
        get => GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public double MaxZoom
    {
        get => GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    public EditorModel EditorModel
    {
        get => GetValue(EditorModelProperty);
        set => SetValue(EditorModelProperty, value);
    }

    public UserDataModel UserDataModel
    {
        get => GetValue(UserDataModelProperty);
        set => SetValue(UserDataModelProperty, value);
    }

    public IEnumerable<RectangleModel> Rectangles
    {
        get => GetValue(RectanglesProperty);
        set => SetValue(RectanglesProperty, value);
    }

    public Bitmap? Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public int GridStep
    {
        get => GetValue(GridStepProperty);
        set => SetValue(GridStepProperty, value);
    }

    // Rectangles stored in IMAGE-PIXEL space (logical coords)
    // private readonly List<Rect> _rects = new();

    // Drag state (in image-pixel space)
    private bool _dragging;
    private Point _dragStart;
    private Point _dragCurrent;

    // Pan state
    private Vector _offset = Vector.Zero;
    private bool _panning;
    private Point _panStartPointer;   // screen space
    private Vector _panStartOffset;   // screen space

    private readonly IBrush _tileA = new SolidColorBrush(Color.FromArgb(255, 192, 192, 192));   // dark gray
    private readonly IBrush _tileB = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));   // slightly lighter

    public ZoomGridCanvas()
    {
        AffectsRender<ZoomGridCanvas>(ImageProperty, ZoomProperty, GridStepProperty, MinZoomProperty, MaxZoomProperty, RectanglesProperty, UserDataModelProperty);
        AffectsMeasure<ZoomGridCanvas>(ImageProperty, ZoomProperty);
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Image == null)
            return new Size(0, 0);

        var imgW = Image.PixelSize.Width * Zoom;
        var imgH = Image.PixelSize.Height * Zoom;

        // Compute bounding box including panning
        // If _offset.X < 0, image is shifted left → control must extend right
        // If _offset.X > 0, image is shifted right → control must extend right to include offset

        double width = imgW + Math.Max(0, _offset.X);      // extend right if panned right
        double height = imgH + Math.Max(0, _offset.Y);     // extend down if panned down

        // If _offset.X is negative, the image goes left → extend left (for layout, can't have negative size)
        double left = Math.Max(0, -_offset.X);
        double top = Math.Max(0, -_offset.Y);

        width += left;
        height += top;

        return new Size(width, height);
    }


    protected override Size ArrangeOverride(Size finalSize)
    {
        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var img = Image;
        if (img is null || Zoom <= 0) return;

        using (context.PushTransform(Matrix.CreateTranslation(_offset.X, _offset.Y)))
        {
            DrawCheckeredBackground(context);

            var destW = img.PixelSize.Width * Zoom;
            var destH = img.PixelSize.Height * Zoom;

            var destRect = new Rect(0, 0, destW, destH);
            var srcRect = new Rect(0, 0, img.PixelSize.Width, img.PixelSize.Height);

            context.DrawImage(img, srcRect, destRect);

            DrawGrid(context, img.PixelSize.Width, img.PixelSize.Height);
            DrawRects(context);

            if (_dragging)
            {
                DrawDragPreview(context);
            }
        }
    }

    private void DrawCheckeredBackground(DrawingContext context)
    {
        if (Image == null)
            return;

        const int tileSize = 16;
        double tileSizeScreen = tileSize * Zoom;

        int imgW = Image.PixelSize.Width;
        int imgH = Image.PixelSize.Height;

        var imageRect = new Rect(0, 0, imgW * Zoom, imgH * Zoom);

        using (context.PushClip(imageRect))
        {
            int tilesX = imgW / tileSize + 1;
            int tilesY = imgH / tileSize + 1;

            for (int y = 0; y < tilesY; y++)
            {
                for (int x = 0; x < tilesX; x++)
                {
                    var tile = ((x + y) % 2 == 0) ? _tileA : _tileB;

                    var rect = new Rect(
                        x * tileSizeScreen,
                        y * tileSizeScreen,
                        tileSizeScreen,
                        tileSizeScreen);

                    context.FillRectangle(tile, rect);
                }
            }
        }
    }

    private void DrawGrid(DrawingContext context, int imgW, int imgH)
    {
        var step = Math.Max(1, GridStep);
        var z = Zoom;

        // Only draw if grid lines will be at least ~3px apart (optional perf/visual tweak)
        if (step * z < 3) return;

        var pen = new Pen(Brushes.LightGray, 1);

        // Vertical lines
        for (int x = 0; x <= imgW; x += step)
        {
            var sx = x * z;
            context.DrawLine(pen, new Point(sx, 0), new Point(sx, imgH * z));
        }

        // Horizontal lines
        for (int y = 0; y <= imgH; y += step)
        {
            var sy = y * z;
            context.DrawLine(pen, new Point(0, sy), new Point(imgW * z, sy));
        }
    }

    private void DrawRects(DrawingContext context)
    {
        var pen = new Pen(Brushes.Lime, 2);
        var selectedPen = new Pen(Brushes.Lime, 4);

        foreach (var r in Rectangles)
        {
            var sr = ToScreenRect(r.Rect);
            context.DrawRectangle(
                null,
                ReferenceEquals(r.UserDataModel, EditorModel.SelectedUserDataModel)
                    ? selectedPen
                    : pen,
                sr);
        }
    }

    private void DrawDragPreview(DrawingContext context)
    {
        var pen = new Pen(Brushes.Yellow, 2);

        var a = _dragStart;
        var b = _dragCurrent;
        var r = MakeRect(a, b);

        context.DrawRectangle(null, pen, ToScreenRect(r));
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        // Wheel up => zoom in, wheel down => zoom out
        var oldZoom = Zoom;

        // Smooth-ish zoom steps
        var factor = e.Delta.Y > 0 ? 1.1 : 1.0 / 1.1;
        var newZoom = Math.Clamp(oldZoom * factor, MinZoom, MaxZoom);

        if (Math.Abs(newZoom - oldZoom) < 0.0001)
        {
            return;
        }

        var cursor = e.GetPosition(this); // SCREEN space point

        // Keep the point under the cursor fixed:
        // screen = offset + image * zoom
        // imageUnderCursor = (cursor - offset) / oldZoom
        // newOffset = cursor - imageUnderCursor * newZoom
        var imageUnderCursor = (cursor - _offset) / oldZoom;
        _offset = cursor - (imageUnderCursor * newZoom);

        Zoom = newZoom;
        InvalidateVisual();
        InvalidateMeasure();

        e.Handled = true;
    }

    public void UpdateZoom(bool zoomIn)
    {
        // Wheel up => zoom in, wheel down => zoom out
        var oldZoom = Zoom;

        // Smooth-ish zoom steps
        var factor = zoomIn ? 1.1 : 1.0 / 1.1;
        var newZoom = Math.Clamp(oldZoom * factor, MinZoom, MaxZoom);

        if (Math.Abs(newZoom - oldZoom) < 0.0001)
        {
            return;
        }

        Zoom = newZoom;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var pt = e.GetCurrentPoint(this);

        if (pt.Properties.IsMiddleButtonPressed)
        {
            _panning = true;
            _panStartPointer = e.GetPosition(this); // screen space
            _panStartOffset = _offset;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (pt.Properties.IsLeftButtonPressed)
        {
            _dragging = true;
            _dragStart = SnapToGrid(ToImagePoint(e.GetPosition(this)));
            _dragCurrent = _dragStart;
            e.Pointer.Capture(this);
            InvalidateVisual();
            e.Handled = true;
        }

        var position = e.GetPosition(this) - _offset;
        foreach (var rect in Rectangles)
        {
            var (outer, inner) = GetOuterInner(rect.Rect);
            if (outer.Contains(position) && !inner.Contains(position))
            {
                UserDataModel = rect.UserDataModel;
                break;
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_panning)
        {
            var now = e.GetPosition(this); // screen space
            var delta = now - _panStartPointer;
            _offset = _panStartOffset + delta;
            InvalidateVisual();
            InvalidateMeasure();
            e.Handled = true;
            return;
        }

        if (_dragging)
        {
            _dragCurrent = SnapToGrid(ToImagePoint(e.GetPosition(this)));
            InvalidateVisual();
            e.Handled = true;
        }

        var position = e.GetPosition(this) - _offset;

        bool reset = true;

        foreach (var rect in Rectangles)
        {
            var (outer, inner) = GetOuterInner(rect.Rect);
            if (outer.Contains(position) && !inner.Contains(position))
            {
                reset = false;
                Cursor = new Cursor(StandardCursorType.Hand);
                break;
            }
        }

        if (reset)
        {
            Cursor = Cursor.Default;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ClipToBounds = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_panning)
        {
            _panning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        if (_dragging)
        {
            _dragging = false;
            e.Pointer.Capture(null);

            var r = MakeRect(_dragStart, _dragCurrent);
            if (r.Width > 0 && r.Height > 0)
            {
                EditorModel.AddRect(r);
            }

            InvalidateVisual();
            e.Handled = true;
        }
    }

    // --- Coordinate helpers ---

    private Point ToImagePoint(Point screenPoint)
        => new Point((screenPoint.X - _offset.X) / Zoom, (screenPoint.Y - _offset.Y) / Zoom);

    private Rect ToScreenRect(Rect imageRect)
        => new Rect(
            imageRect.X * Zoom,
            imageRect.Y * Zoom,
            imageRect.Width * Zoom,
            imageRect.Height * Zoom);

    private Point SnapToGrid(Point imagePoint)
    {
        var step = Math.Max(1, GridStep);
        double sx = Math.Round(imagePoint.X / step) * step;
        double sy = Math.Round(imagePoint.Y / step) * step;

        if (sx < 0)
        {
            sx = 0;
        }

        if (sy < 0)
        {
            sy = 0;
        }

        if (sx > Image?.PixelSize.Width)
        {
            sx = Image.PixelSize.Width;
        }

        if (sy > Image?.PixelSize.Height)
        {
            sy = Image.PixelSize.Height;
        }

        return new Point(sx, sy);
    }

    private static Rect MakeRect(Point a, Point b)
    {
        var x1 = Math.Min(a.X, b.X);
        var y1 = Math.Min(a.Y, b.Y);
        var x2 = Math.Max(a.X, b.X);
        var y2 = Math.Max(a.Y, b.Y);
        return new Rect(x1, y1, x2 - x1, y2 - y1);
    }

    private (Rect outer, Rect inner) GetOuterInner(Rect imageRect)
    {
        var screenRect = ToScreenRect(imageRect);
        var outer = new Rect(screenRect.TopLeft + new Point(-10, -10),
            screenRect.BottomRight + new Point(10, 10));
        var inner = new Rect(screenRect.TopLeft + new Point(10, 10),
            screenRect.BottomRight + new Point(-10, -10));

        return (outer, inner);
    }
}