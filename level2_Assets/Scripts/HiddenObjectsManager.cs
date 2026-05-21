using UnityEngine;

public class HiddenObjectsManager : MonoBehaviour
{
    public static HiddenObjectsManager Instance;

    [Header("All 10 Hidden Objects")]
    public GameObject[] allHiddenObjects;

    void Awake()
    {
        Instance = this;

        // Hide all hidden objects when the scene starts
        DeactivateAll();
    }

    public void ActivateAll()
{
    foreach (GameObject obj in allHiddenObjects)
    {
        if (obj != null)
            obj.SetActive(true);
    }

    Debug.Log("All hidden objects activated.");
}

    public void DeactivateAll()
    {
        foreach (GameObject obj in allHiddenObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        Debug.Log("All hidden objects deactivated.");
    }
}