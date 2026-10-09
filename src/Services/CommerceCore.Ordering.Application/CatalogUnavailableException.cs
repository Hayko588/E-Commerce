namespace CommerceCore.Ordering.Application;

public sealed class CatalogUnavailableException(
    string message,
    Exception? inner = null) : Exception(message, inner);
