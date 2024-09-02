namespace ZEA.Architecture.DDD.Interfaces;

/// <summary>
/// Represents a domain event that is used for indicating state changes within a domain (aka. Service).
/// </summary>
public interface IDomainEvent : IEvent
{
	string EventName { get; }
}