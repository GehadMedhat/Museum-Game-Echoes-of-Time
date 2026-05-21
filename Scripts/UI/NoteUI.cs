/*
 ═══════════════════════════════════════════════════════════
 NoteUI.cs  —  EchoesOfTime  (Ancient Egypt)
 ───────────────────────────────────────────────────────────
 Sprites assigned in Inspector:
   • sideBarSprite  → hieroglyph-sidebar  background
   • piecesSprite   → sidebar-pieces-top & bot
   • dividerSprite  → divider-img
   • bottomSprite   → bottom-img
 ═══════════════════════════════════════════════════════════
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EchoesOfTime.UI;
using Cursor = UnityEngine.Cursor;

public class NoteUI : MonoBehaviour
{
    public static NoteUI Instance { get; private set; }

    [SerializeField] private UIDocument uiDocument;

    // ── Sprite Images (assign in Inspector) ──────────────
    [Header("Decorative Sprites")]
    [SerializeField] private Sprite sideBarSprite;   // side_bar.png
    [SerializeField] private Sprite piecesSprite;    // pieces.png   (sparkle ornament)
    [SerializeField] private Sprite dividerSprite;   // divider_bar.png
    [SerializeField] private Sprite bottomSprite;    // bottom.png

    // ── Visual Elements ──────────────────────────────────
    private VisualElement _overlay;
    private VisualElement _noteCard;
    private VisualElement _illustration;
    private VisualElement _sidebarEl;
    private VisualElement _sidebarPiecesTop;
    private VisualElement _sidebarPiecesBot;
    private VisualElement _dividerImg;
    private VisualElement _bottomImg;
    private Label         _hieroglyphSymbols;
    private Label         _statueName;
    private Label         _noteText;
    private Label         _pageIndicator;
    private Label         _readStamp;
    private Label         _categoryBadge;
    private Button        _closeBtn;
    private Button        _prevBtn;
    private Button        _nextBtn;
    private Button        _narratorBtn;

    // ── State ────────────────────────────────────────────
    private NoteData _currentNote;
    private int      _currentPage;
    private bool     _isOpen;
    private bool     _narratorPlaying;

    public bool IsOpen => _isOpen;

    // ── Coroutine handles ────────────────────────────────
    private Coroutine _glowRoutine;
    private Coroutine _papyrusRoutine;

    // ── Read tracking ────────────────────────────────────
    private static readonly HashSet<string> _readNotes = new HashSet<string>();

    // ── Narrator icons ───────────────────────────────────
    private const string NarratorIdleIcon     = "♪";
    private const string NarratorPlayingIcon  = "⏹";
    private const string NarratorPlayingClass = "narrator-btn--playing";

    // ── Hieroglyph symbols per category ─────────────────
    private static string GetHieroglyphSymbols(NoteCategory cat)
    {
        return cat switch
        {
            NoteCategory.Death     => "▼\n◆\n●\n◆\n▼",
            NoteCategory.Gods      => "▲\n◇\n▲\n◇\n▲",
            NoteCategory.Pharaoh   => "■\n◆\n■\n◆\n■",
            NoteCategory.Afterlife => "◆\n▲\n◆\n▲\n◆",
            _                      => "◆\n○\n◆\n○\n◆",
        };
    }

    // ── Audio ─────────────────────────────────────────────
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   sfxOpen;
    [SerializeField] private AudioClip   sfxClose;
    [SerializeField] private AudioClip   sfxPageTurn;

    [Header("Narrator")]
    [SerializeField] private AudioSource narratorAudioSource;

    // ────────────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;

        var root = uiDocument.rootVisualElement;

        _overlay          = root.Q("overlay");
        _noteCard         = root.Q("note-card");
        _illustration     = root.Q("illustration");
        _sidebarEl        = root.Q("hieroglyph-sidebar");
        _sidebarPiecesTop = root.Q("sidebar-pieces-top");
        _sidebarPiecesBot = root.Q("sidebar-pieces-bot");
        _dividerImg       = root.Q("divider-img");
        _bottomImg        = root.Q("bottom-img");
        _hieroglyphSymbols = root.Q<Label>("hieroglyph-symbols");
        _statueName       = root.Q<Label>("statue-name");
        _noteText         = root.Q<Label>("note-text");
        _pageIndicator    = root.Q<Label>("page-indicator");
        _readStamp        = root.Q<Label>("read-stamp");
        _categoryBadge    = root.Q<Label>("category-badge");
        _closeBtn         = root.Q<Button>("close-btn");
        _prevBtn          = root.Q<Button>("prev-btn");
        _nextBtn          = root.Q<Button>("next-btn");
        _narratorBtn      = root.Q<Button>("narrator-btn");

        _closeBtn.clicked += HideNote;
        _prevBtn.clicked  += PrevPage;
        _nextBtn.clicked  += NextPage;
        if (_narratorBtn != null) _narratorBtn.clicked += PlayNarrator;

        _overlay.style.display = DisplayStyle.None;

        // Apply the 4 decorator sprites once on startup
        ApplySprites();
    }

    // ── Apply Inspector sprites to UI elements ────────────
    private void ApplySprites()
    {
        // side_bar.png  →  entire sidebar background
        if (sideBarSprite != null && _sidebarEl != null)
            _sidebarEl.style.backgroundImage = new StyleBackground(sideBarSprite);

        // pieces.png  →  sparkle ornament top & bottom of sidebar
        if (piecesSprite != null)
        {
            if (_sidebarPiecesTop != null)
                _sidebarPiecesTop.style.backgroundImage = new StyleBackground(piecesSprite);
            if (_sidebarPiecesBot != null)
                _sidebarPiecesBot.style.backgroundImage = new StyleBackground(piecesSprite);
        }

        // divider_bar.png  →  horizontal divider below the title
        if (dividerSprite != null && _dividerImg != null)
            _dividerImg.style.backgroundImage = new StyleBackground(dividerSprite);

        // bottom.png  →  winged leaf ornament above nav buttons
        if (bottomSprite != null && _bottomImg != null)
            _bottomImg.style.backgroundImage = new StyleBackground(bottomSprite);
    }

    // ── Public API ───────────────────────────────────────
    public void ShowNote(NoteData data)
    {
        _currentNote = data;
        _currentPage = 0;
        _isOpen      = true;

        StopNarrator();
        PlaySound(sfxOpen);

        bool alreadyRead = _readNotes.Contains(data.statueName);
        if (_readStamp != null)
            _readStamp.style.display = alreadyRead
                ? DisplayStyle.Flex : DisplayStyle.None;

        RefreshContent();
        _overlay.style.display = DisplayStyle.Flex;

        // Unlock cursor immediately so buttons are clickable during animation
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // Pause time AFTER animation finishes — timeScale=0 freezes coroutines
        StartCoroutine(AnimateInThenPause());
    }

    private IEnumerator AnimateInThenPause()
    {
        yield return StartCoroutine(AnimateIn());
        Time.timeScale = 0f;
    }

    public void HideNote()
    {
        StartCoroutine(AnimateOut());
    }

    // ── Animations ───────────────────────────────────────
    private IEnumerator AnimateIn()
    {
        // Skip time-dependent animations — they freeze when timeScale=0
        // and an invisible noteCard blocks all pointer events
        _noteCard.style.opacity   = 1f;
        _noteCard.style.translate = new Translate(0, 0, 0);
        _overlay.style.opacity    = 1f;

        if (_glowRoutine != null) StopCoroutine(_glowRoutine);
        _glowRoutine = StartCoroutine(UIAnimator.GlowPulse(_statueName, 0.7f, 2.5f));

        StartPageEffects();
        yield break;
    }

    private IEnumerator AnimateOut()
    {
        if (_glowRoutine    != null) StopCoroutine(_glowRoutine);
        if (_papyrusRoutine != null) StopCoroutine(_papyrusRoutine);

        StopNarrator();
        PlaySound(sfxClose);

        if (_currentNote != null) _readNotes.Add(_currentNote.statueName);

        _noteText.style.color = new StyleColor(new Color(0.24f, 0.12f, 0.02f));

        Time.timeScale = 1f;

        // Skip time-dependent animations to avoid coroutine freeze issues
        _overlay.style.display = DisplayStyle.None;
        _isOpen = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        yield break;
    }

    private IEnumerator PageTurn(int newPage)
    {
        PlaySound(sfxPageTurn);
        yield return StartCoroutine(UIAnimator.SlideY(_noteText, 0f, -20f, 0.15f, UIAnimator.EaseIn));
        yield return StartCoroutine(UIAnimator.Fade(_noteText, 1f, 0f, 0.1f));

        _currentPage = newPage;
        RefreshContent();

        _noteText.style.translate = new Translate(0, 20, 0);
        _noteText.style.opacity   = 0f;
        yield return StartCoroutine(UIAnimator.FadeSlide(_noteText, 20f, 0.2f, 0f, UIAnimator.EaseOut));

        StartPageEffects();
    }

    // ── Page Effects ─────────────────────────────────────
    private void StartPageEffects()
    {
        if (_papyrusRoutine != null) StopCoroutine(_papyrusRoutine);
        _papyrusRoutine = StartCoroutine(PapyrusReveal());
    }

    private IEnumerator PapyrusReveal()
    {
        Color startColor = new Color(0.80f, 0.65f, 0.45f);
        Color endColor   = new Color(0.24f, 0.12f, 0.02f);
        float duration   = 1.5f;
        float elapsed    = 0f;

        _noteText.style.color = new StyleColor(startColor);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t     = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            _noteText.style.color = new StyleColor(Color.Lerp(startColor, endColor, eased));
            yield return null;
        }

        _noteText.style.color = new StyleColor(endColor);
    }

    // ── Content ──────────────────────────────────────────
    private void RefreshContent()
    {
        _statueName.text = _currentNote.statueName.ToUpper();
        _noteText.text   = _currentNote.pages[_currentPage];

        int total = _currentNote.pages.Length;
        _pageIndicator.text = total > 1 ? $"{_currentPage + 1} / {total}" : "";

        _prevBtn.style.display = _currentPage > 0
            ? DisplayStyle.Flex : DisplayStyle.None;
        _nextBtn.style.display = _currentPage < total - 1
            ? DisplayStyle.Flex : DisplayStyle.None;

        if (_narratorBtn != null)
            _narratorBtn.style.display = _currentNote.narratorClip != null
                ? DisplayStyle.Flex : DisplayStyle.None;

        if (_hieroglyphSymbols != null)
            _hieroglyphSymbols.text = GetHieroglyphSymbols(_currentNote.category);

        if (_categoryBadge != null)
        {
            foreach (NoteCategory c in System.Enum.GetValues(typeof(NoteCategory)))
                _categoryBadge.RemoveFromClassList($"category-badge--{c.ToString().ToLower()}");

            _categoryBadge.text = _currentNote.category.ToString().ToUpper();
            _categoryBadge.AddToClassList($"category-badge--{_currentNote.category.ToString().ToLower()}");
        }

        if (_currentNote.statueIllustration != null)
        {
            _illustration.style.backgroundImage =
                new StyleBackground(_currentNote.statueIllustration);
            _illustration.style.display = DisplayStyle.Flex;
        }
        else
        {
            _illustration.style.display = DisplayStyle.None;
        }
    }

    private void NextPage()
    {
        if (_currentNote == null) return;
        if (_currentPage < _currentNote.pages.Length - 1)
            StartCoroutine(PageTurn(_currentPage + 1));
    }

    private void PrevPage()
    {
        if (_currentPage > 0)
            StartCoroutine(PageTurn(_currentPage - 1));
    }

    private void Update()
    {
        if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            HideNote();

        if (_narratorPlaying)
        {
            AudioSource src = narratorAudioSource != null ? narratorAudioSource : audioSource;
            if (src != null && !src.isPlaying)
            {
                _narratorPlaying = false;
                SetNarratorButtonState(false);
            }
        }
    }

    // ── Audio ─────────────────────────────────────────────
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    private void PlayNarrator()
    {
        if (_narratorPlaying) { StopNarrator(); return; }

        if (_currentNote == null || _currentNote.narratorClip == null)
        {
            Debug.LogWarning("[NoteUI] PlayNarrator: no narratorClip assigned on this NoteData");
            return;
        }

        AudioSource src = narratorAudioSource != null ? narratorAudioSource : audioSource;
        if (src == null) { Debug.LogWarning("[NoteUI] PlayNarrator: no AudioSource"); return; }

        src.Stop();
        src.clip = _currentNote.narratorClip;
        src.Play();
        _narratorPlaying = true;
        SetNarratorButtonState(true);
    }

    private void StopNarrator()
    {
        AudioSource src = narratorAudioSource != null ? narratorAudioSource : audioSource;
        if (src != null && src.isPlaying) src.Stop();
        _narratorPlaying = false;
        SetNarratorButtonState(false);
    }

    private void SetNarratorButtonState(bool playing)
    {
        if (_narratorBtn == null) return;
        _narratorBtn.text = playing ? NarratorPlayingIcon : NarratorIdleIcon;
        if (playing) _narratorBtn.AddToClassList(NarratorPlayingClass);
        else         _narratorBtn.RemoveFromClassList(NarratorPlayingClass);
    }

    // ── Public utility ────────────────────────────────────
    public static bool IsNoteRead(string statueName)
        => _readNotes.Contains(statueName);
}
