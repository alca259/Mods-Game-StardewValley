using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Alca259.Common;

/// <summary>Provides lightweight in-memory performance sampling for frame diagnostics.</summary>
public class PerformanceMonitor
{
    private const int BufferSize = 60;
    private static readonly IReadOnlyDictionary<string, double> EmptyAverages = new Dictionary<string, double>();

    private readonly Func<bool> _isEnabled;
    private readonly Dictionary<string, Stopwatch> _labelStopwatches = new();
    private readonly Dictionary<string, Queue<double>> _labelSamples = new();
    private readonly Queue<double> _cpuSamples = new();

    private Stopwatch? _cpuStopwatch;
    private TimeSpan _lastCpuTotalProcessorTime;
    private bool _hasCpuBaseline;

    /// <summary>Creates a new monitor controlled by a runtime-enabled flag callback.</summary>
    /// <param name="isEnabled">Callback that indicates whether monitoring should run for each call.</param>
    public PerformanceMonitor(Func<bool> isEnabled)
    {
        ArgumentNullException.ThrowIfNull(isEnabled);
        _isEnabled = isEnabled;
    }

    /// <summary>Starts or restarts the stopwatch for a named sample label.</summary>
    /// <param name="label">Sample label.</param>
    public void BeginSample(string label)
    {
        if (!_isEnabled())
            return;

        if (string.IsNullOrWhiteSpace(label))
            return;

        if (!_labelStopwatches.TryGetValue(label, out Stopwatch? stopwatch))
        {
            stopwatch = new Stopwatch();
            _labelStopwatches[label] = stopwatch;
        }

        stopwatch.Restart();
    }

    /// <summary>Stops the stopwatch for a label and appends elapsed milliseconds to its circular buffer.</summary>
    /// <param name="label">Sample label.</param>
    public void EndSample(string label)
    {
        if (!_isEnabled())
            return;

        if (string.IsNullOrWhiteSpace(label))
            return;

        if (!_labelStopwatches.TryGetValue(label, out Stopwatch? stopwatch))
            return;

        stopwatch.Stop();

        if (!_labelSamples.TryGetValue(label, out Queue<double>? buffer))
        {
            buffer = new Queue<double>(BufferSize);
            _labelSamples[label] = buffer;
        }

        AddSample(buffer, stopwatch.Elapsed.TotalMilliseconds);
    }

    /// <summary>Samples process CPU usage percentage and appends it to a circular buffer.</summary>
    public void SampleProcessCpu()
    {
        if (!_isEnabled())
            return;

        Process process = Process.GetCurrentProcess();
        TimeSpan currentCpuTotal = process.TotalProcessorTime;

        if (!_hasCpuBaseline)
        {
            _lastCpuTotalProcessorTime = currentCpuTotal;
            _cpuStopwatch = Stopwatch.StartNew();
            _hasCpuBaseline = true;
            return;
        }

        if (_cpuStopwatch == null)
        {
            _cpuStopwatch = Stopwatch.StartNew();
            _lastCpuTotalProcessorTime = currentCpuTotal;
            return;
        }

        double elapsedMilliseconds = _cpuStopwatch.Elapsed.TotalMilliseconds;
        if (elapsedMilliseconds <= 0)
        {
            _lastCpuTotalProcessorTime = currentCpuTotal;
            _cpuStopwatch.Restart();
            return;
        }

        double cpuMilliseconds = (currentCpuTotal - _lastCpuTotalProcessorTime).TotalMilliseconds;
        double cpuPercent = cpuMilliseconds / (elapsedMilliseconds * Environment.ProcessorCount) * 100d;

        AddSample(_cpuSamples, cpuPercent);

        _lastCpuTotalProcessorTime = currentCpuTotal;
        _cpuStopwatch.Restart();
    }

    /// <summary>Gets current average values for label and CPU buffers.</summary>
    public IReadOnlyDictionary<string, double> Averages
    {
        get
        {
            if (!_isEnabled())
                return EmptyAverages;

            if (_labelSamples.Count == 0 && _cpuSamples.Count == 0)
                return EmptyAverages;

            var result = new Dictionary<string, double>(_labelSamples.Count + (_cpuSamples.Count > 0 ? 1 : 0));

            foreach (KeyValuePair<string, Queue<double>> pair in _labelSamples)
            {
                if (pair.Value.Count == 0)
                    continue;

                result[pair.Key] = CalculateAverage(pair.Value);
            }

            if (_cpuSamples.Count > 0)
                result["cpu"] = CalculateAverage(_cpuSamples);

            return result;
        }
    }

    private static void AddSample(Queue<double> buffer, double value)
    {
        if (buffer.Count == BufferSize)
            buffer.Dequeue();

        buffer.Enqueue(value);
    }

    private static double CalculateAverage(Queue<double> samples)
    {
        double sum = 0;
        foreach (double sample in samples)
            sum += sample;

        return sum / samples.Count;
    }
}