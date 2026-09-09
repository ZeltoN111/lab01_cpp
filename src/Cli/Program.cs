using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;

var info = new
{
    OsDescription = RuntimeInformation.OSDescription,
    OsVersion = Environment.OSVersion.ToString(),
    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
    ClrVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    BaseDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Склад (товари, партії, залишки, переміщення)"
};

if (args.Contains("--json"))
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    Console.WriteLine(JsonSerializer.Serialize(info, options));
}
else
{
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine("Студент: Сухар Роман, група ФеІ-36");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС (OSDescription) : {info.OsDescription}");
    Console.WriteLine($"ОС (Environment) : {info.OsVersion}");
    Console.WriteLine($"Архітектура процесу : {info.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR) : {info.ClrVersion}");
    Console.WriteLine($"Runtime : {info.Runtime}");
    Console.WriteLine($"Каталог застосунку : {info.BaseDirectory}");
    Console.WriteLine($"Поточний каталог : {info.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область: {info.Domain}");
}