namespace Core.Reports;

// DTO рядка звіту: контракт звіту не залежить від внутрішньої структури Product.
public sealed record StockLine(string Sku, string Name, int Quantity, string Unit);
