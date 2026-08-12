#region
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
#endregion

namespace Chaos.Scripting.Behaviors;

/// <summary>
///     "Action-blocking" CC tag checks below (rooted/blackout/asleep/stasis/silenced) use the exact same tag
///     strings the Monster-side AI scripts (AttackingScript/CastingScript/MoveToTargetScript/WanderingScript/
///     AggroTargetingScript) already check - previously these tags were only ever enforced against Monster
///     subjects, so applying any of these effects to an Aisling set the tag but had zero mechanical effect. This is
///     the other half of that enforcement, wired through the same CanMove/CanUseSkill/CanUseSpell hooks
///     DefaultAislingScript already delegates to here. "Agency-override" effects (Delirium/Feared/Puppeteer) are
///     deliberately NOT included - see the status-effect design investigation for why those need a real design
///     decision, not just a tag check, before touching player input.
/// </summary>
public class RestrictionBehavior
{
    public virtual bool CanDropItem(Aisling aisling, Item item) => aisling.IsAlive;

    public virtual bool CanDropItemOn(Aisling aisling, Item item, Creature target) => aisling.IsAlive;

    public virtual bool CanDropMoney(Aisling aisling, int amount) => aisling.IsAlive;

    public virtual bool CanDropMoneyOn(Aisling aisling, int amount, Creature target) => aisling.IsAlive;

    public virtual bool CanMove(Creature creature)
        => creature.IsAlive
           && !creature.Trackers.Tags.ContainsKey("fortified")
           && !creature.Trackers.Tags.ContainsKey("rooted")
           && !creature.Trackers.Tags.ContainsKey("stasis")
           && !creature.Trackers.Tags.ContainsKey("asleep");

    public virtual bool CanPickupItem(Aisling aisling, GroundItem groundItem) => aisling.IsAlive;

    public virtual bool CanPickupMoney(Aisling aisling, Money money) => aisling.IsAlive;

    public virtual bool CanTalk(Creature creature) => creature.IsAlive;

    public virtual bool CanTurn(Creature creature) => creature.IsAlive;

    public virtual bool CanUseItem(Aisling aisling, Item item) => aisling.IsAlive;

    public virtual bool CanUseSkill(Creature creature, Skill skill)
        => creature.IsAlive
           && !creature.Trackers.Tags.ContainsKey("blackout")
           && !creature.Trackers.Tags.ContainsKey("asleep")
           && !creature.Trackers.Tags.ContainsKey("stasis");

    public virtual bool CanUseSpell(Creature creature, Spell spell)
        => creature.IsAlive
           && !creature.Trackers.Tags.ContainsKey("blackout")
           && !creature.Trackers.Tags.ContainsKey("asleep")
           && !creature.Trackers.Tags.ContainsKey("stasis")
           && !creature.Trackers.Tags.ContainsKey("silenced");
}