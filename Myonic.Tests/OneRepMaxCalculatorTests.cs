using Myonic.Core.Calculators;

namespace Myonic.Tests;

public class OneRepMaxCalculatorTests
{
    [Fact]
    public void Epley_100kg_x8()
    {
        var result = OneRepMaxCalculator.Calculate(OneRepMaxFormula.Epley, 100, 8);
        Assert.Equal(126.6667, result, 4);
    }

    [Fact]
    public void Brzycki_100kg_x8()
    {
        var result = OneRepMaxCalculator.Calculate(OneRepMaxFormula.Brzycki, 100, 8);
        Assert.Equal(124.1379, result, 4);
    }

    [Fact]
    public void OConner_100kg_x8()
    {
        var result = OneRepMaxCalculator.Calculate(OneRepMaxFormula.OConner, 100, 8);
        Assert.Equal(120.0, result, 4);
    }

    [Theory]
    [InlineData(OneRepMaxFormula.Lander, 125.1)]
    [InlineData(OneRepMaxFormula.Lombardi, 123.1)]
    [InlineData(OneRepMaxFormula.Mayhew, 126.3)]
    [InlineData(OneRepMaxFormula.Wathan, 127.7)]
    public void OtherFormulas_100kg_x8(OneRepMaxFormula formula, double expected)
    {
        var result = OneRepMaxCalculator.Calculate(formula, 100, 8);
        Assert.Equal(expected, result, 1);
    }

    [Fact]
    public void OneRep_ReturnsWeightForAllFormulas()
    {
        var result = OneRepMaxCalculator.Calculate(100, 1);

        Assert.Equal(7, result.ByFormula.Count);
        Assert.All(result.ByFormula.Values, v => Assert.Equal(100, v));
        Assert.Equal(100, result.Average);
    }

    [Fact]
    public void Average_IsArithmeticMeanOfAllFormulas()
    {
        var result = OneRepMaxCalculator.Calculate(100, 8);
        Assert.Equal(result.ByFormula.Values.Sum() / 7, result.Average, 6);
    }

    [Theory]
    [InlineData(10, false)]
    [InlineData(11, true)]
    public void LowAccuracy_FlagAboveTenReps(int reps, bool expected)
    {
        var result = OneRepMaxCalculator.Calculate(100, reps);
        Assert.Equal(expected, result.IsLowAccuracy);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-10, 5)]
    [InlineData(100, 0)]
    [InlineData(100, 13)]
    public void InvalidInput_Throws(double weight, int reps)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OneRepMaxCalculator.Calculate(weight, reps));
    }

    [Theory]
    [InlineData("Жим лёжа", true)]
    [InlineData("Жим лежа", true)]
    [InlineData("Жим штанги лёжа", true)]
    [InlineData("Жим лёжа на наклонной скамье", true)]
    [InlineData("Жим гантелей лёжа", true)]
    [InlineData("Жим", true)]
    [InlineData("Bench Press", true)]
    [InlineData("Приседания со штангой", true)]
    [InlineData("Приседания", true)]
    [InlineData("Присед", true)]
    [InlineData("Фронтальные приседания", true)]
    [InlineData("Squat", true)]
    [InlineData("Становая тяга", true)]
    [InlineData("Становая", true)]
    [InlineData("Становая тяга (сумо)", true)]
    [InlineData("Deadlift", true)]
    [InlineData("Жим ногами", false)]
    [InlineData("Французский жим", false)]
    [InlineData("Жим штанги стоя", false)]
    [InlineData("Подъём штанги на бицепс", false)]
    [InlineData("Тяга верхнего блока", false)]
    [InlineData("Тяга штанги в наклоне", false)]
    [InlineData("Румынская тяга", false)]
    [InlineData("Разгибание ног в тренажёре", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsBigThree_RecognizesPowerliftingTriad(string? exerciseName, bool expected)
    {
        Assert.Equal(expected, OneRepMaxCalculator.IsBigThree(exerciseName));
    }

    [Fact]
    public void CalculateBigThreeAverage1Rm_SingleRep_ReturnsWeight()
    {
        Assert.Equal(120.0, OneRepMaxCalculator.CalculateBigThreeAverage1Rm(120, 1));
    }

    [Fact]
    public void CalculateBigThreeAverage1Rm_FiveReps_ReturnsRoundedMean()
    {
        var avg = OneRepMaxCalculator.CalculateBigThreeAverage1Rm(100, 5);
        Assert.Equal(115.5, avg);
    }

    [Fact]
    public void CalculateBigThreeAverage1Rm_AboveMaxReps_ReturnsZero()
    {
        Assert.Equal(0.0, OneRepMaxCalculator.CalculateBigThreeAverage1Rm(100, 15));
    }

    [Theory]
    [InlineData(101.2, 100.0)]
    [InlineData(101.3, 102.5)]
    [InlineData(103.7, 102.5)]
    [InlineData(103.8, 105.0)]
    [InlineData(100.0, 100.0)]
    [InlineData(102.5, 102.5)]
    public void RoundToPlateStep_RoundsToNearest2_5(double input, double expected)
    {
        Assert.Equal(expected, OneRepMaxCalculator.RoundToPlateStep(input, 2.5));
    }
}
