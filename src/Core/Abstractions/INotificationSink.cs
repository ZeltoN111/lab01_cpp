using Core.Events;

namespace Core.Abstractions;

// Додаткове завдання 1: альтернатива події — одержувача сповіщень передають сервісу
// через конструктор. Реалізації (консоль, файл) живуть у Cli, Core про них не знає.
public interface INotificationSink
{
    void Notify(CatalogChangedEventArgs e);
}
