#region
using Chaos.Models.World;
#endregion

namespace Chaos.Collections;

/// <summary>
///     Tracks the set of Aisling names that participated in a fight (dealt or received damage), keyed by name rather
///     than id so a participant who logs out or leaves before the fight ends is still captured
/// </summary>
public class ParticipantSet : IEnumerable<string>
{
    private readonly ConcurrentDictionary<string, byte> Names = new(StringComparer.OrdinalIgnoreCase);

    public bool Add(Aisling aisling) => Names.TryAdd(aisling.Name, 0);

    public void Clear() => Names.Clear();

    /// <inheritdoc />
    public IEnumerator<string> GetEnumerator() => Names.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
