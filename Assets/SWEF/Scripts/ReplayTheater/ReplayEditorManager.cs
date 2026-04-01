using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_REPLAY_AVAILABLE
using SWEF.Replay; // Requires SWEF.Replay
#endif

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// Singleton manager that owns the replay editing workflow.
    /// Maintains the active <see cref="ReplayProject"/>, an undo/redo command stack,
    /// and an auto-save coroutine.
    /// </summary>
    public class ReplayEditorManager : MonoBehaviour
    {
        #region Singleton

        private static ReplayEditorManager _instance;

        /// <summary>Global singleton instance.</summary>
        public static ReplayEditorManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<ReplayEditorManager>();
                return _instance;
            }
        }

        #endregion

        #region Inspector

        [Header("Auto-Save")]
        [SerializeField] private float autoSaveInterval = 60f;

        #endregion

        #region State

        private ReplayProject _currentProject;
        private Stack<EditCommand> _undoStack = new Stack<EditCommand>();
        private Stack<EditCommand> _redoStack = new Stack<EditCommand>();

        #endregion

        #region Events

        /// <summary>Fired when a new <see cref="ReplayProject"/> is created.</summary>
        public event Action<ReplayProject> OnProjectCreated;

        /// <summary>Fired when the current project is saved.</summary>
        public event Action<ReplayProject> OnProjectSaved;

        /// <summary>Fired when a clip is added to the current project.</summary>
        public event Action<ReplayClip> OnClipAdded;

        /// <summary>Fired when a clip is removed from the current project.</summary>
        public event Action<ReplayClip> OnClipRemoved;

        /// <summary>Fired after any undo or redo operation completes.</summary>
        public event Action OnUndoRedo;

        #endregion

        #region Properties

        /// <summary>The currently open <see cref="ReplayProject"/>, or <c>null</c> if none is loaded.</summary>
        public ReplayProject CurrentProject => _currentProject;

        /// <summary>Whether there is at least one command available to undo.</summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>Whether there is at least one command available to redo.</summary>
        public bool CanRedo => _redoStack.Count > 0;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            StartCoroutine(AutoSaveCoroutine());
        }

        #endregion

        #region Public API

        /// <summary>
        /// Creates a new <see cref="ReplayProject"/> with the given title and sets it as current.
        /// </summary>
        /// <param name="title">Human-readable project title.</param>
        /// <returns>The newly created project.</returns>
        public ReplayProject CreateProject(string title)
        {
            _currentProject = new ReplayProject
            {
                projectId      = Guid.NewGuid().ToString(),
                title          = title,
                createdAt      = DateTime.UtcNow,
                lastModifiedAt = DateTime.UtcNow
            };

            _undoStack.Clear();
            _redoStack.Clear();

            Debug.Log($"[SWEF] ReplayEditorManager: Created project '{title}' ({_currentProject.projectId}).");
            OnProjectCreated?.Invoke(_currentProject);
            return _currentProject;
        }

        /// <summary>
        /// Loads an existing project by its identifier.
        /// Override this method to integrate with a persistence layer.
        /// </summary>
        /// <param name="projectId">Identifier of the project to load.</param>
        public void LoadProject(string projectId)
        {
            // Placeholder — persistence integration point
            Debug.Log($"[SWEF] ReplayEditorManager: LoadProject '{projectId}' (not yet persisted).");
        }

        /// <summary>
        /// Saves the current project, updating its <c>lastModifiedAt</c> timestamp.
        /// </summary>
        public void SaveProject()
        {
            if (_currentProject == null)
            {
                Debug.LogWarning("[SWEF] ReplayEditorManager: SaveProject called with no active project.");
                return;
            }

            _currentProject.lastModifiedAt = DateTime.UtcNow;
            Debug.Log($"[SWEF] ReplayEditorManager: Project '{_currentProject.title}' saved.");
            OnProjectSaved?.Invoke(_currentProject);
        }

        /// <summary>
        /// Appends a <see cref="ReplayClip"/> to the current project.
        /// </summary>
        /// <param name="clip">The clip to add.</param>
        public void AddClip(ReplayClip clip)
        {
            if (_currentProject == null)
            {
                Debug.LogWarning("[SWEF] ReplayEditorManager: AddClip — no active project.");
                return;
            }
            if (clip == null) return;

            _currentProject.clips.Add(clip);
            Debug.Log($"[SWEF] ReplayEditorManager: Clip '{clip.clipId}' added.");
            OnClipAdded?.Invoke(clip);
        }

        /// <summary>
        /// Removes the clip with the given identifier from the current project.
        /// </summary>
        /// <param name="clipId">Identifier of the clip to remove.</param>
        public void RemoveClip(string clipId)
        {
            if (_currentProject == null) return;

            var clip = _currentProject.clips.Find(c => c.clipId == clipId);
            if (clip == null)
            {
                Debug.LogWarning($"[SWEF] ReplayEditorManager: RemoveClip — clip '{clipId}' not found.");
                return;
            }

            _currentProject.clips.Remove(clip);
            Debug.Log($"[SWEF] ReplayEditorManager: Clip '{clipId}' removed.");
            OnClipRemoved?.Invoke(clip);
        }

        /// <summary>
        /// Executes an <see cref="EditCommand"/> and pushes it onto the undo stack.
        /// Clears the redo stack.
        /// </summary>
        /// <param name="cmd">The command to execute.</param>
        public void ExecuteCommand(EditCommand cmd)
        {
            if (cmd == null) return;
            cmd.Execute();
            _undoStack.Push(cmd);
            _redoStack.Clear();
            Debug.Log($"[SWEF] ReplayEditorManager: Executed command '{cmd.GetType().Name}'.");
        }

        /// <summary>Undoes the most recent command.</summary>
        public void Undo()
        {
            if (!CanUndo)
            {
                Debug.Log("[SWEF] ReplayEditorManager: Nothing to undo.");
                return;
            }

            var cmd = _undoStack.Pop();
            cmd.Undo();
            _redoStack.Push(cmd);
            Debug.Log($"[SWEF] ReplayEditorManager: Undid '{cmd.GetType().Name}'.");
            OnUndoRedo?.Invoke();
        }

        /// <summary>Redoes the most recently undone command.</summary>
        public void Redo()
        {
            if (!CanRedo)
            {
                Debug.Log("[SWEF] ReplayEditorManager: Nothing to redo.");
                return;
            }

            var cmd = _redoStack.Pop();
            cmd.Execute();
            _undoStack.Push(cmd);
            Debug.Log($"[SWEF] ReplayEditorManager: Redid '{cmd.GetType().Name}'.");
            OnUndoRedo?.Invoke();
        }

        #endregion

        #region Internals

        private IEnumerator AutoSaveCoroutine()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(autoSaveInterval);
                if (_currentProject != null)
                {
                    Debug.Log("[SWEF] ReplayEditorManager: Auto-saving project.");
                    SaveProject();
                }
            }
        }

        #endregion
    }
}
