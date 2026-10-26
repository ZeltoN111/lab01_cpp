using Core.Abstractions;
using Core.Events;

// Додаткове завдання 1: одержувачі сповіщень як реалізації INotificationSink.
// Вони живуть у Cli — Core не містить ні Console, ні роботи з лог-файлом.
public static class LogFormat
{
    public static string Line(CatalogChangedEventArgs e) =>
        $"{e.At:HH:mm:ss} {e.Kind,-8} {e.Id} {e.Delta,4:+0;-0} → залишок {e.QuantityAfter}";
}

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(CatalogChangedEventArgs e) => Console.WriteLine(LogFormat.Line(e));
}

public sealed class FileSink(string path) : INotificationSink
{
    public void Notify(CatalogChangedEventArgs e)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path, LogFormat.Line(e) + Environment.NewLine);
    }
}
