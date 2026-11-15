namespace Core;

// Технічна помилка джерела даних (зіпсований JSON тощо). Не доменна: правила складу не порушено,
// просто файл неможливо прочитати. Початковий виняток зберігається в InnerException.
public sealed class StorageException : Exception
{
    public StorageException(string message, Exception inner) : base(message, inner) { }
}
