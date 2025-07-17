using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

/// <summary>
/// PathfindingUIManager merupakan kelas yang mengelola antarmuka pengguna untuk sistem pathfinding.
/// pengguna dapat mengatur ukuran grid, memilih algoritma pathfinding, menjalankan pathfinding.
/// </summary>

public class PathfindingUIManager : MonoBehaviour
{
    [Header("References")]
    public GridMap gridMap;
    public NPC npc;

    [Header("Grid Controls")]
    public TMP_InputField gridSizeXInput;
    public TMP_InputField gridSizeYInput;
    public Button applyGridSizeButton;
    public Button generateMazeButton;

    [Header("Algorithm Controls")]
    public TMP_Dropdown algorithmDropdown;
    public Button runPathfindingButton;
    public Button resetButton;
    public Toggle allowDiagonalToggle;

    [Header("Visualization Controls")]
    public Slider visualizationSpeedSlider;
    public Slider visualizationBatchSlider;
    public TMP_Text speedValueText;
    public TMP_Text batchValueText;

    [Header("Performance Metrics")]
    public TMP_Text timeEstimateText;
    public TMP_Text pathLengthText;
    public TMP_Text nodesExploredText;
    public TMP_Text memoryUsageText;
    public TMP_Text cpuUsageText;

    [Header("Map Save/Load")]
    public TMP_InputField mapNameInput;
    public Button saveButton;
    public Button loadButton;

    [Header("Application Controls")]
    public Button exitButton;

    [Header("Optimization")]
    [SerializeField] private bool performWarmup = true;
    [SerializeField] private bool showWarmupMessage = false;

    [Header("Maze Generator")]
    public TMP_Dropdown mazeSizeDropdown;
    public TMP_Dropdown mazeDensityDropdown;

    private const float TARGET_FRAME_TIME_MS = 16.67f; // 60 FPS = 16.67ms per frame

    private bool isPathfindingRunning = false;
    private bool isVisualizationRunning = false;
    private bool isNpcMoving = false;

    private void Start()
    {
        InitializeUI();

        applyGridSizeButton.onClick.AddListener(OnApplyGridSize);
        runPathfindingButton.onClick.AddListener(OnRunPathfinding);
        resetButton.onClick.AddListener(OnResetPathfinding);
        algorithmDropdown.onValueChanged.AddListener(OnAlgorithmChanged);
        saveButton.onClick.AddListener(OnSaveMap);
        loadButton.onClick.AddListener(OnLoadMap);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitApplication);

        if (generateMazeButton != null)
            generateMazeButton.onClick.AddListener(OnGenerateMaze);

        if (visualizationSpeedSlider != null)
            visualizationSpeedSlider.onValueChanged.AddListener(OnVisualizationSpeedChanged);

        if (visualizationBatchSlider != null)
            visualizationBatchSlider.onValueChanged.AddListener(OnVisualizationBatchChanged);

        npc.OnPathfindingComplete += UpdatePerformanceMetrics;
        npc.OnPathfindingComplete += OnPathfindingCompleted;

        npc.OnVisualizationComplete += OnVisualizationCompleted;

        npc.OnMovementComplete += OnMovementCompleted;

        ClearPerformanceMetrics();
        InitializeVisualizationControls();

        if (performWarmup)
        {
            StartCoroutine(WarmupPathfindingSystem());
        }

        ShowSaveLocation();
    }

    private IEnumerator WarmupPathfindingSystem()
    {
        yield return null;

        if (showWarmupMessage)
        {
            //Debug.Log("Performing pathfinding warmup...");
        }

        Vector3 npcPos = npc.transform.position;
        int startX = (int)(npcPos.x / gridMap.GridNodeWidth);
        int startY = (int)(npcPos.y / gridMap.GridNodeHeight);

        int destX = gridMap.NumX - 1;
        int destY = gridMap.NumY - 1;

        GridNode destNode = gridMap.GetGridNode(destX, destY);
        if (destNode == null || !destNode.IsWalkable)
        {
            for (int x = 0; x < gridMap.NumX; x++)
            {
                for (int y = 0; y < gridMap.NumY; y++)
                {
                    GridNode testNode = gridMap.GetGridNode(x, y);
                    if (testNode != null && testNode.IsWalkable && (x != startX || y != startY))
                    {
                        destX = x;
                        destY = y;
                        destNode = testNode;
                        break;
                    }
                }
                if (destNode != null && destNode.IsWalkable)
                    break;
            }
        }

        Vector3 originalDestPos = gridMap.Destination.position;

        gridMap.SetDestination(destX, destY);

        GridNode startNode = gridMap.GetGridNode(startX, startY);

        if (startNode != null && destNode != null && destNode.IsWalkable)
        {
            float originalVisualizationSpeed = npc.visualizationSpeed;
            npc.visualizationSpeed = 0f;
            bool originalShowVisualization = npc.showVisualization;
            npc.showVisualization = false;

            foreach (NPC.PathFinderType algoType in System.Enum.GetValues(typeof(NPC.PathFinderType)))
            {
                NPC.PathFinderType originalAlgorithm = npc.pathFinderType;

                npc.ChangeAlgorithm(algoType);
                npc.MoveTo(destNode, true);
                yield return new WaitForSeconds(0.05f);

                npc.ChangeAlgorithm(originalAlgorithm);
            }

            npc.visualizationSpeed = originalVisualizationSpeed;
            npc.showVisualization = originalShowVisualization;
        }
        gridMap.Destination.position = originalDestPos;

        ClearPerformanceMetrics();

        if (showWarmupMessage)
        {
            //Debug.Log("Pathfinding warmup complete");
        }
        gridMap.ResetGridNodeColours();
    }

    private void OnDestroy()
    {
        if (npc != null)
        {
            npc.OnPathfindingComplete -= UpdatePerformanceMetrics;
            npc.OnPathfindingComplete -= OnPathfindingCompleted;
            npc.OnVisualizationComplete -= OnVisualizationCompleted;
            npc.OnMovementComplete -= OnMovementCompleted;
        }

        if (allowDiagonalToggle != null)
        {
            allowDiagonalToggle.onValueChanged.RemoveListener(OnDiagonalMovementChanged);
        }
    }

    private void InitializeUI()
    {
        gridSizeXInput.text = gridMap.NumX.ToString();
        gridSizeYInput.text = gridMap.NumY.ToString();

        gridSizeXInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        gridSizeYInput.contentType = TMP_InputField.ContentType.IntegerNumber;

        gridSizeXInput.onValidateInput += ValidateNumberInput;
        gridSizeYInput.onValidateInput += ValidateNumberInput;

        algorithmDropdown.ClearOptions();
        algorithmDropdown.AddOptions(new System.Collections.Generic.List<string> {
            "A*",
            "Dijkstra",
            "Greedy BFS",
            "Backtracking",
            "BFS"
        });

        if (allowDiagonalToggle != null)
        {
            allowDiagonalToggle.isOn = gridMap.AllowDiagonalMovement;
            allowDiagonalToggle.onValueChanged.AddListener(OnDiagonalMovementChanged);
        }

        if (mazeSizeDropdown != null)
        {
            mazeSizeDropdown.ClearOptions();
            mazeSizeDropdown.AddOptions(new System.Collections.Generic.List<string> {
                "Small",
                "Medium",
                "Big",
                "Very Big"

            });
        }

        if (mazeDensityDropdown != null)
        {
            mazeDensityDropdown.ClearOptions();
            mazeDensityDropdown.AddOptions(new System.Collections.Generic.List<string> {
                "Low",
                "Medium",
                "High"
            });
        }

        ClearPerformanceMetrics();
    }

    private char ValidateNumberInput(string text, int charIndex, char addedChar)
    {
        if (char.IsDigit(addedChar))
        {
            return addedChar;
        }
        else
        {
            return '\0';
        }
    }

    private void ClearPerformanceMetrics()
    {
        timeEstimateText.text = "0";
        pathLengthText.text = "0";
        memoryUsageText.text = "0";
        nodesExploredText.text = "0";
        cpuUsageText.text = "0%";
    }

    private void UpdatePerformanceMetrics(PathfindingMetrics metrics)
    {
        timeEstimateText.text = $"{metrics.timeTaken:F2} ms";
        pathLengthText.text = $"{metrics.pathLength} nodes";
        nodesExploredText.text = $"{metrics.nodesExplored} nodes";
        memoryUsageText.text = FormatBytes(metrics.memoryUsed);

        if (cpuUsageText != null)
        {
            float cpuUsagePercentage = (metrics.timeTaken / TARGET_FRAME_TIME_MS) * 100f;
            cpuUsageText.text = $"{cpuUsagePercentage:F2}%";
        }
    }

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

    private void OnApplyGridSize()
    {
        if (int.TryParse(gridSizeXInput.text, out int newSizeX) &&
            int.TryParse(gridSizeYInput.text, out int newSizeY))
        {
            const int MAX_GRID_SIZE = 200;
            const int MIN_GRID_SIZE = 2;

            if (newSizeX > MAX_GRID_SIZE || newSizeY > MAX_GRID_SIZE)
            {
                Debug.LogWarning($"Grid size cannot exceed {MAX_GRID_SIZE}x{MAX_GRID_SIZE}. Resize operation cancelled.");

                gridSizeXInput.text = gridMap.NumX.ToString();
                gridSizeYInput.text = gridMap.NumY.ToString();

                return;
            }

            if (newSizeX < MIN_GRID_SIZE || newSizeY < MIN_GRID_SIZE)
            {
                Debug.LogWarning($"Grid size cannot be less than {MIN_GRID_SIZE}x{MIN_GRID_SIZE}. Resize operation cancelled.");

                gridSizeXInput.text = gridMap.NumX.ToString();
                gridSizeYInput.text = gridMap.NumY.ToString();

                return;
            }

            if (gridMap.ResizeGrid(newSizeX, newSizeY))
            {
                ClearPerformanceMetrics();
            }
        }
    }

    private void OnRunPathfinding()
    {
        Vector3 npcPos = npc.transform.position;
        int startX = (int)(npcPos.x / gridMap.GridNodeWidth);
        int startY = (int)(npcPos.y / gridMap.GridNodeHeight);

        Vector3 destPos = gridMap.Destination.position;
        int destX = (int)(destPos.x / gridMap.GridNodeWidth);
        int destY = (int)(destPos.y / gridMap.GridNodeHeight);

        GridNode startNode = gridMap.GetGridNode(startX, startY);
        GridNode endNode = gridMap.GetGridNode(destX, destY);

        if (startNode != null && endNode != null)
        {
            ClearPerformanceMetrics();

            isPathfindingRunning = true;
            isVisualizationRunning = true;
            isNpcMoving = true;
            SetUIInteractivity(false);

            npc.MoveTo(endNode);
        }
    }

    private void OnResetPathfinding()
    {
        isPathfindingRunning = false;
        isVisualizationRunning = false;
        isNpcMoving = false;

        SetUIInteractivity(true);

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

    }

    private void OnAlgorithmChanged(int index)
    {
        NPC.PathFinderType newType = (NPC.PathFinderType)index;
        string algorithmName = GetAlgorithmName(newType);

        Debug.Log($"Algorithm changed - Index: {index}, Type: {newType}, Name: {algorithmName}");

        npc.ChangeAlgorithm(newType);
        ClearPerformanceMetrics();
    }

    private string GetAlgorithmName(NPC.PathFinderType type)
    {
        switch (type)
        {
            case NPC.PathFinderType.ASTAR:
                return "A*";
            case NPC.PathFinderType.DIJKSTRA:
                return "Dijkstra";
            case NPC.PathFinderType.GREEDY:
                return "Greedy BFS";
            case NPC.PathFinderType.BACKTRACKING:
                return "Backtracking";
            case NPC.PathFinderType.BFS:
                return "BFS";
            default:
                return "Unknown";
        }
    }

    private void OnSaveMap()
    {
        if (string.IsNullOrEmpty(mapNameInput.text))
        {
            //Debug.LogWarning("Please enter a map name before saving");
            return;
        }

        string saveDirectory = Path.Combine(Application.persistentDataPath, "GridSaves");
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        string filePath = Path.Combine(saveDirectory, $"{mapNameInput.text}.json");
        gridMap.SaveGridState(filePath);

        Debug.Log($"Map saved to: {filePath} (includes grid state, NPC position, and destination position)");
    }

    public void OpenSaveFolder()
    {
        string saveDirectory = Path.Combine(Application.persistentDataPath, "GridSaves");
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }
        System.Diagnostics.Process.Start("explorer.exe", saveDirectory);
    }

    private void OnLoadMap()
    {
        if (string.IsNullOrEmpty(mapNameInput.text))
        {
            return;
        }

        string saveDirectory = Path.Combine(Application.persistentDataPath, "GridSaves");
        string filePath = Path.Combine(saveDirectory, $"{mapNameInput.text}.json");

        if (!File.Exists(filePath))
        {
            return;
        }

        gridMap.LoadGridState(filePath);
        ClearPerformanceMetrics();
    }

    private void OnGenerateMaze()
    {
        int sizeX = 20;
        int sizeY = 20;
        bool isLargeGrid = false;

        const int MAX_GRID_SIZE = 200;
        const int MIN_GRID_SIZE = 2;

        switch (mazeSizeDropdown.value)
        {
            case 0: // Kecil
                sizeX = sizeY = 20;
                break;
            case 1: // Sedang
                sizeX = sizeY = 35;
                break;
            case 2: // Besar
                sizeX = sizeY = 50;
                isLargeGrid = true;
                break;
            case 3: // Sangat Besar
                sizeX = sizeY = 100;
                isLargeGrid = true;
                break;
        }

        if (sizeX > MAX_GRID_SIZE || sizeY > MAX_GRID_SIZE)
        {
            Debug.LogWarning($"Maze size {sizeX}x{sizeY} exceeds maximum of {MAX_GRID_SIZE}x{MAX_GRID_SIZE}. Operation cancelled.");
            return;
        }

        if (sizeX < MIN_GRID_SIZE || sizeY < MIN_GRID_SIZE)
        {
            Debug.LogWarning($"Maze size {sizeX}x{sizeY} is below minimum of {MIN_GRID_SIZE}x{MIN_GRID_SIZE}. Operation cancelled.");
            return;
        }

        if (gridMap.NumX != sizeX || gridMap.NumY != sizeY)
        {
            if (!gridMap.ResizeGrid(sizeX, sizeY))
            {
                return;
            }

            gridSizeXInput.text = sizeX.ToString();
            gridSizeYInput.text = sizeY.ToString();
        }

        float density = 30f;

        switch (mazeDensityDropdown.value)
        {
            case 0: // Low
                density = 10f;
                break;
            case 1: // Medium
                density = 30f;
                break;
            case 2: // High
                density = 50f;
                break;
        }

        bool originalShowVisualization = false;
        float originalVisualizationSpeed = 0f;

        if (isLargeGrid && npc != null)
        {
            originalShowVisualization = npc.showVisualization;
            originalVisualizationSpeed = npc.visualizationSpeed;

            npc.showVisualization = false;
        }

        gridMap.GenerateRandomMaze(density);

        if (isLargeGrid && npc != null)
        {
            npc.showVisualization = originalShowVisualization;
            npc.visualizationSpeed = originalVisualizationSpeed;
        }

        ClearPerformanceMetrics();
    }

    private void ShowSaveLocation()
    {
        string saveDirectory = Path.Combine(Application.persistentDataPath, "GridSaves");
    }

    public void OnExitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // Jika di build
            Application.Quit();
#endif

        Debug.Log("Application exit requested");
    }

    private void InitializeVisualizationControls()
    {
        if (visualizationSpeedSlider != null)
        {
            visualizationSpeedSlider.value = npc.visualizationSpeed;
            UpdateSpeedValueText(npc.visualizationSpeed);
        }

        if (visualizationBatchSlider != null)
        {
            visualizationBatchSlider.value = npc.visualizationBatch;
            UpdateBatchValueText(npc.visualizationBatch);
        }
    }

    private void OnVisualizationSpeedChanged(float newValue)
    {
        npc.visualizationSpeed = newValue;
        UpdateSpeedValueText(newValue);
    }

    private void OnVisualizationBatchChanged(float newValue)
    {
        int batchValue = Mathf.RoundToInt(newValue);
        npc.visualizationBatch = batchValue;
        UpdateBatchValueText(batchValue);
    }

    private void OnShowVisualizationChanged(bool isOn)
    {
        npc.showVisualization = isOn;
    }

    private void UpdateSpeedValueText(float value)
    {
        if (speedValueText != null)
        {
            speedValueText.text = value.ToString("F1") + "s";
        }
    }

    private void UpdateBatchValueText(int value)
    {
        if (batchValueText != null)
        {
            batchValueText.text = value.ToString();
        }
    }
    private void OnDiagonalMovementChanged(bool isOn)
    {
        gridMap.AllowDiagonalMovement = isOn;
        ClearPerformanceMetrics();
    }

    private void OnPathfindingCompleted(PathfindingMetrics metrics)
    {
        isPathfindingRunning = false;

        bool pathfindingFailed = (metrics.pathLength == 0) ||
                                  (npc.pathFinder != null &&
                                  npc.pathFinder.Status == PathFinding.PathFinderStatus.FAILURE);

        if (pathfindingFailed)
        {
            Debug.Log("Pathfinding failed - re-enabling UI controls immediately");
            isVisualizationRunning = false;
            isNpcMoving = false;

            // Only re-enable in editor mode
#if UNITY_EDITOR
            SetUIInteractivity(true);
#else
            // In build, keep disabled
            SetUIInteractivity(false);
#endif

            return;
        }


        if (npc.showVisualization)
        {
            isVisualizationRunning = true;
        }
        else
        {
            isVisualizationRunning = false;

#if UNITY_EDITOR
            SetUIInteractivity(true);
#else
            // In build, keep disabled
            SetUIInteractivity(false);
#endif
        }
    }

    private void OnVisualizationCompleted()
    {
        isVisualizationRunning = false;

        if (npc.IsMoving || npc.wayPoints.Count > 0)
        {
            isNpcMoving = true;
        }
        else
        {
            isNpcMoving = false;

#if UNITY_EDITOR
            SetUIInteractivity(true);
#else
            // In build, keep disabled
            SetUIInteractivity(false);
#endif
        }
    }

    private void OnMovementCompleted()
    {
        isNpcMoving = false;

#if UNITY_EDITOR
        SetUIInteractivity(true);
#else
        // In build, keep disabled
        Debug.Log("Movement complete but keeping buttons disabled in build mode");
        SetUIInteractivity(false);
#endif
    }

    private void SetUIInteractivity(bool enabled)
    {
        bool shouldEnable = enabled && !isPathfindingRunning && !isVisualizationRunning && !isNpcMoving;

#if !UNITY_EDITOR
        if (shouldEnable && (isPathfindingRunning || isVisualizationRunning || isNpcMoving))
        {
            // In builds, once pathfinding started, keep buttons disabled regardless
            Debug.Log("In build - keeping buttons disabled even after completion");
            shouldEnable = false;
        }
#endif

        Debug.Log($"SetUIInteractivity called with enabled={enabled}, pathfinding={isPathfindingRunning}, " +
                 $"visualization={isVisualizationRunning}, movement={isNpcMoving}, shouldEnable={shouldEnable}, " +
                 $"inEditor={Application.isEditor}");


        if (applyGridSizeButton != null)
            applyGridSizeButton.interactable = shouldEnable;

        if (runPathfindingButton != null)
            runPathfindingButton.interactable = shouldEnable;

        if (algorithmDropdown != null)
            algorithmDropdown.interactable = shouldEnable;

        if (allowDiagonalToggle != null)
            allowDiagonalToggle.interactable = shouldEnable;

        if (saveButton != null)
            saveButton.interactable = shouldEnable;

        if (loadButton != null)
            loadButton.interactable = shouldEnable;

        if (generateMazeButton != null)
            generateMazeButton.interactable = shouldEnable;

        if (visualizationSpeedSlider != null)
            visualizationSpeedSlider.interactable = shouldEnable;

        if (visualizationBatchSlider != null)
            visualizationBatchSlider.interactable = shouldEnable;

        if (mazeSizeDropdown != null)
            mazeSizeDropdown.interactable = shouldEnable;

        if (mazeDensityDropdown != null)
            mazeDensityDropdown.interactable = shouldEnable;

        if (gridSizeXInput != null)
            gridSizeXInput.interactable = shouldEnable;

        if (gridSizeYInput != null)
            gridSizeYInput.interactable = shouldEnable;

        if (mapNameInput != null)
            mapNameInput.interactable = shouldEnable;
    }

    private void Update()
    {
        if (npc != null)
        {
            if (npc.IsVisualizingPath && !isVisualizationRunning)
            {
                Debug.Log("Detected active visualization - updating UI state");
                isVisualizationRunning = true;
                SetUIInteractivity(false);
            }

            if (npc.IsMoving && !isNpcMoving)
            {
                Debug.Log("Detected active NPC movement - updating UI state");
                isNpcMoving = true;
                SetUIInteractivity(false);
            }
        }
    }

}