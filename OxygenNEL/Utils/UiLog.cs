using System;
using System.Collections.Generic;
using System.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace OxygenNEL.Utils;

public static class UiLog
{
    public static event Action<string>? Logged;
    private static readonly object _lock = new();
    private static readonly List<string> _buffer = new();

    private class Sink : ILogEventSink
    {
        private readonly MessageTemplateTextFormatter _formatter =
            new("{Timestamp:HH:mm:ss} [{Level}] {Message:lj}{NewLine}{Exception}");

        public void Emit(LogEvent logEvent)
        {
            using var sw = new StringWriter();
            _formatter.Format(logEvent, sw);
            var s = sw.ToString();
            try
            {
                lock (_lock)
                {
                    _buffer.Add(s);
                    if (_buffer.Count > 2000) _buffer.RemoveAt(0);
                }
            }
            catch
            {
            }

            try
            {
                Logged?.Invoke(s);
            }
            catch
            {
            }
        }
    }

    public static ILogEventSink CreateSink()
    {
        return new Sink();
    }

    public static IReadOnlyList<string> GetSnapshot()
    {
        lock (_lock)
        {
            return _buffer.ToArray();
        }
    }
}