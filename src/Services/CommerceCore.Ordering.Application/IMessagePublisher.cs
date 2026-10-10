namespace CommerceCore.Ordering.Application;

// The application layer's only view of messaging. The implementation lives in Infrastructure.
public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
}
