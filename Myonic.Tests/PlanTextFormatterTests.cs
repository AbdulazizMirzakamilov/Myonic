using Myonic.Core.Formatting;

namespace Myonic.Tests;

public class PlanTextFormatterTests
{
    [Fact]
    public void Format_ExactValues()
    {
        var text = PlanTextFormatter.Format(3, 10, null, 80, 8.0, null);
        Assert.Equal("3 × 10, 80 кг @ 8", text);
    }

    [Fact]
    public void Format_WithRepAndRpeRanges()
    {
        var text = PlanTextFormatter.Format(4, 8, 12, 100, 7.5, 8.5);
        Assert.Equal("4 × 8–12, 100 кг @ 7.5–8.5", text);
    }

    [Fact]
    public void Format_MinimalPlan()
    {
        var text = PlanTextFormatter.Format(3, 12, null, null, null, null);
        Assert.Equal("3 × 12", text);
    }
}
