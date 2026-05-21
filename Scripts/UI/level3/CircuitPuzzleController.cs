using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using EchoesOfTime.UI;

/// <summary>
/// CircuitPuzzleController — Level 3.
/// No snap zones — a piece counts as "placed" the moment the player
/// drags it out of the starting tray. All 6 placed = circuit complete.
/// </summary>
public class CircuitPuzzleController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────
    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Data")]
    [SerializeField] private CircuitPuzzleData puzzleData;

    [Header("Museum Model")]
    [SerializeField] private CircuitMuseumModel museumModel;

[Header("Win HUD")]
[SerializeField] private UIDocument backToMapHUD;

    [Header("Scene Navigation")]
    [SerializeField] private string previousLevelScene = "Level2";
    [SerializeField] private string nextLevelScene     = "Level3";

    [Header("Audio")]
    [SerializeField] private AudioSource puzzleSfxSource;
    [SerializeField] private AudioClip   piecePlacedClip;   // plays each time a piece gets a ✔
    [SerializeField] private AudioClip   puzzleWinClip;     // plays on full solve
    [SerializeField] private AudioClip   heartLostClip;     // plays each time a heart is lost
    [SerializeField] private AudioClip   gameOverClip;      // plays when all 3 hearts are gone

    // How far below the top of the canvas a piece must be dropped
    // to count as "placed" (out of the tray).
    private const float TrayHeight = 200f;

    // ── State ─────────────────────────────────────────────────────────
    private int  _hearts           = 3;
    private bool _batteryPlaced    = false;
    private bool _bulbPlaced       = false;
    private bool _switchPlaced     = false;
    private bool _wireGreenPlaced  = false;
    private bool _wireYellowPlaced = false;
    private bool _wireOrangePlaced = false;
    private bool _gameOver         = false;
    private bool _puzzleWon        = false;

    // Prevents scene load being triggered more than once
    private bool _navigating = false;

    // Drag state
    private VisualElement _dragging   = null;
    private Vector2       _dragOffset;

    // ── Cached UI ─────────────────────────────────────────────────────
    private VisualElement _root;
    private VisualElement _puzzleCanvas;
    private VisualElement _battery, _bulb, _switch;
    private VisualElement _wireGreen, _wireYellow, _wireOrange;
    private VisualElement _wireCanvas;
    private VisualElement _winOverlay, _loseOverlay;
    private VisualElement _hintImage;
    private Label         _feedbackLabel;
    private Label[]       _heartLabels = new Label[3];
    private Label         _checkBattery, _checkBulb, _checkSwitch;
    private Label         _checkWireGreen, _checkWireYellow, _checkWireOrange;

    // ── Lifecycle ─────────────────────────────────────────────────────
    private void OnEnable()
    {
        _root = uiDocument.rootVisualElement;
        QueryElements();
        ApplySprites();
        SetupHintImage();
        SetupWireCanvas();
        SetupDragging();
        SetupButtons();
        RefreshHearts();
        RefreshChecklist();

        _puzzleCanvas.RegisterCallback<GeometryChangedEvent>(OnCanvasReady);
        
        var hint = _root.Q("back-to-map-hint");
if (hint != null)
    hint.style.display = DisplayStyle.None;
    
    if (backToMapHUD != null)
    backToMapHUD.rootVisualElement.style.display = DisplayStyle.None;
    
    _root.pickingMode = PickingMode.Position;
    }

    private void OnDisable()
    {
        // Stop all coroutines so nothing tries to touch destroyed UI elements
        StopAllCoroutines();
        _puzzleCanvas?.UnregisterCallback<GeometryChangedEvent>(OnCanvasReady);
    }

    private void OnCanvasReady(GeometryChangedEvent evt)
    {
        float w = _puzzleCanvas.resolvedStyle.width;
        if (w < 10) return;
        ScatterPieces(w);
        RepaintWires();
    }

    // ── Scatter pieces into top tray strip ───────────────────────────
    private void ScatterPieces(float canvasWidth)
    {
        float slot = canvasWidth / 6f;
        SetPos(_battery,    slot * 0 + 10, 10);
        SetPos(_bulb,       slot * 1 + 10, 10);
        SetPos(_switch,     slot * 2 + 10, 10);
        SetPos(_wireGreen,  slot * 3 + 10, 10);
        SetPos(_wireYellow, slot * 4 + 10, 10);
        SetPos(_wireOrange, slot * 5 + 10, 10);
    }

    private void SetPos(VisualElement el, float x, float y)
    {
        el.style.left = x;
        el.style.top  = y;
    }

    // ─────────────────────────────────────────────────────────────────
    // Query & Sprites
    // ─────────────────────────────────────────────────────────────────
    private void QueryElements()
    {
        _puzzleCanvas  = _root.Q("puzzle-canvas");
        _battery       = _root.Q("battery-component");
        _bulb          = _root.Q("bulb-component");
        _switch        = _root.Q("switch-component");
        _wireGreen     = _root.Q("wire-green-component");
        _wireYellow    = _root.Q("wire-yellow-component");
        _wireOrange    = _root.Q("wire-orange-component");
        _wireCanvas    = _root.Q("wire-canvas");
        _winOverlay    = _root.Q("win-overlay");
        _loseOverlay   = _root.Q("lose-overlay");
        _hintImage     = _root.Q("hint-image");
        _feedbackLabel = _root.Q<Label>("feedback-label");

        _heartLabels[0] = _root.Q<Label>("heart-1");
        _heartLabels[1] = _root.Q<Label>("heart-2");
        _heartLabels[2] = _root.Q<Label>("heart-3");

        _checkBattery    = _root.Q<Label>("check-battery");
        _checkBulb       = _root.Q<Label>("check-bulb");
        _checkSwitch     = _root.Q<Label>("check-switch");
        _checkWireGreen  = _root.Q<Label>("check-wire-green");
        _checkWireYellow = _root.Q<Label>("check-wire-yellow");
        _checkWireOrange = _root.Q<Label>("check-wire-orange");
    }

    private void ApplySprites()
    {
        if (puzzleData == null) return;
        SetBg(_battery,    puzzleData.batterySprite);
        SetBg(_bulb,       puzzleData.bulbUnlitSprite);
        SetBg(_switch,     puzzleData.switchSprite);
        SetBg(_wireGreen,  puzzleData.wireGreenSprite);
        SetBg(_wireYellow, puzzleData.wireYellowSprite);
        SetBg(_wireOrange, puzzleData.wireOrangeSprite);
    }

    private void SetupHintImage()
    {
        if (puzzleData?.hintCircuitSprite != null)
            SetBg(_hintImage, puzzleData.hintCircuitSprite);
    }

    private void SetBg(VisualElement el, Sprite s)
    {
        if (el == null || s == null) return;
        el.style.backgroundImage = new StyleBackground(s);
        el.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
    }

    // ─────────────────────────────────────────────────────────────────
    // Wire Canvas
    // ─────────────────────────────────────────────────────────────────
    private void SetupWireCanvas() { }
    private void RepaintWires()    { }

    private Vector2 Centre(VisualElement el) => new Vector2(
        el.resolvedStyle.left + el.resolvedStyle.width  * 0.5f,
        el.resolvedStyle.top  + el.resolvedStyle.height * 0.5f);

    // ─────────────────────────────────────────────────────────────────
    // Dragging
    // ─────────────────────────────────────────────────────────────────
    private void SetupDragging()
    {
        RegisterDrag(_battery);
        RegisterDrag(_bulb);
        RegisterDrag(_switch);
        RegisterDrag(_wireGreen);
        RegisterDrag(_wireYellow);
        RegisterDrag(_wireOrange);
    }

    private void RegisterDrag(VisualElement el)
    {
        el.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0 || _gameOver) return;
            _dragging   = el;
            Vector2 local = _puzzleCanvas.WorldToLocal(evt.position);
            _dragOffset   = local - new Vector2(el.resolvedStyle.left,
                                                el.resolvedStyle.top);
            el.CapturePointer(evt.pointerId);
            el.BringToFront();
            evt.StopPropagation();
        });

        el.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (_dragging != el) return;

            Vector2 local = _puzzleCanvas.WorldToLocal(evt.position);
            float canvasW = _puzzleCanvas.resolvedStyle.width;
            float canvasH = _puzzleCanvas.resolvedStyle.height;
            float elW     = el.resolvedStyle.width;
            float elH     = el.resolvedStyle.height;

            el.style.left = Mathf.Clamp(local.x - _dragOffset.x, 0, canvasW - elW);
            el.style.top  = Mathf.Clamp(local.y - _dragOffset.y, 0, canvasH - elH);

            RepaintWires();
            evt.StopPropagation();
        });

        el.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (_dragging != el) return;
            _dragging = null;
            el.ReleasePointer(evt.pointerId);

            bool nowPlaced = IsPlaced(el);
            bool wasPLaced = GetPlaced(el);         // read BEFORE SetPlaced
            SetPlaced(el, nowPlaced);
            if (nowPlaced && !wasPLaced)
                PlaySfx(piecePlacedClip);
            RefreshChecklist();
            evt.StopPropagation();
        });
    }

    private bool IsPlaced(VisualElement el)
    {
        if (el.resolvedStyle.top <= TrayHeight) return false;

        if (el == _battery || el == _bulb || el == _switch) return true;

        if (el == _wireGreen)  return IsWireNearBothComponents(_wireGreen,  _battery, _bulb);
        if (el == _wireYellow) return IsWireNearBothComponents(_wireYellow, _bulb,    _switch);
        if (el == _wireOrange) return IsWireNearBothComponents(_wireOrange, _switch,  _battery);

        return false;
    }

    private bool IsWireNearBothComponents(VisualElement wire,
                                          VisualElement compA,
                                          VisualElement compB)
    {
        bool aPlaced = compA == _battery ? _batteryPlaced :
                       compA == _bulb    ? _bulbPlaced    : _switchPlaced;
        bool bPlaced = compB == _battery ? _batteryPlaced :
                       compB == _bulb    ? _bulbPlaced    : _switchPlaced;
        if (!aPlaced || !bPlaced) return false;

        const float padding = 100f;

        Vector2 cA   = Centre(compA);
        Vector2 cB   = Centre(compB);
        Vector2 wPos = Centre(wire);

        float minX = Mathf.Min(cA.x, cB.x) - padding;
        float maxX = Mathf.Max(cA.x, cB.x) + padding;
        float minY = Mathf.Min(cA.y, cB.y) - padding;
        float maxY = Mathf.Max(cA.y, cB.y) + padding;

        return wPos.x >= minX && wPos.x <= maxX &&
               wPos.y >= minY && wPos.y <= maxY;
    }

    private void SetPlaced(VisualElement el, bool placed)
    {
        if      (el == _battery)    _batteryPlaced    = placed;
        else if (el == _bulb)       _bulbPlaced       = placed;
        else if (el == _switch)     _switchPlaced     = placed;
        else if (el == _wireGreen)  _wireGreenPlaced  = placed;
        else if (el == _wireYellow) _wireYellowPlaced = placed;
        else if (el == _wireOrange) _wireOrangePlaced = placed;
    }

    private bool GetPlaced(VisualElement el)
    {
        if (el == _battery)    return _batteryPlaced;
        if (el == _bulb)       return _bulbPlaced;
        if (el == _switch)     return _switchPlaced;
        if (el == _wireGreen)  return _wireGreenPlaced;
        if (el == _wireYellow) return _wireYellowPlaced;
        if (el == _wireOrange) return _wireOrangePlaced;
        return false;
    }

    // ─────────────────────────────────────────────────────────────────
    // Buttons
    // ─────────────────────────────────────────────────────────────────
private void SetupButtons()
{
    _root.Q<Button>("check-btn").clicked      += OnCheckPressed;
    _root.Q<Button>("reset-btn").clicked      += ResetPuzzle;
    _root.Q<Button>("exit-btn").clicked       += () => GetComponent<CircuitPuzzleInteractable>()?.ClosePuzzle();

    // Win overlay — "Back to Map" button
    var backToMapBtn = _root.Q<Button>("back-to-map-btn");
    if (backToMapBtn != null)
        backToMapBtn.clicked += OnBackToMap;

    // Lose overlay — "Back to Level 2" button (no retry)
    var goBackBtn = _root.Q<Button>("go-back-btn");
    if (goBackBtn != null)
        goBackBtn.clicked += OnGoBackToLevel2;

    // Keep old button names as fallback in case UXML still has them
    var nextBtn = _root.Q<Button>("next-level-btn");
    if (nextBtn != null) nextBtn.clicked += OnBackToMap;

    var prevBtn = _root.Q<Button>("prev-level-btn");
    if (prevBtn != null) prevBtn.clicked += OnGoBackToLevel2;

    var retryBtn = _root.Q<Button>("retry-btn");
    if (retryBtn != null) retryBtn.clicked += OnGoBackToLevel2;
}

    /// <summary>
    /// Safe scene navigation: stops all coroutines first so nothing
    /// touches destroyed UI elements after the scene unloads.
    /// </summary>
private void NavigateTo(string sceneName)
{
    if (_navigating) return;
    if (string.IsNullOrEmpty(sceneName))
    {
        Debug.LogWarning("[CircuitPuzzleController] Scene name is empty.");
        return;
    }

    _navigating = true;
    StopAllCoroutines();

    // If going "next" and it's the same scene, just close the puzzle
    if (sceneName == UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
    {
        _navigating = false;
        GetComponent<CircuitPuzzleInteractable>()?.ClosePuzzle();
        return;
    }

    SceneManager.LoadScene(sceneName);
}

    private void OnGoBackToLevel2()
    {
        if (_navigating) return;
        _navigating = true;
        StopAllCoroutines();

        // Unlock cursor before loading
        UnityEngine.Time.timeScale   = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible   = true;

        if (EchoesOfTime.UI.LevelManager.Instance != null)
            EchoesOfTime.UI.LevelManager.Instance.LoadLevel("medieval");
        else
            SceneManager.LoadScene(previousLevelScene);
    }

    private void OnBackToMap()
    {
        if (_navigating) return;
        _navigating = true;
        StopAllCoroutines();

        // Unlock cursor before loading
        UnityEngine.Time.timeScale   = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible   = true;

        if (EchoesOfTime.UI.LevelManager.Instance != null)
            EchoesOfTime.UI.LevelManager.Instance.LoadMenu();
        else
            SceneManager.LoadScene("MainMenu");
    }

    // ─────────────────────────────────────────────────────────────────
    // Check
    // ─────────────────────────────────────────────────────────────────
    private void OnCheckPressed()
    {
        if (_gameOver || _puzzleWon) return;

        bool allPlaced = _batteryPlaced    && _bulbPlaced       && _switchPlaced &&
                         _wireGreenPlaced  && _wireYellowPlaced && _wireOrangePlaced;

        if (allPlaced)
        {
            ShowFeedback("✔ Circuit complete! The bulb lights up!", true);
            StartCoroutine(ShowWinDelay());
        }
        else
        {
            _hearts--;
            RefreshHearts();
            ShowFeedback(BuildHintMessage(), false);
            if (_hearts <= 0)
            {
                _gameOver = true;
                PlaySfx(gameOverClip);              // 🔊 all hearts gone
                StartCoroutine(ShowLoseDelay());
            }
            else
            {
                PlaySfx(heartLostClip);             // 🔊 one heart lost
            }
        }
    }

    private string BuildHintMessage()
    {
        if (!_batteryPlaced)    return "⚠ Drag the battery onto the board!";
        if (!_bulbPlaced)       return "⚠ Drag the bulb onto the board!";
        if (!_switchPlaced)     return "⚠ Drag the switch onto the board!";
        if (!_wireGreenPlaced)  return "⚠ Green wire isn't placed yet!";
        if (!_wireYellowPlaced) return "⚠ Yellow wire is missing!";
        if (!_wireOrangePlaced) return "⚠ Orange wire is missing!";
        return "⚠ Make sure all pieces are on the board!";
    }

    // ─────────────────────────────────────────────────────────────────
    // UI Refresh
    // ─────────────────────────────────────────────────────────────────
    private void RefreshHearts()
    {
        for (int i = 0; i < 3; i++)
        {
            _heartLabels[i].EnableInClassList("heart-active", i < _hearts);
            _heartLabels[i].EnableInClassList("heart-lost",   i >= _hearts);
        }
    }

    private void RefreshChecklist()
    {
        SetCheck(_checkBattery,    _batteryPlaced);
        SetCheck(_checkBulb,       _bulbPlaced);
        SetCheck(_checkSwitch,     _switchPlaced);
        SetCheck(_checkWireGreen,  _wireGreenPlaced);
        SetCheck(_checkWireYellow, _wireYellowPlaced);
        SetCheck(_checkWireOrange, _wireOrangePlaced);
    }

    private void SetCheck(Label lbl, bool done)
    {
        lbl.text = done ? "✔" : "○";
        lbl.EnableInClassList("done", done);
    }

    private void ShowFeedback(string msg, bool correct)
    {
        _feedbackLabel.text = msg;
        _feedbackLabel.EnableInClassList("correct",  correct);
        _feedbackLabel.EnableInClassList("wrong",   !correct);
        _feedbackLabel.AddToClassList("visible");
        StartCoroutine(HideFeedbackAfter(3f));
    }

    private IEnumerator HideFeedbackAfter(float s)
    {
        yield return new WaitForSeconds(s);
        // Guard: element may be gone if the scene changed during the wait
        if (_feedbackLabel != null)
            _feedbackLabel.RemoveFromClassList("visible");
    }

    // ─────────────────────────────────────────────────────────────────
    // Win / Lose
    // ─────────────────────────────────────────────────────────────────
private IEnumerator ShowWinDelay()
{
    _puzzleWon = true;
    yield return new WaitForSeconds(0.8f);

    if (puzzleData?.bulbLitSprite != null)
        SetBg(_bulb, puzzleData.bulbLitSprite);

    _winOverlay.RemoveFromClassList("hidden");
    PlaySfx(puzzleWinClip);
    StartCoroutine(PulseBulb());

    PlayerPrefs.SetInt("CircuitPuzzleSolved", 1);
    PlayerPrefs.Save();

    // Mark level complete in LevelManager
    if (EchoesOfTime.UI.LevelManager.Instance != null)
        EchoesOfTime.UI.LevelManager.Instance.CompleteLevel("modern");

    if (museumModel != null)
        museumModel.Reveal();
    else
        Debug.LogWarning("[CircuitPuzzleController] Museum model not assigned!");

yield return new WaitForSeconds(2.5f);

// Manually close puzzle WITHOUT hiding the root (so hint stays visible)
var interactable = GetComponent<CircuitPuzzleInteractable>();
if (interactable != null)
{
    interactable.SetOpen(false);
    interactable.RestoreMovement();
}

// Keep cursor visible so player can click Back to Map
UnityEngine.Cursor.lockState = CursorLockMode.None;
UnityEngine.Cursor.visible   = true;

// Hide only the note panel and overlays
_root.Q("note-panel")?.AddToClassList("hidden");
// Hide only the note panel and overlays
_root.Q(className: "scrim")?.AddToClassList("hidden");
_winOverlay.AddToClassList("hidden");



// Show hint — root stays visible
var hint = _root.Q("back-to-map-hint");
if (hint != null)
    hint.style.display = DisplayStyle.Flex;

// Make root pass through clicks everywhere EXCEPT the hint button
_root.pickingMode = PickingMode.Ignore;
hint.pickingMode  = PickingMode.Position;
_root.Q<Button>("back-to-map-btn").pickingMode = PickingMode.Position;
}

    private IEnumerator ShowLoseDelay()
    {
        yield return new WaitForSeconds(0.5f);
        _loseOverlay.RemoveFromClassList("hidden");
    }

    private IEnumerator PulseBulb()
    {
        var glow = _root.Q("bulb-glow");
        for (int i = 0; i < 6; i++)
        {
            glow.AddToClassList("pulsing");
            yield return new WaitForSeconds(0.4f);
            glow.RemoveFromClassList("pulsing");
            yield return new WaitForSeconds(0.4f);
        }
        glow.AddToClassList("pulsing");
    }

    // ─────────────────────────────────────────────────────────────────
    // Reset
    // ─────────────────────────────────────────────────────────────────
    private void ResetPuzzle()
    {
        _batteryPlaced = _bulbPlaced = _switchPlaced             = false;
        _wireGreenPlaced = _wireYellowPlaced = _wireOrangePlaced = false;
        _puzzleWon = false;

        float w = _puzzleCanvas.resolvedStyle.width;
        if (w > 10) ScatterPieces(w);

        ApplySprites();
        RefreshChecklist();
        RefreshHearts();
        RepaintWires();

        _feedbackLabel.RemoveFromClassList("visible");
        _winOverlay.AddToClassList("hidden");
    }

    // ─────────────────────────────────────────────────────────────────
    // Audio
    // ─────────────────────────────────────────────────────────────────
    private void PlaySfx(AudioClip clip)
    {
        if (puzzleSfxSource == null || clip == null) return;
        puzzleSfxSource.PlayOneShot(clip);
    }
}
