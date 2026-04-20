using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightSchool
{
    /// <summary>
    /// Manages the Flight School skill-tree graph (Phase 84).
    /// Child nodes unlock automatically when the parent's lesson is completed,
    /// forming a directed acyclic progression.
    /// </summary>
    public class SkillTreeController : MonoBehaviour
    {
        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired when a previously-locked node becomes unlocked.</summary>
        public event Action<SkillNode> OnNodeUnlocked;

        /// <summary>Fired when the entire skill tree is rebuilt or replaced.</summary>
        public event Action<SkillTreeData> OnTreeRebuilt;

        // ── Inspector ────────────────────────────────────────────────────────────

        [SerializeField] private FlightSchoolManager schoolManager;

        // ── Public state ─────────────────────────────────────────────────────────

        /// <summary>The current skill-tree dataset.</summary>
        public SkillTreeData Tree { get; private set; } = new SkillTreeData();

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (schoolManager == null) schoolManager = FlightSchoolManager.Instance;
        }

        private void OnEnable()
        {
            if (schoolManager != null)
                schoolManager.OnLessonCompleted += HandleLessonCompleted;
        }

        private void OnDisable()
        {
            if (schoolManager != null)
                schoolManager.OnLessonCompleted -= HandleLessonCompleted;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Replaces the current tree with <paramref name="tree"/>.
        /// Raises <see cref="OnTreeRebuilt"/>. Pass <c>null</c> to reset.
        /// </summary>
        public void SetTree(SkillTreeData tree)
        {
            Tree = tree ?? new SkillTreeData();
            OnTreeRebuilt?.Invoke(Tree);
        }

        /// <summary>
        /// Generates and installs the default Flight School skill tree
        /// (5 root nodes per <see cref="LessonCategory"/>, chained to the
        /// <see cref="CertificationType.MasterAviator"/> capstone).
        /// </summary>
        public SkillTreeData BuildDefaultTree()
        {
            var tree = new SkillTreeData
            {
                nodes = new List<SkillNode>
                {
                    new SkillNode { nodeId = "node_takeoff",         lessonId = "basic_takeoff",         displayName = "First Takeoff",       uiPosition = new Vector2(0, 0),   childNodeIds = new List<string> { "node_level_flight" } },
                    new SkillNode { nodeId = "node_level_flight",    lessonId = "basic_level_flight",    displayName = "Level Flight",         uiPosition = new Vector2(200, 0), childNodeIds = new List<string> { "node_navigation", "node_weather" } },
                    new SkillNode { nodeId = "node_navigation",      lessonId = "nav_waypoint",          displayName = "Waypoint Navigation",  uiPosition = new Vector2(400, -100), childNodeIds = new List<string> { "node_aerobatics", "node_emergency" } },
                    new SkillNode { nodeId = "node_weather",         lessonId = "weather_crosswind",     displayName = "Crosswind Landings",   uiPosition = new Vector2(400, 100),  childNodeIds = new List<string> { "node_formation" } },
                    new SkillNode { nodeId = "node_aerobatics",     lessonId = "aero_loop",             displayName = "Aerobatic Loop",       uiPosition = new Vector2(600, -200), childNodeIds = new List<string> { "node_master" } },
                    new SkillNode { nodeId = "node_emergency",      lessonId = "emergency_engine_out",  displayName = "Engine-Out Procedures", uiPosition = new Vector2(600, 0),    childNodeIds = new List<string> { "node_master" } },
                    new SkillNode { nodeId = "node_formation",      lessonId = "formation_basic",       displayName = "Formation Flying",     uiPosition = new Vector2(600, 200),  childNodeIds = new List<string> { "node_master" } },
                    new SkillNode { nodeId = "node_master",         lessonId = "master_checkride",      displayName = "Master Aviator",       uiPosition = new Vector2(900, 0),    childNodeIds = new List<string>() }
                }
            };

            // Root node starts unlocked.
            tree.nodes[0].isUnlocked = true;

            SetTree(tree);
            return tree;
        }

        /// <summary>
        /// Marks the node matching <paramref name="nodeId"/> as unlocked and
        /// raises <see cref="OnNodeUnlocked"/>. Returns <c>false</c> when the
        /// node is unknown or already unlocked.
        /// </summary>
        public bool UnlockNode(string nodeId)
        {
            var node = Tree?.FindNode(nodeId);
            if (node == null || node.isUnlocked) return false;

            node.isUnlocked = true;
            OnNodeUnlocked?.Invoke(node);
            return true;
        }

        /// <summary>Returns <c>true</c> if the named node is currently unlocked.</summary>
        public bool IsNodeUnlocked(string nodeId)
        {
            var node = Tree?.FindNode(nodeId);
            return node != null && node.isUnlocked;
        }

        /// <summary>
        /// Unlocks all direct children of <paramref name="parentNodeId"/>.
        /// Returns the number of nodes newly unlocked.
        /// </summary>
        public int UnlockChildrenOf(string parentNodeId)
        {
            var parent = Tree?.FindNode(parentNodeId);
            if (parent == null || parent.childNodeIds == null) return 0;

            int count = 0;
            foreach (var childId in parent.childNodeIds)
                if (UnlockNode(childId)) count++;
            return count;
        }

        /// <summary>Returns every node that's currently unlocked.</summary>
        public List<SkillNode> GetUnlockedNodes()
        {
            var result = new List<SkillNode>();
            if (Tree?.nodes == null) return result;
            foreach (var n in Tree.nodes)
                if (n != null && n.isUnlocked) result.Add(n);
            return result;
        }

        /// <summary>Returns the overall completion ratio of the tree in [0, 1].</summary>
        public float ComputeUnlockProgress()
        {
            if (Tree?.nodes == null || Tree.nodes.Count == 0) return 0f;
            int unlocked = 0;
            foreach (var n in Tree.nodes)
                if (n != null && n.isUnlocked) unlocked++;
            return (float)unlocked / Tree.nodes.Count;
        }

        // ── Event handlers ───────────────────────────────────────────────────────

        private void HandleLessonCompleted(FlightLesson lesson)
        {
            if (lesson == null || Tree == null) return;

            foreach (var node in Tree.FindNodesByLesson(lesson.lessonId))
            {
                if (!node.isUnlocked) UnlockNode(node.nodeId);
                UnlockChildrenOf(node.nodeId);
            }
        }
    }
}
