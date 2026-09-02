using FlexAgent.Api;
using Microsoft.Extensions.Logging;

namespace FlexAgent.Runtime.Tests;

public sealed class HostedSessionTelemetryTests
{
    [Fact]
    public void Snapshot_and_command_logs_use_closed_labels_without_payload_text()
    {
        var logger = new CapturingLogger();
        var telemetry = new LoggingHostedSessionTelemetry(logger);

        telemetry.RecordSnapshot("loaded", TimeSpan.FromMilliseconds(12));
        telemetry.RecordCommand("session.message.send.v1", "accepted", TimeSpan.FromMilliseconds(8));
        telemetry.RecordCommand("Hazard identified: blocked exit.", "accepted", TimeSpan.FromMilliseconds(1));
        telemetry.RecordSubscribe("opened", TimeSpan.FromMilliseconds(4));

        Assert.All(logger.Messages, message =>
        {
            Assert.DoesNotContain("Hazard", message, StringComparison.Ordinal);
            Assert.DoesNotContain("blocked", message, StringComparison.Ordinal);
            Assert.DoesNotContain("transcript", message, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Contains(logger.Messages, message => message.Contains("session.hosted.subscribe", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("session.hosted.snapshot", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("session.message.send.v1", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("unknown", StringComparison.Ordinal));
    }

    private sealed class CapturingLogger : ILogger<LoggingHostedSessionTelemetry>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
