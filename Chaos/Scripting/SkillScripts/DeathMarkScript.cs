#region
using Chaos.DarkAges.Definitions;
using Chaos.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     One of Assassin's 5 evolving abilities. Switched from the generic applyEffect script to a dedicated one so
///     each tier can construct <see cref="DeathMarkEffect" /> with its own healing values, the same "properties set
///     by the skill script before applying" convention <see cref="Chaos.Scripting.SkillScripts.BloodlustScript" />
///     established for <see cref="BloodlustEffect" />. Tiers per the locked design ("stronger healing → larger
///     healing radius → grants a temporary buff after a successful kill") mapped to Assassin's own floor arc
///     (Floor3 intro, Floor4, Floor5, Floor6 max) via the same "Level ≈ 2×Floor" ratio used throughout tonight -
///     all placeholder magnitudes, not balance-tested.
/// </summary>
public class DeathMarkScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public DeathMarkScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        var targetPoint = source.DirectionalOffset(source.Direction, Range);
        var target = map.GetEntitiesAtPoints<Creature>(targetPoint).TopOrDefault();

        if ((target == null) || !Filter.IsValidTarget(source, target))
            return;

        source.AnimateBody(BodyAnimation);

        var mark = new DeathMarkEffect
        {
            HealAmount = tier.HealAmount,
            HealRadius = tier.HealRadius,
            GrantsFervorOnKill = tier.GrantsFervor
        };

        target.Effects.Apply(source, mark, this);

        if (Animation != null)
            target.Animate(Animation, source.Id);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, targetPoint);
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor3(Level&lt;=6)=I(intro), Floor4(&lt;=8)=II(stronger
    ///     healing), Floor5(&lt;=10)=III(larger radius), Floor6+(&gt;10)=IV(max, +Fervor buff on collect).
    /// </summary>
    private (int HealAmount, int HealRadius, bool GrantsFervor) GetTierValues() =>
        Subject.Level switch
        {
            <= 6  => (40, 3, false),
            <= 8  => (70, 3, false),
            <= 10 => (70, 5, false),
            _     => (100, 5, true)
        };

    #region ScriptVars
    /// <summary>
    ///     The animation played on the target on mark
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster when the skill is used
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     The filter used to determine whether the tile directly in front of the caster holds a valid target
    /// </summary>
    public TargetFilter Filter { get; init; }

    /// <summary>
    ///     The range, in tiles, at which the target is checked
    /// </summary>
    public int Range { get; init; } = 6;

    /// <summary>
    ///     Sound played on mark
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
