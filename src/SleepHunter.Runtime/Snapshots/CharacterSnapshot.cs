using SleepHunter.Runtime.Characters;

namespace SleepHunter.Runtime.Snapshots;

public sealed record CharacterSnapshot
{
    public CharacterSnapshot(
        CharacterClass characterClass,
        int level,
        int abilityLevel,
        string? name = null,
        uint characterId = 0,
        CharacterUserState userState = CharacterUserState.Unknown,
        int privilegeLevel = 0,
        uint gold = 0,
        uint totalExperience = 0,
        int strength = 0,
        int dexterity = 0,
        int wisdom = 0,
        int constitution = 0,
        int intelligence = 0,
        int statPoints = 0,
        uint experienceToNextLevel = 0,
        uint gamePoints = 0,
        uint abilityToNextLevel = 0,
        uint totalAbility = 0,
        uint weight = 0,
        uint maximumWeight = 0,
        int armorClass = 0,
        int damageModifier = 0,
        int hitModifier = 0,
        ushort attackElement = 0,
        ushort defenseElement = 0,
        ushort magicResistance = 0,
        byte actionState = 0,
        bool showAbilityMetadata = false,
        bool showMasterMetadata = false)
    {
        if (!Enum.IsDefined(characterClass))
        {
            throw new ArgumentOutOfRangeException(
                nameof(characterClass),
                characterClass,
                "The observed character class is not supported.");
        }

        if (level < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level),
                level,
                "Character level cannot be negative.");
        }

        if (abilityLevel < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(abilityLevel),
                abilityLevel,
                "Character ability level cannot be negative.");
        }

        if (!Enum.IsDefined(userState))
        {
            throw new ArgumentOutOfRangeException(
                nameof(userState),
                userState,
                "The observed character user state is not supported.");
        }

        if (strength < 0 ||
            dexterity < 0 ||
            wisdom < 0 ||
            constitution < 0 ||
            intelligence < 0 ||
            statPoints < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(strength),
                "Observed character attributes cannot be negative.");
        }

        if (armorClass is < sbyte.MinValue or > sbyte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(armorClass),
                armorClass,
                "Armor class must fit the signed client field.");
        }

        if (damageModifier is < byte.MinValue or > byte.MaxValue ||
            hitModifier is < byte.MinValue or > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(damageModifier),
                "Damage and hit modifiers must fit the client fields.");
        }

        if (name is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            name = name.Trim();
        }

        Class = characterClass;
        Level = level;
        AbilityLevel = abilityLevel;
        Name = name;
        CharacterId = characterId;
        UserState = userState;
        PrivilegeLevel = privilegeLevel;
        Gold = gold;
        TotalExperience = totalExperience;
        Strength = strength;
        Dexterity = dexterity;
        Wisdom = wisdom;
        Constitution = constitution;
        Intelligence = intelligence;
        StatPoints = statPoints;
        ExperienceToNextLevel = experienceToNextLevel;
        GamePoints = gamePoints;
        AbilityToNextLevel = abilityToNextLevel;
        TotalAbility = totalAbility;
        Weight = weight;
        MaximumWeight = maximumWeight;
        ArmorClass = armorClass;
        DamageModifier = damageModifier;
        HitModifier = hitModifier;
        AttackElement = attackElement;
        DefenseElement = defenseElement;
        MagicResistance = magicResistance;
        ActionState = actionState;
        ShowAbilityMetadata = showAbilityMetadata;
        ShowMasterMetadata = showMasterMetadata;
    }

    public CharacterClass Class { get; }

    public int Level { get; }

    public int AbilityLevel { get; }

    public string? Name { get; }

    public uint CharacterId { get; }

    public CharacterUserState UserState { get; }

    public int PrivilegeLevel { get; }

    public uint Gold { get; }

    public uint TotalExperience { get; }

    public int Strength { get; }

    public int Dexterity { get; }

    public int Wisdom { get; }

    public int Constitution { get; }

    public int Intelligence { get; }

    public int StatPoints { get; }

    public uint ExperienceToNextLevel { get; }

    public uint GamePoints { get; }

    public uint AbilityToNextLevel { get; }

    public uint TotalAbility { get; }

    public uint Weight { get; }

    public uint MaximumWeight { get; }

    public int ArmorClass { get; }

    public int DamageModifier { get; }

    public int HitModifier { get; }

    public ushort AttackElement { get; }

    public ushort DefenseElement { get; }

    public ushort MagicResistance { get; }

    public byte ActionState { get; }

    public bool IsActionLocked => (ActionState & 0x01) != 0;

    public bool ShowAbilityMetadata { get; }

    public bool ShowMasterMetadata { get; }
}
