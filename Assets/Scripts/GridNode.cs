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
    public bool IsWalkable { get; set; }

    internal GridMap gridMap;

    public GridNode(Vector2Int value, GridMap gridMap) : base(value)
    {
        IsWalkable = true;
        this.gridMap = gridMap;
    }

    public override List<PathFinding.Node<Vector2Int>> GetNeighbours()
    {
        return gridMap.GetNeighbours(this);
    }
}
