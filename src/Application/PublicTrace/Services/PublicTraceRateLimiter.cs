using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace IstpetDev.Application.PublicTrace.Services;

/// <summary>
/// Limitador de tasa en memoria por token hash (PA-05).
/// Permite un umbral holgado (por defecto 60 consultas por minuto por token)
/// para soportar escaneos concurrentes en demostraciones de jurados.
/// </summary>
public sealed class PublicTraceRateLimiter : IPublicTraceRateLimiter
{
    private readonly int _maxRequestsPerWindow;
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _requestsByToken = new();
    private readonly object _syncRoot = new();

    public PublicTraceRateLimiter(int maxRequestsPerWindow = 60, TimeSpan? window = null)
    {
        _maxRequestsPerWindow = maxRequestsPerWindow;
        _window = window ?? TimeSpan.FromMinutes(1);
    }

    public bool IsAllowed(string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var queue = _requestsByToken.GetOrAdd(tokenHash, _ => new Queue<DateTime>());

        lock (_syncRoot)
        {
            var cutoff = now - _window;
            while (queue.Count > 0 && queue.Peek() <= cutoff)
            {
                queue.Dequeue();
            }

            if (queue.Count >= _maxRequestsPerWindow)
            {
                return false;
            }

            queue.Enqueue(now);
            return true;
        }
    }
}
