using Core.Reports;

namespace Core.Tests;

public class ReportServiceTests
{
    [Theory] // 3) звіт на кількох наборах
    [InlineData(10, 2)]
    [InlineData(4, 1)]
    [InlineData(1, 0)]
    public void LowStock_CountsOnlyBelowThreshold(int threshold, int expected) =>
        Assert.Equal(expected, ReportService.LowStock(TestData.Items(), threshold).Count);

    [Fact] // 4) детермінований топ-N з тижня 7: SKU-MID і SKU-MIX рівні, вирішує другий ключ
    public void TopByQuantity_IsDeterministic() =>
        Assert.Equal(new[] { "SKU-BIG", "SKU-MID" },
            ReportService.TopByQuantity(TestData.Items(), 2).Select(l => l.Sku));

    [Fact] // порядок джерела не впливає на результат
    public void TopByQuantity_DoesNotDependOnSourceOrder() =>
        Assert.Equal(ReportService.TopByQuantity(TestData.Items(), 3),
            ReportService.TopByQuantity(TestData.Items().Reverse(), 3));

    [Fact]
    public void Totals_OnEmptySource_ReturnsZeros() =>
        Assert.Equal((0, 0, 0d, 0), ReportService.Totals([]));
}
