using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using SleepHunter.Media;
using SleepHunter.Metadata;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.ViewModels.Presentation
{
    public sealed class SkillbookViewModel :
        IEnumerable<SkillViewModel>
    {
        public const int TemuairSkillCount = 36;
        public const int MedeniaSkillCount = 36;
        public const int WorldSkillCount = 18;

        private readonly SkillViewModel[] skills =
            new SkillViewModel[
                TemuairSkillCount +
                MedeniaSkillCount +
                WorldSkillCount];
        private readonly IconManager icons;
        private readonly SkillMetadataManager metadata;

        public IEnumerable<SkillViewModel> TemuairSkills =>
            skills.Where(
                skill =>
                    skill.Panel == ClientPanel.TemuairSkills &&
                    skill.Slot <= TemuairSkillCount);

        public IEnumerable<SkillViewModel> MedeniaSkills =>
            skills.Where(
                skill =>
                    skill.Panel == ClientPanel.MedeniaSkills &&
                    skill.Slot <=
                        TemuairSkillCount + MedeniaSkillCount);

        public IEnumerable<SkillViewModel> WorldSkills =>
            skills.Where(
                skill =>
                    skill.Panel == ClientPanel.WorldSkills &&
                    skill.Slot <= skills.Length);

        public SkillbookViewModel(
            IconManager icons = null,
            SkillMetadataManager metadata = null)
        {
            this.icons = icons;
            this.metadata = metadata ??
                new SkillMetadataManager();

            for (var index = 0; index < skills.Length; index++)
                skills[index] =
                    SkillViewModel.MakeEmpty(index + 1);
        }

        public SkillViewModel GetSkill(string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName))
                return null;

            return skills.FirstOrDefault(
                skill => string.Equals(
                    skill.Name,
                    skillName.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        internal void Apply(
            SkillbookSnapshot snapshot,
            IEnumerable<string> activeSkillNames = null)
        {
            var observedSkills = snapshot?.Skills ?? [];
            var bySlot = observedSkills.ToDictionary(
                skill => skill.Slot);
            var activeSkills = activeSkillNames?
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ??
                [];

            for (var slot = 1; slot <= skills.Length; slot++)
            {
                var skill = skills[slot - 1];
                if (!bySlot.TryGetValue(slot, out var observed))
                {
                    ResetSkill(skill);
                    continue;
                }

                var skillMetadata =
                    metadata.GetSkill(observed.Name);
                skill.IsEmpty = false;
                skill.Name = observed.Name;
                skill.Icon = GetSkillIcon(observed.Icon);
                skill.CurrentLevel = observed.CurrentLevel;
                skill.MaximumLevel = observed.MaximumLevel;
                skill.ManaCost = observed.ManaCost;
                skill.Cooldown = observed.Cooldown;
                skill.OpensDialog = observed.OpensDialog;
                skill.CanImprove =
                    skillMetadata?.CanImprove ?? true;
                skill.MinHealthPercent =
                    observed.HealthCondition
                        .MinimumPercentExclusive;
                skill.MaxHealthPercent =
                    observed.HealthCondition
                        .MaximumPercentInclusive;
                skill.CooldownProgress =
                    observed.CooldownProgress;
                skill.IsOnCooldown =
                    observed.IsCooldownVisualActive;
                skill.HasClientCooldownProgress =
                    observed.IsCooldownVisualActive ||
                    observed.CooldownProgress > 0 ||
                    observed.CooldownStartedAt > 0 ||
                    observed.CooldownEndsAt > 0;
                skill.IsActive =
                    activeSkills.Contains(observed.Name);
            }
        }

        internal void Reset() => Apply(SkillbookSnapshot.Empty);

        public IEnumerator<SkillViewModel> GetEnumerator()
        {
            foreach (var skill in skills)
            {
                if (!skill.IsEmpty)
                    yield return skill;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static void ResetSkill(SkillViewModel skill)
        {
            skill.IsEmpty = true;
            skill.Name = null;
            skill.Icon = null;
            skill.CurrentLevel = 0;
            skill.MaximumLevel = 0;
            skill.ManaCost = 0;
            skill.Cooldown = TimeSpan.Zero;
            skill.OpensDialog = false;
            skill.CanImprove = true;
            skill.MinHealthPercent = null;
            skill.MaxHealthPercent = null;
            skill.CooldownProgress = 0;
            skill.IsOnCooldown = false;
            skill.HasClientCooldownProgress = false;
            skill.IsActive = false;
        }

        private ImageSource GetSkillIcon(int index)
        {
            try
            {
                return icons?.GetSkillIcon(index);
            }
            catch
            {
                return null;
            }
        }
    }
}
