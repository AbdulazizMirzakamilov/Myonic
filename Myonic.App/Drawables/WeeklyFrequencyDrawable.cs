using Microsoft.Maui.Graphics;
using Myonic.Core.Models;

namespace Myonic.Drawables;

public class WeeklyFrequencyDrawable : IDrawable
{
    public List<WeeklyVolumePoint> Points { get; set; } = [];

    public Color BarColor { get; set; } = Color.FromArgb("#38BDF8"); // KineticIceBlue
    public Color EmptyBarColor { get; set; } = Color.FromArgb("#1F242D");
    public Color GridColor { get; set; } = Color.FromArgb("#2A313E");
    public Color AvgLineColor { get; set; } = Color.FromArgb("#C4F135"); // KineticVolt
    public Color TextColor { get; set; } = Color.FromArgb("#94A3B8");
    public Color ValueTextColor { get; set; } = Color.FromArgb("#FFFFFF");

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        try
        {
            float w = dirtyRect.Width;
            float h = dirtyRect.Height;

            if (w <= 0 || h <= 0) return;

            canvas.Antialias = true;
            canvas.Font = Microsoft.Maui.Graphics.Font.Default;

            if (Points.Count == 0 || Points.All(p => p.WorkoutsCount <= 0))
            {
                DrawEmptyState(canvas, w, h);
                return;
            }

            DrawFrequency(canvas, w, h);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WeeklyFrequencyDrawable] Draw error: {ex}");
        }
    }

    private void DrawEmptyState(ICanvas canvas, float w, float h)
    {
        canvas.FontColor = TextColor;
        canvas.FontSize = 13;
        canvas.DrawString("Нет данных о частоте тренировок за этот период", 0, 0, w, h, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private void DrawFrequency(ICanvas canvas, float w, float h)
    {
        const float padTop = 24;
        const float padBottom = 26;
        const float padLeft = 14;
        const float padRight = 14;

        float plotW = w - padLeft - padRight;
        float plotH = h - padTop - padBottom;
        if (plotW <= 0 || plotH <= 0) return;

        int maxCount = Points.Max(p => p.WorkoutsCount);
        if (maxCount < 4) maxCount = 4; // visual headroom for at least 4/week

        double avgCount = Points.Count > 0 ? Points.Average(p => p.WorkoutsCount) : 0;

        float baselineY = h - padBottom;
        canvas.StrokeColor = GridColor;
        canvas.StrokeSize = 1;
        canvas.DrawLine(padLeft, baselineY, w - padRight, baselineY);

        int count = Points.Count;
        float gap = Math.Max(4, Math.Min(12, (plotW / count) * 0.25f));
        float totalGaps = gap * (count - 1);
        float barWidth = Math.Max(8, (plotW - totalGaps) / count);

        int labelStep = count > 10 ? 3 : (count > 6 ? 2 : 1);

        for (int i = 0; i < count; i++)
        {
            var pt = Points[i];
            float x = padLeft + i * (barWidth + gap);

            float barH = pt.WorkoutsCount > 0
                ? (float)((pt.WorkoutsCount / (double)maxCount) * (plotH - 6))
                : 0;

            if (pt.WorkoutsCount > 0 && barH < 6) barH = 6;
            float y = baselineY - barH;

            if (pt.WorkoutsCount > 0)
            {
                canvas.FillColor = BarColor;
                canvas.FillRoundedRectangle(x, y, barWidth, barH, 4);

                // Count label above bar
                canvas.FontColor = ValueTextColor;
                canvas.FontSize = 10;
                canvas.DrawString(pt.WorkoutsCount.ToString(), x - 8, y - 15, barWidth + 16, 14, HorizontalAlignment.Center, VerticalAlignment.Center);
            }
            else
            {
                canvas.FillColor = EmptyBarColor;
                canvas.FillRoundedRectangle(x, baselineY - 4, barWidth, 4, 2);
            }

            // X-axis date label
            if (i % labelStep == 0 || i == count - 1)
            {
                canvas.FontColor = TextColor;
                canvas.FontSize = 10;
                canvas.DrawString(pt.Label, x - 12, baselineY + 6, barWidth + 24, 16, HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }

        // Draw Average dashed reference line if average > 0
        if (avgCount > 0)
        {
            float avgY = baselineY - (float)((avgCount / maxCount) * (plotH - 6));
            canvas.StrokeColor = AvgLineColor;
            canvas.StrokeSize = 1.5f;
            canvas.StrokeDashPattern = [4, 4];
            canvas.DrawLine(padLeft, avgY, w - padRight, avgY);
            canvas.StrokeDashPattern = null;

            canvas.FontColor = AvgLineColor;
            canvas.FontSize = 9;
            canvas.DrawString($"Ср: {avgCount:F1}/нед", padLeft + 4, avgY - 14, HorizontalAlignment.Left);
        }
    }
}
