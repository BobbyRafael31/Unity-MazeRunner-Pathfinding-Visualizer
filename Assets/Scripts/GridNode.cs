using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kelas GridNode merepresentasikan sebuah node/sel di dalam grid untuk algoritma pathfinding.
/// Kelas ini mewarisi dari PathFinding.Node<Vector2Int> dan mengimplementasikan fungsionalitas spesifik
/// untuk pathfinding berbasis grid.
/// </summary>
public class GridNode : PathFinding.Node<Vector2Int>
{
  /// <summary>
  /// Menentukan apakah node ini dapat dilalui oleh karakter.
  /// True jika node dapat dilalui, false jika node adalah penghalang.
  /// </summary>
  public bool IsWalkable { get; set; }

  /// <summary>
  /// Referensi ke GridMap yang mengelola seluruh grid.
  /// Digunakan untuk mendapatkan tetangga dan operasi lain yang berhubungan dengan grid.
  /// </summary>
  public GridMap gridMap; // Change to internal or public

  /// <summary>
  /// Constructor untuk membuat GridNode baru.
  /// </summary>
  /// <param name="value">Koordinat Vector2Int yang merepresentasikan posisi node di dalam grid</param>
  /// <param name="gridMap">Referensi ke GridMap yang mengelola grid ini</param>
  public GridNode(Vector2Int value, GridMap gridMap)
    : base(value)
  {
    IsWalkable = true; // Secara default node dapat dilalui
    this.gridMap = gridMap; // Simpan referensi ke GridMap
  }

  /// <summary>
  /// Mengimplementasikan metode abstrak dari kelas dasar untuk mendapatkan daftar node tetangga.
  /// Metode ini akan memanggil GridMap.GetNeighbours() untuk mendapatkan semua node tetangga yang dapat dilalui.
  /// </summary>
  /// <returns>Daftar node tetangga yang dapat dicapai dari node ini</returns>
  public override
    List<PathFinding.Node<Vector2Int>> GetNeighbours()
  {
    // Return an empty list for now.
    // Later we will call gridMap's GetNeighbours
    // function.
    //return new List<PathFinding.Node<Vector2Int>>();
    return gridMap.GetNeighbours(this);
  }
}
