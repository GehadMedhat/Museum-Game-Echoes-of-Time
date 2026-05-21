using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

[RequireComponent(typeof(UIDocument))]
public class CircuitPuzzleInteractable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;

    [Header("Circuit Info (shown before puzzle)")]
    [SerializeField] private ExhibitData circuitInfoData;

    [Header("Audio - Puzzle Open / Close")]
    [SerializeField] private AudioSource puzzleAudioSource;
    [SerializeField] private AudioClip   puzzleOpenClip;
    [SerializeField] private AudioClip   puzzleCloseClip;

    private bool _isOpen = false;

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        uiDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    public void OnInteract()
    {
        if (_isOpen) return;

        if (circuitInfoData == null || ModernNoteUI.Instance == null)
        {
            // No info panel — go straight to puzzle if not solved
            if (PlayerPrefs.GetInt("CircuitPuzzleSolved", 0) == 0)
                OpenPuzzle();
            return;
        }

        playerController?.SetPuzzleOpen(true);

        bool alreadySolved = PlayerPrefs.GetInt("CircuitPuzzleSolved", 0) == 1;

        if (alreadySolved)
        {
            // Read-only note — null callback hides the Start Repair button
            ModernNoteUI.Instance.ShowPanel(circuitInfoData, onClose: null);

            // Still need to restore movement when the note closes.
            // We do this via the Escape / close-btn path which sets timeScale=1
            // and cursor back — movement is restored when SetPuzzleOpen is called
            // from the NoteUI side. Add a small Update watch instead:
            _waitingForNoteClose = true;
        }
        else
        {
            // Show button — callback opens puzzle after note closes
            ModernNoteUI.Instance.ShowPanel(circuitInfoData, onClose: () =>
            {
                playerController?.SetPuzzleOpen(false);
                OpenPuzzle();
            });
        }
    }

    // Watch for the read-only note closing so we can restore movement
    private bool _waitingForNoteClose = false;

    private void Update()
    {
        if (!_waitingForNoteClose) return;
        if (ModernNoteUI.Instance != null && !ModernNoteUI.Instance.IsOpen)
        {
            _waitingForNoteClose = false;
            playerController?.SetPuzzleOpen(false);
        }
    }

    private void OpenPuzzle()
    {
        _isOpen = true;
        uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;

        if (playerController != null)
            playerController.SetPuzzleOpen(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        PlayPuzzleSound(puzzleOpenClip);
    }

    public void ClosePuzzle()
    {
        _isOpen = false;
        uiDocument.rootVisualElement.style.display = DisplayStyle.None;

        if (playerController != null)
        {
            playerController.SetPuzzleOpen(false);
            playerController.ResetInteractCooldown();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        PlayPuzzleSound(puzzleCloseClip);
    }

    public bool IsOpen => _isOpen;

    private void PlayPuzzleSound(AudioClip clip)
    {
        if (puzzleAudioSource == null || clip == null) return;
        puzzleAudioSource.PlayOneShot(clip);
    }
    
    public void SetOpen(bool value)
{
    _isOpen = value;
}

public void RestoreMovement()
{
    _isOpen = false;
    if (playerController != null)
    {
        playerController.SetPuzzleOpen(false);
        playerController.ResetInteractCooldown();
                playerController.canMove = true;  // ← add this

    }
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
}
}
