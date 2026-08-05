#region
using System.Collections.Frozen;
using Chaos.DarkAges.Definitions;
using Chaos.MetaData.Abstractions;
#endregion

namespace Chaos.MetaData.ClassMetaData;

/// <summary>
///     Represents a collection of <see cref="AbilityMetaNode" /> that can be split into sub-sequences
/// </summary>
public sealed class AbilityMetaNodeCollection : MetaNodeCollection<AbilityMetaNode>, ISplittingMetaNodeCollection<AbilityMetaNode>
{
    /// <inheritdoc />
    public IEnumerable<MetaDataBase<AbilityMetaNode>> Split()
    {
        var nodesByClass = Nodes.OrderBy(node => node.Class)
                                .ThenBy(node => node.IsSkill)
                                .ThenBy(node => node.Level)
                                .GroupBy(node => node.Class)
                                .ToFrozenDictionary(grp => grp.Key, grp => grp.ToArray());

        foreach (var nodeGroup in nodesByClass)
        {
            var name = $"SClass{(byte)nodeGroup.Key}";

            var metadata = new AbilityMetaData(name);
            IEnumerable<AbilityMetaNode> nodes = nodeGroup.Value;

            // anyone can learn universal (unregistered-restricted) skills
            if (nodeGroup.Key is not BaseClass.Unregistered)
                nodes = nodesByClass.TryGetValue(BaseClass.Unregistered, out var universalNodes) ? nodes.Concat(universalNodes) : nodes;

            foreach (var node in nodes)
                metadata.AddNode(node);

            metadata.Compress();

            yield return metadata;
        }
    }
}