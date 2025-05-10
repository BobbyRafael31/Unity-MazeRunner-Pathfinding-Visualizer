using PathFinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public struct PathfindingMetrics
{
    // Untuk Pengukuran Kinerja
    public float timeTaken;      // in milliseconds
    public int pathLength;
    public int nodesExplored; // number of nodes in path
    public long memoryUsed;     // memory used by pathfinding in bytes

    // Untuk Visualisasi
    public int maxOpenListSize; // maximum size of open list during pathfinding
    public int maxClosedListSize; // maximum size of closed list during pathfinding

    // Tambahan untuk cost metrics
    public float totalGCost;    // Total biaya G untuk jalur (jarak sebenarnya)
    public float totalHCost;    // Total biaya H untuk jalur (heuristik)
    public float totalFCost;    // Total biaya F untuk jalur (G + H)

}

public class NPC : MonoBehaviour
{
    public float speed = 2.0f;
    public Queue<Vector2> wayPoints = new Queue<Vector2>();

    // Event that fires when pathfinding is complete with performance metrics
    public event Action<PathfindingMetrics> OnPathfindingComplete;

    public enum PathFinderType
    {
        ASTAR,
        DIJKSTRA,
        GREEDY,
        BACKTRACKING,
        BFS,
    }

    [SerializeField]
    public PathFinderType pathFinderType = PathFinderType.ASTAR;

    PathFinder<Vector2Int> pathFinder = null;

    public GridMap Map { get; set; }

    // List to store all steps for visualization playback
    private List<PathfindingVisualizationStep> visualizationSteps = new List<PathfindingVisualizationStep>();
    private bool isVisualizingPath = false;

    // Properties to control visualization
    [SerializeField]

    // Visualization speed is time between visualization steps
    public float visualizationSpeed = 0.0f; // Default 0; set higher for slower visualization

    // Visualization batch is the number of steps to visualize at once
    public int visualizationBatch = 1; // Default 1; set higher value for faster visualization

    [SerializeField]
    public bool showVisualization = true; // Whether to show visualization at all

    // Struct to store each step of the pathfinding process for visualization
    private struct PathfindingVisualizationStep
    {
        public enum StepType { CurrentNode, OpenList, ClosedList, FinalPath }
        public StepType type;
        public Vector2Int position;

        public PathfindingVisualizationStep(StepType type, Vector2Int position)
        {
            this.type = type;
            this.position = position;
        }
    }

    private IEnumerator Coroutine_MoveOverSeconds(GameObject objectToMove, Vector3 end, float seconds)
    {
        float elaspedTime = 0.0f;
        Vector3 startingPos = objectToMove.transform.position;

        while (elaspedTime < seconds)
        {
            objectToMove.transform.position =
              Vector3.Lerp(startingPos, end, elaspedTime / seconds);
            elaspedTime += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }
        objectToMove.transform.position = end;
    }

    IEnumerator Coroutine_MoveToPoint(Vector2 p, float speed)
    {
        Vector3 endP = new Vector3(p.x, p.y, transform.position.z);
        float duration = (transform.position - endP).magnitude / speed;

        yield return StartCoroutine(
          Coroutine_MoveOverSeconds(
            transform.gameObject, endP, duration));
    }

    public IEnumerator Coroutine_MoveTo()
    {
        while (true)
        {
            while (wayPoints.Count > 0)
            {
                yield return StartCoroutine(
                  Coroutine_MoveToPoint(
                    wayPoints.Dequeue(),
                    speed));
            }
            yield return null;
        }
    }

    private void AddWayPoint(GridNode node)
    {
        wayPoints.Enqueue(new Vector2(
          node.Value.x * Map.GridNodeWidth,
          node.Value.y * Map.GridNodeHeight));

        // We set a color to show the path.
        GridNodeView gnv = Map.GetGridNodeView(node.Value.x, node.Value.y);
        gnv.SetInnerColor(Map.COLOR_PATH);
    }

    public void SetStartNode(GridNode node)
    {
        wayPoints.Clear();
        transform.position = new Vector3(
          node.Value.x * Map.GridNodeWidth,
          node.Value.y * Map.GridNodeHeight,
          transform.position.z);
    }

    private void Start()
    {
        // Initialize pathfinder based on type
        InitializePathFinder();

        // Start the movement coroutine
        StartCoroutine(Coroutine_MoveTo());
    }

    private void InitializePathFinder()
    {
        // Hitung perkiraan jumlah node dalam grid
        int estimatedNodeCount = 0;
        if (Map != null)
        {
            estimatedNodeCount = Map.NumX * Map.NumY;
        }

        // Log informasi ukuran grid dan strategi optimisasi
        bool isLargeGrid = estimatedNodeCount > 2500;

        // Create new pathfinder instance
        switch (pathFinderType)
        {
            case PathFinderType.ASTAR:
                pathFinder = new AStarPathFinder<Vector2Int>(estimatedNodeCount);
                break;
            case PathFinderType.DIJKSTRA:
                pathFinder = new DijkstraPathFinder<Vector2Int>(estimatedNodeCount);
                break;
            case PathFinderType.GREEDY:
                pathFinder = new GreedyPathFinder<Vector2Int>();
                break;
            case PathFinderType.BACKTRACKING:
                pathFinder = new BacktrackingPathFinder<Vector2Int>();
                break;
            case PathFinderType.BFS:
                pathFinder = new BFSPathFinder<Vector2Int>();
                break;
        }

        // Set up callbacks
        pathFinder.onSuccess = OnSuccessPathFinding;
        pathFinder.onFailure = OnFailurePathFinding;

        // Gunakan setting asli
        pathFinder.HeuristicCost = GridMap.GetManhattanCost;
        pathFinder.NodeTraversalCost = GridMap.GetEuclideanCost;
    }

    public void MoveTo(GridNode destination, bool silentMode = false)
    {
        // inialisaasi pathfinder jika belum ada
        if (pathFinder == null)
        {
            InitializePathFinder();
        }


        if (pathFinder.Status == PathFinderStatus.RUNNING)
        {
            return;
        }

        GridNode start = Map.GetGridNode(
          (int)(transform.position.x / Map.GridNodeWidth),
          (int)(transform.position.y / Map.GridNodeHeight));

        if (start == null || destination == null)
        {
            return;
        }

        SetStartNode(start);

        // Reset grid colors
        if (!silentMode)
        {
            Map.ResetGridNodeColours();
        }

        visualizationSteps.Clear();
        isVisualizingPath = false;

        // jika gagal menginisialisasi pathfinder, tidak perlu melanjutkan
        if (!pathFinder.Initialise(start, destination))
        {
            return;
        }

        StartCoroutine(Coroutine_FindPathStep(silentMode));
    }

    IEnumerator Coroutine_FindPathStep(bool silentMode = false)
    {
        yield return StartCoroutine(MeasurePerformance(silentMode));

        // Start visualization after calculation is complete
        if (pathFinder.Status == PathFinderStatus.SUCCESS && showVisualization && !silentMode)
        {
            yield return StartCoroutine(VisualizePathfinding());
        }
    }

    IEnumerator MeasurePerformance(bool silentMode = false)
    {
        // Memory tracking for pathfinding structures - tetap untuk visualisasi
        int maxOpenListSize = 0;
        int currentOpenListSize = 0;
        int maxClosedListSize = 0;
        int currentClosedListSize = 0;

        // Pre-allocate visualizationSteps with estimated capacity to avoid reallocations
        visualizationSteps = new List<PathfindingVisualizationStep>(4);

        GC.Collect();
        GC.WaitForPendingFinalizers(); // Tunggu semua finalizers selesai

        // ===== MEMORY MEASUREMENT START: Ukur memory sebelum algoritma =====
        long memoryBefore = System.GC.GetTotalMemory(false);

        // Setup callbacks before running algorithm
        SetupCallbacks(silentMode, ref maxOpenListSize, ref currentOpenListSize,
                       ref maxClosedListSize, ref currentClosedListSize);

        // ===== STOPWATCH START: Pengukuran waktu algoritma =====
        Stopwatch algorithmTimer = Stopwatch.StartNew();

        // Counter untuk jumlah step yang dilakukan algoritma
        int stepCount = 0;

        // Execute the pathfinding algorithm synchronously in a single frame without visualization
        while (pathFinder.Status == PathFinderStatus.RUNNING)
        {
            stepCount++;
            pathFinder.Step();
        }

        // ===== STOPWATCH STOP: Akhir pengukuran waktu algoritma =====
        algorithmTimer.Stop();

        // ===== MEMORY MEASUREMENT END: Ukur memory setelah algoritma =====
        long memoryAfter = System.GC.GetTotalMemory(false);
        long memoryUsed = memoryAfter - memoryBefore;

        // float miliseconds = algorithmTimer.ElapsedMilliseconds;

        //UnityEngine.Debug.Log("$algorithmTimer.ElapsedTicks: " + algorithmTimer.ElapsedTicks);
        //UnityEngine.Debug.Log("$Stopwatch.Frequency: " + Stopwatch.Frequency);
        //float seconds = (float)algorithmTimer.ElapsedTicks / Stopwatch.Frequency;
        //UnityEngine.Debug.Log("$seconds: " + seconds);

        float milliseconds = (algorithmTimer.ElapsedTicks * 1000.0f) / Stopwatch.Frequency;

        // Calculate path length once and reuse
        int pathLength = 0;
        int nodesExplored = 0;
        float totalGCost = 0;
        float totalHCost = 0;
        float totalFCost = 0;

        // Add memory for path reconstruction (final path)
        if (pathFinder.Status == PathFinderStatus.SUCCESS)
        {
            pathLength = CalculatePathLength();
            nodesExplored = pathFinder.ClosedListCount;

            // Hitung total G, H, dan F cost
            CalculatePathCosts(out totalGCost, out totalHCost, out totalFCost);
        }

        // Create and send metrics - waktu pengukuran algoritma yang tepat
        PathfindingMetrics metrics = new PathfindingMetrics
        {
            timeTaken = milliseconds,  // Waktu algoritma yang diukur dengan stopwatch
            pathLength = pathLength,
            nodesExplored = nodesExplored,
            memoryUsed = memoryUsed,
            maxOpenListSize = maxOpenListSize,
            maxClosedListSize = maxClosedListSize,
            totalGCost = totalGCost,
            totalHCost = totalHCost,
            totalFCost = totalFCost,
        };

        // Report metrics before visualization
        if (!silentMode)
        {
            OnPathfindingComplete?.Invoke(metrics);
        }

        // Path visualization and handling
        HandlePathFindingResult(silentMode, pathLength);

        // Pastikan untuk mengembalikan nilai di akhir coroutine
        yield return null;
    }

    
    /// <summary>
    /// Setup callbacks for tracking nodes in open/closed lists and visualization
    /// </summary>
    private void SetupCallbacks(bool silentMode, ref int maxOpenListSize, ref int currentOpenListSize,
                               ref int maxClosedListSize, ref int currentClosedListSize)
    {
        // Buat variabel lokal untuk menghindari masalah dengan ref parameter dalam lambda
        int localCurrentOpenListSize = currentOpenListSize;
        int localMaxOpenListSize = maxOpenListSize;
        int localCurrentClosedListSize = currentClosedListSize;
        int localMaxClosedListSize = maxClosedListSize;

        if (silentMode)
        {
            // In silent mode, just set minimal callbacks for metrics
            pathFinder.onAddToOpenList = (node) =>
            {
                localCurrentOpenListSize++;
                if (localCurrentOpenListSize > localMaxOpenListSize)
                    localMaxOpenListSize = localCurrentOpenListSize;
            };

            pathFinder.onAddToClosedList = (node) =>
            {
                localCurrentClosedListSize++;
                if (localCurrentClosedListSize > localMaxClosedListSize)
                    localMaxClosedListSize = localCurrentClosedListSize;
                localCurrentOpenListSize--; // When a node is moved from open to closed list
            };
        }
        else
        {
            // In regular mode, track and prepare for visualization
            pathFinder.onAddToOpenList = (node) =>
            {
                visualizationSteps.Add(new PathfindingVisualizationStep(
                    PathfindingVisualizationStep.StepType.OpenList,
                    node.Location.Value));

                localCurrentOpenListSize++;
                if (localCurrentOpenListSize > localMaxOpenListSize)
                    localMaxOpenListSize = localCurrentOpenListSize;
            };

            pathFinder.onAddToClosedList = (node) =>
            {
                visualizationSteps.Add(new PathfindingVisualizationStep(
                    PathfindingVisualizationStep.StepType.ClosedList,
                    node.Location.Value));

                localCurrentClosedListSize++;
                if (localCurrentClosedListSize > localMaxClosedListSize)
                    localMaxClosedListSize = localCurrentClosedListSize;

                localCurrentOpenListSize--; // When a node is moved from open to closed list
            };

            pathFinder.onChangeCurrentNode = (node) =>
            {
                visualizationSteps.Add(new PathfindingVisualizationStep(
                    PathfindingVisualizationStep.StepType.CurrentNode,
                    node.Location.Value));
            };



        }

        // Setelah lambda selesai dijalankan, perbarui variabel ref
        maxOpenListSize = localMaxOpenListSize;
        currentOpenListSize = localCurrentOpenListSize;
        maxClosedListSize = localMaxClosedListSize;
        currentClosedListSize = localCurrentClosedListSize;
    }

    /// <summary>
    /// Handle path finding result (success or failure)
    /// </summary>
    private void HandlePathFindingResult(bool silentMode, int pathLength)
    {

        if (pathFinder.Status == PathFinderStatus.SUCCESS)
        {
            OnSuccessPathFinding();

            // In non-silent mode, prepare visualization data for the path
            if (!silentMode && showVisualization)
            {
                // Add the path nodes for visualization in efficient batched way
                PathFinder<Vector2Int>.PathFinderNode node = pathFinder.CurrentNode;
                List<Vector2Int> pathPositions = new List<Vector2Int>(pathLength); // Pre-allocate with known size

                // Build path in reverse order
                while (node != null)
                {
                    pathPositions.Add(node.Location.Value);
                    node = node.Parent;
                }

                // Process path in correct order
                for (int i = pathPositions.Count - 1; i >= 0; i--)
                {
                    visualizationSteps.Add(new PathfindingVisualizationStep(
                        PathfindingVisualizationStep.StepType.FinalPath,
                        pathPositions[i]));
                }
            }
        }
        else if (pathFinder.Status == PathFinderStatus.FAILURE)
        {
            OnFailurePathFinding();
        }
    }

    /// <summary>
    /// Memformat ukuran byte menjadi string yang lebih mudah dibaca
    /// </summary>
    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    void OnSuccessPathFinding()
    {
        float totalGCost = 0;
        float totalHCost = 0;
        float totalFCost = 0;

        // Hitung biaya-biaya path menggunakan metode yang sudah ada
        CalculatePathCosts(out totalGCost, out totalHCost, out totalFCost);

        // Informasi dasar
        int pathLength = CalculatePathLength();

    }

    void OnFailurePathFinding()
    {
        UnityEngine.Debug.Log("Pathfinding failed");
    }

    /// <summary>
    /// Changes the pathfinding algorithm at runtime
    /// </summary>
    public void ChangeAlgorithm(PathFinderType newType)
    {
        // Don't change if pathfinding is in progress
        if (pathFinder != null && pathFinder.Status == PathFinderStatus.RUNNING)
        {
            UnityEngine.Debug.Log("Cannot change algorithm while pathfinding is running");
            return;
        }

        pathFinderType = newType;

        // Hitung perkiraan jumlah node dalam grid
        int estimatedNodeCount = 0;
        if (Map != null)
        {
            estimatedNodeCount = Map.NumX * Map.NumY;
        }

        // Create new pathfinder instance
        switch (pathFinderType)
        {
            case PathFinderType.ASTAR:
                pathFinder = new AStarPathFinder<Vector2Int>(estimatedNodeCount);
                break;
            case PathFinderType.DIJKSTRA:
                pathFinder = new DijkstraPathFinder<Vector2Int>(estimatedNodeCount);
                break;
            case PathFinderType.GREEDY:
                pathFinder = new GreedyPathFinder<Vector2Int>();
                break;
            case PathFinderType.BACKTRACKING:
                pathFinder = new BacktrackingPathFinder<Vector2Int>();
                break;
            case PathFinderType.BFS:
                pathFinder = new BFSPathFinder<Vector2Int>();
                break;
        }

        // Set up callbacks
        pathFinder.onSuccess = OnSuccessPathFinding;
        pathFinder.onFailure = OnFailurePathFinding;

        // Gunakan setting asli
        pathFinder.HeuristicCost = GridMap.GetManhattanCost;
        pathFinder.NodeTraversalCost = GridMap.GetEuclideanCost;
    }

    private int CalculatePathLength()
    {
        int pathLength = 0;
        PathFinder<Vector2Int>.PathFinderNode node = pathFinder.CurrentNode;
        while (node != null)
        {
            pathLength++;
            node = node.Parent;
        }
        return pathLength;
    }

    IEnumerator VisualizePathfinding()
    {
        if (!showVisualization)
            yield break;

        isVisualizingPath = true;

        // First, ensure grid is reset
        Map.ResetGridNodeColours();

        // Visualize each step with a delay - use batch processing for efficiency
        int stepCount = visualizationSteps.Count;
        int batchSize = Mathf.Min(visualizationBatch, stepCount); // set higher value for faster visualization

        for (int i = 0; i < stepCount; i += batchSize)
        {
            int end = Mathf.Min(i + batchSize, stepCount);

            // Process a batch of steps
            for (int j = i; j < end; j++)
            {
                var step = visualizationSteps[j];
                GridNodeView gnv = Map.GetGridNodeView(step.position.x, step.position.y);
                if (gnv != null)
                {
                    switch (step.type)
                    {
                        case PathfindingVisualizationStep.StepType.CurrentNode:
                            gnv.SetInnerColor(Map.COLOR_CURRENT_NODE);
                            break;
                        case PathfindingVisualizationStep.StepType.OpenList:
                            gnv.SetInnerColor(Map.COLOR_ADD_TO_OPENLIST);
                            break;
                        case PathfindingVisualizationStep.StepType.ClosedList:
                            gnv.SetInnerColor(Map.COLOR_ADD_TO_CLOSEDLIST);
                            break;
                        case PathfindingVisualizationStep.StepType.FinalPath:
                            gnv.SetInnerColor(Map.COLOR_PATH);
                            // Also add the waypoint when we process the path
                            if (step.type == PathfindingVisualizationStep.StepType.FinalPath)
                            {
                                GridNode pathNode = Map.GetGridNode(step.position.x, step.position.y);
                                AddWayPoint(pathNode);
                            }
                            break;
                    }
                }
            }

            // Yield after each batch to prevent frame drops
            yield return new WaitForSeconds(visualizationSpeed);
        }

        isVisualizingPath = false;
    }

    /// <summary>
    /// Menghitung biaya G, H, dan F untuk jalur
    /// </summary>
    private void CalculatePathCosts(out float totalGCost, out float totalHCost, out float totalFCost)
    {
        // Inisialisasi nilai awal
        totalGCost = 0;
        totalHCost = 0;
        totalFCost = 0;

        // Jika tidak ada path yang ditemukan, return nilai 0
        if (pathFinder.CurrentNode == null)
            return;

        // Untuk algoritma yang menggunakan heuristik
        bool usesHeuristic = pathFinderType == PathFinderType.ASTAR ||
                            pathFinderType == PathFinderType.GREEDY;

        // Node final berisi total cost jalur
        PathFinder<Vector2Int>.PathFinderNode finalNode = pathFinder.CurrentNode;

        // G cost adalah biaya sebenarnya dari start ke goal, sudah terakumulasi di node akhir
        totalGCost = finalNode.GCost;

        // H cost di node final idealnya 0 (sudah di tujuan), 
        // tapi untuk info lengkap, kita dapat path's H cost dari node awal
        if (usesHeuristic)
        {
            // H cost dari node awal ke tujuan (untuk referensi)
            totalHCost = finalNode.HCost;

            // F cost adalah G + H di node akhir
            totalFCost = finalNode.FCost;

        }
        else
        {
            // Algoritma tanpa heuristik (seperti Dijkstra)
            totalFCost = totalGCost;
        }

        //// Hitung rata-rata biaya per langkah untuk analisis
        //int pathLength = CalculatePathLength();
        //float avgCostPerStep = pathLength > 0 ? totalGCost / pathLength : 0;
    }
}