namespace Myonic.Drawables;

public record WeightDataPoint(DateTime Date, double Weight);

public class WeightTrendDrawable : IDrawable
{
    public List<WeightDataPoint> DataPoints { get; set; } = [];
    public Color LineColor { get; set; } = Color.FromArgb("#C4F135");
    public Color FillColor { get; set; } = Color.FromArgb("#1AC4F135");
    public Color GridColor { get; set; } = Color.FromArgb("#2AFFFFFF");
    public Color TextColor { get; set; } = Color.FromArgb("#80FFFFFF");
    public Color DotColor { get; set; } = Color.FromArgb("#C4F135");
    public Color DotCoreColor { get; set; } = Colors.White;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var w = dirtyRect.Width;
        var h = dirtyRect.Height;

        if (w <= 0 || h <= 0) return;

        canvas.Antialias = true;

        if (DataPoints.Count == 0)
        {
            DrawEmptyState(canvas, w, h);
            return;
        }

        if (DataPoints.Count == 1)
        {
            DrawSinglePoint(canvas, w, h);
            return;
        }

        DrawChart(canvas, w, h);
    }

    private void DrawEmptyState(ICanvas canvas, float w, float h)
    {
        canvas.FontColor = TextColor;
        canvas.FontSize = 13;
        canvas.DrawString("Добавьте первый замер веса", w / 2, h / 2, HorizontalAlignment.Center);
    }

    private void DrawSinglePoint(ICanvas canvas, float w, float h)
    {
        var pt = DataPoints[0];

        // Horizontal reference line
        float y = h / 2;
        canvas.StrokeColor = GridColor;
        canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = [4, 4];
        canvas.DrawLine(0, y, w, y);
        canvas.StrokeDashPattern = null;

        // Weight label
        canvas.FontColor = TextColor;
        canvas.FontSize = 11;
        canvas.DrawString($"{pt.Weight:F1} кг", 4, y - 14, HorizontalAlignment.Left);

        // Dot
        float cx = w / 2;
        DrawDot(canvas, cx, y, 6);
    }

    private void DrawChart(ICanvas canvas, float w, float h)
    {
        const float padTop = 20;
        const float padBottom = 24;
        const float padLeft = 40;
        const float padRight = 12;

        float plotW = w - padLeft - padRight;
        float plotH = h - padTop - padBottom;

        if (plotW <= 0 || plotH <= 0) return;

        var sorted = DataPoints.OrderBy(p => p.Date).ToList();

        double minW = sorted.Min(p => p.Weight);
        double maxW = sorted.Max(p => p.Weight);

        // Add padding so the line doesn't touch edges
        double range = maxW - minW;
        if (range < 0.5) range = 2.0; // at least 2 kg visual range
        double padKg = range * 0.15;
        double yMin = minW - padKg;
        double yMax = maxW + padKg;
        double yRange = yMax - yMin;

        var minDate = sorted.First().Date;
        var maxDate = sorted.Last().Date;
        double totalDays = Math.Max((maxDate - minDate).TotalDays, 1);

        // Map data to pixel coordinates
        float ToX(DateTime d) => padLeft + (float)(((d - minDate).TotalDays / totalDays) * plotW);
        float ToY(double weight) => padTop + plotH - (float)(((weight - yMin) / yRange) * plotH);

        // Draw horizontal grid lines (min and max)
        DrawGridLine(canvas, padLeft, w - padRight, ToY(minW), $"{minW:F1}");
        DrawGridLine(canvas, padLeft, w - padRight, ToY(maxW), $"{maxW:F1}");

        // Build path points
        var points = sorted.Select(p => new PointF(ToX(p.Date), ToY(p.Weight))).ToList();

        // Draw gradient fill under the line
        DrawGradientFill(canvas, points, padTop + plotH, padLeft, w - padRight);

        // Draw the main line
        canvas.StrokeColor = LineColor;
        canvas.StrokeSize = 2.5f;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        var path = new PathF();
        path.MoveTo(points[0]);
        for (int i = 1; i < points.Count; i++)
        {
            path.LineTo(points[i]);
        }
        canvas.DrawPath(path);

        // Draw dots on data points
        foreach (var pt in points)
        {
            DrawDot(canvas, pt.X, pt.Y, 4.5f);
        }
    }

    private void DrawGridLine(ICanvas canvas, float x1, float x2, float y, string label)
    {
        canvas.StrokeColor = GridColor;
        canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = [4, 4];
        canvas.DrawLine(x1, y, x2, y);
        canvas.StrokeDashPattern = null;

        canvas.FontColor = TextColor;
        canvas.FontSize = 10;
        canvas.DrawString(label, 2, y - 6, HorizontalAlignment.Left);
    }

    private void DrawGradientFill(ICanvas canvas, List<PointF> points, float bottomY, float leftX, float rightX)
    {
        if (points.Count < 2) return;

        var fillPath = new PathF();
        fillPath.MoveTo(points[0].X, bottomY);
        foreach (var pt in points)
        {
            fillPath.LineTo(pt);
        }
        fillPath.LineTo(points.Last().X, bottomY);
        fillPath.Close();

        var minY = points.Min(p => p.Y);
        var paint = new LinearGradientPaint
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops =
            [
                new PaintGradientStop(0.0f, FillColor),
                new PaintGradientStop(1.0f, Colors.Transparent)
            ]
        };
        canvas.SetFillPaint(paint, new RectF(leftX, minY, rightX - leftX, bottomY - minY));

        canvas.FillPath(fillPath);
    }

    private void DrawDot(ICanvas canvas, float cx, float cy, float radius)
    {
        // Outer glow
        canvas.FillColor = DotColor;
        canvas.FillCircle(cx, cy, radius);

        // Inner core
        canvas.FillColor = DotCoreColor;
        canvas.FillCircle(cx, cy, radius * 0.45f);
    }
}
