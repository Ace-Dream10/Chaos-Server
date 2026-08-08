#region
using System.Text;
using Chaos.IO.Memory;
using Chaos.Networking.Converters.Server;
using Chaos.Networking.Entities.Server;
using FluentAssertions;
#endregion

namespace Chaos.Networking.Tests;

/// <summary>
///     Round-trip tests for the Description/IsPassive fields added to SkillInfo's wire format - confirms the
///     protocol extension actually carries these end to end, not just that the C# properties exist.
/// </summary>
public sealed class AddSkillToPaneConverterTests
{
    [Test]
    public void RoundTrip_ShouldPreserveDescriptionAndIsPassive_ForPassiveSkill()
    {
        var converter = new AddSkillToPaneConverter();

        var original = new AddSkillToPaneArgs
        {
            Skill = new SkillInfo
            {
                Slot = 5,
                Sprite = 46,
                PanelName = "Carnage",
                Description = "(Passive) The lower your Health, the more damage you deal.",
                IsPassive = true
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

    [Test]
    public void RoundTrip_ShouldPreserveEmptyDescriptionAndFalseIsPassive_ForOrdinaryActiveSkill()
    {
        var converter = new AddSkillToPaneConverter();

        var original = new AddSkillToPaneArgs
        {
            Skill = new SkillInfo
            {
                Slot = 1,
                Sprite = 46,
                PanelName = "Cyclone",
                Description = string.Empty,
                IsPassive = false
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
