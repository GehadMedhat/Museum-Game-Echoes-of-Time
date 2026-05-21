/*
 PuzzleUI.cs — EchoesOfTime v4
 Pure pixel-based puzzle placement.
 No percent anchors. Pieces use exact px coords on a 550x550 canvas.
 Drag freely, snap when close enough to correct position.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EchoesOfTime.UI;
using Cursor = UnityEngine.Cursor;

public class PuzzleUI : MonoBehaviour
{
    public static PuzzleUI Instance { get; private set; }
    [SerializeField] private UIDocument uiDocument;

    // ── Elements ──────────────────────────────────────────
    private VisualElement _overlay;
    private VisualElement _card;
    private Label         _title;
    private VisualElement _heartsRow;
    private VisualElement _canvas;
    private VisualElement _pieceBank;
    private Button        _checkBtn;
    private Button        _closeBtn;
    private Label         _feedback;
    private VisualElement _completePanel;
    private Button        _nextHallBtn;
    private Label         _progressLabel;
    private VisualElement _progressFill;
    private VisualElement _backToMapHint;   // subtle hint shown after closing the panel
    private Button        _backToMapBtn;

    // ── State ─────────────────────────────────────────────
    private PuzzleData _data;
    private int        _index;
    private bool       _isOpen;
    private int        _placedCount;
    private bool[]     _locked;

    // Drag state
    private VisualElement _dragEl;
    private int           _dragPieceIdx;
    private Vector2       _dragOffset;   // offset from piece top-left to mouse
    private bool          _isDragging;

    // ── Audio ─────────────────────────────────────────────
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   sfxOpen;
    [SerializeField] private AudioClip   sfxClose;
    [SerializeField] private AudioClip   sfxSnapCorrect;
    [SerializeField] private AudioClip   sfxWin;
    [SerializeField] private AudioClip   sfxLoseHeart;
    [SerializeField] private AudioClip   sfxLoseAll;

    [Header("Level Progression")]
    [SerializeField] private string currentEra       = "ancient";  // era this scene represents
    [SerializeField] private string fallbackNextScene = "";        // fill in Inspector if no LevelManager
    [SerializeField] private string fallbackMenuScene = "MainMenu";// fill in Inspector if no LevelManager

    // Snap threshold in pixels
    private const float SNAP_THRESHOLD_PX = 60f;
    private const int   N  = 5;
    private const float CANVAS_SIZE = 550f;

    private void Awake()
    {
        Instance = this;
        var root = uiDocument.rootVisualElement;

        _overlay       = root.Q("puzzle-overlay");
        _card          = root.Q("puzzle-card");
        _title         = root.Q<Label>("puzzle-title");
        _heartsRow     = root.Q("hearts-row");
        _canvas        = root.Q("ghost-canvas");
        _pieceBank     = root.Q("piece-bank");
        _checkBtn      = root.Q<Button>("check-btn");
        _closeBtn      = root.Q<Button>("puzzle-close");
        _feedback      = root.Q<Label>("puzzle-feedback");
        _completePanel = root.Q("hall-complete-panel");
        _nextHallBtn   = root.Q<Button>("next-hall-btn");
        _progressLabel = root.Q<Label>("progress-label");
        _progressFill  = root.Q("progress-fill");
        _backToMapHint = root.Q("back-to-map-hint");
        _backToMapBtn  = root.Q<Button>("back-to-map-btn");

        _checkBtn.clicked    += OnCheck;
        _closeBtn.clicked    += ClosePuzzle;
        _nextHallBtn.clicked += OnNextHall;
        if (_backToMapBtn != null)
            _backToMapBtn.clicked += OnBackToMap;

        _overlay.style.display = DisplayStyle.None;
        if (_backToMapHint != null)
            _backToMapHint.style.display = DisplayStyle.None;
        _isOpen = false;
    }

    // ── Open ──────────────────────────────────────────────
    public void OpenPuzzle(PuzzleData data, int index)
    {
        StopAllCoroutines();

        _data        = data;
        _index       = index;
        _isOpen      = true;
        _placedCount = 0;
        _locked      = new bool[N];
        _isDragging  = false;
        _dragEl      = null;

        _title.text = $"✦  {data.shapeName.ToUpper()}  ✦";
        _feedback.text = "";
        _feedback.style.display      = DisplayStyle.None;
        _completePanel.style.display = DisplayStyle.None;

        UpdateProgress(0);
        BuildHearts(PuzzleManager.Instance.Hearts);
        BuildCanvas(data);
        BuildPieceBank(data);

        _overlay.style.opacity = 1f;
        _card.style.opacity    = 1f;
        _card.style.translate  = new Translate(0, 0, 0);
        _overlay.style.display = DisplayStyle.Flex;
        PlaySound(sfxOpen);

        Time.timeScale   = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    // ── Build Canvas ──────────────────────────────────────
    private void BuildCanvas(PuzzleData data)
    {
        _canvas.Clear();

        if (data.ghostOutline != null)
            _canvas.style.backgroundImage = new StyleBackground(data.ghostOutline);

        // Register drag events on canvas
        _canvas.RegisterCallback<MouseMoveEvent>(OnCanvasMouseMove);
        _canvas.RegisterCallback<MouseUpEvent>(OnCanvasMouseUp);
    }

    // ── Build Bank ────────────────────────────────────────
    private void BuildPieceBank(PuzzleData data)
    {
        _pieceBank.Clear();

        var order = new List<int> { 0, 1, 2, 3, 4 };
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        foreach (int pi in order)
            if (!_locked[pi])
                _pieceBank.Add(MakeBankPiece(pi));
    }

    // ── Piece Factories ───────────────────────────────────
    private VisualElement MakeBankPiece(int pi)
    {
        var pd    = _data.pieces[pi];
        var piece = new VisualElement();
        piece.AddToClassList("puzzle-piece");
        piece.userData = pi;

        if (pd.pieceSprite != null)
            piece.style.backgroundImage = new StyleBackground(pd.pieceSprite);

        piece.style.backgroundColor = Color.clear;

        // Bank piece: show at half scale so bank doesn't overflow
        piece.style.width  = pd.width  * 0.45f;
        piece.style.height = pd.height * 0.45f;

        var lbl = new Label(pd.pieceName);
        lbl.AddToClassList("piece-lbl");
        piece.Add(lbl);

        piece.RegisterCallback<MouseDownEvent>(e => OnBankPieceMouseDown(piece, pi, e));
        return piece;
    }

    private VisualElement MakeCanvasPiece(int pi, float left, float top, bool locked = false)
    {
        var pd    = _data.pieces[pi];
        var piece = new VisualElement();
        piece.AddToClassList(locked ? "piece-locked" : "canvas-piece");
        piece.userData = pi;

        if (pd.pieceSprite != null)
            piece.style.backgroundImage = new StyleBackground(pd.pieceSprite);

        piece.style.backgroundColor = Color.clear;

        // Exact pixel size from PuzzleData
        piece.style.width    = pd.width;
        piece.style.height   = pd.height;
        piece.style.position = Position.Absolute;
        piece.style.left     = left;
        piece.style.top      = top;

        return piece;
    }

    // ── Drag from Bank ────────────────────────────────────
    private void OnBankPieceMouseDown(VisualElement piece, int pi, MouseDownEvent e)
    {
        if (_isDragging) return;

        _dragPieceIdx = pi;
        _isDragging   = true;

        _pieceBank.Remove(piece);

        var pd = _data.pieces[pi];

        // Place on canvas centred at cursor
        float startLeft = e.localMousePosition.x - pd.width  * 0.5f;
        float startTop  = e.localMousePosition.y - pd.height * 0.5f;

        // dragOffset = mouse pos relative to piece top-left
        _dragOffset = new Vector2(pd.width * 0.5f, pd.height * 0.5f);

        _dragEl = MakeCanvasPiece(pi, startLeft, startTop);
        _dragEl.AddToClassList("piece-floating");
        _canvas.Add(_dragEl);

        e.StopPropagation();
    }

    // ── Mouse Move ────────────────────────────────────────
    private void OnCanvasMouseMove(MouseMoveEvent e)
    {
        if (!_isDragging || _dragEl == null) return;

        float newLeft = e.localMousePosition.x - _dragOffset.x;
        float newTop  = e.localMousePosition.y - _dragOffset.y;

        _dragEl.style.left = newLeft;
        _dragEl.style.top  = newTop;
    }

    // ── Mouse Up ─────────────────────────────────────────
    private void OnCanvasMouseUp(MouseUpEvent e)
    {
        if (!_isDragging || _dragEl == null) return;
        _isDragging = false;

        var pd = _data.pieces[_dragPieceIdx];

        // Current top-left of dragged piece on canvas
        float currentLeft = _dragEl.style.left.value.value;
        float currentTop  = _dragEl.style.top.value.value;

        // Distance from current position to correct position (pixel)
        float dx   = currentLeft - pd.left;
        float dy   = currentTop  - pd.top;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);

        _canvas.Remove(_dragEl);
        _dragEl = null;

        if (dist <= SNAP_THRESHOLD_PX)
        {
            LockPiece(_dragPieceIdx);
        }
        else
        {
            ReturnToBank(_dragPieceIdx);
            PuzzleManager.Instance.SubmitResult(_index, false); // wrong drop = lose a heart
        }
    }

    // ── Lock Piece ────────────────────────────────────────
    private void LockPiece(int pi)
    {
        _locked[pi] = true;
        _placedCount++;
        UpdateProgress(_placedCount);

        var pd     = _data.pieces[pi];
        var locked = MakeCanvasPiece(pi, pd.left, pd.top, locked: true);
        _canvas.Add(locked);

        _feedback.style.display = DisplayStyle.None;
        PlaySound(sfxSnapCorrect);
        StartCoroutine(FlashGold(locked));
    }

    private IEnumerator FlashGold(VisualElement el)
    {
        el.AddToClassList("piece-snap-flash");
        yield return new WaitForSecondsRealtime(0.4f);
        el.RemoveFromClassList("piece-snap-flash");
    }

    // ── Return to Bank ────────────────────────────────────
    private void ReturnToBank(int pi)
    {
        _pieceBank.Add(MakeBankPiece(pi));
    }

    // ── Check ─────────────────────────────────────────────
    private void OnCheck()
    {
        if (_placedCount < N)
        {
            PuzzleManager.Instance.SubmitResult(_index, false); // checking early = lose a heart
            return;
        }

        bool allLocked = true;
        for (int i = 0; i < N; i++)
            if (!_locked[i]) { allLocked = false; break; }

        PuzzleManager.Instance.SubmitResult(_index, allLocked);
    }

    // ── Helpers ───────────────────────────────────────────
    private void BuildHearts(int count)
    {
        _heartsRow.Clear();
        for (int i = 0; i < 3; i++)
        {
            var h = new Label("♥");
            h.AddToClassList(i < count ? "heart-full" : "heart-empty");
            _heartsRow.Add(h);
        }
    }

    private void UpdateProgress(int placed)
    {
        _placedCount = placed;
        if (_progressLabel != null)
            _progressLabel.text = $"{placed} / {N} pieces placed";
        if (_progressFill != null)
            _progressFill.style.width = Length.Percent((float)placed / N * 100f);
    }

    private void ShowFeedback(string msg, bool good)
    {
        _feedback.text = msg;
        _feedback.EnableInClassList("fb-good", good);
        _feedback.EnableInClassList("fb-bad",  !good);
        _feedback.style.display = DisplayStyle.Flex;
    }

    // ── Public Callbacks ──────────────────────────────────
    public void OnPuzzleSolved()
    {
        PlaySound(sfxWin);
        ShowFeedback("✓  Amazing! You restored it!", true);
        StartCoroutine(AutoCloseAfterSolve());
    }

    public void OnWrongAnswer(int heartsLeft)
    {
        BuildHearts(heartsLeft);
        if (heartsLeft == 0)
        {
            PlaySound(sfxLoseAll);
            ShowFeedback("No hearts left! Starting over...", false);
        }
        else
        {
            PlaySound(sfxLoseHeart);
            ShowFeedback($"Not quite! {heartsLeft} ❤ left — try again!", false);
        }
    }

    public void ShowHallComplete()
    {
        _completePanel.style.display = DisplayStyle.Flex;
        StartCoroutine(UIAnimator.FadeSlide(_completePanel, 30f, 0.5f));
    }

    public void FullReset()
    {
        StopAllCoroutines();
        _isOpen = false;
        _overlay.style.display = DisplayStyle.None;
        if (_backToMapHint != null)
            _backToMapHint.style.display = DisplayStyle.None;
        Time.timeScale   = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private IEnumerator AutoCloseAfterSolve()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        if (_completePanel.style.display != DisplayStyle.Flex)
            DoClose();
    }

    private void ClosePuzzle() => DoClose();

    private void DoClose()
    {
        StopAllCoroutines();
        PlaySound(sfxClose);
        _isOpen     = false;
        _isDragging = false;
        _dragEl     = null;
        _overlay.style.display = DisplayStyle.None;
        Time.timeScale   = 1f;
        // Return cursor to gameplay state (locked in 3D level)
        // OnBackToMap/OnNextHall will override this to None before loading a new scene
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        if (PuzzleManager.Instance != null)
            PuzzleManager.Instance.NotifyClosed();

        // Show "← Back to Map" hint whenever the hall is fully solved
        // (player closed the complete-panel instead of clicking Continue)
        bool hallDone = PuzzleManager.Instance != null && PuzzleManager.Instance.AllSolved;
        if (_backToMapHint != null && hallDone)
        {
            _backToMapHint.style.display = DisplayStyle.Flex;
            StartCoroutine(HideHintAfterDelay(12f));
        }
    }

    private IEnumerator HideHintAfterDelay(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (_backToMapHint != null)
            _backToMapHint.style.display = DisplayStyle.None;
    }

    private void OnNextHall()
    {
        // Unlock cursor BEFORE any scene load so the next scene starts with a usable cursor
        Time.timeScale   = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        FullReset();
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.CompleteAndLoadNext(currentEra);
        }
        else if (!string.IsNullOrEmpty(fallbackNextScene))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(fallbackNextScene);
        }
        else
        {
            Debug.LogError("[PuzzleUI] LevelManager not found AND fallbackNextScene is empty. " +
                           "Set 'Fallback Next Scene' in the PuzzleUI Inspector.");
        }
    }

    private void OnBackToMap()
    {
        if (_backToMapHint != null)
            _backToMapHint.style.display = DisplayStyle.None;

        // Always unlock cursor FIRST — before any scene loads
        Time.timeScale   = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.CompleteLevel(currentEra);
            LevelManager.Instance.LoadMenu();
        }
        else if (!string.IsNullOrEmpty(fallbackMenuScene))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(fallbackMenuScene);
        }
        else
        {
            Debug.LogError("[PuzzleUI] LevelManager not found AND fallbackMenuScene is empty.");
        }
    }

    private void Update()
    {
        if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            DoClose();
    }

    // ── Audio Helper ──────────────────────────────────────
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}
