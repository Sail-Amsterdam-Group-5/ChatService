using Prometheus;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Chat.Application.Services;

public class MetricsService
{
    private readonly Counter _messagesSent;
    private readonly Counter _messagesDeleted;
    private readonly Gauge _activeChats;
    private readonly Histogram _messageProcessingTime;

    public MetricsService()
    {
        _messagesSent = Metrics.CreateCounter(
            "chat_messages_total",
            "Total number of messages sent");

        _messagesDeleted = Metrics.CreateCounter(
            "chat_messages_deleted_total",
            "Total number of messages deleted");

        _activeChats = Metrics.CreateGauge(
            "chat_active_chats",
            "Number of active chats");

        _messageProcessingTime = Metrics.CreateHistogram(
            "chat_message_processing_seconds",
            "Time taken to process messages",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
            });
    }

    public void IncrementMessagesSent() => _messagesSent.Inc();
    public void IncrementMessagesDeleted() => _messagesDeleted.Inc();
    public void SetActiveChats(int count) => _activeChats.Set(count);

    public Prometheus.ITimer BeginMessageProcessing() =>
        _messageProcessingTime.NewTimer();
}