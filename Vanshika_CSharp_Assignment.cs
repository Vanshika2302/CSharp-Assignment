using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Net;
using System.Text;
using System.Text.Json;

const string ApiUrl =
    "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";
const double LowHoursThreshold = 100.0;

using var http = new HttpClient();
string json = await http.GetStringAsync(ApiUrl);

var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var entries = JsonSerializer.Deserialize<List<TimeEntry>>(json, options) ?? new List<TimeEntry>();

var totals = entries
    .Where(e => e.DeletedOn == null)
    .Where(e => !string.IsNullOrWhiteSpace(e.EmployeeName))
    .Where(e => e.EndTimeUtc >= e.StarTimeUtc)
    .GroupBy(e => e.EmployeeName!.Trim())
    .Select(g => new EmployeeTotal(g.Key, g.Sum(e => (e.EndTimeUtc - e.StarTimeUtc).TotalHours)))
    .OrderByDescending(t => t.Hours)
    .ToList();

if (totals.Count == 0)
{
    Console.WriteLine("No valid time entries were returned by the API.");
    return;
}

var html = new StringBuilder();
html.AppendLine("<!DOCTYPE html>");
html.AppendLine("<html><head><meta charset=\"utf-8\"><title>Employee Time Report</title>");
html.AppendLine("<style>");
html.AppendLine("body{font-family:Arial,sans-serif;margin:30px;}");
html.AppendLine("table{border-collapse:collapse;width:460px;}");
html.AppendLine("th,td{border:1px solid #999;padding:8px 12px;text-align:left;}");
html.AppendLine("th{background:#2f5597;color:#fff;}");
html.AppendLine("tr.low td{background:#ffc7ce;}");
html.AppendLine("</style></head><body>");
html.AppendLine("<h2>Total Time Worked per Employee</h2>");
html.AppendLine($"<p>Rows highlighted in red: less than {LowHoursThreshold:0} hours worked.</p>");
html.AppendLine("<table><tr><th>Name</th><th>Total Time Worked</th></tr>");
foreach (var t in totals)
{
    string cls = t.Hours < LowHoursThreshold ? " class=\"low\"" : "";
    var ts = TimeSpan.FromHours(t.Hours);
    string display = $"{(int)ts.TotalHours}h {ts.Minutes:00}m ({t.Hours:F2} hours)";
    html.AppendLine($"<tr{cls}><td>{WebUtility.HtmlEncode(t.Name)}</td><td>{display}</td></tr>");
}
html.AppendLine("</table></body></html>");
File.WriteAllText("EmployeeReport.html", html.ToString(), Encoding.UTF8);

double grandTotal = totals.Sum(t => t.Hours);
Color[] palette =
{
    Color.FromArgb(68,114,196),  Color.FromArgb(237,125,49),  Color.FromArgb(165,165,165),
    Color.FromArgb(255,192,0),   Color.FromArgb(91,155,213),  Color.FromArgb(112,173,71),
    Color.FromArgb(158,72,14),   Color.FromArgb(99,99,99),    Color.FromArgb(153,115,0),
    Color.FromArgb(38,68,120),   Color.FromArgb(124,175,221), Color.FromArgb(67,104,43)
};

const int width = 900, height = 600;
using (var bmp = new Bitmap(width, height))
using (var g = Graphics.FromImage(bmp))
{
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
    g.Clear(Color.White);

    using var titleFont = new Font("Arial", 16, FontStyle.Bold);
    using var legendFont = new Font("Arial", 11);
    g.DrawString("Share of Total Time Worked by Employee", titleFont, Brushes.Black, 20, 15);

    var pieRect = new Rectangle(30, 70, 480, 480);
    float startAngle = 0f;
    int legendY = 90;

    for (int i = 0; i < totals.Count; i++)
    {
        var t = totals[i];
        double share = t.Hours / grandTotal;
        float sweep = (float)(share * 360.0);
        using var brush = new SolidBrush(palette[i % palette.Length]);

        if (totals.Count == 1)
            g.FillEllipse(brush, pieRect);
        else
        {
            g.FillPie(brush, pieRect, startAngle, sweep);
            g.DrawPie(Pens.White, pieRect, startAngle, sweep);
        }

        // legend: colour box + name + percentage
        g.FillRectangle(brush, 540, legendY, 16, 16);
        g.DrawString($"{t.Name} - {share * 100:F1}%", legendFont, Brushes.Black, 565, legendY - 2);
        legendY += 28;

        startAngle += sweep;
    }

    bmp.Save("EmployeePieChart.png", ImageFormat.Png);
}

Console.WriteLine("Done. Created EmployeeReport.html and EmployeePieChart.png");
foreach (var t in totals)
    Console.WriteLine($"{t.Name,-20} {t.Hours,8:F2} h");

record TimeEntry(
    string? Id,
    string? EmployeeName,
    DateTime StarTimeUtc,  
    DateTime EndTimeUtc,
    string? EntryNotes,
    DateTime? DeletedOn);

record EmployeeTotal(string Name, double Hours);