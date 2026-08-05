#region
using System.Text;
using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Converters.Client;
using Chaos.Networking.Entities.Client;
using FluentAssertions;
#endregion

namespace Chaos.Networking.Tests;

/// <summary>
///     Byte-pinning coverage for the C→S "create group box" packet. Per the class of bug this guards against (a
///     consistent Rogue/Monk swap round-trips with itself perfectly, so a round-trip test alone would never have
///     caught it), these tests assert the exact wire bytes/field mapping directly rather than only checking that
///     deserializing your own serialized output returns your input.
/// </summary>
public sealed class GroupInviteConverterTests
{
    [Test]
    public void Serialize_ShouldWriteClassCapBytes_InWarriorWizardRoguePriestMonkOrder()
    {
        var converter = new GroupInviteConverter();

        var args = new GroupInviteArgs
        {
            ClientGroupSwitch = ClientGroupSwitch.CreateGroupbox,
            TargetName = "Ace",
            GroupBoxInfo = new CreateGroupBoxInfo
            {
                Name = "Test Group",
                Note = "Note",
                MinLevel = 10,
                MaxLevel = 99,
                MaxWarriors = 1,
                MaxWizards = 2,
                MaxRogues = 3,
                MaxPriests = 4,
                MaxMonks = 5
            }
        };

        var writer = new SpanWriter(Encoding.ASCII);
        converter.Serialize(ref writer, args);
        var span = writer.ToSpan();

        //header: switch(1) + "Ace" string8(1+3) + "Test Group" string8(1+10) + "Note" string8(1+4) + minLevel(1) + maxLevel(1)
        var headerLength = 1 + (1 + 3) + (1 + 10) + (1 + 4) + 1 + 1;

        var classCapBytes = span[headerLength..(headerLength + 5)]
                             .ToArray();

        classCapBytes.Should()
                     .Equal([1, 2, 3, 4, 5], "the wire order must be Warrior, Wizard, Rogue, Priest, Monk");
    }

    [Test]
    public void Deserialize_ShouldMapHandConstructedBytes_ToCorrectlyNamedProperties()
    {
        var converter = new GroupInviteConverter();

        //hand-constructed independent of Serialize - proves Deserialize's read order directly
        var writer = new SpanWriter(Encoding.ASCII);
        writer.WriteByte((byte)ClientGroupSwitch.CreateGroupbox);
        writer.WriteString8("Ace");
        writer.WriteString8("Test Group");
        writer.WriteString8("Note");
        writer.WriteByte(10); //minLevel
        writer.WriteByte(99); //maxLevel
        writer.WriteByte(1); //warriors
        writer.WriteByte(2); //wizards
        writer.WriteByte(3); //rogues
        writer.WriteByte(4); //priests
        writer.WriteByte(5); //monks

        var span = writer.ToSpan();
        var reader = new SpanReader(Encoding.ASCII, in span);
        var result = converter.Deserialize(ref reader);

        result.GroupBoxInfo!.MaxWarriors
              .Should()
              .Be(1);

        result.GroupBoxInfo.MaxWizards
              .Should()
              .Be(2);

        result.GroupBoxInfo.MaxRogues
              .Should()
              .Be(3);

        result.GroupBoxInfo.MaxPriests
              .Should()
              .Be(4);

        result.GroupBoxInfo.MaxMonks
              .Should()
              .Be(5);
    }

    [Test]
    public void RoundTrip_ShouldPreserveAllFields()
    {
        var converter = new GroupInviteConverter();

        var original = new GroupInviteArgs
        {
            ClientGroupSwitch = ClientGroupSwitch.CreateGroupbox,
            TargetName = "Ace",
            GroupBoxInfo = new CreateGroupBoxInfo
            {
                Name = "Test Group",
                Note = "Note",
                MinLevel = 10,
                MaxLevel = 99,
                MaxWarriors = 1,
                MaxWizards = 2,
                MaxRogues = 3,
                MaxPriests = 4,
                MaxMonks = 5
            }
        };

        var writer = new SpanWriter(Encoding.ASCII);
        converter.Serialize(ref writer, original);
        var span = writer.ToSpan();

        var reader = new SpanReader(Encoding.ASCII, in span);
        var result = converter.Deserialize(ref reader);

        result.Should()
              .BeEquivalentTo(original);
    }
}
