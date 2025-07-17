using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// GridMap adalah komponen utama yang mengelola grid untuk algoritma pathfinding.
/// Kelas ini bertanggung jawab untuk membuat, menampilkan, dan mengelola node-node grid
/// serta memberikan fungsionalitas untuk pathfinding.
/// </summary>

public class GridMap : MonoBehaviour
{
    [SerializeField]
    int numX;

    [SerializeField]
    int numY;

    [SerializeField]
    GameObject gridNodeViewPrefab;

    [SerializeField]
    bool allowDiagonalMovement = false;

    public bool AllowDiagonalMovement
    {
        get { return allowDiagonalMovement; }
        set { allowDiagonalMovement = value; }
    }

    public Color COLOR_WALKABLE = Color.white;
    public Color COLOR_NONWALKABLE = Color.black;
    public Color COLOR_CURRENT_NODE = Color.cyan;
    public Color COLOR_ADD_TO_OPENLIST = Color.green;
    public Color COLOR_ADD_TO_CLOSEDLIST = Color.grey;
    public Color COLOR_PATH = Color.blue;

    public int NumX { get { return numX; } }
    public int NumY { get { return numY; } }

    [SerializeField]
    NPC npc;

    [SerializeField]
    Transform destination;

    public Transform Destination { get { return destination; } }

    float gridNodeWidth = 1.0f;
    float gridNodeHeight = 1.0f;

    public float GridNodeWidth { get { return gridNodeWidth; } }
    public float GridNodeHeight { get { return gridNodeHeight; } }

    private GridNodeView[,] gridNodeViews = null;
    private GridNodeView lastToggledNode = null;

    private Vector2 lastMousePosition;


    void Start()
    {
        gridNodeViews = new GridNodeView[NumX, NumY];
        for (int i = 0; i < NumX; i++)
        {
            for (int j = 0; j < NumY; j++)
            {
                GameObject obj = Instantiate(
                  gridNodeViewPrefab,
                  new Vector3(
                    i * GridNodeWidth,
                    j * GridNodeHeight,
                    0.0f),
                  Quaternion.identity);

                obj.name = "GridNode_" + i.ToString() + "_" + j.ToString();
                GridNodeView gnv = obj.GetComponent<GridNodeView>();
                gridNodeViews[i, j] = gnv;
                gnv.Node = new GridNode(new Vector2Int(i, j), this);

                obj.transform.SetParent(transform);
            }
        }

        SetCameraPosition();

        npc.Map = this;
        npc.SetStartNode(gridNodeViews[0, 0].Node);
    }

    void SetCameraPosition()
    {
        float gridCenterX = ((numX - 1) * GridNodeWidth) / 2;
        float gridCenterY = ((numY - 1) * GridNodeHeight) / 2;

        float gridWidth = (numX - 1) * GridNodeWidth;
        float gridHeight = (numY - 1) * GridNodeHeight;

        float baseX = 5.6f;
        float baseY = 10.7f;
        float baseOrthoSize = 12.4f;
        float baseGridSize = 20f;

        float scaleFactor = Mathf.Max(numX, numY) / baseGridSize;
        float xPos = baseX * scaleFactor;
        float yPos = baseY * scaleFactor;
        float orthoSize = baseOrthoSize * scaleFactor;

        Camera.main.transform.position = new Vector3(
            xPos,
            yPos,
            -100.0f);

        Camera.main.orthographicSize = orthoSize;
    }

    private static readonly int[,] directions = new int[,] {
        { 0, 1 },  // Atas
        { 1, 0 },  // Kanan
        { 0, -1 }, // Bawah
        { -1, 0 }, // Kiri
        { 1, 1 },  // Kanan Atas
        { 1, -1 }, // Kanan Bawah
        { -1, -1 }, // Kiri Bawah
        { -1, 1 }  // Kiri Atas
    };

    public List<PathFinding.Node<Vector2Int>> GetNeighbours(PathFinding.Node<Vector2Int> loc)
    {
        List<PathFinding.Node<Vector2Int>> neighbours = new List<PathFinding.Node<Vector2Int>>(8);

        int x = loc.Value.x;
        int y = loc.Value.y;

        for (int dir = 0; dir < 4; dir++)
        {
            int nx = x + directions[dir, 0];
            int ny = y + directions[dir, 1];

            if (nx >= 0 && nx < numX && ny >= 0 && ny < numY && gridNodeViews[nx, ny].Node.IsWalkable)
            {
                neighbours.Add(gridNodeViews[nx, ny].Node);
            }
        }

        if (allowDiagonalMovement)
        {
            for (int dir = 4; dir < 8; dir++)
            {
                int nx = x + directions[dir, 0];
                int ny = y + directions[dir, 1];

                if (nx >= 0 && nx < numX && ny >= 0 && ny < numY && gridNodeViews[nx, ny].Node.IsWalkable)
                    neighbours.Add(gridNodeViews[nx, ny].Node);
            }
        }

        return neighbours;
    }

    public void RayCastAndToggleWalkable()
    {
        Vector2 rayPos = new Vector2(
          Camera.main.ScreenToWorldPoint(Input.mousePosition).x,
          Camera.main.ScreenToWorldPoint(Input.mousePosition).y);

        RaycastHit2D hit = Physics2D.Raycast(rayPos, Vector2.zero, 0f);

        if (hit)
        {
            GameObject obj = hit.transform.gameObject;
            GridNodeView gnv = obj.GetComponent<GridNodeView>();

            if (gnv != null && gnv != lastToggledNode)
            {
                ToggleWalkable(gnv);
                lastToggledNode = gnv;
            }
        }
        else
        {
            lastToggledNode = null;
        }
    }

    public void RayCastAndSetDestination()
    {
        if (npc != null && (npc.pathFinder?.Status == PathFinding.PathFinderStatus.RUNNING ||
                            npc.IsVisualizingPath ||
                            npc.IsMoving))
        {
            return;
        }

        Vector2 rayPos = new Vector2(
          Camera.main.ScreenToWorldPoint(Input.mousePosition).x,
          Camera.main.ScreenToWorldPoint(Input.mousePosition).y);

        RaycastHit2D hit = Physics2D.Raycast(rayPos, Vector2.zero, 0f);

        if (hit)
        {
            GameObject obj = hit.transform.gameObject;
            GridNodeView gnv = obj.GetComponent<GridNodeView>();

            Vector3 pos = destination.position;
            pos.x = gnv.Node.Value.x * gridNodeWidth;
            pos.y = gnv.Node.Value.y * gridNodeHeight;
            destination.position = pos;
        }
    }

    public void ToggleWalkable(GridNodeView gnv)
    {
        if (gnv == null)
            return;

        int x = gnv.Node.Value.x;
        int y = gnv.Node.Value.y;

        gnv.Node.IsWalkable = !gnv.Node.IsWalkable;

        if (gnv.Node.IsWalkable)
        {
            gnv.SetInnerColor(COLOR_WALKABLE);
        }
        else
        {
            gnv.SetInnerColor(COLOR_NONWALKABLE);
        }
    }

    public void RayCastAndSetNPCPosition()
    {
        if (npc != null && (npc.pathFinder?.Status == PathFinding.PathFinderStatus.RUNNING ||
                            npc.IsVisualizingPath ||
                            npc.IsMoving))
        {
            return;
        }

        Vector2 rayPos = new Vector2(
          Camera.main.ScreenToWorldPoint(Input.mousePosition).x,
          Camera.main.ScreenToWorldPoint(Input.mousePosition).y);

        RaycastHit2D hit = Physics2D.Raycast(rayPos, Vector2.zero, 0f);

        if (hit)
        {

            GameObject obj = hit.transform.gameObject;
            GridNodeView gnv = obj.GetComponent<GridNodeView>();

            if (gnv != null && gnv.Node.IsWalkable)
            {
                npc.SetStartNode(gnv.Node);
            }
        }
    }

    void Update()
    {
        bool isPathfindingActive = npc != null && (npc.pathFinder?.Status == PathFinding.PathFinderStatus.RUNNING ||
                                                 npc.IsVisualizingPath ||
                                                 npc.IsMoving);
        // Middle mouse button
        if (Input.GetMouseButton(2))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            Vector3 moveDirection = new Vector3(-mouseX, -mouseY, 0) * Camera.main.orthographicSize * 0.15f;
            Camera.main.transform.position += moveDirection;
        }

        // Left mouse button
        if (Input.GetMouseButton(0))
        {
            Vector2 currentMousePosition = new Vector2(
                Camera.main.ScreenToWorldPoint(Input.mousePosition).x,
                Camera.main.ScreenToWorldPoint(Input.mousePosition).y);

            if (currentMousePosition != lastMousePosition)
            {
                if (isPathfindingActive)
                {
                    lastMousePosition = currentMousePosition;
                    return;
                }

                // Shift + Left Click
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    RayCastAndToggleWalkable();
                }
                else
                {
                    RayCastAndSetNPCPosition();
                }

                lastMousePosition = currentMousePosition;
            }
        }
        else
        {
            lastToggledNode = null;
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (!isPathfindingActive)
            {
                RayCastAndSetDestination();
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {

            Camera.main.orthographicSize = Mathf.Clamp(
                Camera.main.orthographicSize - scroll * Camera.main.orthographicSize,
                1.0f,
                200.0f);
        }
    }

    public GridNode GetGridNode(int x, int y)
    {
        if (x >= 0 && x < numX && y >= 0 && y < numY)
            return gridNodeViews[x, y].Node;

        return null;
    }

    public GridNodeView GetGridNodeView(int x, int y)
    {
        if (x >= 0 && x < numX && y >= 0 && y < numY)
            return gridNodeViews[x, y];

        return null;
    }

    public static float GetManhattanCost(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    public static float GetCostBetweenTwoCells(Vector2Int a, Vector2Int b)
    {
        return Mathf.Sqrt(
          (a.x - b.x) * (a.x - b.x) +
          (a.y - b.y) * (a.y - b.y)
          );
    }

    public static float GetEuclideanCost(Vector2Int a, Vector2Int b)
    {
        return GetCostBetweenTwoCells(a, b);
    }

    public void OnChangeCurrentNode(PathFinding.PathFinder<Vector2Int>.PathFinderNode node)
    {
        int x = node.Location.Value.x;
        int y = node.Location.Value.y;
        GridNodeView gnv = gridNodeViews[x, y];
        gnv.SetInnerColor(COLOR_CURRENT_NODE);
    }

    public void OnAddToOpenList(PathFinding.PathFinder<Vector2Int>.PathFinderNode node)
    {
        int x = node.Location.Value.x;
        int y = node.Location.Value.y;
        GridNodeView gnv = gridNodeViews[x, y];
        gnv.SetInnerColor(COLOR_ADD_TO_OPENLIST);
    }

    public void OnAddToClosedList(PathFinding.PathFinder<Vector2Int>.PathFinderNode node)
    {
        int x = node.Location.Value.x;
        int y = node.Location.Value.y;
        GridNodeView gnv = gridNodeViews[x, y];
        gnv.SetInnerColor(COLOR_ADD_TO_CLOSEDLIST);
    }

    public void ResetGridNodeColours()
    {
        for (int i = 0; i < numX; ++i)
        {
            for (int j = 0; j < numY; ++j)
            {
                GridNodeView gnv = gridNodeViews[i, j];
                if (gnv.Node.IsWalkable)
                {
                    gnv.SetInnerColor(COLOR_WALKABLE);
                }
                else
                {
                    gnv.SetInnerColor(COLOR_NONWALKABLE);
                }
            }
        }
    }

    [System.Serializable]
    public class SerializablePosition
    {
        public float x;
        public float y;

        public SerializablePosition(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [System.Serializable]
    public class GridState
    {
        public bool[,] walkableStates;
        public SerializablePosition npcPosition;
        public SerializablePosition destinationPosition;
    }

    public void SaveGridState(string filePath)
    {
        try
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            GridState gridState = new GridState();
            gridState.walkableStates = new bool[numX, numY];

            for (int i = 0; i < numX; i++)
            {
                for (int j = 0; j < numY; j++)
                {
                    gridState.walkableStates[i, j] = gridNodeViews[i, j].Node.IsWalkable;
                }
            }

            gridState.npcPosition = new SerializablePosition(
                npc.transform.position.x / GridNodeWidth,
                npc.transform.position.y / GridNodeHeight
            );

            gridState.destinationPosition = new SerializablePosition(
                destination.position.x / GridNodeWidth,
                destination.position.y / GridNodeHeight
            );

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            string json = JsonConvert.SerializeObject(gridState, settings);
            File.WriteAllText(filePath, json);
            Debug.Log($"Grid state saved to {filePath}");

        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving grid state: {e.Message}");
        }
    }

    public void LoadGridState(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Save file not found: {filePath}");
                return;
            }

            string json = File.ReadAllText(filePath);

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            GridState gridState = JsonConvert.DeserializeObject<GridState>(json, settings);

            if (gridState.walkableStates.GetLength(0) != numX ||
                gridState.walkableStates.GetLength(1) != numY)
            {
                Debug.LogWarning($"Grid size mismatch. File: {gridState.walkableStates.GetLength(0)}x{gridState.walkableStates.GetLength(1)}, Current: {numX}x{numY}. Resizing grid...");
                ResizeGrid(gridState.walkableStates.GetLength(0), gridState.walkableStates.GetLength(1));
            }

            for (int i = 0; i < numX; i++)
            {
                for (int j = 0; j < numY; j++)
                {
                    gridNodeViews[i, j].Node.IsWalkable = gridState.walkableStates[i, j];
                    gridNodeViews[i, j].SetInnerColor(gridState.walkableStates[i, j] ? COLOR_WALKABLE : COLOR_NONWALKABLE);
                }
            }

            bool hasPositionData = gridState.npcPosition != null && gridState.destinationPosition != null;

            if (npc != null)
            {
                if (hasPositionData)
                {
                    int npcX = Mathf.Clamp(Mathf.RoundToInt(gridState.npcPosition.x), 0, numX - 1);
                    int npcY = Mathf.Clamp(Mathf.RoundToInt(gridState.npcPosition.y), 0, numY - 1);

                    GridNode npcNode = GetGridNode(npcX, npcY);
                    if (npcNode != null && npcNode.IsWalkable)
                    {
                        npc.SetStartNode(npcNode);
                    }
                    else
                    {
                        FindWalkableNodeAndSetNPC();
                    }
                }
                else
                {
                    FindWalkableNodeAndSetNPC();
                }
            }

            if (destination != null)
            {
                if (hasPositionData)
                {
                    int destX = Mathf.Clamp(Mathf.RoundToInt(gridState.destinationPosition.x), 0, numX - 1);
                    int destY = Mathf.Clamp(Mathf.RoundToInt(gridState.destinationPosition.y), 0, numY - 1);

                    SetDestination(destX, destY);
                }
                else
                {
                    FindWalkableNodeAndSetDestination();
                }
            }

            Debug.Log($"Grid state loaded from {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading grid state: {e.Message}");
        }
    }

    private void FindWalkableNodeAndSetNPC()
    {
        for (int i = 0; i < numX; i++)
        {
            for (int j = 0; j < numY; j++)
            {
                GridNode node = GetGridNode(i, j);
                if (node != null && node.IsWalkable)
                {
                    npc.SetStartNode(node);
                    return;
                }
            }
        }
    }

    public bool ResizeGrid(int newSizeX, int newSizeY)
    {
        const int MAX_GRID_SIZE = 200;
        const int MIN_GRID_SIZE = 2;

        if (newSizeX > MAX_GRID_SIZE || newSizeY > MAX_GRID_SIZE)
        {
            Debug.LogWarning($"Attempted to resize grid beyond maximum size of {MAX_GRID_SIZE}x{MAX_GRID_SIZE}. Operation cancelled.");
            return false;
        }

        if (newSizeX < MIN_GRID_SIZE || newSizeY < MIN_GRID_SIZE)
        {
            Debug.LogWarning($"Attempted to resize grid below minimum size of {MIN_GRID_SIZE}x{MIN_GRID_SIZE}. Operation cancelled.");
            return false;
        }

        if (gridNodeViews != null)
        {
            for (int i = 0; i < numX; i++)
            {
                for (int j = 0; j < numY; j++)
                {
                    if (gridNodeViews[i, j] != null)
                    {
                        Destroy(gridNodeViews[i, j].gameObject);
                    }
                }
            }
        }

        numX = newSizeX;
        numY = newSizeY;

        gridNodeViews = new GridNodeView[NumX, NumY];
        for (int i = 0; i < NumX; i++)
        {
            for (int j = 0; j < NumY; j++)
            {
                GameObject obj = Instantiate(
                    gridNodeViewPrefab,
                    new Vector3(
                        i * GridNodeWidth,
                        j * GridNodeHeight,
                        0.0f),
                    Quaternion.identity);

                obj.name = "GridNode_" + i.ToString() + "_" + j.ToString();
                GridNodeView gnv = obj.GetComponent<GridNodeView>();
                gridNodeViews[i, j] = gnv;
                gnv.Node = new GridNode(new Vector2Int(i, j), this);
                obj.transform.SetParent(transform);
            }
        }

        SetCameraPosition();

        if (npc != null)
        {
            npc.SetStartNode(gridNodeViews[0, 0].Node);
        }

        return true;
    }

    public void SetDestination(int x, int y)
    {
        if (x >= 0 && x < numX && y >= 0 && y < numY)
        {
            destination.position = new Vector3(
                x * GridNodeWidth,
                y * GridNodeHeight,
                destination.position.z);
        }
    }

    public void GenerateRandomMaze(float density = 35f)
    {
        if (density <= 0f)
        {
            for (int x = 0; x < numX; x++)
            {
                for (int y = 0; y < numY; y++)
                {
                    GridNode node = GetGridNode(x, y);
                    if (node != null)
                    {
                        node.IsWalkable = true;
                        GridNodeView gnv = GetGridNodeView(x, y);
                        if (gnv != null)
                        {
                            gnv.SetInnerColor(COLOR_WALKABLE);
                        }
                    }
                }
            }
            return;
        }
        else if (density >= 100f)
        {
            for (int x = 0; x < numX; x++)
            {
                for (int y = 0; y < numY; y++)
                {
                    GridNode node = GetGridNode(x, y);
                    if (node != null)
                    {
                        node.IsWalkable = false;
                        GridNodeView gnv = GetGridNodeView(x, y);
                        if (gnv != null)
                        {
                            gnv.SetInnerColor(COLOR_NONWALKABLE);
                        }
                    }
                }
            }

            return;
        }

        GenerateRecursiveBacktrackingMaze(density);
    }

    public void GenerateRecursiveBacktrackingMaze(float density = 30f)
    {
        for (int x = 0; x < numX; x++)
        {
            for (int y = 0; y < numY; y++)
            {
                GridNode node = GetGridNode(x, y);
                if (node != null)
                {
                    node.IsWalkable = false;
                    GridNodeView gnv = GetGridNodeView(x, y);
                    if (gnv != null)
                    {
                        gnv.SetInnerColor(COLOR_NONWALKABLE);
                    }
                }
            }
        }

        int totalTiles = numX * numY;
        int targetWallCount = Mathf.RoundToInt((density / 100f) * totalTiles);

        System.Random random = new System.Random();

        // Arah: Atas (0), Kanan (1), Bawah (2), Kiri (3)
        int[] dx = { 0, 1, 0, -1 };
        int[] dy = { 1, 0, -1, 0 };

        int startX = random.Next(0, numX);
        int startY = random.Next(0, numY);

        MakeCellWalkable(startX, startY);

        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(startX, startY));

        // Jalankan recursive backtracking
        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek();

            List<int> directions = new List<int>();

            for (int i = 0; i < 4; i++)
            {
                int nx = current.x + dx[i] * 2;
                int ny = current.y + dy[i] * 2;

                if (nx >= 0 && nx < numX && ny >= 0 && ny < numY)
                {
                    GridNode neighborNode = GetGridNode(nx, ny);
                    if (neighborNode != null && !neighborNode.IsWalkable)
                    {
                        directions.Add(i);
                    }
                }
            }

            if (directions.Count > 0)
            {
                int direction = directions[random.Next(0, directions.Count)];

                int nx = current.x + dx[direction] * 2;
                int ny = current.y + dy[direction] * 2;
                int wallX = current.x + dx[direction];
                int wallY = current.y + dy[direction];

                MakeCellWalkable(nx, ny);
                MakeCellWalkable(wallX, wallY);

                stack.Push(new Vector2Int(nx, ny));
            }
            else
            {
                stack.Pop();
            }
        }

        int currentWallCount = CountNonWalkableCells();

        if (currentWallCount > targetWallCount)
        {
            RemoveRandomWalls(currentWallCount - targetWallCount);
        }
        else if (currentWallCount < targetWallCount)
        {
            AddRandomWalls(targetWallCount - currentWallCount);
        }

        EnsureNodeAndNeighborsWalkable((int)(npc.transform.position.x / gridNodeWidth),
                                      (int)(npc.transform.position.y / gridNodeHeight));

        EnsureNodeAndNeighborsWalkable((int)(destination.position.x / gridNodeWidth),
                                      (int)(destination.position.y / gridNodeHeight));
    }

    private int CountNonWalkableCells()
    {
        int count = 0;
        for (int x = 0; x < numX; x++)
        {
            for (int y = 0; y < numY; y++)
            {
                GridNode node = GetGridNode(x, y);
                if (node != null && !node.IsWalkable)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private void MakeCellWalkable(int x, int y)
    {
        GridNode node = GetGridNode(x, y);
        if (node != null)
        {
            node.IsWalkable = true;
            GridNodeView gnv = GetGridNodeView(x, y);
            if (gnv != null)
            {
                gnv.SetInnerColor(COLOR_WALKABLE);
            }
        }
    }

    private void MakeCellNonWalkable(int x, int y)
    {
        GridNode node = GetGridNode(x, y);
        if (node != null)
        {
            node.IsWalkable = false;
            GridNodeView gnv = GetGridNodeView(x, y);
            if (gnv != null)
            {
                gnv.SetInnerColor(COLOR_NONWALKABLE);
            }
        }
    }

    private void RemoveRandomWalls(int count)
    {
        System.Random random = new System.Random();

        List<Vector2Int> walls = new List<Vector2Int>();
        for (int x = 0; x < numX; x++)
        {
            for (int y = 0; y < numY; y++)
            {
                GridNode node = GetGridNode(x, y);
                if (node != null && !node.IsWalkable)
                {
                    walls.Add(new Vector2Int(x, y));
                }
            }
        }

        for (int i = 0; i < walls.Count; i++)
        {
            int j = random.Next(i, walls.Count);
            Vector2Int temp = walls[i];
            walls[i] = walls[j];
            walls[j] = temp;
        }

        for (int i = 0; i < count && i < walls.Count; i++)
        {
            Vector2Int wall = walls[i];
            MakeCellWalkable(wall.x, wall.y);
        }
    }

    private void AddRandomWalls(int count)
    {
        System.Random random = new System.Random();

        List<Vector2Int> paths = new List<Vector2Int>();
        for (int x = 0; x < numX; x++)
        {
            for (int y = 0; y < numY; y++)
            {
                GridNode node = GetGridNode(x, y);
                if (node != null && node.IsWalkable)
                {
                    if ((x != (int)(npc.transform.position.x / gridNodeWidth) ||
                         y != (int)(npc.transform.position.y / gridNodeHeight)) &&
                        (x != (int)(destination.position.x / gridNodeWidth) ||
                         y != (int)(destination.position.y / gridNodeHeight)))
                    {
                        paths.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        for (int i = 0; i < paths.Count; i++)
        {
            int j = random.Next(i, paths.Count);
            Vector2Int temp = paths[i];
            paths[i] = paths[j];
            paths[j] = temp;
        }

        int added = 0;
        for (int i = 0; i < paths.Count && added < count; i++)
        {
            Vector2Int path = paths[i];

            if (!IsPathCritical(path.x, path.y))
            {
                MakeCellNonWalkable(path.x, path.y);
                added++;
            }
        }
    }

    private bool IsPathCritical(int x, int y)
    {
        int walkableNeighbors = 0;

        // Arah: Atas, Kanan, Bawah, Kiri
        int[] dx = { 0, 1, 0, -1 };
        int[] dy = { 1, 0, -1, 0 };

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];

            if (nx >= 0 && nx < numX && ny >= 0 && ny < numY)
            {
                GridNode neighbor = GetGridNode(nx, ny);
                if (neighbor != null && neighbor.IsWalkable)
                {
                    walkableNeighbors++;
                }
            }
        }
        return walkableNeighbors <= 2;
    }

    private void EnsureNodeAndNeighborsWalkable(int x, int y)
    {
        MakeCellWalkable(x, y);

        int[] dx = { 0, 1, 0, -1 };
        int[] dy = { 1, 0, -1, 0 };

        bool hasWalkableNeighbor = false;

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];

            if (nx >= 0 && nx < numX && ny >= 0 && ny < numY)
            {
                GridNode neighbor = GetGridNode(nx, ny);
                if (neighbor != null && neighbor.IsWalkable)
                {
                    hasWalkableNeighbor = true;
                    break;
                }
            }
        }

        if (!hasWalkableNeighbor)
        {
            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];

                if (nx >= 0 && nx < numX && ny >= 0 && ny < numY)
                {
                    MakeCellWalkable(nx, ny);
                    break; // Cukup buat satu tetangga
                }
            }
        }
    }

    private void FindWalkableNodeAndSetDestination()
    {
        int npcX = (int)(npc.transform.position.x / GridNodeWidth);
        int npcY = (int)(npc.transform.position.y / GridNodeHeight);

        int destX = numX - 1;
        int destY = numY - 1;

        GridNode destinationNode = GetGridNode(destX, destY);
        if (destinationNode != null && destinationNode.IsWalkable)
        {
            SetDestination(destX, destY);
            return;
        }

        for (int i = numX - 1; i >= 0; i--)
        {
            for (int j = numY - 1; j >= 0; j--)
            {
                if (i == npcX && j == npcY)
                    continue;

                GridNode node = GetGridNode(i, j);
                if (node != null && node.IsWalkable)
                {
                    SetDestination(i, j);
                    return;
                }
            }
        }
        SetDestination(npcX, npcY);
    }
}