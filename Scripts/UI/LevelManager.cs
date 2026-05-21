/*
 ═══════════════════════════════════════════════════════════════
 LevelManager.cs
 ───────────────────────────────────────────────────────────────
 Singleton that persists across scenes.
 Responsibilities:
   • Owns the authoritative unlocked / completed level state
   • Provides static API used by MainMenuController
   • Handles async scene loading with a fade overlay
   • Fires events so in-level UI can react to completion
 ═══════════════════════════════════════════════════════════════
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoesOfTime.UI
{
    public class LevelManager : MonoBehaviour
    {
        // ─── Singleton ───────────────────────────────────────────
        public static LevelManager Instance { get; private set; }

        // ─── Events ──────────────────────────────────────────────
        public static event Action<string> OnLevelCompleted;
        public static event Action<string> OnLevelUnlocked;

        // ─── Scene names (set in Inspector or ProjectSettings) ───
        [Header("Scene Names")]
        [SerializeField] public string menuSceneName    = "MainMenu";
        [SerializeField] public string ancientSceneName = "Level_AncientEgypt";
        [SerializeField] public string medievalSceneName= "Level_MedievalEurope";
        [SerializeField] public string modernSceneName  = "Level_ModernAge";

        [Header("Transition")]
        [SerializeField] private float fadeDuration = 0.45f;

        // ─── PlayerPrefs keys ────────────────────────────────────
        private const string KEY_UNLOCKED  = "EoT_Unlocked";
        private const string KEY_COMPLETED = "EoT_Completed";

        // ─── State ───────────────────────────────────────────────
        private readonly HashSet<string> unlocked  = new() { "ancient" };
        private readonly HashSet<string> completed = new();

        private static readonly string[] EraOrder = { "ancient", "medieval", "modern" };

        // ─────────────────────────────────────────────────────────
        //  LIFECYCLE
        // ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Whenever the menu/map scene loads, force cursor to be visible and unlocked.
        /// This is a safety net in case a level scene left the cursor locked.
        /// </summary>
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
                                   UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (scene.name == menuSceneName)
            {
                Time.timeScale   = 1f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
            else if (scene.name == ancientSceneName  ||
                     scene.name == medievalSceneName ||
                     scene.name == modernSceneName)
            {
                // PlayerController.Start() locks the cursor on the same frame.
                // We wait two frames so our unlock runs AFTER it.
                StartCoroutine(UnlockCursorAfterStart());
            }
        }

        private System.Collections.IEnumerator UnlockCursorAfterStart()
        {
            yield return null;  // wait for all Start() calls
            yield return null;  // one extra frame safety
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        // ─────────────────────────────────────────────────────────
        //  PUBLIC QUERY API
        // ─────────────────────────────────────────────────────────

        public bool IsUnlocked(string era)  => unlocked.Contains(era);
        public bool IsCompleted(string era) => completed.Contains(era);

        public int CompletedCount => completed.Count;
        public int TotalLevels    => EraOrder.Length;

        /// <summary>Returns the fraction of levels completed (0–1).</summary>
        public float ProgressFraction =>
            Mathf.Max((float)completed.Count / TotalLevels, 0.033f);

        public string ProgressLabel =>
            completed.Count == 0
                ? "1 of 3 Eras Unlocked"
                : $"{completed.Count} of 3 Eras Complete";

        // ─────────────────────────────────────────────────────────
        //  COMPLETE A LEVEL
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Call this from in-level code when the player finishes the era.
        /// Unlocks the next era, saves, fires events, then returns to menu.
        /// </summary>
        public void CompleteLevel(string era)
        {
            if (completed.Add(era))
                OnLevelCompleted?.Invoke(era);

            // Unlock next
            int idx = Array.IndexOf(EraOrder, era);
            if (idx >= 0 && idx < EraOrder.Length - 1)
            {
                string next = EraOrder[idx + 1];
                if (unlocked.Add(next))
                    OnLevelUnlocked?.Invoke(next);
            }

            Save();
        }

        /// <summary>
        /// Completes the given era AND immediately loads the next era's scene.
        /// Call this from the "Continue to Hall 2 →" button in PuzzleUI.
        /// </summary>
        public void CompleteAndLoadNext(string era)
        {
            CompleteLevel(era);          // marks done, unlocks next, saves

            int idx = Array.IndexOf(EraOrder, era);
            if (idx >= 0 && idx < EraOrder.Length - 1)
            {
                string nextEra = EraOrder[idx + 1];
                LoadLevel(nextEra);
            }
            else
            {
                // All eras done — go back to menu (or a credits screen)
                LoadMenu();
            }
        }

        // ─────────────────────────────────────────────────────────
        //  SCENE LOADING
        // ─────────────────────────────────────────────────────────

        public void LoadLevel(string era)
        {
            if (!IsUnlocked(era)) return;
            string scene = EraToScene(era);
            StartCoroutine(LoadSceneAsync(scene));
        }

        public void LoadMenu()
        {
            StartCoroutine(LoadSceneAsync(menuSceneName));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            // Optional: fire a global fade-out event here if you have a
            // SceneFader component listening.

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogError($"[LevelManager] Scene '{sceneName}' not found. " +
                               "Add it to File > Build Settings.");
                yield break;
            }

            op.allowSceneActivation = false;

            // Wait until nearly ready, then activate
            while (op.progress < 0.9f)
                yield return null;

            yield return new WaitForSecondsRealtime(fadeDuration);

            op.allowSceneActivation = true;
        }

        // ─────────────────────────────────────────────────────────
        //  RESET (debug / cheat)
        // ─────────────────────────────────────────────────────────

        [ContextMenu("Reset All Progress")]
        public void ResetProgress()
        {
            unlocked.Clear();
            unlocked.Add("ancient");
            completed.Clear();
            Save();
            Debug.Log("[LevelManager] Progress reset.");
        }

        // ─────────────────────────────────────────────────────────
        //  PERSISTENCE
        // ─────────────────────────────────────────────────────────

        private void Save()
        {
            PlayerPrefs.SetString(KEY_UNLOCKED,  string.Join(",", unlocked));
            PlayerPrefs.SetString(KEY_COMPLETED, string.Join(",", completed));
            PlayerPrefs.Save();
        }

        private void Load()
        {
            string rawU = PlayerPrefs.GetString(KEY_UNLOCKED,  "ancient");
            string rawC = PlayerPrefs.GetString(KEY_COMPLETED, "");

            foreach (var s in rawU.Split(','))
                if (!string.IsNullOrWhiteSpace(s)) unlocked.Add(s.Trim());

            foreach (var s in rawC.Split(','))
                if (!string.IsNullOrWhiteSpace(s)) completed.Add(s.Trim());

            // Safety: ancient always unlocked
            unlocked.Add("ancient");
        }

        // ─────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────

        private string EraToScene(string era) => era switch
        {
            "ancient"  => ancientSceneName,
            "medieval" => medievalSceneName,
            "modern"   => modernSceneName,
            _          => menuSceneName
        };
    }
}
