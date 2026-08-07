#region
using Chaos.DarkAges.Definitions;
using Chaos.Models.World;
using Chaos.Scripting.AislingScripts.Abstractions;
#endregion

namespace Chaos.Scripting.AislingScripts;

/// <summary>
///     Unbroken - a true always-on Berserker passive, following the same pattern as
///     <see cref="BerserkerRageScript" />: no-ops unless the subject is currently a Berserker, no skill activation
///     or cooldown involved. Automatically keeps <see cref="ReadyTag" /> armed; the actual death-prevention effect
///     (survive a lethal hit at 1 HP, consume the tag) is handled directly in
///     <see cref="Chaos.Scripting.FunctionalScripts.ApplyDamage.ApplyAttackDamageScript" />, mirroring how
///     Phoenix Rise's tag is checked there. Unlike Phoenix Rise (a castable buff, one save per cast), this re-arms
///     itself automatically after <see cref="RearmInterval" /> - that's the actual difference between an activated
///     defensive cooldown and a true passive.
/// </summary>
/// <remarks>
///     Corrected from an earlier build that shipped this as a learnable, cooldown-gated active skill (a straight
///     rename of the pre-existing "Axe Block" full-damage-block mechanic) - that mechanic cannot become an
///     always-on passive without either becoming permanent invulnerability (if left as unconditional damage
///     negation) or needing a genuine redesign. This is that redesign: paired with Carnage per the locked design
///     ("Unbroken keeps you alive at low HP, Carnage rewards being there").
/// </remarks>
public class BerserkerUnbrokenScript : AislingScriptBase
{
    public const string ReadyTag = "unbroken_ready";

    /// <summary>
    ///     Placeholder value, not balance-tested. How often the death-save re-arms.
    /// </summary>
    private static readonly TimeSpan RearmInterval = TimeSpan.FromSeconds(60);

    private TimeSpan SinceLastArm = RearmInterval;

    /// <inheritdoc />
    public BerserkerUnbrokenScript(Aisling subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Subject.UserStatSheet.BaseClass != BaseClass.Berserker)
            return;

        if (Subject.Trackers.Tags.ContainsKey(ReadyTag))
            return;

        SinceLastArm += delta;

        if (SinceLastArm < RearmInterval)
            return;

        SinceLastArm = TimeSpan.Zero;
        Subject.Trackers.Tags[ReadyTag] = bool.TrueString;
    }
}
