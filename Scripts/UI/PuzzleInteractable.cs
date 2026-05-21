// ════════════════════════════════════════════════
// PuzzleInteractable.cs
// Add this to each puzzle shape GameObject
// (coin, eagle, key) in your scene
// ════════════════════════════════════════════════

using UnityEngine;

public class PuzzleInteractable : MonoBehaviour
{
    [SerializeField] public int puzzleIndex; // 0=coin 1=eagle 2=key

    public void OnInteract()
    {
        PuzzleManager.Instance.OpenPuzzle(puzzleIndex);
    }
}
