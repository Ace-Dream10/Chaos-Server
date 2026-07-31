#region
using Chaos.Collections;
using Chaos.Collections.Abstractions;
using Chaos.DarkAges.Definitions;
using Chaos.Formulae;
using Chaos.Geometry;
using Chaos.Models.Data;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Models.World.Abstractions;
using Chaos.Scripting.AislingScripts.Abstractions;
using Chaos.Scripting.Behaviors;
using Chaos.Services.Other.Abstractions;
using Chaos.Services.Servers.Options;
using Chaos.Storage.Abstractions;
using Chaos.Time;
using Chaos.Time.Abstractions;
#endregion

namespace Chaos.Scripting.AislingScripts;

public class DefaultAislingScript : AislingScriptBase
{
    /// <summary>
    ///     How long an Aisling can remain dead before being auto-revived at a Stacia's Shrine (or banished to the
    ///     Underworld, if hardcore)
    /// </summary>
    private static readonly TimeSpan DeathTimerDuration = TimeSpan.FromSeconds(12);

    /// <summary>
    ///     How often the skull visual is re-played while the Aisling is dead
    /// </summary>
    private static readonly TimeSpan SkullAnimationRefreshInterval = TimeSpan.FromMilliseconds(500);

    private static readonly Animation SkullAnimation = new()
    {
        TargetAnimation = 24,
        AnimationSpeed = 100
    };

    /// <summary>
    ///     PLACEHOLDER - no real Stacia's Shrine map exists yet, so this points at the class testing grounds (next
    ///     to the class NPCs) for now. Swap this for a real location once it's built. The Underworld below is still
    ///     unbuilt and points at monsterTest.
    /// </summary>
    private static readonly Point StaciasShrinePoint = new(233, 85);

    private const string StaciasShrineMapInstanceId = "map20003";
    private static readonly Point UnderworldPoint = new(5, 35);
    private const string UnderworldMapInstanceId = "monsterTest";

    /// <summary>
    ///     The percentage of max HP an Aisling is revived with when the death timer expires (non-hardcore)
    /// </summary>
    private const int ShrineReviveHpPct = 25;

    private readonly IStore<BulletinBoard> BoardStore;
    private readonly ISimpleCache Cache;
    private readonly IIntervalTimer ClearOrangeBarTimer;
    private readonly IMapTraversalService MapTraversalService;
    private readonly IStore<MailBox> MailStore;
    private readonly IIntervalTimer SleepAnimationTimer;
    private TimeSpan SinceDeath;
    private TimeSpan SinceLastSkullAnimation;
    private SocialStatus PreAfkSocialStatus { get; set; }
    protected virtual RelationshipBehavior RelationshipBehavior { get; }
    protected virtual RestrictionBehavior RestrictionBehavior { get; }
    protected virtual VisibilityBehavior VisibilityBehavior { get; }

    /// <inheritdoc />
    public DefaultAislingScript(
        Aisling subject,
        IStore<MailBox> mailStore,
        IStore<BulletinBoard> boardStore,
        ISimpleCache cache,
        IMapTraversalService mapTraversalService)
        : base(subject)
    {
        MailStore = mailStore;
        BoardStore = boardStore;
        Cache = cache;
        MapTraversalService = mapTraversalService;
        RestrictionBehavior = new RestrictionBehavior();
        VisibilityBehavior = new VisibilityBehavior();
        RelationshipBehavior = new RelationshipBehavior();
        SleepAnimationTimer = new IntervalTimer(TimeSpan.FromSeconds(5), false);
        ClearOrangeBarTimer = new IntervalTimer(TimeSpan.FromSeconds(WorldOptions.Instance.ClearOrangeBarTimerSecs), false);
    }

    /// <inheritdoc />
    public override bool CanDropItem(Item item) => RestrictionBehavior.CanDropItem(Subject, item);

    /// <inheritdoc />
    public override bool CanDropItemOn(Aisling source, Item item) => RestrictionBehavior.CanDropItemOn(source, item, Subject);

    /// <inheritdoc />
    public override bool CanDropMoney(int amount) => RestrictionBehavior.CanDropMoney(Subject, amount);

    /// <inheritdoc />
    public override bool CanDropMoneyOn(Aisling source, int amount) => RestrictionBehavior.CanDropMoneyOn(source, amount, Subject);

    /// <inheritdoc />
    public override bool CanMove() => RestrictionBehavior.CanMove(Subject);

    /// <inheritdoc />
    public override bool CanPickupItem(GroundItem groundItem) => RestrictionBehavior.CanPickupItem(Subject, groundItem);

    /// <inheritdoc />
    public override bool CanPickupMoney(Money money) => RestrictionBehavior.CanPickupMoney(Subject, money);

    /// <inheritdoc />
    public override bool CanSee(VisibleEntity entity) => VisibilityBehavior.CanSee(Subject, entity);

    /// <inheritdoc />
    public override bool CanTalk() => RestrictionBehavior.CanTalk(Subject);

    /// <inheritdoc />
    public override bool CanTurn() => RestrictionBehavior.CanTurn(Subject);

    /// <inheritdoc />
    public override bool CanUseItem(Item item) => RestrictionBehavior.CanUseItem(Subject, item);

    /// <inheritdoc />
    public override bool CanUseSkill(Skill skill) => RestrictionBehavior.CanUseSkill(Subject, skill);

    /// <inheritdoc />
    public override bool CanUseSpell(Spell spell) => RestrictionBehavior.CanUseSpell(Subject, spell);

    /// <inheritdoc />
    public override IEnumerable<BoardBase> GetBoardList()
    {
        //mailbox board
        yield return MailStore.Load(Subject.Name);

        //change this to whatever naming scheme you want to follow for guild boards
        if (Subject.Guild is not null && BoardStore.Exists(Subject.Guild.Name))
            yield return BoardStore.Load(Subject.Guild.Name);

        yield return BoardStore.Load("public_test_board");

        //things like... get board based on Nation, Guild, Enums, Flags, whatever
        //e.g.
        //var nationBoard = Subject.Nation switch
        //{
        //    Nation.Exile      => BoardStore.Load("nation_board_exile"),
        //    Nation.Suomi      => BoardStore.Load("nation_board_suomi"),
        //    Nation.Ellas      => BoardStore.Load("nation_board_ellas"),
        //    Nation.Loures     => BoardStore.Load("nation_board_loures"),
        //    Nation.Mileth     => BoardStore.Load("nation_board_mileth"),
        //    Nation.Tagor      => BoardStore.Load("nation_board_tagor"),
        //    Nation.Rucesion   => BoardStore.Load("nation_board_rucesion"),
        //    Nation.Noes       => BoardStore.Load("nation_board_noes"),
        //    Nation.Illuminati => BoardStore.Load("nation_board_illuminati"),
        //    Nation.Piet       => BoardStore.Load("nation_board_piet"),
        //    Nation.Atlantis   => BoardStore.Load("nation_board_atlantis"),
        //    Nation.Abel       => BoardStore.Load("nation_board_abel"),
        //    Nation.Undine     => BoardStore.Load("nation_board_undine"),
        //    Nation.Purgatory  => BoardStore.Load("nation_board_purgatory"),
        //    _                 => throw new ArgumentOutOfRangeException()
        //};
        //
        //yield return nationBoard;
    }

    /// <inheritdoc />
    public override bool IsFriendlyTo(Creature creature) => RelationshipBehavior.IsFriendlyTo(Subject, creature);

    /// <inheritdoc />
    public override bool IsHostileTo(Creature creature) => RelationshipBehavior.IsHostileTo(Subject, creature);

    /// <inheritdoc />
    public override void OnDeath()
    {
        Subject.IsDead = true;
        Subject.Refresh(true);
        Subject.Display();

        Subject.Animate(SkullAnimation, Subject.Id);
        SinceLastSkullAnimation = TimeSpan.Zero;
        SinceDeath = TimeSpan.Zero;
    }

    /// <summary>
    ///     Called when the death timer expires without the Aisling being revived. Hardcore Aislings are banished to
    ///     the Underworld (alive, but unable to leave). Everyone else is auto-revived at a Stacia's Shrine.
    /// </summary>
    private void HandleDeathTimerExpired()
    {
        if (Subject.Hardcore)
        {
            Subject.IsBanished = true;
            Subject.IsDead = false;
            Subject.StatSheet.SetHealthPct(100);
            Subject.SendOrangeBarMessage("Your journey ends here. You have been banished to the Underworld.");

            var underworld = Cache.Get<MapInstance>(UnderworldMapInstanceId);
            MapTraversalService.TraverseMap(Subject, underworld, UnderworldPoint);
        } else
        {
            Subject.IsDead = false;
            Subject.StatSheet.SetHealthPct(ShrineReviveHpPct);
            Subject.SendOrangeBarMessage("Stacia's Shrine has called your soul back to the living.");

            var shrine = Cache.Get<MapInstance>(StaciasShrineMapInstanceId);
            MapTraversalService.TraverseMap(Subject, shrine, StaciasShrinePoint);
        }

        Subject.Refresh(true);
    }

    /// <inheritdoc />
    public override void OnStatIncrease(Stat stat)
    {
        if (stat == Stat.STR)
            Subject.UserStatSheet.SetMaxWeight(LevelUpFormulae.Default.CalculateMaxWeight(Subject));
    }

    /// <inheritdoc />
    public override void Update(TimeSpan delta)
    {
        SleepAnimationTimer.Update(delta);
        ClearOrangeBarTimer.Update(delta);

        if (Subject.IsDead)
        {
            SinceLastSkullAnimation += delta;

            if (SinceLastSkullAnimation >= SkullAnimationRefreshInterval)
            {
                SinceLastSkullAnimation = TimeSpan.Zero;
                Subject.Animate(SkullAnimation, Subject.Id);
            }

            SinceDeath += delta;

            if (SinceDeath >= DeathTimerDuration)
                HandleDeathTimerExpired();
        }

        if (SleepAnimationTimer.IntervalElapsed)
        {
            var lastManualAction = Subject.Trackers.LastManualAction;

            var isAfk = !lastManualAction.HasValue
                        || (DateTime.UtcNow.Subtract(lastManualAction.Value)
                                    .TotalMinutes
                            > WorldOptions.Instance.SleepAnimationTimerMins);

            if (isAfk)
            {
                if (Subject.IsAlive)
                    Subject.AnimateBody(BodyAnimation.Snore);

                //set player to daydreaming if they are currently set to awake
                if (Subject.Options.SocialStatus != SocialStatus.DayDreaming)
                {
                    PreAfkSocialStatus = Subject.Options.SocialStatus;
                    Subject.Options.SocialStatus = SocialStatus.DayDreaming;
                }
            } else if (Subject.Options.SocialStatus == SocialStatus.DayDreaming)
                Subject.Options.SocialStatus = PreAfkSocialStatus;
        }

        if (ClearOrangeBarTimer.IntervalElapsed)
        {
            var lastOrangeBarMessage = Subject.Trackers.LastOrangeBarMessage;
            var now = DateTime.UtcNow;

            //clear if
            //an orange bar message has ever been sent
            //and the last message was sent after the last clear
            //and the time since the last message is greater than the clear timer
            var shouldClear = lastOrangeBarMessage.HasValue
                              && (lastOrangeBarMessage > (Subject.Trackers.LastOrangeBarMessageClear ?? DateTime.MinValue))
                              && (now.Subtract(lastOrangeBarMessage.Value)
                                     .TotalSeconds
                                  > WorldOptions.Instance.ClearOrangeBarTimerSecs);

            if (shouldClear)
            {
                Subject.SendServerMessage(ServerMessageType.OrangeBar1, string.Empty);
                Subject.Trackers.LastOrangeBarMessage = lastOrangeBarMessage;
                Subject.Trackers.LastOrangeBarMessageClear = now;
            }
        }
    }
}