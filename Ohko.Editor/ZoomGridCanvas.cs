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
    public static readonly StyledProperty<Bitmap?> ImageProperty =
        AvaloniaProperty.Register<ZoomGridCanvas, Bitmap?>(nameof(Image));

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<ZoomGridCanvas, double>(nameof(Zoom), 10);

    /// <summary>Grid step in image-pixel units. 1 means every pixel, 8 means every 8 pixels, etc.</summary>
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
    private readonly List<Rect> _rects = new();

    // Drag state (in image-pixel space)
    private bool _dragging;
    private Point _dragStart;
    private Point _dragCurrent;

    // Pan state
    private Vector _offset = Vector.Zero;
    private bool _panning;
    private Point _panStartPointer;   // screen space
    private Vector _panStartOffset;   // screen space

    public ZoomGridCanvas()
    {
        // Important: make sure the control actually receives pointer events.
        // If a control has no background/filled area it can be “hit-test invisible” in some cases;
        // easiest is to have a (transparent) background via styling or just rely on Control hit testing.
        // (If you implement this as a Panel, set Background=Transparent.)  [oai_citation:3‡GitHub](https://github.com/AvaloniaUI/Avalonia/discussions/9794?utm_source=chatgpt.com)
        AffectsRender<ZoomGridCanvas>(ImageProperty, ZoomProperty, GridStepProperty, MinZoomProperty, MaxZoomProperty);
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var img = Image;
        if (img is null || Zoom <= 0) return;

        using (context.PushTransform(Matrix.CreateTranslation(_offset.X, _offset.Y)))
        {
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

    private void DrawGrid(DrawingContext context, int imgW, int imgH)
    {
        var step = Math.Max(1, GridStep);
        var z = Zoom;

        // Only draw if grid lines will be at least ~3px apart (optional perf/visual tweak)
        if (step * z < 3) return;

        var pen = new Pen(Brushes.Gray, 1);

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

        foreach (var r in _rects)
        {
            var sr = ToScreenRect(r);
            context.DrawRectangle(null, pen, sr);
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
            e.Handled = true;
            return;
        }

        if (_dragging)
        {
            _dragCurrent = SnapToGrid(ToImagePoint(e.GetPosition(this)));
            InvalidateVisual();
            e.Handled = true;
        }
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
                _rects.Add(r);
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
}