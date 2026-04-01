// StationModuleGenerator.cs — SWEF Space Station & Orbital Docking System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SpaceStation
{
    /// <summary>
    /// A connected segment within the station layout graph.
    /// </summary>
    [Serializable]
    public class StationSegmentNode
    {
        /// <summary>Unique identifier for this node within the layout.</summary>
        public string nodeId;

        /// <summary>The type of module this node represents.</summary>
        public StationSegmentType segmentType;

        /// <summary>World-space transform offset from the station root.</summary>
        public Vector3 localPosition;

        /// <summary>World-space rotation offset from the station root.</summary>
        public Quaternion localRotation;

        /// <summary>Identifiers of adjacent nodes connected via hatches.</summary>
        public List<string> connectedNodeIds = new List<string>();
    }

    /// <summary>
    /// A fully connected graph of <see cref="StationSegmentNode"/> instances
    /// produced by <see cref="StationModuleGenerator.GenerateLayout"/>.
    /// </summary>
    [Serializable]
    public class StationLayout
    {
        /// <summary>Identifier of the station this layout belongs to.</summary>
        public string stationId;

        /// <summary>All segment nodes in the layout.</summary>
        public List<StationSegmentNode> nodes = new List<StationSegmentNode>();

        /// <summary>Returns the first node of the given type, or null.</summary>
        public StationSegmentNode FindFirst(StationSegmentType type)
        {
            foreach (StationSegmentNode node in nodes)
                if (node.segmentType == type) return node;
            return null;
        }

        /// <summary>Returns true when the layout contains at least one node of each required type.</summary>
        public bool IsValid()
        {
            bool hasDocking  = false;
            bool hasHabitat  = false;
            bool hasCommand  = false;

            foreach (StationSegmentNode node in nodes)
            {
                if (node.segmentType == StationSegmentType.Docking)  hasDocking  = true;
                if (node.segmentType == StationSegmentType.Habitat)  hasHabitat  = true;
                if (node.segmentType == StationSegmentType.Command)  hasCommand  = true;
            }
            return hasDocking && hasHabitat && hasCommand;
        }
    }

    /// <summary>
    /// Static utility that procedurally generates a <see cref="StationLayout"/>
    /// from a <see cref="StationDefinition"/>.
    /// </summary>
    public static class StationModuleGenerator
    {
        // Minimum spacing between adjacent modules (metres)
        private const float ModuleSpacing = 20f;

        /// <summary>
        /// Generates a connected station layout.  Ensures at least one Docking,
        /// Habitat, and Command module exist.  Uses <paramref name="seed"/> for
        /// deterministic output.
        /// </summary>
        public static StationLayout GenerateLayout(StationDefinition definition, int seed = 0)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var layout = new StationLayout { stationId = definition.stationId };
            var rng    = new System.Random(seed);

            // Determine segment list — guarantee minimum requirements
            var segments = new List<StationSegmentType>(definition.segments);
            EnsureRequiredSegments(segments);

            // Place segments in a linear chain along the X axis
            for (int i = 0; i < segments.Count; i++)
            {
                string nodeId = $"{definition.stationId}_seg_{i}";
                var node = new StationSegmentNode
                {
                    nodeId        = nodeId,
                    segmentType   = segments[i],
                    localPosition = new Vector3(i * ModuleSpacing, 0f, 0f),
                    localRotation = Quaternion.identity
                };

                // Connect to the previous node bidirectionally
                if (i > 0)
                {
                    string prevId = $"{definition.stationId}_seg_{i - 1}";
                    node.connectedNodeIds.Add(prevId);
                    layout.nodes[i - 1].connectedNodeIds.Add(nodeId);
                }

                layout.nodes.Add(node);
            }

            return layout;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static void EnsureRequiredSegments(List<StationSegmentType> segments)
        {
            if (!segments.Contains(StationSegmentType.Docking))
                segments.Insert(0, StationSegmentType.Docking);
            if (!segments.Contains(StationSegmentType.Habitat))
                segments.Add(StationSegmentType.Habitat);
            if (!segments.Contains(StationSegmentType.Command))
                segments.Add(StationSegmentType.Command);
        }
    }
}
