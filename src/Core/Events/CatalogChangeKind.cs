namespace Core.Events;

// Вид зміни стану складу. Значення передається явно після кожної успішної операції.
public enum CatalogChangeKind { Added, Received, Issued, Removed }
