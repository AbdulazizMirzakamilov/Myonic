using Microsoft.Maui.Graphics;
using Myonic.Core.Models;

namespace Myonic.Drawables;

public class WeeklyVolumeBarDrawable : IDrawable
{
    public List<WeeklyVolumePoint> Points { get; set; } = [];

    public Color BarColor { get; set; } = Color.FromArgb("#C4F135"); // KineticVolt
    public Color PeakBarColor { get; set; } = Color.FromArgb("#E5FF7F");
    public Color EmptyBarColor { get; set; } = Color.FromArgb("#1F242D");
    public Color GridColor { get; set; } = Color.FromArgb("#2A313E");
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

            if (Points.Count == 0 || Points.All(p => p.VolumeKg <= 0))
            {
                DrawEmptyState(canvas, w, h);
                return;
            }

            DrawBars(canvas, w, h);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WeeklyVolumeBarDrawable] Draw error: {ex}");
        }
    }

    private void DrawEmptyState(ICanvas canvas, float w, float h)
    {
        canvas.FontColor = TextColor;
        canvas.FontSize = 13;
        canvas.DrawString("Нет данных о тренировочном объёме за этот период", 0, 0, w, h, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private void DrawBars(ICanvas canvas, float w, float h)
    {
        const float padTop = 26;
        const float padBottom = 26;
        const float padLeft = 14;
        const float padRight = 14;

        float plotW = w - padLeft - padRight;
        float plotH = h - padTop - padBottom;
        if (plotW <= 0 || plotH <= 0) return;

        double maxVol = Points.Max(p => p.VolumeKg);
        if (maxVol <= 0) maxVol = 1.0;

        // Baseline grid line
        float baselineY = h - padBottom;
        canvas.StrokeColor = GridColor;
        canvas.StrokeSize = 1;
        canvas.DrawLine(padLeft, baselineY, w - padRight, baselineY);

        int count = Points.Count;
        float gap = Math.Max(4, Math.Min(12, (plotW / count) * 0.25f));
        float totalGaps = gap * (count - 1);
        float barWidth = Math.Max(8, (plotW - totalGaps) / count);

        // Date label decimation if many points
        int labelStep = count > 10 ? 3 : (count > 6 ? 2 : 1);

        for (int i = 0; i < count; i++)
        {
            var pt = Points[i];
            float x = padLeft + i * (barWidth + gap);

            float barH = pt.VolumeKg > 0
                ? (float)((pt.VolumeKg / maxVol) * (plotH - 6))
                : 0;

            if (pt.VolumeKg > 0 && barH < 5) barH = 5;

            float y = baselineY - barH;

            if (pt.VolumeKg > 0)
            {
                bool isPeak = Math.Abs(pt.VolumeKg - maxVol) < 0.001;
                canvas.FillColor = isPeak ? PeakBarColor : BarColor;
                canvas.FillRoundedRectangle(x, y, barWidth, barH, 4);

                // Show volume label if it fits or if it is peak
                if (isPeak || count <= 6 || (i % labelStep == 0 && barWidth >= 22))
                {
                    canvas.FontColor = isPeak ? BarColor : ValueTextColor;
                    canvas.FontSize = 10;
                    string valStr = pt.VolumeKg >= 1000
                        ? $"{pt.VolumeKg / 1000.0:F1}т"
                        : $"{pt.VolumeKg:N0}";
                    canvas.DrawString(valStr, x - 10, y - 16, barWidth + 20, 14, HorizontalAlignment.Center, VerticalAlignment.Center);
                }
            }
            else
            {
                // Subtle empty bar placeholder
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
    }
}
