using UnityEngine;

[CreateAssetMenu(fileName = "PuzzleData", menuName = "EchoesOfTime/Puzzle Data")]
public class PuzzleData : ScriptableObject
{
    [Header("Identity")]
    public string shapeName;

    [Header("Images")]
    public Sprite ghostOutline;
    public Sprite assembledImage;

    [System.Serializable]
    public class PieceInfo
    {
        public string pieceName;
        public Sprite pieceSprite;

        [Header("Pixel position on 550x550 canvas")]
        public float left;    // px from left edge of canvas
        public float top;     // px from top edge of canvas
        public float width;   // px width of piece
        public float height;  // px height of piece
    }

    [Header("Pieces (5)")]
    public PieceInfo[] pieces = new PieceInfo[5];
}
