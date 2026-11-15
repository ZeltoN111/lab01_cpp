namespace Core.Reports;

// DTO групи: ключ групування, кількість позицій і сума залишків.
public sealed record GroupSummary(string Key, int Count, int TotalQuantity);
