using UnityEngine;

[CreateAssetMenu(menuName = "Museum/Artifact")]
public class ArtifactData : ScriptableObject
{
    public string artifactName;
    public string era;
    public string description;
    public string funFact;

    public AudioClip narrationSound;
}