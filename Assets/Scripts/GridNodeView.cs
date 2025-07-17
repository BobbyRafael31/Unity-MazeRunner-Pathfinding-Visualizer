using UnityEngine;

/// <summary>
/// Kelas GridNodeView bertanggung jawab untuk visualisasi node grid dalam sistem pathfinding.
/// Kelas ini menghubungkan data GridNode dengan representasi visualnya di Unity.
/// </summary>
public class GridNodeView : MonoBehaviour
{
    [SerializeField]
    SpriteRenderer innerSprite;

    [SerializeField]
    SpriteRenderer outerSprite;

    public GridNode Node { get; set; }

    public void SetInnerColor(Color col)
    {
        innerSprite.color = col;
    }

    public void SetOuterColor(Color col)
    {
        outerSprite.color = col;
    }
}
