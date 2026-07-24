using SleepHunter.IO.Process;

namespace SleepHunter.Tests.IO.Process
{
    [TestFixture]
    public sealed class RuntimeMemoryReaderTests
    {
        [TestCase(".?AVWorldObject_Human@@", "WorldObject_Human")]
        [TestCase(".?AUExample@@", "Example")]
        [TestCase("ChatInputPane", "ChatInputPane")]
        public void ShouldNormalizeMsvcRttiNames(string decoratedName, string expected)
        {
            Assert.That(RuntimeMemoryReader.NormalizeRttiClassName(decoratedName), Is.EqualTo(expected));
        }
    }
}
