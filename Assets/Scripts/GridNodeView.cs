using UnityEngine;

/// <summary>
/// Kelas GridNodeView bertanggung jawab untuk visualisasi node grid dalam sistem pathfinding.
/// Kelas ini menghubungkan data GridNode dengan representasi visualnya di Unity.
/// </summary>
public class GridNodeView : MonoBehaviour
{
    /// <summary>
    /// Referensi ke SpriteRenderer untuk bagian dalam node.
    /// </summary>
    [SerializeField]
    SpriteRenderer innerSprite;

    /// <summary>
    /// Referensi ke SpriteRenderer untuk bagian luar node.
    /// </summary>
    [SerializeField]
    SpriteRenderer outerSprite;

    /// <summary>
    /// Properti yang menyimpan referensi ke objek GridNode yang terkait dengan view ini.
    /// </summary>
    public GridNode Node { get; set; }

    /// <summary>
    /// Mengatur warna sprite bagian dalam dari node.
    /// </summary>
    /// <param name="col">Warna yang akan diaplikasikan pada sprite bagian dalam.</param>
    public void SetInnerColor(Color col)
    {
        innerSprite.color = col;
    }

    /// <summary>
    /// Mengatur warna sprite bagian luar dari node.
    /// </summary>
    /// <param name="col">Warna yang akan diaplikasikan pada sprite bagian luar.</param>
    public void SetOuterColor(Color col)
    {
        outerSprite.color = col;
    }
}
