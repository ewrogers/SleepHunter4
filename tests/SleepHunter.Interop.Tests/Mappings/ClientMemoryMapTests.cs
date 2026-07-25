using SleepHunter.Interop.Mappings;
using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Tests.Mappings;

public sealed class ClientMemoryMapTests
{
    [Test]
    public void ShouldFindImmutableVariablesCaseInsensitively()
    {
        var variable = new MemoryVariableDefinition(
            "CharacterName",
            new PointerChain(new MemoryAddress(0x1000)),
            MemoryValueKind.Text,
            maximumLength: 16);
        var map = new ClientMemoryMap(
            "  USDA 7.41  ",
            PointerWidth.Bit32,
            [variable]);

        Assert.Multiple(() =>
        {
            Assert.That(map.VersionKey, Is.EqualTo("USDA 7.41"));
            Assert.That(map.PointerWidth, Is.EqualTo(PointerWidth.Bit32));
            Assert.That(map.Find("charactername"), Is.SameAs(variable));
            Assert.That(map.Find("missing"), Is.Null);
            Assert.That(map.Variables, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ShouldRejectInvalidAndDuplicateDefinitions()
    {
        var first = new MemoryVariableDefinition(
            "value",
            new PointerChain(new MemoryAddress(0x1000)),
            MemoryValueKind.Unsigned32);
        var duplicate = new MemoryVariableDefinition(
            "VALUE",
            new PointerChain(new MemoryAddress(0x2000)),
            MemoryValueKind.Unsigned32);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                () => _ = new ClientMemoryMap(
                    "version",
                    PointerWidth.Bit32,
                    [first, duplicate]));
            Assert.Throws<ArgumentException>(
                () => _ = new MemoryVariableDefinition(
                    "text",
                    new PointerChain(new MemoryAddress(0x1000)),
                    MemoryValueKind.Text));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new MemoryVariableDefinition(
                    "value",
                    new PointerChain(new MemoryAddress(0x1000)),
                    MemoryValueKind.Unsigned32,
                    recordSize: -1));
        });
    }
}
