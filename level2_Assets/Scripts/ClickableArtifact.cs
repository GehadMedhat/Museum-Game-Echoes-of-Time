using UnityEngine;

public class ClickableArtifact : MonoBehaviour
{
    public ArtifactData data;
    public int artifactIndex = 0;
    public float clickDistance = 30f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(
                Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 200f))
            {
                if (hit.transform == this.transform ||
                    hit.transform.IsChildOf(this.transform) ||
                    this.transform.IsChildOf(hit.transform))
                {
                    MuseumManager.Instance.ShowArtifact(
                        data, artifactIndex);
                }
            }
        }
    }
}