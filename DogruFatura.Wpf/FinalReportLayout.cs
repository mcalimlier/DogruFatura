namespace DogruFatura.Wpf;

/// <summary>
/// Nihai raporun WPF önizlemesi ile PDF çıktısında kullanılan ortak fiziksel ölçüler.
/// Temel değerler PDF point (72 dpi) cinsindedir; WPF tarafı 96 dpi DIP'e dönüştürülür.
/// </summary>
public static class FinalReportLayout
{
    public const double PointsPerInch = 72.0;
    public const double DipsPerInch = 96.0;
    public const double CentimetersPerInch = 2.54;

    public const double SideMarginCentimeters = 0.75;
    public static double SideMarginPoints => CentimetersToPoints(SideMarginCentimeters);
    public static double SideMarginDips => PointsToDips(SideMarginPoints);

    public const double TableTopPoints = 58.5;
    public const double HeaderHeightPoints = 27.0;
    public const double RowHeightPoints = 20.25;
    public const double TotalHeightPoints = 23.25;

    public static double TableTopDips => PointsToDips(TableTopPoints);
    public static double HeaderHeightDips => PointsToDips(HeaderHeightPoints);
    public static double RowHeightDips => PointsToDips(RowHeightPoints);
    public static double TotalHeightDips => PointsToDips(TotalHeightPoints);

    public static readonly double[] ColumnWeights =
        { 4.43, 24.86, 22.57, 17.86, 13.29, 18.29, 16.57, 22.14, 14.86, 14.29 };

    public static readonly string[] Headers =
    {
        "No", "Okul Adı", "Hizmet Numarası", "Fatura No", "Fatura Tarihi",
        "Son Ödeme Tarihi", "Ödenecek Tutar", "KDV Tutarı", "Damga Vergisi", "Ödeme Tutarı"
    };

    public static double[] CalculateColumnWidths(double tableWidth)
    {
        var totalWeight = ColumnWeights.Sum();
        return ColumnWeights.Select(weight => tableWidth * weight / totalWeight).ToArray();
    }

    public static double CentimetersToPoints(double centimeters) =>
        centimeters / CentimetersPerInch * PointsPerInch;

    public static double PointsToDips(double points) =>
        points / PointsPerInch * DipsPerInch;
}
