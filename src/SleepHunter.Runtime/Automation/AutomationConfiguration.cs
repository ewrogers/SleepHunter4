using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Runtime.Automation;

public sealed record AutomationConfiguration
{
    public static AutomationConfiguration Disabled { get; } = new();

    public AutomationConfiguration(
        bool spellsEnabled = false,
        bool skillsEnabled = false,
        bool floweringEnabled = false,
        bool flowerBeforeSpells = false,
        SpellExecutionPolicy? spellPolicy = null,
        SpellStaffCatalog? spellStaffCatalog = null,
        SkillExecutionPolicy? skillPolicy = null,
        FlowerExecutionPolicy? flowerPolicy = null,
        FlowerStaffCatalog? flowerStaffCatalog = null,
        ObservationChangePolicy? observationChanges = null,
        PanelPreservationPolicy? panelPreservation = null)
    {
        SpellsEnabled = spellsEnabled;
        SkillsEnabled = skillsEnabled;
        FloweringEnabled = floweringEnabled;
        FlowerBeforeSpells = flowerBeforeSpells;
        SpellPolicy = spellPolicy ?? SpellExecutionPolicy.Default;
        SpellStaffCatalog = spellStaffCatalog ?? SpellStaffCatalog.Empty;
        SkillPolicy = skillPolicy ?? SkillExecutionPolicy.Default;
        FlowerPolicy = flowerPolicy ?? FlowerExecutionPolicy.Default;
        FlowerStaffCatalog =
            flowerStaffCatalog ?? FlowerStaffCatalog.Empty;
        ObservationChanges =
            observationChanges ?? ObservationChangePolicy.Default;
        PanelPreservation =
            panelPreservation ?? PanelPreservationPolicy.Disabled;
    }

    public bool SpellsEnabled { get; }

    public bool SkillsEnabled { get; }

    public bool FloweringEnabled { get; }

    public bool FlowerBeforeSpells { get; }

    public SpellExecutionPolicy SpellPolicy { get; }

    public SpellStaffCatalog SpellStaffCatalog { get; }

    public SkillExecutionPolicy SkillPolicy { get; }

    public FlowerExecutionPolicy FlowerPolicy { get; }

    public FlowerStaffCatalog FlowerStaffCatalog { get; }

    public ObservationChangePolicy ObservationChanges { get; }

    public PanelPreservationPolicy PanelPreservation { get; }

    public bool IsEnabled =>
        SpellsEnabled ||
        SkillsEnabled ||
        FloweringEnabled;
}
