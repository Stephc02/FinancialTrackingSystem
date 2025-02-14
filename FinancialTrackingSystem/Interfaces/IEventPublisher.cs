namespace FinancialTrackingSystem.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishEventAsync(string eventName, object eventData);
    }

}
