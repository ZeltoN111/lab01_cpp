namespace Core.Reports;

// Додаткове завдання 1: рух по товару (запис журналу переміщень) і зведення по товару.
public sealed record StockMovement(string ProductId, int Delta);

public sealed record MovementLine(string Sku, int Delta);

public sealed record MovementSummary(string Sku, string Name, int Moves, int NetDelta);
