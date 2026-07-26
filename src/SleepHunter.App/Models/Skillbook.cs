using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using SleepHunter.Media;
using SleepHunter.Metadata;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Models
{
    public sealed class Skillbook : IEnumerable<Skill>
    {
        public const int TemuairSkillCount = 36;
        public const int MedeniaSkillCount = 36;
        public const int WorldSkillCount = 18;

        private readonly Skill[] skills =
            new Skill[
                TemuairSkillCount +
                MedeniaSkillCount +
                WorldSkillCount];
        private readonly ConcurrentDictionary<string, bool>
            activeSkills =
                new(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<Skill> TemuairSkills =>
            skills.Where(
                skill =>
                    skill.Panel == InterfacePanel.TemuairSkills &&
                    skill.Slot <= TemuairSkillCount);

        public IEnumerable<Skill> MedeniaSkills =>
            skills.Where(
                skill =>
                    skill.Panel == InterfacePanel.MedeniaSkills &&
                    skill.Slot <=
                        TemuairSkillCount + MedeniaSkillCount);

        public IEnumerable<Skill> WorldSkills =>
            skills.Where(
                skill =>
                    skill.Panel == InterfacePanel.WorldSkills &&
                    skill.Slot <= skills.Length);

        public Skillbook()
        {
            for (var index = 0; index < skills.Length; index++)
                skills[index] = Skill.MakeEmpty(index + 1);
        }

        public Skill GetSkill(string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName))
                return null;

            return skills.FirstOrDefault(
                skill => string.Equals(
                    skill.Name,
                    skillName.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        public bool? IsActive(string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName))
                return null;

            return activeSkills.TryGetValue(
                skillName.Trim(),
                out var activeState)
                    ? activeState
                    : null;
        }

        public bool? ToggleActive(
            string skillName,
            bool? isActive = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(skillName);
            skillName = skillName.Trim();

            var hasPrevious = activeSkills.TryGetValue(
                skillName,
                out var previous);
            bool? wasActive = hasPrevious
                ? previous
                : null;
            var next = isActive ?? !previous;
            activeSkills[skillName] = next;

            var skill = GetSkill(skillName);
            if (skill is not null)
                skill.IsActive = next;

            return wasActive;
        }

        public void ClearActiveSkills()
        {
            activeSkills.Clear();
            foreach (var skill in skills)
                skill.IsActive = false;
        }

        internal void Apply(SkillbookSnapshot snapshot)
        {
            var observedSkills = snapshot?.Skills ?? [];
            var bySlot = observedSkills.ToDictionary(
                skill => skill.Slot);

            for (var slot = 1; slot <= skills.Length; slot++)
            {
                var skill = skills[slot - 1];
                if (!bySlot.TryGetValue(slot, out var observed))
                {
                    ResetSkill(skill);
                    continue;
                }

                var metadata = SkillMetadataManager.Instance
                    .GetSkill(observed.Name);
                skill.IsEmpty = false;
                skill.Name = observed.Name;
                skill.Icon = GetSkillIcon(observed.Icon);
                skill.CurrentLevel = observed.CurrentLevel;
                skill.MaximumLevel = observed.MaximumLevel;
                skill.ManaCost = observed.ManaCost;
                skill.Cooldown = observed.Cooldown;
                skill.OpensDialog = observed.OpensDialog;
                skill.CanImprove = metadata?.CanImprove ?? true;
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
                    IsActive(observed.Name) == true;
            }
        }

        internal void Reset() => Apply(SkillbookSnapshot.Empty);

        public IEnumerator<Skill> GetEnumerator()
        {
            foreach (var skill in skills)
            {
                if (!skill.IsEmpty)
                    yield return skill;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static void ResetSkill(Skill skill)
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

        private static ImageSource GetSkillIcon(int index)
        {
            try
            {
                return IconManager.Instance.GetSkillIcon(index);
            }
            catch
            {
                return null;
            }
        }
    }
}
