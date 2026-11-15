using System.Diagnostics;
using Core;
using Core.Domain;
using Core.Reports;

// Додаткове завдання 4: час звіту на згенерованих даних. Тут немає запитів — лише вимірювання
// варіантів із Core/Reports/ReportVariants.cs.
public static class Benchmark
{
    public static void Run(int count = 100_000, int runs = 20)
    {
        var data = SampleData.Generate(count);
        Console.WriteLine($"Записів: {count:N0}, прогонів: {runs} (середнє, мс; перший прогін — прогрів)");

        Measure("A потоковий ланцюжок        ", () => ReportVariants.Streaming(data, 10), runs);
        Measure("B ToList посеред ланцюжка   ", () => ReportVariants.MidToList(data, 10), runs);
        Measure("C запит перебрано тричі     ", () => ReportVariants.UsedThrice(data, 10), runs);
        Measure("D матеріалізовано один раз  ", () => ReportVariants.Materialized(data, 10), runs);
    }

    private static void Measure<T>(string title, Func<T> report, int runs)
    {
        report(); // прогрів JIT
        GC.Collect(); // очистка пам'яті перед вимірюванням
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < runs; i++) report();
        sw.Stop();
        Console.WriteLine($"  {title} {sw.Elapsed.TotalMilliseconds / runs,8:F2} мс"); // середнє
    }
}
