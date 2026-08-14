#region
using Chaos.Collections;
using Chaos.Common.Abstractions;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Models.Panel;
using Chaos.Models.World;
using Chaos.Scripting.SpellScripts;
using Chaos.Services.Factories.Abstractions;
using Chaos.Testing.Infrastructure.Harnesses;
using Chaos.Testing.Infrastructure.Mocks;
using FluentAssertions;
using Moq;
#endregion

namespace Chaos.Tests;

/// <summary>
///     Real functional tests for HazardFieldScript's "lineN" footprint, added for Fire Wall's rework from a 3x3
///     square blob to a real wall shape per playtest feedback.
/// </summary>
public sealed class HazardFieldScriptTests
{
    private static void EnsureSpellVars(Spell spell, string scriptKey) => spell.Template.ScriptVars[scriptKey] = new EmptyScriptVars();

    [Test]
    public void LineFootprint_ShouldPlaceHazardsInARow_ExtendingInTheCastersFacingDirection()
    {
        MapInstance? map = null;
        var monsterFactoryMock = new Mock<IMonsterFactory>();

        monsterFactoryMock
            .Setup(
                f => f.Create(
                    It.IsAny<string>(),
                    It.IsAny<MapInstance>(),
                    It.IsAny<Chaos.Geometry.Abstractions.IPoint>(),
                    It.IsAny<ICollection<string>?>()))
            .Returns(
                (string templateKey, MapInstance _, Chaos.Geometry.Abstractions.IPoint spawnPoint, ICollection<string>? _) =>
                    MockMonster.Create(map!, templateSetup: t => t with { TemplateKey = templateKey }, setup: m => m.WarpTo(spawnPoint)));

        var serviceProvider = MockServiceProvider.CreateBuilder()
                                                  .SetupService(monsterFactoryMock.Object)
                                                  .Build()
                                                  .Object;

        var harness = new SpellScriptHarness<HazardFieldScript>(
            scriptFactory: spell => new HazardFieldScript(spell, monsterFactoryMock.Object)
            {
                Footprint = "line5",
                HazardTemplateKey = "elemental_hazard"
            },
            spellSetup: s => EnsureSpellVars(s, "hazardField"),
            serviceProvider: serviceProvider);

        map = harness.Map;
        harness.Source.Direction = Direction.Right;
        var originPoint = Point.From(harness.Source);

        harness.Use();

        var hazardPoints = map.GetEntities<Monster>()
                              .Select(Point.From)
                              .OrderBy(p => p.X)
                              .ToList();

        hazardPoints.Should()
                    .HaveCount(5, "a \"line5\" footprint should place exactly 5 hazard tiles");

        for (var i = 0; i < 5; i++)
            hazardPoints[i]
                .Should()
                .Be(new Point(originPoint.X + i, originPoint.Y), $"hazard {i} should extend {i} tiles right of the caster");
    }

    private sealed class EmptyScriptVars : IScriptVars
    {
        public bool ContainsKey(string key) => false;
        public object? Get(Type type, string name) => null;
        public T? Get<T>(string name) => default;
        public T GetRequired<T>(string key) => throw new KeyNotFoundException(key);
        public void Set<T>(string name, T value) { }
    }
}
