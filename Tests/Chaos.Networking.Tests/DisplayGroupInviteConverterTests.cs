#region
using System.Text;
using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Converters.Server;
using Chaos.Networking.Entities.Server;
using FluentAssertions;
#endregion

namespace Chaos.Networking.Tests;

/// <summary>
///     Byte-pinning coverage for the S→C "display group box" packet, covering the fixed Rogue/Monk byte-order bug
///     (Deserialize previously read Monk where Rogue's values are on the wire, and vice versa - the same class of
///     bug fixed upstream in dalib PR #22). Per that bug's own lesson, a round-trip test alone cannot catch a
///     consistent swap, so these assert the exact wire bytes and the hand-constructed-to-property mapping directly.
/// </summary>
/// <remarks>
///     NOTE: while writing these, a second, separate, unrelated bug was found in this same converter -
///     <c>Serialize</c> only writes the group box body when <c>ServerGroupSwitch.ShowGroupBox</c> (the value
///     actually used in production, see <c>WorldServer.cs</c>), but <c>Deserialize</c> only reads it back when the
///     switch is <c>ServerGroupSwitch.Invite</c> (a different numeric value: Invite=1, ShowGroupBox=4). This means
///     no single switch value currently makes a real round trip of the group-box body succeed - it's a distinct bug
///     from the byte-order issue this task fixed, out of scope for this fix, and has NOT been touched. Reported
///     separately; these tests are written to accurately reflect - not paper over - that existing behavior.
/// </remarks>
public sealed class DisplayGroupInviteConverterTests
{
    [Test]
    public void Serialize_ShouldWriteClassCapPairs_InWarriorWizardRoguePriestMonkOrder()
    {
        var converter = new DisplayGroupInviteConverter();

        var args = new DisplayGroupInviteArgs
        {
            ServerGroupSwitch = ServerGroupSwitch.ShowGroupBox, //the value actually used in production
            SourceName = "Ace",
            GroupBoxInfo = new DisplayGroupBoxInfo
            {
                Name = "Test Group",
                Note = "Note",
                MinLevel = 10,
                MaxLevel = 99,
                MaxWarriors = 1,
                CurrentWarriors = 11,
                MaxWizards = 2,
                CurrentWizards = 12,
                MaxRogues = 3,
                CurrentRogues = 13,
                MaxPriests = 4,
                CurrentPriests = 14,
                MaxMonks = 5,
                CurrentMonks = 15
            }
        };

        var writer = new SpanWriter(Encoding.ASCII);
        converter.Serialize(ref writer, args);
        var span = writer.ToSpan();

        //header: switch(1) + "Ace" string8(1+3) + "Test Group" string8(1+10) + "Note" string8(1+4) + minLevel(1) + maxLevel(1)
        var headerLength = 1 + (1 + 3) + (1 + 10) + (1 + 4) + 1 + 1;

        var classCapBytes = span[headerLength..(headerLength + 10)]
                             .ToArray();

        classCapBytes.Should()
                     .Equal(
                         [1, 11, 2, 12, 3, 13, 4, 14, 5, 15],
                         "the wire order must be (Max,Current) pairs for Warrior, Wizard, Rogue, Priest, Monk");
    }

    [Test]
    public void Deserialize_ShouldMapHandConstructedBytes_ToCorrectlyNamedProperties()
    {
        var converter = new DisplayGroupInviteConverter();

        //hand-constructed independent of Serialize - proves Deserialize's read order directly.
        //Uses ServerGroupSwitch.Invite because that's what Deserialize's own condition currently checks for the
        //group-box body (see the class remarks above) - this is not the production value, but it IS what exercises
        //Deserialize's box-info-reading branch as the code stands today.
        var writer = new SpanWriter(Encoding.ASCII);
        writer.WriteByte((byte)ServerGroupSwitch.Invite);
        writer.WriteString8("Ace");
        writer.WriteString8("Test Group");
        writer.WriteString8("Note");
        writer.WriteByte(10); //minLevel
        writer.WriteByte(99); //maxLevel
        writer.WriteByte(1); //maxWarriors
        writer.WriteByte(11); //currentWarriors
        writer.WriteByte(2); //maxWizards
        writer.WriteByte(12); //currentWizards
        writer.WriteByte(3); //maxRogues
        writer.WriteByte(13); //currentRogues
        writer.WriteByte(4); //maxPriests
        writer.WriteByte(14); //currentPriests
        writer.WriteByte(5); //maxMonks
        writer.WriteByte(15); //currentMonks

        var span = writer.ToSpan();
        var reader = new SpanReader(Encoding.ASCII, in span);
        var result = converter.Deserialize(ref reader);

        result.GroupBoxInfo!.MaxWarriors
              .Should()
              .Be(1);

        result.GroupBoxInfo.CurrentWarriors
              .Should()
              .Be(11);

        result.GroupBoxInfo.MaxWizards
              .Should()
              .Be(2);

        result.GroupBoxInfo.CurrentWizards
              .Should()
              .Be(12);

        result.GroupBoxInfo.MaxRogues
              .Should()
              .Be(3);

        result.GroupBoxInfo.CurrentRogues
              .Should()
              .Be(13);

        result.GroupBoxInfo.MaxPriests
              .Should()
              .Be(4);

        result.GroupBoxInfo.CurrentPriests
              .Should()
              .Be(14);

        result.GroupBoxInfo.MaxMonks
              .Should()
              .Be(5);

        result.GroupBoxInfo.CurrentMonks
              .Should()
              .Be(15);
    }

    [Test]
    public void RoundTrip_ShouldPreserveSwitchAndSourceName_WhenNoGroupBoxBodyInvolved()
    {
        //Deliberately NOT testing a round trip of the group-box body here: because of the separate Invite/
        //ShowGroupBox mismatch documented in the class remarks, there is currently no single switch value for
        //which Serialize writes the body AND Deserialize reads it back on the same object - that's a distinct,
        //unfixed bug, not something to paper over with a misleading "passing" round-trip test. This covers the
        //part that legitimately round-trips today: the switch type and source name for a switch value that
        //involves no group-box body on either side.
        var converter = new DisplayGroupInviteConverter();

        var original = new DisplayGroupInviteArgs
        {
            ServerGroupSwitch = ServerGroupSwitch.RequestToJoin,
            SourceName = "Ace",
            GroupBoxInfo = null
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
