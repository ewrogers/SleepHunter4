using System.Collections.Immutable;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Flowering;

public sealed class FlowerScheduleState : IEquatable<FlowerScheduleState>
{
    public static FlowerScheduleState Empty { get; } = new(
        ImmutableDictionary<FlowerQueueEntryId, FlowerSchedule>.Empty);

    private FlowerScheduleState(
        ImmutableDictionary<FlowerQueueEntryId, FlowerSchedule> schedules)
    {
        Schedules = schedules;
    }

    public ImmutableDictionary<FlowerQueueEntryId, FlowerSchedule> Schedules
    {
        get;
    }

    public MacroTimestamp? GetReadyAt(FlowerQueueEntryId entryId) =>
        Schedules.TryGetValue(entryId, out var schedule)
            ? schedule.ReadyAt
            : null;

    public FlowerScheduleState RecordUse(
        FlowerQueueEntry entry,
        MacroTimestamp currentTime)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.Interval is not { } interval)
        {
            var removed = Schedules.Remove(entry.Id);
            return ReferenceEquals(removed, Schedules)
                ? this
                : new FlowerScheduleState(removed);
        }

        var schedule = new FlowerSchedule(
            interval,
            currentTime.Add(interval));
        var schedules = Schedules.SetItem(entry.Id, schedule);
        return Schedules.TryGetValue(entry.Id, out var current) &&
               current == schedule
            ? this
            : new FlowerScheduleState(schedules);
    }

    public bool Equals(FlowerScheduleState? other) =>
        other is not null &&
        Schedules.Count == other.Schedules.Count &&
        Schedules.All(
            entry =>
                other.Schedules.TryGetValue(entry.Key, out var schedule) &&
                schedule == entry.Value);

    public override bool Equals(object? obj) =>
        obj is FlowerScheduleState other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var entry in Schedules.OrderBy(
                     entry => entry.Key.Value))
        {
            hash.Add(entry.Key);
            hash.Add(entry.Value);
        }

        return hash.ToHashCode();
    }

    internal FlowerScheduleState Synchronize(
        FlowerQueueState queue,
        MacroTimestamp currentTime)
    {
        ArgumentNullException.ThrowIfNull(queue);

        var intervalEntries = queue.Entries
            .Where(entry => entry.Interval is not null)
            .ToDictionary(entry => entry.Id);
        var schedules = Schedules.RemoveRange(
            Schedules.Keys.Where(
                entryId => !intervalEntries.ContainsKey(entryId)));

        foreach (var entry in intervalEntries.Values)
        {
            var interval = entry.Interval!.Value;
            if (schedules.TryGetValue(entry.Id, out var schedule) &&
                schedule.Interval == interval)
            {
                continue;
            }

            schedules = schedules.SetItem(
                entry.Id,
                new FlowerSchedule(
                    interval,
                    currentTime.Add(interval)));
        }

        return ReferenceEquals(schedules, Schedules)
            ? this
            : new FlowerScheduleState(schedules);
    }
}
