#region
using Chaos.Models.Data;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.EffectScripts;
using Chaos.Scripting.ReactorTileScripts.Abstractions;
#endregion

namespace Chaos.Scripting.ReactorTileScripts;

/// <summary>
///     Placed by Trickster's Web Trap. Roots and slows the first hostile monster to step on it, then removes
///     itself - whether triggered or simply left alone until <see cref="TrapDurationMs" /> elapses. Decrements the
///     owner's active-trap counter either way, so Web Trap's own "max traps active" check stays accurate.
/// </summary>
public class WebTrapReactorScript : ConfigurableReactorTileScriptBase
{
    public const string TrapCountCounterKey = "webTrapCount";

    private TimeSpan Elapsed;
    private bool Removed;

    /// <inheritdoc />
    public WebTrapReactorScript(ReactorTile subject)
        : base(subject) { }

    /// <inheritdoc />
    public override void OnWalkedOn(Creature source)
    {
        if (Removed || (Subject.Owner is not { } owner))
            return;

        if ((source is not Monster monster) || !monster.IsAlive || !owner.IsHostileTo(monster))
            return;

        var rootEffect = new RootEffect();
        rootEffect.SetDuration(TimeSpan.FromMilliseconds(RootDurationMs));
        monster.Effects.Apply(owner, rootEffect, this);

        var slowEffect = new SlowEffect();
        slowEffect.SetDuration(TimeSpan.FromMilliseconds(SlowDurationMs));
        monster.Effects.Apply(owner, slowEffect, this);

        if (Animation != null)
            monster.Animate(Animation, owner.Id);

        RemoveTrap();
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        if (Removed)
            return;

        Elapsed += delta;

        if (Elapsed >= TimeSpan.FromMilliseconds(TrapDurationMs))
            RemoveTrap();
    }

    private void RemoveTrap()
    {
        if (Removed)
            return;

        Removed = true;

        if ((Subject.Owner is Aisling ownerAisling) && ownerAisling.Trackers.Counters.TryGetValue(TrapCountCounterKey, out var count))
            ownerAisling.Trackers.Counters.Set(TrapCountCounterKey, Math.Max(0, count - 1));

        Map.RemoveEntity(Subject);
    }

    #region ScriptVars
    /// <summary>
    ///     The animation played on the triggering monster
    /// </summary>
    public Animation? Animation { get; init; }

    /// <summary>
    ///     How long, in milliseconds, the triggering monster is rooted for
    /// </summary>
    public int RootDurationMs { get; init; } = 3000;

    /// <summary>
    ///     How long, in milliseconds, the triggering monster is slowed for
    /// </summary>
    public int SlowDurationMs { get; init; } = 5000;

    /// <summary>
    ///     How long, in milliseconds, the trap lingers before disappearing untriggered
    /// </summary>
    public int TrapDurationMs { get; init; } = 30000;
    #endregion
}
