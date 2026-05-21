using UnityEngine;

public class ClickDetector : MonoBehaviour
{
    private int layerMask = 0;

    void Start()
    {
        int layer = LayerMask.NameToLayer("HiddenObject");

        if (layer == -1)
        {
            Debug.LogError(
                "ClickDetector: Layer 'HiddenObject' not found! " +
                "Go to Edit > Project Settings > Tags and Layers, " +
                "add a layer called exactly 'HiddenObject', " +
                "and assign it to all 10 hidden objects."
            );
            return;
        }

        layerMask = LayerMask.GetMask("HiddenObject");
        Debug.Log("ClickDetector ready. LayerMask = " + layerMask);
    }

    void Update()
    {
        // Safety checks
        if (MuseumManager.Instance == null) return;
        if (!MuseumManager.Instance.findingPhase) return;
        if (layerMask == 0) return;
        if (Camera.main == null) return;

        // Detect left mouse click
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Raycast only against the HiddenObject layer
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, layerMask))
            {
                // Try to get HiddenObject from the hit object
                HiddenObject hiddenObj =
                    hit.transform.GetComponent<HiddenObject>();

                // If not found, search in parent
                if (hiddenObj == null)
                    hiddenObj =
                        hit.transform.GetComponentInParent<HiddenObject>();

                if (hiddenObj != null)
                {
                    Debug.Log("Clicked hidden object: " + hit.transform.name);
                    hiddenObj.RegisterClick();
                }
                else
                {
                    Debug.LogWarning(
                        "Object was hit but no HiddenObject component found on: "
                        + hit.transform.name
                    );
                }
            }
        }
    }
}