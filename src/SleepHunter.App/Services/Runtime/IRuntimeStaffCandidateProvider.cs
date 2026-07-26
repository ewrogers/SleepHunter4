using System.Collections.Immutable;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Characters;

namespace SleepHunter.Services.Runtime
{
    public interface IRuntimeStaffCandidateProvider
    {
        ImmutableArray<StaffCandidate> GetCandidates(
            string spellName,
            CharacterClass characterClass);
    }
}
