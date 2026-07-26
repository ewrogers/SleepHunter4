using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Automation.Spells;

public static class TargetLocator
{
    public const int MaximumTileDistance = 10;

    public static TargetLocation Locate(
        SpellTarget target,
        ClientSnapshot source,
        ClientRosterSnapshot roster)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(roster);

        if (target.Kind != SpellTargetKind.Character)
        {
            return new TargetLocation(
                TargetLocationStatus.Resolved,
                target);
        }

        if (!roster.HasObservation)
        {
            return Unresolved(TargetLocationStatus.RosterUnavailable);
        }

        if (source.Location is not { } sourceLocation)
        {
            return Unresolved(TargetLocationStatus.SourceUnavailable);
        }

        var observedSource = roster.Clients.FirstOrDefault(
            entry => entry.Client == source.Client);
        if (observedSource is not
            {
                Presence: ClientPresence.InWorld,
                Location: { } observedSourceLocation
            })
        {
            return Unresolved(TargetLocationStatus.SourceUnavailable);
        }

        if (observedSourceLocation != sourceLocation)
        {
            return Unresolved(TargetLocationStatus.SourceChanged);
        }

        var observedTarget = roster.Clients.FirstOrDefault(
            entry => string.Equals(
                entry.CharacterName,
                target.CharacterName,
                StringComparison.OrdinalIgnoreCase));
        if (observedTarget is not
            {
                Presence: ClientPresence.InWorld,
                Location: { } targetLocation
            })
        {
            return Unresolved(TargetLocationStatus.TargetUnavailable);
        }

        if (targetLocation.MapNumber != sourceLocation.MapNumber ||
            !string.Equals(
                targetLocation.MapName,
                sourceLocation.MapName,
                StringComparison.Ordinal))
        {
            return Unresolved(TargetLocationStatus.DifferentMap);
        }

        var deltaX = (long)targetLocation.X - sourceLocation.X;
        var deltaY = (long)targetLocation.Y - sourceLocation.Y;
        if (Math.Abs(deltaX) > MaximumTileDistance ||
            Math.Abs(deltaY) > MaximumTileDistance)
        {
            return Unresolved(TargetLocationStatus.OutOfRange);
        }

        return new TargetLocation(
            TargetLocationStatus.Resolved,
            SpellTarget.RelativeTile(
                (int)deltaX,
                (int)deltaY,
                target.Offset));
    }

    private static TargetLocation Unresolved(TargetLocationStatus status) =>
        new(status, Target: null);
}
