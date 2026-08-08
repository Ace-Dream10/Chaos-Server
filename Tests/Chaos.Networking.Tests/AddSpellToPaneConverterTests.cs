#region
using System.Text;
using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Converters.Server;
using Chaos.Networking.Entities.Server;
using FluentAssertions;
#endregion

namespace Chaos.Networking.Tests;

/// <summary>
///     Round-trip tests for the Description/IsPassive fields added to SpellInfo's wire format, alongside the
///     existing fields (Prompt/CastLines/SpellType) to confirm the extension didn't disturb them.
/// </summary>
public sealed class AddSpellToPaneConverterTests
{
    [Test]
    public void RoundTrip_ShouldPreserveAllFields_IncludingDescriptionAndIsPassive()
    {
        var converter = new AddSpellToPaneConverter();

        var original = new AddSpellToPaneArgs
        {
            Spell = new SpellInfo
            {
                Slot = 3,
                Sprite = 39,
                SpellType = SpellType.Targeted,
                PanelName = "Absolute Zero",
                Prompt = string.Empty,
                CastLines = 0,
                Description = "Strike a single enemy with devastating cold.",
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
