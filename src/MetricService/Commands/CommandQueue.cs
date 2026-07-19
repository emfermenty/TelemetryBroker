using System.Collections.Concurrent;
using Module.Metrics.V1;

namespace MetricService.Commands;

public class CommandQueue
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<Command>> _queues = new();

    public void Enqueue(string moduleId, Command command)
    {
        var queue = _queues.GetOrAdd(moduleId, static _ => new ConcurrentQueue<Command>());
        queue.Enqueue(command);
    }
    public IReadOnlyList<Command> DequeueAll(string moduleId)
    {
        if(!_queues.TryGetValue(moduleId, out var queue))
        {
            return Array.Empty<Command>();
        }
        var commands = new List<Command>();
        while(queue.TryDequeue(out var command))
        {
            commands.Add(command);
        }
        return commands;
    }
}
