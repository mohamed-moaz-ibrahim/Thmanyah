using System;
using System.Collections.Concurrent;


namespace Thmanyah.Shared.Services
{
    /// <summary>
    /// Lightweight in-memory domain event publisher. Supports Subscribe and Publish.
    /// Can be replaced with a RabbitMQ-backed publisher later.
    /// </summary>
    public class InMemoryEventPublisher : IDomainEventPublisher
    {
        private readonly ConcurrentDictionary<Type, List<Func<object, Task>>> _handlers = new();

        public void Subscribe<TEvent>(Func<TEvent, Task> handler)
        {
            var list = _handlers.GetOrAdd(typeof(TEvent), _ => new List<Func<object, Task>>());
            list.Add(evt => handler((TEvent)evt));
        }

        public async Task PublishAsync<TEvent>(TEvent evt)
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var list))
            {
                foreach (var h in list)
                {
                    try
                    {
                        await h(evt).ConfigureAwait(false);
                    }
                    catch
                    {
                        // swallow - subscribers should handle their own faults
                    }
                }
            }
        }
    }
}
