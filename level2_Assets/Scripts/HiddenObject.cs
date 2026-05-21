using UnityEngine;

public class HiddenObject : MonoBehaviour
{
    private bool found = false;

    void OnEnable()
    {
        // Reset when the object becomes active
        found = false;
    }

    // Called by ClickDetector when this hidden object is clicked
    public void RegisterClick()
    {
        if (found) return;

        found = true;

        if (MuseumManager.Instance != null)
            MuseumManager.Instance.ObjectFound();

        // Hide this object after it is found
        gameObject.SetActive(false);
    }
}