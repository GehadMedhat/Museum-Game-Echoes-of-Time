/*
 ═══════════════════════════════════════════════════════════
 ExhibitInteractable.cs  —  ModernMuseum
 ───────────────────────────────────────────────────────────
 Attach this to any exhibit GameObject in the Modern Museum.
 Assign an ExhibitData asset in the Inspector.
 PlayerController calls OnInteract() when the player
 presses E near the exhibit.
 ═══════════════════════════════════════════════════════════
*/

using UnityEngine;

public class ExhibitInteractable : MonoBehaviour
{
    [Header("Exhibit")]
    [SerializeField] private ExhibitData exhibitData;

    public void OnInteract()
    {
        if (exhibitData == null)
        {
            Debug.LogWarning($"[ExhibitInteractable] No ExhibitData assigned on {gameObject.name}!");
            return;
        }

        ModernNoteUI.Instance.ShowPanel(exhibitData);
    }
}
