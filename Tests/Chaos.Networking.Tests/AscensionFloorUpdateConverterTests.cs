#region
using System.Text;
using Chaos.IO.Memory;
using Chaos.Networking.Converters.Server;
using Chaos.Networking.Entities.Server;
using FluentAssertions;
#endregion

namespace Chaos.Networking.Tests;

public sealed class AscensionFloorUpdateConverterTests
{
    [Test]
    public void RoundTrip_ShouldPreserveAllFields_WhenBossAliveAndNotCleared()
    {
        var converter = new AscensionFloorUpdateConverter();

        var original = new AscensionFloorUpdateArgs
        {
            CurrentFloor = 3,
            BossAlive = true,
            BossName = "Test Boss",
            FirstClearers = [],
            HighestFloorCleared = 2
        };

        var writer = new SpanWriter(Encoding.ASCII);
        converter.Serialize(ref writer, original);
        var span = writer.ToSpan();

        var reader = new SpanReader(Encoding.ASCII, in span);
        var result = converter.Deserialize(ref reader);

        result.Should()
              .BeEquivalentTo(original);
    }

    [Test]
    public void RoundTrip_ShouldPreserveAllFields_WhenBossDeadAndCleared()
    {
        var converter = new AscensionFloorUpdateConverter();

        var original = new AscensionFloorUpdateArgs
        {
            CurrentFloor = 1,
            BossAlive = false,
            BossName = "Floor One Boss",
            FirstClearers = ["Ace", "Dream", "Hate"],
            HighestFloorCleared = 1
        };

        var writer = new SpanWriter(Encoding.ASCII);
        converter.Serialize(ref writer, original);
        var span = writer.ToSpan();

        var reader = new SpanReader(Encoding.ASCII, in span);
        var result = converter.Deserialize(ref reader);

        result.Should()
              .BeEquivalentTo(original);
    }

    [Test]
    public void RoundTrip_ShouldPreserveNullBossName_WhenNotInChamber()
    {
        var converter = new AscensionFloorUpdateConverter();

        var original = new AscensionFloorUpdateArgs
        {
            CurrentFloor = 0,
            BossAlive = false,
            BossName = null,
            FirstClearers = [],
            HighestFloorCleared = 0
        };

        var writer = new SpanWriter(Encoding.ASCII);
        converter.Serialize(ref writer, original);
        var span = writer.ToSpan();

        var reader = new SpanReader(Encoding.ASCII, in span);
        var result = converter.Deserialize(ref reader);

        result.Should()
              .BeEquivalentTo(original);
    }

    [Test]
    public void OpCode_ShouldMatchAscensionFloorUpdateOpCode()
    {
        var converter = new AscensionFloorUpdateConverter();

        converter.OpCode
                 .Should()
                 .Be((byte)Chaos.Networking.Abstractions.Definitions.ServerOpCode.AscensionFloorUpdate);
    }
}
