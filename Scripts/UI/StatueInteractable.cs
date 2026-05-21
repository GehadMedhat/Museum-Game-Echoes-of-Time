using UnityEngine;

public class StatueInteractable : MonoBehaviour
{
    [SerializeField] public NoteData noteData;

    public void OnInteract()
    {
        if (noteData != null)
            NoteUI.Instance.ShowNote(noteData);
        else
            Debug.LogWarning($"[StatueInteractable] No NoteData assigned on {gameObject.name}");
    }
}
