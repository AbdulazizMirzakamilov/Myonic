namespace Myonic.Core.Models;

public class BodyMeasurement
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public double WeightKg { get; set; }
    public double? BodyFatPercentage { get; set; }
    public double? NeckCm { get; set; }
    public double? ChestCm { get; set; }
    public double? WaistCm { get; set; }
    public double? HipsCm { get; set; }
    public double? BicepsCm { get; set; }
    public double? ThighCm { get; set; }
    public double? CalfCm { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public record WeightTrendStats(
    double CurrentWeight,
    double StartWeight,
    double DeltaKg,
    double MinWeight,
    double MaxWeight,
    int MeasurementsCount);
