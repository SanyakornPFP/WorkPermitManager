using System.Globalization;
public static class DateHelper
{
    public static string ToThaiDate(this DateTime date)
    {
        var thaiCulture = new CultureInfo("th-TH");
        var thaiDate = date.ToString("dd 'เดือน' MMMM 'พ.ศ.' yyyy", thaiCulture);
        var thaiYear = date.Year + 543; // Convert to Buddhist Era
        return thaiDate.Replace(date.Year.ToString(), thaiYear.ToString());
    }
}