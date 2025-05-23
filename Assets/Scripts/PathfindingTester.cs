using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PathfindingTester : MonoBehaviour
{
    [Header("References")]
    public GridMap gridMap;
    public NPC npc;
    public Button startTestButton;
    public TMP_Text statusText;
    public Slider progressBar;

    [Header("Test Configuration")]
    [Tooltip("Number of times to run each test combination")]
    public int testsPerCombination = 3;
    
    [Tooltip("Delay between tests in seconds")]
    public float delayBetweenTests = 0.5f;
    
    [Tooltip("Whether to save results to a CSV file")]
    public bool saveResultsToFile = true;
    
    [Tooltip("File name for test results (CSV)")]
    public string resultsFileName = "pathfinding_tests.csv";

    // Test matrix parameters
    private NPC.PathFinderType[] algorithmsToTest = new NPC.PathFinderType[] {
        NPC.PathFinderType.ASTAR,
        NPC.PathFinderType.DIJKSTRA,
        NPC.PathFinderType.GREEDY,
        NPC.PathFinderType.BACKTRACKING,
        NPC.PathFinderType.BFS
    };

    private Vector2Int[] gridSizesToTest = new Vector2Int[] {
        new Vector2Int(20, 20),
        new Vector2Int(35, 35),
        new Vector2Int(50, 50),
        new Vector2Int(40, 25)   // Another non-square grid
    };

    private float[] mazeDensitiesToTest = new float[] {
        0f,   // Empty (no walls)
        10f,  // Very low
        30f,  // Medium
        50f,  // High
        100f  // Fully blocked (all walls)
    };

    private bool[] diagonalMovementOptions = new bool[] {
        true,
        false
    };

    // Test state tracking
    private bool isTestingRunning = false;
    private int totalTests;
    private int completedTests;
    private List<PathfindingTestResult> testResults = new List<PathfindingTestResult>();
    
    // Current test parameters
    private NPC.PathFinderType currentTestAlgorithm;
    private Vector2Int currentTestGridSize;
    private float currentTestDensity;
    private bool currentTestDiagonal;
    private int currentTestIndex;

    // Structure to store test results
    private struct PathfindingTestResult
    {
        public NPC.PathFinderType algorithm;
        public Vector2Int gridSize;
        public float mazeDensity;
        public bool diagonalMovement;
        public float timeTaken;
        public int pathLength;
        public int nodesExplored;
        public long memoryUsed;
        public bool pathFound;
        public int testIndex;
    }

    private void Start()
    {
        // Subscribe to NPC's pathfinding completion event
        npc.OnPathfindingComplete += OnPathfindingComplete;
        
        // Set up the button listener
        startTestButton.onClick.AddListener(StartTesting);
        
        // Initial status message
        UpdateStatus("Ready to start testing. Click the Start Tests button.");
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (npc != null)
        {
            npc.OnPathfindingComplete -= OnPathfindingComplete;
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log(message);
        
        // Update progress bar if available
        if (progressBar != null && totalTests > 0)
        {
            progressBar.value = (float)completedTests / totalTests;
        }
    }

    public void StartTesting()
    {
        if (isTestingRunning)
        {
            Debug.LogWarning("Tests are already running.");
            return;
        }

        // Clear previous results
        testResults.Clear();
        
        // Calculate total number of tests
        totalTests = algorithmsToTest.Length * 
                    gridSizesToTest.Length * 
                    mazeDensitiesToTest.Length * 
                    diagonalMovementOptions.Length *
                    testsPerCombination;
        
        completedTests = 0;
        isTestingRunning = true;
        
        UpdateStatus($"Starting {totalTests} tests...");
        
        // Disable button during testing
        startTestButton.interactable = false;
        
        // Start the test coroutine
        StartCoroutine(RunTestMatrix());
    }

    private IEnumerator RunTestMatrix()
    {
        // Iterate through all test combinations
        foreach (var algorithm in algorithmsToTest)
        {
            foreach (var gridSize in gridSizesToTest)
            {
                foreach (var density in mazeDensitiesToTest)
                {
                    foreach (var useDiagonals in diagonalMovementOptions)
                    {
                        for (int testIndex = 0; testIndex < testsPerCombination; testIndex++)
                        {
                            yield return StartCoroutine(RunSingleTest(algorithm, gridSize, density, useDiagonals, testIndex));
                            
                            // Wait between tests
                            yield return new WaitForSeconds(delayBetweenTests);
                        }
                    }
                }
            }
        }

        // All tests completed
        isTestingRunning = false;
        startTestButton.interactable = true;
        
        // Save results if enabled
        if (saveResultsToFile)
        {
            SaveResultsToCSV();
        }
        
        UpdateStatus($"Testing complete! {completedTests} tests run. Results saved to {resultsFileName}");
    }

    private IEnumerator RunSingleTest(NPC.PathFinderType algorithm, Vector2Int gridSize, float density, bool useDiagonals, int testIndex)
    {
        // Update status with current test info
        UpdateStatus($"Test {completedTests+1}/{totalTests}: {algorithm} - Grid: {gridSize.x}x{gridSize.y} - Density: {density}% - Diagonals: {useDiagonals}");
        
        // Configure the test environment
        yield return StartCoroutine(SetupTestEnvironment(algorithm, gridSize, density, useDiagonals));
        
        // Generate start and destination points
        GridNode startNode = FindValidStartNode();
        GridNode destNode = FindValidDestinationNode(startNode);
        
        if (startNode == null || destNode == null)
        {
            Debug.LogWarning($"Failed to find valid start/destination nodes for test - density: {density}%");
            
            // Record the test as "impossible" with a failed path
            PathfindingTestResult result = new PathfindingTestResult
            {
                algorithm = algorithm,
                gridSize = gridSize,
                mazeDensity = density,
                diagonalMovement = useDiagonals,
                timeTaken = 0f,
                pathLength = 0,
                nodesExplored = 0,
                memoryUsed = 0,
                pathFound = false,
                testIndex = testIndex
            };
            
            testResults.Add(result);
            completedTests++;
            yield break;
        }
        
        // Position NPC at start node
        npc.SetStartNode(startNode);
        
        // Position destination
        gridMap.SetDestination(destNode.Value.x, destNode.Value.y);
        
        // Wait a frame to ensure everything is set up
        yield return null;
        
        // Store the current test parameters
        currentTestAlgorithm = algorithm;
        currentTestGridSize = gridSize;
        currentTestDensity = density;
        currentTestDiagonal = useDiagonals;
        currentTestIndex = testIndex;
        
        // Flag to track if callback was triggered
        bool callbackTriggered = false;
        
        // Setup a temporary callback listener to detect if the event fires
        System.Action<PathfindingMetrics> tempCallback = (metrics) => { callbackTriggered = true; };
        npc.OnPathfindingComplete += tempCallback;
        
        // Run pathfinding in silent mode - now the event will still fire with our NPC fix
        npc.MoveTo(destNode, true);
        
        // Wait for pathfinding to complete
        float timeout = 10.0f;
        float elapsed = 0f;
        
        while (npc.pathFinder != null && npc.pathFinder.Status == PathFinding.PathFinderStatus.RUNNING && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        
        // Wait a bit more to ensure completion
        yield return new WaitForSeconds(0.1f);
        
        // Remove the temporary callback
        npc.OnPathfindingComplete -= tempCallback;
        
        // If callback wasn't triggered but pathfinding is complete, manually record the result
        if (!callbackTriggered && npc.pathFinder != null && npc.pathFinder.Status != PathFinding.PathFinderStatus.RUNNING)
        {
            Debug.LogWarning($"Callback wasn't triggered for test {completedTests+1}. Recording results manually.");
            
            // Create a metrics object with available data
            PathfindingMetrics metrics = new PathfindingMetrics
            {
                timeTaken = elapsed * 1000f, // convert to ms
                pathLength = CalculatePathLength(npc.pathFinder),
                nodesExplored = npc.pathFinder.ClosedListCount,
                memoryUsed = npc.LastMeasuredMemoryUsage // Get the last memory measurement from NPC
            };
            
            // Create a test result entry directly
            PathfindingTestResult result = new PathfindingTestResult
            {
                algorithm = currentTestAlgorithm,
                gridSize = currentTestGridSize,
                mazeDensity = currentTestDensity,
                diagonalMovement = currentTestDiagonal,
                timeTaken = metrics.timeTaken,
                pathLength = metrics.pathLength,
                nodesExplored = metrics.nodesExplored,
                memoryUsed = metrics.memoryUsed,
                pathFound = npc.pathFinder.Status == PathFinding.PathFinderStatus.SUCCESS,
                testIndex = currentTestIndex
            };
            
            // Add to results directly
            testResults.Add(result);
            
            // Log result
            Debug.Log($"Test {completedTests} (manual): {GetAlgorithmName(result.algorithm)} - {result.timeTaken:F2}ms - Path: {result.pathLength} - Nodes: {result.nodesExplored}");
        }
        
        // Increment test counter
        completedTests++;
    }

    private int CalculatePathLength(PathFinding.PathFinder<Vector2Int> pathFinder)
    {
        if (pathFinder == null || pathFinder.CurrentNode == null)
            return 0;
            
        int length = 0;
        var node = pathFinder.CurrentNode;
        while (node != null)
        {
            length++;
            node = node.Parent;
        }
        return length;
    }

    private IEnumerator SetupTestEnvironment(NPC.PathFinderType algorithm, Vector2Int gridSize, float density, bool useDiagonals)
    {
        Debug.Log($"Setting up test environment: Algorithm={algorithm}, Grid={gridSize.x}x{gridSize.y}, Density={density}, Diagonals={useDiagonals}");
        
        // Resize grid
        gridMap.ResizeGrid(gridSize.x, gridSize.y);
        yield return null;
        
        // Set algorithm
        npc.ChangeAlgorithm(algorithm);
        yield return null;
        
        // Set diagonal movement
        gridMap.AllowDiagonalMovement = useDiagonals;
        yield return null;
        
        // Generate maze with specified density
        gridMap.GenerateRandomMaze(density);
        yield return null;
        
        // Note: We no longer change visualization settings here
        // This is handled in the RunSingleTest method
        
        yield return null;
    }

    private GridNode FindValidStartNode()
    {
        // Find a walkable node for start position, starting from the top left
        for (int x = 0; x < gridMap.NumX; x++)
        {
            for (int y = 0; y < gridMap.NumY; y++)
            {
                GridNode node = gridMap.GetGridNode(x, y);
                if (node != null && node.IsWalkable)
                {
                    return node;
                }
            }
        }
        return null;
    }

    private GridNode FindValidDestinationNode(GridNode startNode)
    {
        if (startNode == null)
            return null;
            
        // Find a walkable node far from the start position, starting from the bottom right
        int maxDistance = 0;
        GridNode bestNode = null;
        
        // First try to find a node with good distance
        for (int x = gridMap.NumX - 1; x >= 0; x--)
        {
            for (int y = gridMap.NumY - 1; y >= 0; y--)
            {
                GridNode node = gridMap.GetGridNode(x, y);
                if (node != null && node.IsWalkable && node != startNode)
                {
                    int distance = Mathf.Abs(x - startNode.Value.x) + Mathf.Abs(y - startNode.Value.y);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        bestNode = node;
                    }
                }
            }
        }
        
        // If we found a node and the distance is decent, use it
        if (bestNode != null && maxDistance > gridMap.NumX / 4)
        {
            return bestNode;
        }
        
        // If no good node was found or distance is too small, try harder to find any walkable node
        // different from start node (this helps with very high density mazes)
        for (int x = 0; x < gridMap.NumX; x++)
        {
            for (int y = 0; y < gridMap.NumY; y++)
            {
                GridNode node = gridMap.GetGridNode(x, y);
                if (node != null && node.IsWalkable && node != startNode)
                {
                    return node; // Return the first walkable node that isn't the start
                }
            }
        }
        
        // If we get here and bestNode is null, there's only one walkable node in the entire grid
        // In this case, we have to return null and the test should be skipped
        return bestNode;
    }

    private void OnPathfindingComplete(PathfindingMetrics metrics)
    {
        if (!isTestingRunning)
        {
            Debug.Log("OnPathfindingComplete called but test is not running - ignoring");
            return;
        }

        Debug.Log($"OnPathfindingComplete called for test {completedTests+1} with algorithm {currentTestAlgorithm}");

        // Create a test result entry
        PathfindingTestResult result = new PathfindingTestResult
        {
            algorithm = currentTestAlgorithm,
            gridSize = currentTestGridSize,
            mazeDensity = currentTestDensity,
            diagonalMovement = currentTestDiagonal,
            timeTaken = metrics.timeTaken,
            pathLength = metrics.pathLength,
            nodesExplored = metrics.nodesExplored,
            memoryUsed = metrics.memoryUsed,
            pathFound = npc.pathFinder.Status == PathFinding.PathFinderStatus.SUCCESS,
            testIndex = currentTestIndex
        };
        
        // Add to results
        testResults.Add(result);
        
        // Log result
        Debug.Log($"Test {completedTests+1}: {GetAlgorithmName(result.algorithm)} - {result.timeTaken:F2}ms - Path: {result.pathLength} - Nodes: {result.nodesExplored} - Success: {result.pathFound} - Results count: {testResults.Count}");
    }

    private void SaveResultsToCSV()
    {
        try
        {
            Debug.Log($"Saving {testResults.Count} test results to CSV...");
            
            if (testResults.Count == 0)
            {
                Debug.LogWarning("No test results to save!");
                return;
            }
            
            // Create directory if it doesn't exist
            string directory = Path.Combine(Application.persistentDataPath, "TestResults");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            string filePath = Path.Combine(directory, resultsFileName);
            
            StringBuilder csv = new StringBuilder();
            
            // Write header
            csv.AppendLine("Algorithm,GridSizeX,GridSizeY,Density,DiagonalMovement,TimeTaken,PathLength,NodesExplored,MemoryUsed,PathFound,TestIndex");
            
            // Write each result
            foreach (var result in testResults)
            {
                csv.AppendLine($"{result.algorithm},{result.gridSize.x},{result.gridSize.y},{result.mazeDensity},{result.diagonalMovement}," +
                              $"{result.timeTaken},{result.pathLength},{result.nodesExplored},{result.memoryUsed},{result.pathFound},{result.testIndex}");
            }
            
            // Write file
            File.WriteAllText(filePath, csv.ToString());
            
            Debug.Log($"Test results saved to {filePath}");
            
            // Make it easier to find in Windows Explorer
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving test results: {e.Message}");
        }
    }

    // Utility method to get algorithm name as a string
    private string GetAlgorithmName(NPC.PathFinderType algorithm)
    {
        switch (algorithm)
        {
            case NPC.PathFinderType.ASTAR: return "A*";
            case NPC.PathFinderType.DIJKSTRA: return "Dijkstra";
            case NPC.PathFinderType.GREEDY: return "Greedy";
            case NPC.PathFinderType.BACKTRACKING: return "Backtracking";
            case NPC.PathFinderType.BFS: return "BFS";
            default: return "Unknown";
        }
    }
} 