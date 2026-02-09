using System;
using System.Threading.Tasks;

namespace Thmanyah.Shared.Services
{
    public interface IDomainEventPublisher
    {
        Task PublishAsync<TEvent>(TEvent evt);

        void Subscribe<TEvent>(Func<TEvent, System.Threading.Tasks.Task> handler);
    }
}
