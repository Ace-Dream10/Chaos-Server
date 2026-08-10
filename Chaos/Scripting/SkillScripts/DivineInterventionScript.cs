#region
using Chaos.DarkAges.Definitions;
using Chaos.Extensions;
using Chaos.Extensions.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.SkillScripts.Abstractions;
#endregion

namespace Chaos.Scripting.SkillScripts;

/// <summary>
///     Protection that evolves throughout the game.
/// </summary>
/// <remarks>
///     One of Valkyrie's 6 evolving abilities - evolves through TARGET SCOPE, not just numbers, per the locked
///     design ("Self → Ally → Small AoE → Party-wide"): Tier I shields only the caster; Tier II adds the ally
///     directly ahead (the tile-in-front convention several other skills already use for a single ally/enemy
///     target); Tier III shields nearby allies in a small radius; Tier IV shields the caster's whole real Group
///     (<see cref="Aisling.Group" />, via the same membership check <see cref="Chaos.Definitions.TargetFilter.GroupOnly" /> already
///     uses) rather than approximating "party-wide" with a large radius scan. Tier scales with the skill's own
///     level, re-derived for Divine Intervention's own floor arc (Floor 7 intro through Floor 10 max) - see
///     <see cref="GetTierValues" />.
/// </remarks>
public class DivineInterventionScript : ConfigurableSkillScriptBase
{
    /// <inheritdoc />
    public DivineInterventionScript(Skill subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnUse(ActivationContext context)
    {
        var source = context.Source;
        var map = context.TargetMap;
        var tier = GetTierValues();

        source.AnimateBody(BodyAnimation);

        if (Sound.HasValue)
            map.PlaySound(Sound.Value, source);

        foreach (var target in ResolveTargets(context, tier.Scope))
            ApplyShield(target, tier.ShieldAmount, tier.DurationMs);
    }

    private void ApplyShield(Creature target, int shieldAmount, int durationMs)
    {
        var shield = new DivineInterventionShieldEffect { ShieldAmount = shieldAmount };
        shield.SetDuration(TimeSpan.FromMilliseconds(durationMs));
        target.Effects.Apply(target, shield, this);

        if (Animation != null)
            target.Animate(Animation, target.Id);
    }

    private IEnumerable<Creature> ResolveTargets(ActivationContext context, int scope)
    {
        var source = context.Source;
        var map = context.TargetMap;

        yield return source;

        if (scope == 0)
            yield break;

        if (scope == 1)
        {
            var targetPoint = source.DirectionalOffset(source.Direction);
            var ally = map.GetEntitiesAtPoints<Creature>(targetPoint).TopOrDefault();

            if ((ally != null) && source.IsFriendlyTo(ally))
                yield return ally;

            yield break;
        }

        if (scope == 2)
        {
            foreach (var nearby in map.GetEntitiesWithinRange<Creature>(source, SmallAoeRange))
                if ((nearby != source) && source.IsFriendlyTo(nearby))
                    yield return nearby;

            yield break;
        }

        //scope 3: real party membership, not an approximated radius
        if ((source is Aisling { Group: not null } aisling))
            foreach (var member in aisling.Group)
                if (member.Id != aisling.Id)
                    yield return member;
    }

    /// <summary>
    ///     Placeholder tier values - not balance-tested. Floor7(Level&lt;=14)=I(intro,self), Floor8(&lt;=16)=II
    ///     (+targeted ally), Floor9(&lt;=18)=III(+small AoE), Floor10+(&gt;18)=IV(max, party-wide). Scope: 0=self
    ///     only, 1=self+targeted ally, 2=self+small AoE, 3=self+real group.
    /// </summary>
    private (int ShieldAmount, int DurationMs, int Scope) GetTierValues() =>
        Subject.Level switch
        {
            <= 14 => (80, 15000, 0),
            <= 16 => (110, 17000, 1),
            <= 18 => (140, 19000, 2),
            _     => (180, 22000, 3)
        };

    private const int SmallAoeRange = 3;

    #region ScriptVars
    /// <summary>
    ///     The animation played on each shielded target
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     The body animation played by the caster
    /// </summary>
    public BodyAnimation BodyAnimation { get; init; }

    /// <summary>
    ///     Sound played on cast
    /// </summary>
    public byte? Sound { get; init; }
    #endregion
}
