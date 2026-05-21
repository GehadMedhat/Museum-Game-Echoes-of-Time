using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance { get; private set; }

    [Header("Puzzles — assign all 3 PuzzleData assets")]
    [SerializeField] private PuzzleData[] puzzles;

    [Header("Hearts")]
    [SerializeField] private int maxHearts = 3;

    public UnityEvent OnAllSolved;

    private int    _hearts;
    private bool[] _solved;
    private int    _activeIndex;

    public int    Hearts      => _hearts;
    public bool   AllSolved   => _solved != null && AllDone();
    public bool[] Solved      => _solved;
    public int    PuzzleCount => puzzles.Length;

    private void Awake()
    {
        Instance     = this;
        _hearts      = maxHearts;
        _solved      = new bool[puzzles.Length];
        _activeIndex = -1;
    }

    public PuzzleData GetPuzzle(int i) => puzzles[i];

    public void OpenPuzzle(int index)
    {
        if (_solved[index]) return;
        _activeIndex = index;
        _hearts = maxHearts;   // reset to full hearts every time a puzzle opens
        PuzzleUI.Instance.OpenPuzzle(puzzles[index], index);
    }

    public void SubmitResult(int index, bool correct)
    {
        if (correct)
        {
            _solved[index] = true;
            StartCoroutine(AssembleInWorld(index));
            PuzzleUI.Instance.OnPuzzleSolved();
            if (AllDone()) StartCoroutine(CompleteHall());
        }
        else
        {
            _hearts = Mathf.Max(0, _hearts - 1);
            PuzzleUI.Instance.OnWrongAnswer(_hearts);

            // BUG FIX 2: 0 hearts = reset progress but DON'T touch 3D models
            // broken shapes stay broken — player just loses progress
            if (_hearts == 0)
                StartCoroutine(ResetProgressOnly());
        }
    }

    private bool AllDone()
    {
        foreach (var s in _solved) if (!s) return false;
        return true;
    }

    private IEnumerator CompleteHall()
    {
        yield return new WaitForSecondsRealtime(1.2f);
        PuzzleUI.Instance.ShowHallComplete();
        OnAllSolved?.Invoke();
    }

    private IEnumerator AssembleInWorld(int index)
    {
        yield return new WaitForSecondsRealtime(0.6f);

        PuzzleSceneLink link = FindLink(index);
        if (link == null)
        {
            Debug.LogWarning($"No PuzzleSceneLink found for puzzle index {index}.");
            yield break;
        }

        // BUG FIX 3: Save original scale before animating
        Vector3 brokenOriginalScale = link.brokenRoot != null
            ? link.brokenRoot.transform.localScale : Vector3.one;
        Vector3 solvedOriginalScale = link.solvedRoot != null
            ? link.solvedRoot.transform.localScale : Vector3.one;

        // Scale down broken root to zero then hide
        if (link.brokenRoot != null)
        {
            float t = 0f;
            Vector3 startScale = link.brokenRoot.transform.localScale;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                link.brokenRoot.transform.localScale =
                    Vector3.Lerp(startScale, Vector3.zero, t / 0.4f);
                yield return null;
            }
            link.brokenRoot.SetActive(false);
            // Restore original scale so reset works correctly later
            link.brokenRoot.transform.localScale = brokenOriginalScale;
        }

        // Scale up solved root from zero to its ORIGINAL scale (not Vector3.one!)
        if (link.solvedRoot != null)
        {
            link.solvedRoot.SetActive(true);
            link.solvedRoot.transform.localScale = Vector3.zero;
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                link.solvedRoot.transform.localScale =
                    Vector3.Lerp(Vector3.zero, solvedOriginalScale, t / 0.4f);
                yield return null;
            }
            link.solvedRoot.transform.localScale = solvedOriginalScale;
        }
    }

    // BUG FIX 2: Reset only hearts + UI progress, NOT the 3D models
  private IEnumerator ResetProgressOnly()
{
    // Wait in real time (not game time — timeScale is 0!)
    yield return new WaitForSecondsRealtime(2f);
    
    _hearts = maxHearts;
    
    // Restore time FIRST before any UI calls
    Time.timeScale   = 1f;
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible   = false;
    
    // Then reset UI state
    PuzzleUI.Instance.FullReset();
    
    // Reset cooldown so player can interact immediately
    // (call this via a public method on PlayerController)
    var player = Object.FindFirstObjectByType<PlayerController>();
    if (player != null) player.ResetInteractCooldown();
}

    // BUG FIX 1: Public method so UI close button also fully resets state
    public void NotifyClosed()
    {
        _activeIndex = -1;
    }

    private PuzzleSceneLink FindLink(int index)
    {
        var links = Object.FindObjectsByType<PuzzleSceneLink>(FindObjectsSortMode.None);
        foreach (var l in links) if (l.puzzleIndex == index) return l;
        return null;
    }
}
