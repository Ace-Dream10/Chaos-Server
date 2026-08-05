#region
using Chaos.Models.Data;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
using Moq;
#endregion

namespace Chaos.Tests;

public sealed class SpellAdminOnlyTests
{
    [Test]
    public void CanUse_ShouldReturnFalse_WhenAdminOnlySpell_AndCasterIsNotAdmin()
    {
        var map = MockMapInstance.Create();
        var caster = MockAisling.Create(map, setup: a => a.IsAdmin = false);
        var spell = MockSpell.Create(templateSetup: t => t with { AdminOnly = true });

        var canUse = caster.CanUse(spell, caster, null, out var context);

        canUse.Should()
              .BeFalse();

        context.Should()
               .BeNull();
    }

    [Test]
    public void CanUse_ShouldReturnTrue_WhenAdminOnlySpell_AndCasterIsAdmin()
    {
        var map = MockMapInstance.Create();
        var caster = MockAisling.Create(map, setup: a => a.IsAdmin = true);
        MockAisling.SetupScriptAllows(caster);
        var spell = MockSpell.Create(templateSetup: t => t with { AdminOnly = true });
        Mock.Get(spell.Script)
            .Setup(s => s.CanUse(It.IsAny<SpellContext>()))
            .Returns(true);

        var canUse = caster.CanUse(spell, caster, null, out var context);

        canUse.Should()
              .BeTrue();

        context.Should()
               .NotBeNull();
    }

    [Test]
    public void CanUse_ShouldReturnTrue_WhenNotAdminOnlySpell_AndCasterIsNotAdmin()
    {
        //regression check: AdminOnly defaults to false and must not affect ordinary spells for non-admins
        var map = MockMapInstance.Create();
        var caster = MockAisling.Create(map, setup: a => a.IsAdmin = false);
        MockAisling.SetupScriptAllows(caster);
        var spell = MockSpell.Create();
        Mock.Get(spell.Script)
            .Setup(s => s.CanUse(It.IsAny<SpellContext>()))
            .Returns(true);

        var canUse = caster.CanUse(spell, caster, null, out var context);

        canUse.Should()
              .BeTrue();

        context.Should()
               .NotBeNull();
    }

    [Test]
    public void CanUse_ShouldReturnFalse_WhenAdminOnlySpell_AndCasterIsMonster()
    {
        //monsters have no IsAdmin flag at all - admin-only spells must never be usable by monster AI
        var map = MockMapInstance.Create();
        var monster = MockMonster.Create(map);
        var spell = MockSpell.Create(templateSetup: t => t with { AdminOnly = true });

        var canUse = monster.CanUse(spell, monster, null, out var context);

        canUse.Should()
              .BeFalse();

        context.Should()
               .BeNull();
    }
}
