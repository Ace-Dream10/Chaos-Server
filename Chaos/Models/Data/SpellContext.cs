#region
using Chaos.Collections;
using Chaos.Geometry.Abstractions;
using Chaos.Models.World.Abstractions;
#endregion

namespace Chaos.Models.Data;

public sealed record SpellContext : ActivationContext
{
    /// <summary>
    ///     The response to the prompt entered by the activator
    /// </summary>
    public string? PromptResponse { get; init; }

    /// <inheritdoc />
    public SpellContext(Creature source, Creature target, string? promptResponse = null)
        : base(source, target)
        => PromptResponse = promptResponse;

    /// <summary>
    ///     Ground-targeted cast - no entity target, just a map point. See <c>SpellTemplate.GroundTargeted</c>.
    /// </summary>
    public SpellContext(Creature source, IPoint target, MapInstance map, string? promptResponse = null)
        : base(source, target, map)
        => PromptResponse = promptResponse;
}