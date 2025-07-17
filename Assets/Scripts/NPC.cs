using PathFinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Metrik yang digunakan untuk mengukur performa pathfinding.
/// Dibuat dalam bentuk struct untuk efisiensi memori dan kemudahan penggunaan.
/// </summary>
public struct PathfindingMetrics
{
    public float timeTaken;      // miliseconds
    public int pathLength;
    public int nodesExplored;
    public long memoryUsed;     // bytes

    public int maxOpenListSize;
    public int maxClosedListSize;

    public float totalGCost;    // Total biaya G untuk jalur (jarak sebenarnya)
    public float totalHCost;    // Total biaya H untuk jalur (heuristik)
    public float totalFCost;    // Total biaya F untuk jalur (G + H)
}

/// <summary>
/// NPC adalah komponen utama yang mengelola pergerakan NPC dalam sistem pathfinding.
/// Kelas ini bertanggung jawab untuk membuat, menampilkan, dan mengelola jalur untuk NPC.
/// </summary>
public class NPC : MonoBehaviour
{
    public float speed = 2.0f;
    public Queue<Vector2> wayPoints = new Queue<Vector2>();

    public event Action<PathfindingMetrics> OnPathfindingComplete;
    public long LastMeasuredMemoryUsage { get; private set; } = 0;

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

    public PathFinder<Vector2Int> pathFinder = null;

    public GridMap Map { get; set; }

    private List<PathfindingVisualizationStep> visualizationSteps = new List<PathfindingVisualizationStep>();
    private bool isVisualizingPath = false;
    private bool isMoving = false;

    public bool IsVisualizingPath => isVisualizingPath;
    public bool IsMoving => isMoving;

    public event Action OnVisualizationComplete;
    public event Action OnMovementComplete;

    public float visualizationSpeed = 0.0f;
    public int visualizationBatch = 1;

    public bool showVisualization = true;

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
                if (!isMoving && wayPoints.Count > 0)
                {
                    isMoving = true;
                    UnityEngine.Debug.Log("NPC movement started");
                }

                yield return StartCoroutine(
                  Coroutine_MoveToPoint(
                    wayPoints.Dequeue(),
                    speed));
            }

            if (isMoving && wayPoints.Count == 0)
            {
                isMoving = false;
                UnityEngine.Debug.Log("NPC movement complete, invoking OnMovementComplete event");
                OnMovementComplete?.Invoke();
            }

            yield return null;
        }
    }

    private void AddWayPoint(GridNode node)
    {
        wayPoints.Enqueue(new Vector2(
          node.Value.x * Map.GridNodeWidth,
          node.Value.y * Map.GridNodeHeight));

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
        InitializePathFinder();
        StartCoroutine(Coroutine_MoveTo());
    }

    private void InitializePathFinder()
    {
        int estimatedNodeCount = 0;
        if (Map != null)
        {
            estimatedNodeCount = Map.NumX * Map.NumY;
        }

        bool isLargeGrid = estimatedNodeCount > 2500;

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

        pathFinder.onSuccess = OnSuccessPathFinding;
        pathFinder.onFailure = OnFailurePathFinding;

        pathFinder.HeuristicCost = GridMap.GetManhattanCost;
        pathFinder.NodeTraversalCost = GridMap.GetEuclideanCost;
    }

    public void MoveTo(GridNode destination, bool silentMode = false)
    {
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

        if (!silentMode)
        {
            Map.ResetGridNodeColours();
        }

        visualizationSteps.Clear();
        isVisualizingPath = false;

        if (!pathFinder.Initialise(start, destination))
        {
            return;
        }

        StartCoroutine(Coroutine_FindPathStep(silentMode));
    }

    IEnumerator Coroutine_FindPathStep(bool silentMode = false)
    {
        yield return StartCoroutine(MeasurePerformance(silentMode));

        if (showVisualization && !silentMode &&
            (pathFinder.Status == PathFinderStatus.SUCCESS || pathFinder.Status == PathFinderStatus.FAILURE))
        {
            yield return StartCoroutine(VisualizePathfinding());
        }
    }

    IEnumerator MeasurePerformance(bool silentMode = false)
    {
        int maxOpenListSize = 0;
        int currentOpenListSize = 0;
        int maxClosedListSize = 0;
        int currentClosedListSize = 0;

        visualizationSteps = new List<PathfindingVisualizationStep>(4);

        long memoryBefore = System.GC.GetTotalMemory(false);

        SetupCallbacks(silentMode, ref maxOpenListSize, ref currentOpenListSize,
                       ref maxClosedListSize, ref currentClosedListSize);

        Stopwatch algorithmTimer = Stopwatch.StartNew();

        int stepCount = 0;

        while (pathFinder.Status == PathFinderStatus.RUNNING)
        {
            stepCount++;
            pathFinder.Step();
        }

        algorithmTimer.Stop();

        long memoryAfter = System.GC.GetTotalMemory(false);
        long memoryUsed = memoryAfter - memoryBefore;

        LastMeasuredMemoryUsage = memoryUsed > 0 ? memoryUsed : 1024;

        float milliseconds = (algorithmTimer.ElapsedTicks * 1000.0f) / Stopwatch.Frequency;

        int pathLength = 0;
        int nodesExplored = 0;
        float totalGCost = 0;
        float totalHCost = 0;
        float totalFCost = 0;

        if (pathFinder.Status == PathFinderStatus.SUCCESS)
        {
            pathLength = CalculatePathLength();
            nodesExplored = pathFinder.ClosedListCount;

            CalculatePathCosts(out totalGCost, out totalHCost, out totalFCost);
        }

        PathfindingMetrics metrics = new PathfindingMetrics
        {
            timeTaken = milliseconds,
            pathLength = pathLength,
            nodesExplored = nodesExplored,
            memoryUsed = memoryUsed,
            maxOpenListSize = maxOpenListSize,
            maxClosedListSize = maxClosedListSize,
            totalGCost = totalGCost,
            totalHCost = totalHCost,
            totalFCost = totalFCost,
        };

        OnPathfindingComplete?.Invoke(metrics);

        HandlePathFindingResult(silentMode, pathLength);

        yield return null;
    }

    private void SetupCallbacks(
        bool silentMode,
        ref int maxOpenListSize,
        ref int currentOpenListSize,
        ref int maxClosedListSize,
        ref int currentClosedListSize)
    {
        int localCurrentOpenListSize = currentOpenListSize;
        int localMaxOpenListSize = maxOpenListSize;
        int localCurrentClosedListSize = currentClosedListSize;
        int localMaxClosedListSize = maxClosedListSize;

        if (silentMode)
        {
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
                localCurrentOpenListSize--;
            };
        }
        else
        {
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

                localCurrentOpenListSize--;
            };

            pathFinder.onChangeCurrentNode = (node) =>
            {
                visualizationSteps.Add(new PathfindingVisualizationStep(
                    PathfindingVisualizationStep.StepType.CurrentNode,
                    node.Location.Value));
            };



        }
        maxOpenListSize = localMaxOpenListSize;
        currentOpenListSize = localCurrentOpenListSize;
        maxClosedListSize = localMaxClosedListSize;
        currentClosedListSize = localCurrentClosedListSize;
    }

    private void HandlePathFindingResult(bool silentMode, int pathLength)
    {
        if (pathFinder.Status == PathFinderStatus.SUCCESS)
        {
            OnSuccessPathFinding();

            if (!silentMode && showVisualization)
            {
                PathFinder<Vector2Int>.PathFinderNode node = pathFinder.CurrentNode;
                List<Vector2Int> pathPositions = new List<Vector2Int>(pathLength);

                while (node != null)
                {
                    pathPositions.Add(node.Location.Value);
                    node = node.Parent;
                }

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

            UnityEngine.Debug.Log($"Pathfinding failed - visualization will show {visualizationSteps.Count} exploration steps");
        }
    }

    void OnSuccessPathFinding()
    {
        float totalGCost = 0;
        float totalHCost = 0;
        float totalFCost = 0;

        CalculatePathCosts(out totalGCost, out totalHCost, out totalFCost);

        int pathLength = CalculatePathLength();

    }

    void OnFailurePathFinding()
    {
        UnityEngine.Debug.Log("Pathfinding failed");
    }

    public void ChangeAlgorithm(PathFinderType newType)
    {
        if (pathFinder != null && pathFinder.Status == PathFinderStatus.RUNNING)
        {
            UnityEngine.Debug.Log("Cannot change algorithm while pathfinding is running");
            return;
        }

        pathFinderType = newType;

        int estimatedNodeCount = 0;
        if (Map != null)
        {
            estimatedNodeCount = Map.NumX * Map.NumY;
        }

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

        pathFinder.onSuccess = OnSuccessPathFinding;
        pathFinder.onFailure = OnFailurePathFinding;

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

        UnityEngine.Debug.Log("Path visualization starting");
        isVisualizingPath = true;

        Map.ResetGridNodeColours();

        int stepCount = visualizationSteps.Count;
        int batchSize = Mathf.Min(visualizationBatch, stepCount);

        bool pathfindingFailed = pathFinder.Status == PathFinderStatus.FAILURE;

        if (pathfindingFailed)
        {
            UnityEngine.Debug.Log($"Visualizing failed pathfinding attempt with {stepCount} steps");
        }

        for (int i = 0; i < stepCount; i += batchSize)
        {
            int end = Mathf.Min(i + batchSize, stepCount);

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

                            if (!pathfindingFailed)
                            {
                                GridNode pathNode = Map.GetGridNode(step.position.x, step.position.y);
                                AddWayPoint(pathNode);
                            }
                            break;
                    }
                }
            }
            yield return new WaitForSeconds(visualizationSpeed);
        }

        isVisualizingPath = false;
        UnityEngine.Debug.Log("Path visualization complete, invoking OnVisualizationComplete event");
        OnVisualizationComplete?.Invoke();
    }

    private void CalculatePathCosts(out float totalGCost, out float totalHCost, out float totalFCost)
    {
        totalGCost = 0;
        totalHCost = 0;
        totalFCost = 0;

        if (pathFinder.CurrentNode == null)
            return;

        bool usesHeuristic = pathFinderType == PathFinderType.ASTAR ||
                            pathFinderType == PathFinderType.GREEDY;

        PathFinder<Vector2Int>.PathFinderNode finalNode = pathFinder.CurrentNode;

        totalGCost = finalNode.GCost;

        if (usesHeuristic)
        {
            totalHCost = finalNode.HCost;
            totalFCost = finalNode.FCost;

        }
        else
        {
            totalFCost = totalGCost;
        }
    }
}