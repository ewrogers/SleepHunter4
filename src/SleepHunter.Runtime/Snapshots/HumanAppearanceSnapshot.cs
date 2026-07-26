namespace SleepHunter.Runtime.Snapshots;

public sealed record HumanAppearanceSnapshot(
    byte ResourcePrefix,
    ushort HeadSprite,
    ushort BodySprite,
    ushort ArmsSprite,
    ushort BootsSprite,
    ushort PantsSprite,
    ushort ArmorSprite,
    ushort WeaponSprite,
    ushort ShieldSprite,
    ushort OvercoatSprite,
    ushort Accessory1Sprite,
    ushort Accessory2Sprite,
    ushort Accessory3Sprite,
    bool IsTranslucent);
