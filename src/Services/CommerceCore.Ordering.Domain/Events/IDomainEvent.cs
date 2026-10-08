namespace CommerceCore.Ordering.Domain.Events
{
    public interface IDomainEvent
    {
        DateTime OccurredOnUtc { get; }
    }
}
