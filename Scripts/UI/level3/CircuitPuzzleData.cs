using UnityEngine;

[CreateAssetMenu(fileName = "CircuitPuzzleData", menuName = "Puzzle/Circuit Puzzle Data")]
public class CircuitPuzzleData : ScriptableObject
{
    [Header("Component Sprites")]
    public Sprite batterySprite;       // battery image
    public Sprite bulbUnlitSprite;     // bulb before win (dark)
    public Sprite bulbLitSprite;       // bulb after win  (glowing)
    public Sprite switchSprite;        // switch image

    [Header("Wire Sprites")]
    public Sprite wireGreenSprite;     // green wire  — Battery(+) → Bulb
    public Sprite wireYellowSprite;    // yellow wire — Bulb       → Switch
    public Sprite wireOrangeSprite;    // orange wire — Switch     → Battery(-)

    [Header("Wire Colors (for drawing on canvas)")]
    public Color wireColorGreen  = new Color(0.18f, 0.72f, 0.25f);
    public Color wireColorYellow = new Color(0.95f, 0.82f, 0.10f);
    public Color wireColorOrange = new Color(0.95f, 0.50f, 0.10f);

    [Header("Hint Image")]
    public Sprite hintCircuitSprite;   // small corner image shown during puzzle
    // NOTE: No connectedCircuitSprite needed — the museum model is a hidden 3D
    //       GameObject that gets revealed on win via CircuitMuseumModel.cs
}
