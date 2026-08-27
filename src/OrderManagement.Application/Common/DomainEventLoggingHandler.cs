using Mediator;
using Microsoft.Extensions.Logging;

namespace OrderManagement.Application.Common;

/// <summary>
/// Подписчик доменных событий по умолчанию: фиксирует факт наступления события в журнале.
/// Нужен не только ради наблюдаемости — до этого события публиковались в пустоту, и сбой
/// в их публикации нельзя было заметить. Новые реакции добавляются новыми подписчиками,
/// существующий код при этом не меняется (принцип открытости/закрытости).
/// </summary>
public sealed partial class DomainEventLoggingHandler(ILogger<DomainEventLoggingHandler> logger)
    : INotificationHandler<DomainEventNotification>
{
    /// <inheritdoc />
    public ValueTask Handle(DomainEventNotification notification, CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            LogDomainEventPublished(logger, notification.DomainEvent.GetType().Name);
        }

        return ValueTask.CompletedTask;
    }

    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Information,
        Message = "Доменное событие {EventName} опубликовано")]
    private static partial void LogDomainEventPublished(ILogger logger, string eventName);
}
