/*
 ═══════════════════════════════════════════════════════════
 ModernNoteUI.cs  —  ModernMuseum
 ───────────────────────────────────────────────────────────
 Sprite Inspector slots (assign in Unity Inspector):
   BACKGROUND PANELS   panelBgSprite       → bg-panel
   OVERLAYS            overlayCiruitSprite → overlay-circuit
                       overlayCornerSprite → overlay-corner-tl/tr
   TOP / BOTTOM        topDecorSprite      → top-decor
                       bottomDecorSprite   → bottom-decor
   ACCENT              accentBarSprite     → accent-bar
   IMAGE FRAME         imgFrameSprite      → img-frame
   SIDE                sideDecorSprite     → side-decor
   OTHER               barcodeSprite       → barcode
 ═══════════════════════════════════════════════════════════
*/

using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using EchoesOfTime.UI;
using Cursor = UnityEngine.Cursor;

public class ModernNoteUI : MonoBehaviour
{
    public static ModernNoteUI Instance { get; private set; }

    [SerializeField] private UIDocument uiDocument;

    // ── Decorative Sprites (assign in Inspector) ──────────
    [Header("Decorative Sprites")]
    [SerializeField] private Sprite panelBgSprite;       // panel_bg_01 / 02 / 03
    [SerializeField] private Sprite overlayCircuitSprite;// overlay_circuit_01
    [SerializeField] private Sprite overlayCornerSprite; // overlay_corner_01
    [SerializeField] private Sprite topDecorSprite;      // top_decor_01 / 02
    [SerializeField] private Sprite bottomDecorSprite;   // bottom_decor_01 / 02
    [SerializeField] private Sprite accentBarSprite;     // accent_bar_01
    [SerializeField] private Sprite imgFrameSprite;      // frame_03 / 04
    [SerializeField] private Sprite sideDecorSprite;     // side_decor_01
    [SerializeField] private Sprite barcodeSprite;       // any barcode-style sprite

    // ── Audio ─────────────────────────────────────────────
    [Header("Audio")]
    [SerializeField] private AudioSource noteAudioSource;
    [SerializeField] private AudioClip   noteOpenClip;
    [SerializeField] private AudioClip   noteCloseClip;
    [SerializeField] private AudioClip   pageForwardClip;
    [SerializeField] private AudioClip   pageBackClip;

    [Header("Narrator")]
    [SerializeField] private AudioSource narratorAudioSource;

    // ── Visual Elements ──────────────────────────────────
    private VisualElement _overlay;
    private VisualElement _noteCard;
    private VisualElement _bgPanel;
    private VisualElement _overlayCircuit;
    private VisualElement _overlayCornerTL;
    private VisualElement _overlayCornerTR;
    private VisualElement _topDecor;
    private VisualElement _bottomDecor;
    private VisualElement _accentBar;
    private VisualElement _imgFrame;
    private VisualElement _illustration;
    private VisualElement _sideDecor;
    private VisualElement _barcode;
    private Label         _exhibitName;
    private Label         _noteText;
    private Label         _pageIndicator;
    private Label         _artifactIdValue;
    private Button        _closeBtn;
    private Button        _prevBtn;
    private Button        _nextBtn;
    private Button        _nextFullBtn;
    private Button        _startRepairBtn;
    private Button        _narratorBtn;

    // ── State ────────────────────────────────────────────
    private ExhibitData   _currentExhibit;
    private int           _currentPage;
    private bool          _isOpen;
    private bool          _narratorPlaying;
    private System.Action _onCloseCallback;

    public bool IsOpen => _isOpen;

    private Coroutine _glowRoutine;

    private const string NarratorIdleIcon     = "🔊";
    private const string NarratorPlayingIcon  = "⏹";
    private const string NarratorPlayingClass = "narrator-btn--playing";

    // ────────────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;

        var root = uiDocument.rootVisualElement;

        _overlay         = root.Q("overlay");
        _noteCard        = root.Q("note-card");
        _bgPanel         = root.Q("bg-panel");
        _overlayCircuit  = root.Q("overlay-circuit");
        _overlayCornerTL = root.Q("overlay-corner-tl");
        _overlayCornerTR = root.Q("overlay-corner-tr");
        _topDecor        = root.Q("top-decor");
        _bottomDecor     = root.Q("bottom-decor");
        _accentBar       = root.Q("accent-bar");
        _imgFrame        = root.Q("img-frame");
        _illustration    = root.Q("illustration");
        _sideDecor       = root.Q("side-decor");
        _barcode         = root.Q("barcode");
        _exhibitName     = root.Q<Label>("statue-name");
        _noteText        = root.Q<Label>("note-text");
        _pageIndicator   = root.Q<Label>("page-indicator");
        _artifactIdValue = root.Q<Label>("artifact-id-value");
        _closeBtn        = root.Q<Button>("close-btn");
        _prevBtn         = root.Q<Button>("prev-btn");
        _nextBtn         = root.Q<Button>("next-btn");
        _nextFullBtn     = root.Q<Button>("next-full-btn");
        _startRepairBtn  = root.Q<Button>("start-repair-btn");
        _narratorBtn     = root.Q<Button>("narrator-btn");

        _closeBtn.clicked       += OnCloseBtnPressed;
        _prevBtn.clicked        += PrevPage;
        _nextBtn.clicked        += NextPage;
        if (_nextFullBtn != null) _nextFullBtn.clicked    += NextPage;
        _startRepairBtn.clicked += HidePanel;
        if (_narratorBtn != null) _narratorBtn.clicked += PlayNarrator;

        _overlay.style.display = DisplayStyle.None;

        ApplySprites();
    }

    // ── Apply all decorator sprites ───────────────────────
    private void ApplySprites()
    {
        SetBg(_bgPanel,         panelBgSprite);
        SetBg(_overlayCircuit,  overlayCircuitSprite);
        SetBg(_overlayCornerTL, overlayCornerSprite);
        SetBg(_overlayCornerTR, overlayCornerSprite);
        SetBg(_topDecor,        topDecorSprite);
        SetBg(_bottomDecor,     bottomDecorSprite);
        SetBg(_accentBar,       accentBarSprite);
        SetBg(_imgFrame,        imgFrameSprite);
        SetBg(_sideDecor,       sideDecorSprite);
        SetBg(_barcode,         barcodeSprite);
    }

    private static void SetBg(VisualElement el, Sprite sprite)
    {
        if (el == null || sprite == null) return;
        el.style.backgroundImage = new StyleBackground(sprite);
    }

    // ── Public API ───────────────────────────────────────
    public void ShowPanel(ExhibitData data)
        => ShowPanel(data, null);

    public void ShowPanel(ExhibitData data, System.Action onClose)
    {
        _currentExhibit  = data;
        _currentPage     = 0;
        _isOpen          = true;
        _onCloseCallback = onClose;

        StopNarrator();
        RefreshContent();

        _overlay.style.display = DisplayStyle.Flex;

        _startRepairBtn.style.display = onClose != null
            ? DisplayStyle.Flex : DisplayStyle.None;

        StartCoroutine(AnimateIn());
        PlaySound(noteOpenClip);

        Time.timeScale   = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void HidePanel()
    {
        StartCoroutine(AnimateOut());
    }

    private void OnCloseBtnPressed()
    {
        _onCloseCallback = null;
        HidePanel();
    }

    // ── Animations ───────────────────────────────────────
    private IEnumerator AnimateIn()
    {
        _noteCard.style.opacity   = 0f;
        _noteCard.style.translate = new Translate(0, 40, 0);

        yield return StartCoroutine(UIAnimator.Fade(_overlay, 0f, 1f, 0.25f, UIAnimator.EaseOut));
        yield return StartCoroutine(UIAnimator.FadeSlide(_noteCard, 40f, 0.4f, 0f, UIAnimator.EaseOut));

        if (_glowRoutine != null) StopCoroutine(_glowRoutine);
        _glowRoutine = StartCoroutine(UIAnimator.GlowPulse(_exhibitName, 0.7f, 2.5f));
    }

    private IEnumerator AnimateOut()
    {
        if (_glowRoutine != null) StopCoroutine(_glowRoutine);
        StopNarrator();
        PlaySound(noteCloseClip);

        yield return StartCoroutine(UIAnimator.FadeSlide(_noteCard, 40f, 0.3f));
        yield return StartCoroutine(UIAnimator.Fade(_overlay, 1f, 0f, 0.2f));

        _overlay.style.display = DisplayStyle.None;
        _isOpen = false;

        Time.timeScale   = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        var cb = _onCloseCallback;
        _onCloseCallback = null;
        cb?.Invoke();
    }

    private IEnumerator PageTurn(int newPage)
    {
        yield return StartCoroutine(UIAnimator.SlideY(_noteText, 0f, -20f, 0.15f, UIAnimator.EaseIn));
        yield return StartCoroutine(UIAnimator.Fade(_noteText, 1f, 0f, 0.1f));

        _currentPage = newPage;
        RefreshContent();

        _noteText.style.translate = new Translate(0, 20, 0);
        _noteText.style.opacity   = 0f;
        yield return StartCoroutine(UIAnimator.FadeSlide(_noteText, 20f, 0.2f, 0f, UIAnimator.EaseOut));
    }

    // ── Content ──────────────────────────────────────────
    private void RefreshContent()
    {
        // Title: first word white, rest cyan — using rich text
        string raw = _currentExhibit.exhibitName.ToUpper();
        string[] words = raw.Split(' ');
        if (words.Length >= 2)
        {
            string first = words[0];
            string rest  = string.Join(" ", words, 1, words.Length - 1);
            _exhibitName.text = $"{first} <color=#00D4FF>{rest}</color>";
            _exhibitName.enableRichText = true;
        }
        else
        {
            _exhibitName.text = $"<color=#00D4FF>{raw}</color>";
            _exhibitName.enableRichText = true;
        }

        _noteText.text = _currentExhibit.pages[_currentPage];

        // Artifact ID — use exhibit name as ID base if no dedicated field
        if (_artifactIdValue != null)
            _artifactIdValue.text = $"{raw.Replace(" ", "").Substring(0, Mathf.Min(3, raw.Length))}-{System.DateTime.Now.Year}-01";

        int total = _currentExhibit.pages.Length;
        _pageIndicator.text = total > 1 ? $"{_currentPage + 1} / {total}" : "";

        _prevBtn.style.display     = _currentPage > 0            ? DisplayStyle.Flex : DisplayStyle.None;
        _nextBtn.style.display     = _currentPage < total - 1    ? DisplayStyle.Flex : DisplayStyle.None;
        if (_nextFullBtn != null)
            _nextFullBtn.style.display = _currentPage < total - 1    ? DisplayStyle.Flex : DisplayStyle.None;

        if (_narratorBtn != null)
            _narratorBtn.style.display = _currentExhibit.narratorClip != null
                ? DisplayStyle.Flex : DisplayStyle.None;

        if (_currentExhibit.exhibitIllustration != null)
        {
            _illustration.style.backgroundImage =
                new StyleBackground(_currentExhibit.exhibitIllustration);
            _illustration.style.display = DisplayStyle.Flex;
        }
        else
        {
            _illustration.style.display = DisplayStyle.None;
        }
    }

    private void NextPage()
    {
        if (_currentExhibit == null) return;
        if (_currentPage < _currentExhibit.pages.Length - 1)
        {
            PlaySound(pageForwardClip);
            StartCoroutine(PageTurn(_currentPage + 1));
        }
    }

    private void PrevPage()
    {
        if (_currentPage > 0)
        {
            PlaySound(pageBackClip);
            StartCoroutine(PageTurn(_currentPage - 1));
        }
    }

    private void Update()
    {
        if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            HidePanel();

        if (_narratorPlaying)
        {
            AudioSource src = narratorAudioSource != null ? narratorAudioSource : noteAudioSource;
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
        if (noteAudioSource == null || clip == null) return;
        noteAudioSource.PlayOneShot(clip);
    }

    private void PlayNarrator()
    {
        if (_narratorPlaying) { StopNarrator(); return; }

        if (_currentExhibit == null || _currentExhibit.narratorClip == null)
        {
            Debug.LogWarning("[ModernNoteUI] PlayNarrator: no narratorClip on ExhibitData");
            return;
        }

        AudioSource src = narratorAudioSource != null ? narratorAudioSource : noteAudioSource;
        if (src == null) { Debug.LogWarning("[ModernNoteUI] PlayNarrator: no AudioSource"); return; }

        src.Stop();
        src.clip = _currentExhibit.narratorClip;
        src.Play();
        _narratorPlaying = true;
        SetNarratorButtonState(true);
    }

    private void StopNarrator()
    {
        AudioSource src = narratorAudioSource != null ? narratorAudioSource : noteAudioSource;
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
}
