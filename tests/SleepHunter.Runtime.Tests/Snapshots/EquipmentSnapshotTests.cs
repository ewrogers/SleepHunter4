using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Tests.Snapshots;

public sealed class EquipmentSnapshotTests
{
    [Test]
    public void ShouldNormalizeEquipmentAndRequireBothHandsToBeEmpty()
    {
        var armed = new EquipmentSnapshot(" weapon ", " shield ");
        var weaponOnly = new EquipmentSnapshot("weapon");
        var shieldOnly = new EquipmentSnapshot(
            weaponName: null,
            shieldName: "shield");
        var disarmed = new EquipmentSnapshot(
            weaponName: null,
            shieldName: null);

        Assert.Multiple(() =>
        {
            Assert.That(armed.WeaponName, Is.EqualTo("weapon"));
            Assert.That(armed.ShieldName, Is.EqualTo("shield"));
            Assert.That(armed.IsDisarmed, Is.False);
            Assert.That(weaponOnly.IsDisarmed, Is.False);
            Assert.That(shieldOnly.IsDisarmed, Is.False);
            Assert.That(disarmed.IsDisarmed, Is.True);
        });
    }
}
