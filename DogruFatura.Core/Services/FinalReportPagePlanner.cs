namespace DogruFatura.Core.Services;

public sealed record FinalReportPageSlice(
    int StartIndex,
    int Count,
    bool IsFinalPage);

public static class FinalReportPagePlanner
{
    public const int RegularPageCapacity = 19;
    public const int FinalPageCapacity = 12;

    public static IReadOnlyList<FinalReportPageSlice> Plan(int itemCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(itemCount);
        if (itemCount == 0)
            return Array.Empty<FinalReportPageSlice>();

        var pageCount = itemCount <= FinalPageCapacity
            ? 1
            : (int)Math.Ceiling((itemCount - FinalPageCapacity) / (double)RegularPageCapacity) + 1;

        var finalCount = Math.Min(FinalPageCapacity, (int)Math.Ceiling(itemCount / (double)pageCount));
        var remaining = itemCount - finalCount;
        var regularPageCount = pageCount - 1;
        var slices = new List<FinalReportPageSlice>(pageCount);
        var startIndex = 0;

        for (var pageIndex = 0; pageIndex < regularPageCount; pageIndex++)
        {
            var pagesLeft = regularPageCount - pageIndex;
            var count = (int)Math.Ceiling(remaining / (double)pagesLeft);
            if (count > RegularPageCapacity)
                throw new InvalidOperationException("Nihai çıktı sayfalama kapasitesi aşıldı.");

            slices.Add(new FinalReportPageSlice(startIndex, count, false));
            startIndex += count;
            remaining -= count;
        }

        slices.Add(new FinalReportPageSlice(startIndex, finalCount, true));
        return slices;
    }
}
