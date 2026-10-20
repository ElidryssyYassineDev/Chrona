namespace Chrona.Shared.Application;

public interface IDomainEvent { }

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent);
}

public interface IDomainEventPublisher
{
    Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent;
}

// Publication synchrone en processus (03-module-design.md, §5) : pas de broker, pas d'outbox en v1.
public sealed class InProcessDomainEventPublisher(IServiceProvider services) : IDomainEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        var handlers = services.GetService(typeof(IEnumerable<IDomainEventHandler<TEvent>>))
            as IEnumerable<IDomainEventHandler<TEvent>> ?? [];

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(domainEvent);
        }
    }
}

public sealed class NoOpDomainEventPublisher : IDomainEventPublisher
{
    public static readonly NoOpDomainEventPublisher Instance = new();

    public Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent => Task.CompletedTask;
}