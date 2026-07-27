using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SleepHunter.Extensions;
using SleepHunter.Metadata;
using SleepHunter.Models;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Characters;

namespace SleepHunter.Services.Runtime
{
    public sealed class RuntimeStaffCandidateProvider :
        IRuntimeStaffCandidateProvider
    {
        private readonly Func<
            IEnumerable<StaffMetadata>> getStaves;
        private readonly Func<string, string, int?> getLines;

        public RuntimeStaffCandidateProvider(
            StaffMetadataManager staffMetadata)
        {
            ArgumentNullException.ThrowIfNull(staffMetadata);
            getStaves = () => staffMetadata.Staves;
            getLines = staffMetadata.GetLinesWithStaff;
        }

        internal RuntimeStaffCandidateProvider(
            Func<IEnumerable<StaffMetadata>> getStaves,
            Func<string, string, int?> getLines)
        {
            this.getStaves = getStaves ??
                throw new ArgumentNullException(nameof(getStaves));
            this.getLines = getLines ??
                throw new ArgumentNullException(nameof(getLines));
        }

        public ImmutableArray<StaffCandidate> GetCandidates(
            string spellName,
            CharacterClass characterClass)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(spellName);
            if (!Enum.IsDefined(characterClass))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(characterClass),
                    characterClass,
                    "The runtime character class is not supported.");
            }

            var staves = getStaves().ToArray();
            if (staves.Any(staff => staff is null))
            {
                throw new InvalidOperationException(
                    "Staff metadata cannot contain null entries.");
            }

            var candidates = ImmutableArray.CreateBuilder<StaffCandidate>();
            foreach (var staff in staves
                         .OrderBy(
                             staff => staff.Name,
                             StringComparer.OrdinalIgnoreCase)
                         .ThenBy(
                             staff => staff.Name,
                             StringComparer.Ordinal))
            {
                if (!TryGetRequiredClass(
                        staff.Class,
                        characterClass,
                        out var requiredClass))
                {
                    continue;
                }

                var castLines = getLines(staff.Name, spellName);
                if (castLines is null)
                {
                    continue;
                }

                candidates.Add(
                    new StaffCandidate(
                        staff.Name,
                        requiredClass,
                        staff.Level,
                        staff.AbilityLevel,
                        castLines.Value));
            }

            return candidates.ToImmutable();
        }

        private static bool TryGetRequiredClass(
            CharacterClassFlags allowedClasses,
            CharacterClass characterClass,
            out CharacterClass? requiredClass)
        {
            if ((allowedClasses & ~CharacterClassFlags.All) != 0)
            {
                throw new InvalidOperationException(
                    $"Staff metadata contains unsupported class flags '{allowedClasses}'.");
            }

            if (allowedClasses == CharacterClassFlags.All)
            {
                requiredClass = null;
                return true;
            }

            if (characterClass == CharacterClass.Unknown)
            {
                requiredClass = null;
                return false;
            }

            var playerClass = ToCharacterClassFlags(characterClass);
            if (!allowedClasses.Includes(playerClass))
            {
                requiredClass = null;
                return false;
            }

            requiredClass = characterClass;
            return true;
        }

        private static CharacterClassFlags ToCharacterClassFlags(
            CharacterClass characterClass) =>
            characterClass switch
            {
                CharacterClass.Peasant => CharacterClassFlags.Peasant,
                CharacterClass.Warrior => CharacterClassFlags.Warrior,
                CharacterClass.Wizard => CharacterClassFlags.Wizard,
                CharacterClass.Priest => CharacterClassFlags.Priest,
                CharacterClass.Rogue => CharacterClassFlags.Rogue,
                CharacterClass.Monk => CharacterClassFlags.Monk,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(characterClass),
                    characterClass,
                    "Unknown characters cannot use class-specific staves.")
            };
    }
}
