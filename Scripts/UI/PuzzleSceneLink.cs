using UnityEngine;

/// <summary>
/// Attach this to a GameObject IN THE SCENE (not a ScriptableObject).
/// Holds all scene-side references for one puzzle.
/// ScriptableObjects cannot reference scene objects — this MonoBehaviour bridges the gap.
/// </summary>
public class PuzzleSceneLink : MonoBehaviour
{
    [Header("Match this to PuzzleManager puzzles array index")]
    public int puzzleIndex;  // 0=Coin, 1=Eagle, 2=Key

    [Header("Scene References — drag from Hierarchy")]
    public GameObject brokenRoot;    // Parent holding all scattered pieces
    public GameObject solvedRoot;    // Assembled model (set inactive by default)
    public GameObject[] worldPieces; // The 5 individual broken pieces
}
