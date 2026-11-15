namespace Core;

// Порушення бізнес-правила складу (кількість 0, видача більша за залишок, недозволений перехід стану).
// Некоректні аргументи лишаються ArgumentException, технічні збої — IOException/StorageException.
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}
