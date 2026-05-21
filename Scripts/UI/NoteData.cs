using UnityEngine;

public enum NoteCategory { General, Death, Gods, Pharaoh, Afterlife }

[CreateAssetMenu(fileName = "NoteData", menuName = "EchoesOfTime/Note Data")]
public class NoteData : ScriptableObject
{
    [Header("Statue Info")]
    public string statueName;

    [TextArea(3, 8)]
    public string[] pages;            // one entry per page

    public Sprite statueIllustration; // optional image shown on the note

    [Header("Narrator")]
    public AudioClip narratorClip;    // voice-over clip for this note

    [Header("Category")]
    public NoteCategory category;     // drives the hieroglyph sidebar symbols
}
