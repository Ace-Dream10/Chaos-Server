#region
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;
#endregion

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="AscensionFloorUpdateArgs" />. Custom Elysium
///     opcode, not a retail packet shape - free to use a straightforward wire format rather than match any existing
///     retail convention.
/// </summary>
public sealed class AscensionFloorUpdateConverter : PacketConverterBase<AscensionFloorUpdateArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.AscensionFloorUpdate;

    /// <inheritdoc />
    public override AscensionFloorUpdateArgs Deserialize(ref SpanReader reader)
    {
        var currentFloor = reader.ReadByte();
        var bossAlive = reader.ReadBoolean();
        var hasBossName = reader.ReadBoolean();
        var bossName = hasBossName ? reader.ReadString8() : null;

        var firstClearerCount = reader.ReadByte();
        var firstClearers = new List<string>(firstClearerCount);

        for (var i = 0; i < firstClearerCount; i++)
            firstClearers.Add(reader.ReadString8());

        var highestFloorCleared = reader.ReadByte();

        return new AscensionFloorUpdateArgs
        {
            CurrentFloor = currentFloor,
            BossAlive = bossAlive,
            BossName = bossName,
            FirstClearers = firstClearers,
            HighestFloorCleared = highestFloorCleared
        };
    }

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, AscensionFloorUpdateArgs args)
    {
        writer.WriteByte(args.CurrentFloor);
        writer.WriteBoolean(args.BossAlive);
        writer.WriteBoolean(args.BossName is not null);

        if (args.BossName is not null)
            writer.WriteString8(args.BossName);

        writer.WriteByte((byte)args.FirstClearers.Count);

        foreach (var clearer in args.FirstClearers)
            writer.WriteString8(clearer);

        writer.WriteByte(args.HighestFloorCleared);
    }
}
