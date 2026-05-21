/*
 ═══════════════════════════════════════════════════════════════
 MainMenuController.cs  (Map Level Screen edition)
 ───────────────────────────────────────────────────────────────
 Changes from the original:
   • screen-levels now shows Game_Map.png with 3 badge buttons
     positioned absolutely over the map image.
   • RefreshMapButtons() applies gold/gray badge images and
     enables/disables the "Enter →" CTA based on LevelManager.
   • Everything else (Home, Info, Play screens) is unchanged.

 SETUP:
   1. Replace your old MainMenuController.cs with this file.
   2. In MainMenu.uxml replace the screen-levels block with
      the content from MapLevelScreen.uxml.
   3. Append MapLevelScreen.uss to MainMenu.uss.
   4. Place your images in Assets/Resources/UI/:
        Game_Map.png, 1.png, 2.png, 2_gray.png, 3.png, 3_gray.png
 ═══════════════════════════════════════════════════════════════
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace EchoesOfTime.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("References")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Transition")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip   sfxAmbient;   // looping background music
        [SerializeField] private AudioClip   sfxClick;     // any button click
        [SerializeField] private string      level1SceneName; // scene to load for Level 1

        // ─── Screens ─────────────────────────────────────────────
        private VisualElement screenHome;
        private VisualElement screenInfo;
        private VisualElement screenLevels;
        private VisualElement screenPlay;
        private VisualElement fadeOverlay;

private VisualElement _allCompleteBanner;
private Button        _btnResetProgress;

        // ─── Home buttons ────────────────────────────────────────
        private Button btnIdea, btnGoal, btnDevelopers, btnDiveIn;

        // ─── Info labels ─────────────────────────────────────────
        private Label  lblInfoCategory, lblInfoTitle, lblInfoBody;
        private Button btnBackFromInfo;

        // ─── Map badge containers (whole clickable zone) ──────────
        private VisualElement mapBtnAncient, mapBtnMedieval, mapBtnModern;

        // ─── Map badge image elements (swap gold / gray) ──────────
        private VisualElement mapBadgeAncient, mapBadgeMedieval, mapBadgeModern;

        // ─── Map "Enter →" CTA buttons ───────────────────────────
        private Button ctaAncient, ctaMedieval, ctaModern;

        // ─── Progress bar + text (reused from old screen) ─────────
        private VisualElement progressFill;
        private Label         progressText;
        private Button        btnBackFromLevels;

        // ─── Play screen ─────────────────────────────────────────
        private Label  lblPlayEraTag, lblPlayTitle, lblPlayDesc;
        private Button btnMarkComplete, btnBackFromPlay;

        // ─── Loading Screen ──────────────────────────────────────
        private VisualElement loadingScreen;
        private VisualElement loadingBar;
        private Label         loadingLabel;

        // ─── Runtime state ───────────────────────────────────────
        private string    activeEra = "ancient";
        private Coroutine glowCoroutine;

        // ─────────────────────────────────────────────────────────
        //  STATIC DATA
        // ─────────────────────────────────────────────────────────

        private static readonly Dictionary<string, (string Category, string Title, string Body)> InfoPages =
            new()
            {
                ["idea"] = (
                    "Game Idea",
                    "The Idea Behind the Game",
                    "Imagine walking into a museum but the artifacts have all gone missing. " +
                    "The <color=#F0D060><i>Echoes of Time</i></color> are fading. " +
                    "History itself is in danger.\n\n" +
                    "Echoes of Time is a <color=#F0D060><i>3D educational adventure game</i></color> " +
                    "set across three great eras: Ancient Egypt, Medieval Europe, and the Modern Age.\n\n" +
                    "As a young museum visitor you discover that precious artifacts have been displaced. " +
                    "Each artifact is an anchor to the past without them, history will be " +
                    "<color=#F0D060><i>erased forever</i></color>.\n\n" +
                    "Inspired by <color=#F0D060><i>Zelda: Breath of the Wild</i></color>, " +
                    "Echoes of Time turns history into something you <i>live</i>."
                ),
                ["goal"] = (
                    "Game Goal",
                    "Your Mission",
                    "Your goal is to <color=#F0D060><i>restore all missing artifacts</i></color> " +
                    "before the Echoes of Time are lost forever.\n\n" +
                    "<b>In each level you must:</b>\n\n" +
                    "🏺  <color=#F0D060><i>Explore</i></color> the hall and find hidden artifacts\n" +
                    "🧩  <color=#F0D060><i>Solve</i></color> reconstruction puzzles\n" +
                    "📜  <color=#F0D060><i>Unlock</i></color> the artifact's story\n" +
                    "📊  <color=#F0D060><i>Fill</i></color> your Knowledge Meter\n\n" +
                    "Complete all three eras and become a " +
                    "<color=#F0D060><i>Guardian of the Museum</i></color>."
                ),
                ["developers"] = (
                    "The Team",
                    "The Developers",
                    "Echoes of Time was created by students who believe " +
                    "<color=#F0D060><i>history should be an adventure</i></color>.\n\n" +
                    "👩‍💻  <color=#F0D060><i>Gehad Medhat Ali</i></color>\n" +
                    "Ancient Era - Level 1 \n\n" +
                    "👨‍💻  <color=#F0D060><i>Aisha Ibrahim Mohamed</i></color>\n" +
                    "Medieval Era - Level 2\n\n" +
                    "👩‍🎨  <color=#F0D060><i>Amr Khaled Khedr</i></color>\n" +
                    "Modern Era - Level 3\n\n" +
                    "Built with <color=#F0D060><i>Unity + Blender + C#</i></color>."
                )
            };

        private static readonly Dictionary<string, (string EraTag, string Title, string Desc)> LevelMeta =
            new()
            {
                ["ancient"]  = ("Ancient Egypt · Level 1",  "Ancient Hall",
                    "Five sacred artifacts have been scattered across the hall. " +
                    "Explore every corner, restore each piece, and hear the voices of Ancient Egypt."),
                ["medieval"] = ("Medieval Europe · Level 2", "Medieval Hall",
                    "Knights, manuscripts, and maps await. " +
                    "The age of chivalry needs your help to restore its lost legacy."),
                ["modern"]   = ("The Modern Age · Level 3",  "Modern Hall",
                    "From the light bulb to the space capsule — the greatest inventions of humanity " +
                    "have been scattered. Restore them all.")
            };

        // ─────────────────────────────────────────────────────────
        //  UNITY LIFECYCLE
        // ─────────────────────────────────────────────────────────

private void OnEnable()
{
    if (uiDocument == null)
        uiDocument = GetComponent<UIDocument>();

    var root = uiDocument.rootVisualElement;
    QueryAllElements(root);
    
    // Set fireworks target immediately so it's ready
    FindFirstObjectByType<MenuParticleController>()
        ?.SetFireworksTarget(screenLevels);
    
    BindAllCallbacks();
    InitialScreenState();
    StartAmbient();
}

        private void OnDisable()
        {
            if (glowCoroutine != null)
            {
                StopCoroutine(glowCoroutine);
                glowCoroutine = null;
            }
            if (audioSource != null) audioSource.Stop();
        }

        // ─────────────────────────────────────────────────────────
        //  ELEMENT QUERIES
        // ─────────────────────────────────────────────────────────

        private void QueryAllElements(VisualElement root)
        {
            // ── Screens ──
            screenHome   = root.Q("screen-home");
            screenInfo   = root.Q("screen-info");
            screenLevels = root.Q("screen-levels");
            screenPlay   = root.Q("screen-play");
            fadeOverlay  = root.Q("fade-overlay");

            // Loading screen
            loadingScreen = root.Q("loading-screen");
            loadingBar    = root.Q("loading-bar-fill");
            loadingLabel  = root.Q<Label>("loading-label");
            if (loadingScreen != null)
                loadingScreen.style.display = DisplayStyle.None;

            // ── Home ──
            btnIdea       = root.Q<Button>("btn-idea");
            btnGoal       = root.Q<Button>("btn-goal");
            btnDevelopers = root.Q<Button>("btn-developers");
            btnDiveIn     = root.Q<Button>("btn-divein");
            FindFirstObjectByType<MenuParticleController>()
    ?.SetFireworksTarget(screenLevels);

            // ── Info ──
            lblInfoCategory = root.Q<Label>("info-category");
            lblInfoTitle    = root.Q<Label>("info-title");
            lblInfoBody     = root.Q<Label>("info-body-text");
            btnBackFromInfo = root.Q<Button>("btn-back-info");

            // ── MAP: badge container elements ──
            mapBtnAncient  = root.Q("map-btn-ancient");
            mapBtnMedieval = root.Q("map-btn-medieval");
            mapBtnModern   = root.Q("map-btn-modern");

            // ── MAP: badge image elements (child VisualElements) ──
            mapBadgeAncient  = root.Q("map-badge-ancient");
            mapBadgeMedieval = root.Q("map-badge-medieval");
            mapBadgeModern   = root.Q("map-badge-modern");

            // ── MAP: CTA buttons ──
            ctaAncient  = root.Q<Button>("card-cta-ancient");
            ctaMedieval = root.Q<Button>("card-cta-medieval");
            ctaModern   = root.Q<Button>("card-cta-modern");

            // ── Progress ──
            progressFill     = root.Q("progress-fill");
            progressText     = root.Q<Label>("progress-text");
            btnBackFromLevels = root.Q<Button>("btn-back-levels");

            // ── Play ──
            lblPlayEraTag   = root.Q<Label>("play-era-tag");
            lblPlayTitle    = root.Q<Label>("play-title");
            lblPlayDesc     = root.Q<Label>("play-desc");
            btnMarkComplete = root.Q<Button>("btn-mark-complete");
            btnBackFromPlay = root.Q<Button>("btn-back-play");

_allCompleteBanner = root.Q("all-complete-banner");
_btnResetProgress  = root.Q<Button>("btn-reset-progress");
        }

        // ─────────────────────────────────────────────────────────
        //  BIND CALLBACKS
        // ─────────────────────────────────────────────────────────

        private void BindAllCallbacks()
        {
            // Home
            btnIdea?.RegisterCallback<ClickEvent>(_       => { PlayClick(); NavigateToInfo("idea"); });
            btnGoal?.RegisterCallback<ClickEvent>(_       => { PlayClick(); NavigateToInfo("goal"); });
            btnDevelopers?.RegisterCallback<ClickEvent>(_ => { PlayClick(); NavigateToInfo("developers"); });
            btnDiveIn?.RegisterCallback<ClickEvent>(_     => { PlayClick(); NavigateToLevels(); });

            // Info
            btnBackFromInfo?.RegisterCallback<ClickEvent>(_ => { PlayClick(); NavigateToHome(); });

            // Map level buttons
            ctaAncient?.RegisterCallback<ClickEvent>(_  => { PlayClick(); EnterLevel("ancient"); });
            ctaMedieval?.RegisterCallback<ClickEvent>(_ => { PlayClick(); EnterLevel("medieval"); });
            ctaModern?.RegisterCallback<ClickEvent>(_   => { PlayClick(); EnterLevel("modern"); });

            // Back from levels
            btnBackFromLevels?.RegisterCallback<ClickEvent>(_ => { PlayClick(); NavigateToHome(); });

            // Play screen
            btnMarkComplete?.RegisterCallback<ClickEvent>(_ => { PlayClick(); OnMarkComplete(); });
            btnBackFromPlay?.RegisterCallback<ClickEvent>(_ => { PlayClick(); NavigateToLevels(); });
            
_btnResetProgress?.RegisterCallback<ClickEvent>(_ => {
    PlayClick();
    LevelManager.Instance?.ResetProgress();
    if (_allCompleteBanner != null)
    {
        _allCompleteBanner.AddToClassList("hidden");
        _allCompleteBanner.style.display = DisplayStyle.None;  // ← add this
    }
    RefreshMapButtons();
});
        }

        // ─────────────────────────────────────────────────────────
        //  INITIAL STATE
        // ─────────────────────────────────────────────────────────

        private void InitialScreenState()
        {
            // Guard — log any screen that failed to resolve so you can spot
            // a missing name in the UXML immediately without a NullReference crash.
            if (screenHome   == null) Debug.LogError("[MainMenu] 'screen-home' not found in UXML.");
            if (screenInfo   == null) Debug.LogError("[MainMenu] 'screen-info' not found in UXML.");
            if (screenLevels == null) Debug.LogError("[MainMenu] 'screen-levels' not found in UXML.");
            if (screenPlay   == null) Debug.LogError("[MainMenu] 'screen-play' not found in UXML.");
            if (fadeOverlay  == null) Debug.LogError("[MainMenu] 'fade-overlay' not found in UXML.");

            // If returning from a level (any level completed), open map screen directly
            bool returningFromLevel = LevelManager.Instance != null
                                      && LevelManager.Instance.CompletedCount > 0;

            SetVisible(screenHome,   !returningFromLevel);
            SetVisible(screenInfo,   false);
            SetVisible(screenLevels, returningFromLevel);
            SetVisible(screenPlay,   false);

            if (returningFromLevel)
                RefreshMapButtons();

            if (fadeOverlay != null)
            {
                fadeOverlay.style.display = DisplayStyle.Flex;
                fadeOverlay.style.opacity = 0f;
                fadeOverlay.pickingMode   = PickingMode.Ignore;
            }
        }

        // ─────────────────────────────────────────────────────────
        //  NAVIGATION
        // ─────────────────────────────────────────────────────────

        private void NavigateToHome()
            => StartCoroutine(FadeToScreen(screenHome));

        private void NavigateToLevels()
        {
            RefreshMapButtons();
            StartCoroutine(FadeToScreen(screenLevels));
        }

        private void NavigateToInfo(string key)
        {
            if (!InfoPages.TryGetValue(key, out var page)) return;
            lblInfoCategory.text = page.Category;
            lblInfoTitle.text    = page.Title;
            lblInfoBody.text     = page.Body;
            StartCoroutine(FadeToScreen(screenInfo));
        }

        private void EnterLevel(string era)
        {
            LevelManager mgr = LevelManager.Instance;
            if (mgr != null && !mgr.IsUnlocked(era)) return;
            if (mgr == null && era != "ancient")       return; // editor fallback

            activeEra = era;

            // All eras: load their scene directly via LevelManager (or fallback by name)
            if (mgr != null)
            {
                StartCoroutine(FadeAndLoadLevelManager(era));
                return;
            }

            // No LevelManager (editor fallback) — ancient only via level1SceneName field
            if (era == "ancient" && !string.IsNullOrEmpty(level1SceneName))
            {
                StartCoroutine(FadeAndLoadScene(level1SceneName));
                return;
            }

            // Absolute fallback: load by era name directly
            string fallbackScene = era switch
            {
                "ancient"  => level1SceneName,
                "medieval" => "SampleScene",
                "modern"   => "level3",
                _          => ""
            };
            if (!string.IsNullOrEmpty(fallbackScene))
                StartCoroutine(FadeAndLoadScene(fallbackScene));
        }

        /// <summary>
        /// Fades out the menu then asks LevelManager to load the era scene.
        /// LevelManager.LoadLevel() handles its own async loading.
        /// We just need to fade first.
        /// </summary>
        private IEnumerator FadeAndLoadLevelManager(string era)
        {
            if (fadeOverlay != null)
            {
                fadeOverlay.pickingMode = PickingMode.Position;
                yield return StartCoroutine(UIAnimator.Fade(fadeOverlay, 0f, 1f, fadeDuration));
            }

            // Show loading screen
            if (loadingScreen != null)
            {
                loadingScreen.style.display = DisplayStyle.Flex;
                if (loadingBar   != null) loadingBar.style.width = Length.Percent(0f);
                if (loadingLabel != null) loadingLabel.text = "Loading...";
            }

            if (fadeOverlay != null)
                yield return StartCoroutine(UIAnimator.Fade(fadeOverlay, 1f, 0f, fadeDuration));

            LevelManager.Instance.LoadLevel(era);
        }

        private IEnumerator FadeAndLoadScene(string sceneName)
        {
            // 1. Fade the menu to black
            if (fadeOverlay != null)
            {
                fadeOverlay.pickingMode = PickingMode.Position;
                yield return StartCoroutine(UIAnimator.Fade(fadeOverlay, 0f, 1f, fadeDuration));
            }

            // 2. Show loading screen
            if (loadingScreen != null)
            {
                loadingScreen.style.display = DisplayStyle.Flex;
                if (loadingBar  != null) loadingBar.style.width  = Length.Percent(0f);
                if (loadingLabel != null) loadingLabel.text = "Loading...";
            }

            // Fade overlay back out to reveal loading screen
            if (fadeOverlay != null)
                yield return StartCoroutine(UIAnimator.Fade(fadeOverlay, 1f, 0f, fadeDuration));

            // 3. Start async load
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
            load.allowSceneActivation = false;

            // 4. Update progress bar while loading
            while (load.progress < 0.9f)
            {
                float pct = Mathf.Clamp01(load.progress / 0.9f) * 100f;
                if (loadingBar   != null) loadingBar.style.width  = Length.Percent(pct);
                if (loadingLabel != null) loadingLabel.text = $"Loading... {(int)pct}%";
                yield return null;
            }

            // Fill to 100%
            if (loadingBar   != null) loadingBar.style.width  = Length.Percent(100f);
            if (loadingLabel != null) loadingLabel.text = "Ready!";
            yield return new WaitForSecondsRealtime(0.4f);

            // 5. Fade to black then activate
            if (fadeOverlay != null)
            {
                fadeOverlay.pickingMode = PickingMode.Position;
                yield return StartCoroutine(UIAnimator.Fade(fadeOverlay, 0f, 1f, fadeDuration));
            }

            load.allowSceneActivation = true;
        }

        private void OnMarkComplete()
        {
            LevelManager.Instance?.CompleteLevel(activeEra);
            NavigateToLevels();
        }

        // ─────────────────────────────────────────────────────────
        //  MAP BUTTON REFRESH
        //  ─────────────────────────────────────────────────────────
        //  Rules:
        //    Era 1 (ancient)  — always gold, always clickable
        //    Era 2 (medieval) — gold if ancient completed, else gray
        //    Era 3 (modern)   — gold if medieval completed, else gray
        // ─────────────────────────────────────────────────────────

private void RefreshMapButtons()
{
    LevelManager mgr = LevelManager.Instance;
    bool ancientUnlocked  = true;
    bool medievalUnlocked = mgr != null ? mgr.IsUnlocked("medieval") : false;
    bool modernUnlocked   = mgr != null ? mgr.IsUnlocked("modern")   : false;

    ApplyMapButton(mapBtnAncient,  mapBadgeAncient,  ctaAncient,  ancientUnlocked);
    ApplyMapButton(mapBtnMedieval, mapBadgeMedieval, ctaMedieval, medievalUnlocked);
    ApplyMapButton(mapBtnModern,   mapBadgeModern,   ctaModern,   modernUnlocked);

    float pct         = mgr != null ? mgr.ProgressFraction * 100f : 33.3f;
    progressText.text = mgr != null ? mgr.ProgressLabel : "1 of 3 Eras Unlocked";
    StartCoroutine(UIAnimator.AnimateProgressBar(progressFill, pct));

    bool allDone = mgr != null && mgr.IsCompleted("ancient")
                               && mgr.IsCompleted("medieval")
                               && mgr.IsCompleted("modern");

    if (_allCompleteBanner != null)
    {
if (allDone)
{
    _allCompleteBanner.RemoveFromClassList("hidden");
    _allCompleteBanner.style.display = DisplayStyle.Flex;
    StartCoroutine(TriggerFireworksDelayed());
}
else
{
    _allCompleteBanner.AddToClassList("hidden");
    _allCompleteBanner.style.display = DisplayStyle.None;  // ← add this
}
    }
}

        /// <summary>
        /// Toggles gold vs gray badge image and enables/disables the Enter button.
        /// </summary>
        private static void ApplyMapButton(
            VisualElement container,
            VisualElement badge,
            Button        cta,
            bool          isUnlocked)
        {
            if (container == null || badge == null || cta == null) return;

            if (isUnlocked)
            {
                // Gold badge
                badge.RemoveFromClassList("map-badge--gray");
                badge.AddToClassList("map-badge--gold");

                // Clickable
                container.RemoveFromClassList("map-btn--locked");
                container.pickingMode = PickingMode.Position;

                cta.RemoveFromClassList("map-enter-btn--locked");
                cta.SetEnabled(true);
                cta.text = "Enter  →";
            }
            else
            {
                // Gray badge
                badge.RemoveFromClassList("map-badge--gold");
                badge.AddToClassList("map-badge--gray");

                // Locked / non-interactive
                container.AddToClassList("map-btn--locked");
                container.pickingMode = PickingMode.Ignore;

                cta.AddToClassList("map-enter-btn--locked");
                cta.SetEnabled(false);
                cta.text = "🔒 Locked";
            }
        }

        // ─────────────────────────────────────────────────────────
        //  SCREEN TRANSITIONS
        // ─────────────────────────────────────────────────────────

        private static void SetVisible(VisualElement el, bool visible)
        {
            if (el == null) return;   // ← null guard: missing UXML element won't crash
            el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            el.style.opacity = visible ? 1f : 0f;
            el.pickingMode   = visible ? PickingMode.Position : PickingMode.Ignore;
            el.RemoveFromClassList(visible ? "screen--hidden" : "screen--active");
            el.AddToClassList   (visible ? "screen--active"  : "screen--hidden");
        }

        private IEnumerator FadeToScreen(VisualElement target)
        {
            if (target == null) yield break;

            if (fadeOverlay != null)
            {
                fadeOverlay.pickingMode = PickingMode.Position;
                yield return StartCoroutine(UIAnimator.Fade(fadeOverlay, 0f, 1f, fadeDuration));
            }

            SetVisible(screenHome,   false);
            SetVisible(screenInfo,   false);
            SetVisible(screenLevels, false);
            SetVisible(screenPlay,   false);

            target.style.display = DisplayStyle.Flex;
            target.style.opacity = 1f;
            target.pickingMode   = PickingMode.Position;
            target.RemoveFromClassList("screen--hidden");
            target.AddToClassList("screen--active");

            if (fadeOverlay != null)
            {
                yield return StartCoroutine(UIAnimator.Fade(fadeOverlay, 1f, 0f, fadeDuration));
                fadeOverlay.pickingMode = PickingMode.Ignore;
            }

            StartEntranceAnimations(target);
        }

        private void StartEntranceAnimations(VisualElement target)
        {
            if (target == screenHome)
            {
                var homeTop     = target.Q("home-top");
                var infoButtons = target.Q("info-buttons");
                var diveinWrap  = target.Q("divein-wrap");
                if (homeTop     != null) StartCoroutine(UIAnimator.FadeSlide(homeTop,     -20f, 0.7f));
                if (infoButtons != null) StartCoroutine(UIAnimator.FadeSlide(infoButtons,  20f, 0.7f, 0.15f));
                if (diveinWrap  != null) StartCoroutine(UIAnimator.FadeSlide(diveinWrap,   20f, 0.7f, 0.28f));
            }
            else if (target == screenInfo)
            {
                var infoHeader = target.Q("info-header");
                var infoBody   = target.Q("info-body");
                if (infoHeader != null) StartCoroutine(UIAnimator.FadeSlide(infoHeader, -20f, 0.55f));
                if (infoBody   != null) StartCoroutine(UIAnimator.FadeSlide(infoBody,    20f, 0.55f, 0.12f));
            }
            else if (target == screenLevels)
            {
                // Map screen — gentle fade-in of the map itself + HUD
                var mapBg  = target.Q("map-bg");
                var mapHud = target.Q("map-hud");
                if (mapBg  != null) StartCoroutine(UIAnimator.FadeSlide(mapBg,   0f, 0.6f));
                if (mapHud != null) StartCoroutine(UIAnimator.FadeSlide(mapHud, 20f, 0.6f, 0.2f));
            }
        }
private IEnumerator TriggerFireworksDelayed()
{
    // Wait two frames for UIDocument to fully resolve layout
    yield return null;
    yield return null;

    var particles = FindFirstObjectByType<MenuParticleController>();
    particles?.SetFireworksTarget(screenLevels);
    particles?.TriggerFireworks();
}
        // ─────────────────────────────────────────────────────────
        //  AUDIO HELPERS
        // ─────────────────────────────────────────────────────────

        private void StartAmbient()
        {
            if (audioSource == null || sfxAmbient == null) return;
            audioSource.clip   = sfxAmbient;
            audioSource.loop   = true;
            audioSource.volume = 0.6f;
            audioSource.Play();
        }

        private void PlayClick()
        {
            if (audioSource != null && sfxClick != null)
                audioSource.PlayOneShot(sfxClick);
        }
    }
}
