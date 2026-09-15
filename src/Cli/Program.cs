using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Core;

EnvironmentReport report = EnvironmentInfo.Collect();

if (args.Contains("--json", StringComparer.OrdinalIgnoreCase))
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
    };

    Console.WriteLine(JsonSerializer.Serialize(report, options));
}
else
{
    Console.WriteLine("CrossApp – інформація про середовище");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС : {report.OsDescription}");
    Console.WriteLine($"Runtime : {report.FrameworkDescription}");
    Console.WriteLine($"Архітектура : {report.ProcessArchitecture}");
    Console.WriteLine($"RID (визначено): {report.DetectedRid}");
    Console.WriteLine($"RID (від .NET) : {report.ReportedRid}");
    Console.WriteLine($"Каталог : {report.BaseDirectory}");
    Console.WriteLine($"Збірка : {report.BuildNote}");
}
