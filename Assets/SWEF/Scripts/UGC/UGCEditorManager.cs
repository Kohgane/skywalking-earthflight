// UGCEditorManager.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.UGC
{
    /// <summary>
    /// Phase 108 — Singleton MonoBehaviour that acts as the central manager for the
    /// UGC editor.  Handles project lifecycle (create / open / save / load), an
    /// undo/redo command stack, editor-mode enter/exit, and periodic auto-save.
    ///
    /// <para>Attach to a persistent scene object — uses DontDestroyOnLoad.</para>
    /// </summary>
    public sealed class UGCEditorManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static UGCEditorManager Instance { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when a new project is created. Argument is the new content record.</summary>
        public event Action<UGCContent> OnProjectCreated;

        /// <summary>Raised after the project is saved to disk. Argument is the saved content record.</summary>
        public event Action<UGCContent> OnProjectSaved;

        /// <summary>Raised after a project is loaded from disk. Argument is the loaded content record.</summary>
        public event Action<UGCContent> OnProjectLoaded;

        /// <summary>Raised when the editor mode is toggled. Argument is <c>true</c> when entering editor mode.</summary>
        public event Action<bool> OnEditorModeChanged;

        // ── Inspector ──────────────────────────────────────────────────────────

        [Tooltip("Override auto-save interval (seconds). Defaults to UGCConfig.AutoSaveIntervalSeconds.")]
        [SerializeField] private float _autoSaveInterval = UGCConfig.AutoSaveIntervalSeconds;

        // ── Public state ───────────────────────────────────────────────────────

        /// <summary>The content project currently open in the editor. <c>null</c> when no project is open.</summary>
        public UGCContent CurrentProject { get; private set; }

        /// <summary>Returns <c>true</c> while the editor mode is active.</summary>
        public bool IsEditorModeActive { get; private set; }

        /// <summary>Returns <c>true</c> if the current project has unsaved changes.</summary>
        public bool HasUnsavedChanges { get; set; }

        // ── Undo / redo ────────────────────────────────────────────────────────

        /// <summary>Returns <c>true</c> if there is at least one action that can be undone.</summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>Returns <c>true</c> if there is at least one action that can be redone.</summary>
        public bool CanRedo => _redoStack.Count > 0;

        private readonly Stack<IEditorCommand> _undoStack = new Stack<IEditorCommand>();
        private readonly Stack<IEditorCommand> _redoStack = new Stack<IEditorCommand>();

        // ── Auto-save ──────────────────────────────────────────────────────────

        private Coroutine _autoSaveCoroutine;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureProjectsDirectory();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API — project lifecycle ─────────────────────────────────────

        /// <summary>
        /// Creates a new UGC content project and enters editor mode.
        /// </summary>
        /// <param name="authorId">Player identifier of the creator.</param>
        /// <param name="authorName">Display name of the creator.</param>
        /// <param name="type">Type of content being created.</param>
        public UGCContent CreateProject(string authorId, string authorName, UGCContentType type)
        {
            var content = UGCContent.Create(authorId, authorName, type);
            CurrentProject = content;
            ClearHistory();
            HasUnsavedChanges = true;

            OnProjectCreated?.Invoke(content);
            Debug.Log($"[UGCEditorManager] New project created: {content.contentId} ({type})");
            return content;
        }

        /// <summary>
        /// Saves the current project to the projects directory as JSON.
        /// </summary>
        public bool SaveProject()
        {
            if (CurrentProject == null)
            {
                Debug.LogWarning("[UGCEditorManager] SaveProject — no project open.");
                return false;
            }

            CurrentProject.updatedAt = DateTime.UtcNow.ToString("o");
            string json = JsonUtility.ToJson(CurrentProject, prettyPrint: true);
            string path = GetProjectFilePath(CurrentProject.contentId);

            try
            {
                File.WriteAllText(path, json);
                HasUnsavedChanges = false;
                OnProjectSaved?.Invoke(CurrentProject);
                Debug.Log($"[UGCEditorManager] Project saved: {path}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UGCEditorManager] SaveProject failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads a project from disk by its content ID.
        /// </summary>
        /// <param name="contentId">The content ID to load.</param>
        public bool LoadProject(string contentId)
        {
            string path = GetProjectFilePath(contentId);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[UGCEditorManager] LoadProject — file not found: {path}");
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                var content = JsonUtility.FromJson<UGCContent>(json);
                CurrentProject = content;
                ClearHistory();
                HasUnsavedChanges = false;
                OnProjectLoaded?.Invoke(content);
                Debug.Log($"[UGCEditorManager] Project loaded: {contentId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UGCEditorManager] LoadProject failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Returns a list of all saved project content IDs.
        /// </summary>
        public List<string> ListSavedProjects()
        {
            var ids = new List<string>();
            string dir = GetProjectsDirectory();
            if (!Directory.Exists(dir)) return ids;

            foreach (string file in Directory.GetFiles(dir, "*.json"))
                ids.Add(Path.GetFileNameWithoutExtension(file));

            return ids;
        }

        // ── Public API — editor mode ───────────────────────────────────────────

        /// <summary>
        /// Enters editor mode — pauses gameplay and activates the editor HUD.
        /// </summary>
        public void EnterEditorMode()
        {
            if (IsEditorModeActive) return;
            IsEditorModeActive = true;
            Time.timeScale = 0f;

            StartAutoSave();
            OnEditorModeChanged?.Invoke(true);
            Debug.Log("[UGCEditorManager] Editor mode entered.");
        }

        /// <summary>
        /// Exits editor mode — resumes gameplay and stops auto-save.
        /// </summary>
        public void ExitEditorMode()
        {
            if (!IsEditorModeActive) return;
            IsEditorModeActive = false;
            Time.timeScale = 1f;

            StopAutoSave();
            OnEditorModeChanged?.Invoke(false);
            Debug.Log("[UGCEditorManager] Editor mode exited.");
        }

        // ── Public API — undo / redo ───────────────────────────────────────────

        /// <summary>
        /// Executes <paramref name="command"/>, records it in the undo stack, and clears redo history.
        /// </summary>
        public void ExecuteCommand(IEditorCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
            HasUnsavedChanges = true;

            // Trim stack to max history depth
            while (_undoStack.Count > UGCConfig.MaxUndoHistory)
            {
                var arr = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = arr.Length - 2; i >= 0; i--)
                    _undoStack.Push(arr[i]);
            }
        }

        /// <summary>
        /// Reverses the most recently executed command.
        /// </summary>
        public void Undo()
        {
            if (!CanUndo) return;
            var cmd = _undoStack.Pop();
            cmd.Undo();
            _redoStack.Push(cmd);
            HasUnsavedChanges = true;
        }

        /// <summary>
        /// Re-applies the most recently undone command.
        /// </summary>
        public void Redo()
        {
            if (!CanRedo) return;
            var cmd = _redoStack.Pop();
            cmd.Execute();
            _undoStack.Push(cmd);
            HasUnsavedChanges = true;
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        private void StartAutoSave()
        {
            StopAutoSave();
            _autoSaveCoroutine = StartCoroutine(AutoSaveLoop());
        }

        private void StopAutoSave()
        {
            if (_autoSaveCoroutine != null)
            {
                StopCoroutine(_autoSaveCoroutine);
                _autoSaveCoroutine = null;
            }
        }

        private IEnumerator AutoSaveLoop()
        {
            while (IsEditorModeActive)
            {
                yield return new WaitForSecondsRealtime(_autoSaveInterval);
                if (HasUnsavedChanges && CurrentProject != null)
                    SaveProject();
            }
        }

        private string GetProjectsDirectory()
        {
            return Path.Combine(Application.persistentDataPath, UGCConfig.ProjectsDirectory);
        }

        private string GetProjectFilePath(string contentId)
        {
            return Path.Combine(GetProjectsDirectory(), contentId + ".json");
        }

        private void EnsureProjectsDirectory()
        {
            string dir = GetProjectsDirectory();
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Command pattern interface
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Interface for reversible editor commands used by the undo/redo system.
    /// </summary>
    public interface IEditorCommand
    {
        /// <summary>Performs the editor action.</summary>
        void Execute();

        /// <summary>Reverses the editor action.</summary>
        void Undo();
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Built-in command implementations
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Phase 108 — Command that adds a waypoint to the current project.
    /// </summary>
    public sealed class AddWaypointCommand : IEditorCommand
    {
        private readonly UGCContent  _content;
        private readonly UGCWaypoint _waypoint;

        /// <summary>Initialises the command.</summary>
        public AddWaypointCommand(UGCContent content, UGCWaypoint waypoint)
        {
            _content  = content;
            _waypoint = waypoint;
        }

        /// <inheritdoc/>
        public void Execute() => _content.waypoints.Add(_waypoint);

        /// <inheritdoc/>
        public void Undo() => _content.waypoints.Remove(_waypoint);
    }

    /// <summary>
    /// Phase 108 — Command that removes a waypoint from the current project.
    /// </summary>
    public sealed class RemoveWaypointCommand : IEditorCommand
    {
        private readonly UGCContent  _content;
        private readonly UGCWaypoint _waypoint;
        private int _index;

        /// <summary>Initialises the command.</summary>
        public RemoveWaypointCommand(UGCContent content, UGCWaypoint waypoint)
        {
            _content  = content;
            _waypoint = waypoint;
        }

        /// <inheritdoc/>
        public void Execute()
        {
            _index = _content.waypoints.IndexOf(_waypoint);
            _content.waypoints.Remove(_waypoint);
        }

        /// <inheritdoc/>
        public void Undo()
        {
            if (_index >= 0 && _index <= _content.waypoints.Count)
                _content.waypoints.Insert(_index, _waypoint);
            else
                _content.waypoints.Add(_waypoint);
        }
    }

    /// <summary>
    /// Phase 108 — Command that adds a trigger to the current project.
    /// </summary>
    public sealed class AddTriggerCommand : IEditorCommand
    {
        private readonly UGCContent _content;
        private readonly UGCTrigger _trigger;

        /// <summary>Initialises the command.</summary>
        public AddTriggerCommand(UGCContent content, UGCTrigger trigger)
        {
            _content = content;
            _trigger = trigger;
        }

        /// <inheritdoc/>
        public void Execute() => _content.triggers.Add(_trigger);

        /// <inheritdoc/>
        public void Undo() => _content.triggers.Remove(_trigger);
    }

    /// <summary>
    /// Phase 108 — Command that adds a zone to the current project.
    /// </summary>
    public sealed class AddZoneCommand : IEditorCommand
    {
        private readonly UGCContent _content;
        private readonly UGCZone    _zone;

        /// <summary>Initialises the command.</summary>
        public AddZoneCommand(UGCContent content, UGCZone zone)
        {
            _content = content;
            _zone    = zone;
        }

        /// <inheritdoc/>
        public void Execute() => _content.zones.Add(_zone);

        /// <inheritdoc/>
        public void Undo() => _content.zones.Remove(_zone);
    }
}
