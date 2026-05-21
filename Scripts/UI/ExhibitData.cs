using UnityEngine;

[CreateAssetMenu(fileName = "ExhibitData", menuName = "ModernMuseum/Exhibit Data")]
public class ExhibitData : ScriptableObject
{
    [Header("Exhibit Info")]
    public string exhibitName;

    [TextArea(3, 8)]
    public string[] pages;           // one entry per page

    public Sprite exhibitIllustration; // optional image shown on the panel

    [Header("Narrator")]
    public AudioClip narratorClip;    // voice-over clip for this exhibit
}
