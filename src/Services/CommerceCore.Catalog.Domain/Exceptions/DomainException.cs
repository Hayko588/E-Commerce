namespace CommerceCore.Catalog.Domain.Exceptions;

public class DomainException(string message) : Exception(message);

public sealed class ConflictException(string message) : DomainException(message);