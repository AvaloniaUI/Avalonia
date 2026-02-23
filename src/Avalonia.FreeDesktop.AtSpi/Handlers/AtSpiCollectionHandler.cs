using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Automation.Peers;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    /// <summary>
    /// Implements the AT-SPI Collection interface for match-based node queries.
    /// </summary>
    internal sealed class AtSpiCollectionHandler(AtSpiServer server, AtSpiNode node) : IOrgA11yAtspiCollection
    {
        private enum MatchType
        {
            Invalid = 0,
            All = 1,
            Any = 2,
            None = 3,
            Empty = 4,
        }

        public uint Version => CollectionVersion;

        public ValueTask<List<AtSpiObjectReference>> GetMatchesAsync(
            AtSpiMatchRule rule, uint sortby, int count, bool traverse)
        {
            var results = new List<AtSpiObjectReference>();
            CollectMatches(node, rule, count, traverse, results, skipSelf: true);
            return ValueTask.FromResult(results);
        }

        public ValueTask<List<AtSpiObjectReference>> GetMatchesToAsync(
            DBusObjectPath currentObject, AtSpiMatchRule rule, uint sortby, uint tree,
            bool limitScope, int count, bool traverse)
        {
            // GetMatchesTo: find matches after currentObject in tree order
            var results = new List<AtSpiObjectReference>();
            var found = false;
            CollectMatchesOrdered(node, rule, count, traverse, results,
                currentObject.ToString(), ref found, after: true);
            return ValueTask.FromResult(results);
        }

        public ValueTask<List<AtSpiObjectReference>> GetMatchesFromAsync(
            DBusObjectPath currentObject, AtSpiMatchRule rule, uint sortby, uint tree,
            int count, bool traverse)
        {
            // GetMatchesFrom: find matches before currentObject in tree order
            var results = new List<AtSpiObjectReference>();
            var found = false;
            CollectMatchesOrdered(node, rule, count, traverse, results,
                currentObject.ToString(), ref found, after: false);
            return ValueTask.FromResult(results);
        }

        public ValueTask<AtSpiObjectReference> GetActiveDescendantAsync()
        {
            // Not implemented in most toolkits
            return ValueTask.FromResult(server.GetNullReference());
        }

        private void CollectMatches(
            AtSpiNode parent, AtSpiMatchRule rule, int count, bool traverse,
            List<AtSpiObjectReference> results, bool skipSelf)
        {
            if (count > 0 && results.Count >= count)
                return;

            if (!skipSelf && MatchesRule(parent, rule))
            {
                results.Add(server.GetReference(parent));
                if (count > 0 && results.Count >= count)
                    return;
            }

            var children = parent.Peer.GetChildren();
            foreach (var childPeer in children)
            {
                var childNode = AtSpiNode.GetOrCreate(childPeer, server);
                if (childNode is null)
                    continue;
                server.EnsureNodeRegistered(childNode);

                if (MatchesRule(childNode, rule))
                {
                    results.Add(server.GetReference(childNode));
                    if (count > 0 && results.Count >= count)
                        return;
                }

                if (traverse)
                    CollectMatches(childNode, rule, count, traverse, results, skipSelf: true);
            }
        }

        private void CollectMatchesOrdered(
            AtSpiNode parent, AtSpiMatchRule rule, int count, bool traverse,
            List<AtSpiObjectReference> results, string targetPath, ref bool pastTarget,
            bool after)
        {
            if (count > 0 && results.Count >= count)
                return;

            var children = parent.Peer.GetChildren();
            foreach (var childPeer in children)
            {
                if (count > 0 && results.Count >= count)
                    return;

                var childNode = AtSpiNode.GetOrCreate(childPeer, server);
                if (childNode is null)
                    continue;
                server.EnsureNodeRegistered(childNode);

                if (string.Equals(childNode.Path, targetPath, StringComparison.Ordinal))
                {
                    pastTarget = true;
                    if (traverse)
                        CollectMatchesOrdered(childNode, rule, count, traverse, results,
                            targetPath, ref pastTarget, after);
                    continue;
                }

                var shouldInclude = after ? pastTarget : !pastTarget;
                if (shouldInclude && MatchesRule(childNode, rule))
                {
                    results.Add(server.GetReference(childNode));
                    if (count > 0 && results.Count >= count)
                        return;
                }

                if (traverse)
                    CollectMatchesOrdered(childNode, rule, count, traverse, results,
                        targetPath, ref pastTarget, after);
            }
        }

        private static bool MatchesRule(AtSpiNode node, AtSpiMatchRule rule)
        {
            var match = MatchesStates(node, rule.States, (MatchType)rule.StateMatchType)
                     && MatchesRoles(node, rule.Roles, (MatchType)rule.RoleMatchType)
                     && MatchesInterfaces(node, rule.Interfaces, (MatchType)rule.InterfaceMatchType)
                     && MatchesAttributes(node, rule.Attributes, (MatchType)rule.AttributeMatchType);

            return rule.Invert ? !match : match;
        }

        private static bool MatchesStates(AtSpiNode node, List<int> ruleStates, MatchType matchType)
        {
            if (matchType is MatchType.Invalid or MatchType.Empty)
                return matchType != MatchType.Empty || IsEmptyBitSet(ruleStates);

            if (IsEmptyBitSet(ruleStates))
                return true;

            var nodeStates = node.ComputeStates();
            var nodeLow = nodeStates.Count > 0 ? nodeStates[0] : 0u;
            var nodeHigh = nodeStates.Count > 1 ? nodeStates[1] : 0u;
            var ruleLow = ruleStates.Count > 0 ? (uint)ruleStates[0] : 0u;
            var ruleHigh = ruleStates.Count > 1 ? (uint)ruleStates[1] : 0u;

            return matchType switch
            {
                MatchType.All => (nodeLow & ruleLow) == ruleLow && (nodeHigh & ruleHigh) == ruleHigh,
                MatchType.Any => (nodeLow & ruleLow) != 0 || (nodeHigh & ruleHigh) != 0,
                MatchType.None => (nodeLow & ruleLow) == 0 && (nodeHigh & ruleHigh) == 0,
                _ => true,
            };
        }

        private static bool MatchesRoles(AtSpiNode node, List<int> ruleRoles, MatchType matchType)
        {
            if (matchType is MatchType.Invalid or MatchType.Empty)
                return matchType != MatchType.Empty || IsEmptyBitSet(ruleRoles);

            if (IsEmptyBitSet(ruleRoles))
                return true;

            var role = (uint)AtSpiNode.ToAtSpiRole(node.Peer.GetAutomationControlType(), node.Peer);
            var bucket = (int)(role / 32);
            var bit = (int)(role % 32);
            var isSet = bucket < ruleRoles.Count && ((uint)ruleRoles[bucket] & (1u << bit)) != 0;

            return matchType switch
            {
                MatchType.All or MatchType.Any => isSet,
                MatchType.None => !isSet,
                _ => true,
            };
        }

        private static bool MatchesInterfaces(AtSpiNode node, List<string> ruleInterfaces, MatchType matchType)
        {
            if (matchType is MatchType.Invalid or MatchType.Empty)
                return matchType != MatchType.Empty || ruleInterfaces.Count == 0;

            if (ruleInterfaces.Count == 0)
                return true;

            var nodeInterfaces = node.GetSupportedInterfaces();

            return matchType switch
            {
                MatchType.All => AllInterfacesPresent(nodeInterfaces, ruleInterfaces),
                MatchType.Any => AnyInterfacePresent(nodeInterfaces, ruleInterfaces),
                MatchType.None => !AnyInterfacePresent(nodeInterfaces, ruleInterfaces),
                _ => true,
            };
        }

        private static bool AllInterfacesPresent(HashSet<string> nodeInterfaces, List<string> required)
        {
            return required.All(iface => nodeInterfaces.Contains(ResolveInterfaceName(iface)));
        }

        private static bool AnyInterfacePresent(HashSet<string> nodeInterfaces, List<string> required)
        {
            return required.Any(iface => nodeInterfaces.Contains(ResolveInterfaceName(iface)));
        }

        private static string ResolveInterfaceName(string name)
        {
            // ATs may pass short names like "Action" or full names like "org.a11y.atspi.Action"
            return name.Contains('.') ? name : $"org.a11y.atspi.{name}";
        }

        private static bool MatchesAttributes(AtSpiNode node, Dictionary<string, string>? ruleAttrs, MatchType matchType)
        {
            if (matchType is MatchType.Invalid or MatchType.Empty)
                return matchType != MatchType.Empty || (ruleAttrs == null || ruleAttrs.Count == 0);

            if (ruleAttrs == null || ruleAttrs.Count == 0)
                return true;

            // Build node attributes (same as AccessibleHandler.GetAttributesAsync)
            var nodeAttrs = new Dictionary<string, string>(StringComparer.Ordinal) { ["toolkit"] = "Avalonia" };
            var name = node.Peer.GetName();
            if (!string.IsNullOrEmpty(name))
                nodeAttrs["explicit-name"] = "true";

            return matchType switch
            {
                MatchType.All => AllAttributesMatch(nodeAttrs, ruleAttrs),
                MatchType.Any => AnyAttributeMatches(nodeAttrs, ruleAttrs),
                MatchType.None => !AnyAttributeMatches(nodeAttrs, ruleAttrs),
                _ => true,
            };
        }

        private static bool AllAttributesMatch(Dictionary<string, string> nodeAttrs, Dictionary<string, string> required)
        {
            foreach (var kv in required)
            {
                if (!nodeAttrs.TryGetValue(kv.Key, out var value) ||
                    !string.Equals(value, kv.Value, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static bool AnyAttributeMatches(Dictionary<string, string> nodeAttrs, Dictionary<string, string> required)
        {
            foreach (var kv in required)
            {
                if (nodeAttrs.TryGetValue(kv.Key, out var value) &&
                    string.Equals(value, kv.Value, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static bool IsEmptyBitSet(List<int>? values)
        {
            if (values == null || values.Count == 0)
                return true;
            return values.All(v => v == 0);
        }
    }
}
