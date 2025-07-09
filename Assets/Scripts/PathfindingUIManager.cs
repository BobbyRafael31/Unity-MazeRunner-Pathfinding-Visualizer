using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

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
    public Toggle allowDiagonalToggle; // Toggle untuk diagonal movement

    [Header("Visualization Controls")]
    public Slider visualizationSpeedSlider;
    public Slider visualizationBatchSlider;
    // public Toggle showVisualizationToggle;
    public TMP_Text speedValueText;
    public TMP_Text batchValueText;

    [Header("Performance Metrics")]
    public TMP_Text timeEstimateText;
    public TMP_Text pathLengthText;
    public TMP_Text nodesExploredText;
    public TMP_Text memoryUsageText;
    public TMP_Text cpuUsageText; // Text untuk menampilkan penggunaan CPU

    [Header("Map Save/Load")]
    public TMP_InputField mapNameInput;
    public Button saveButton;
    public Button loadButton;

    [Header("Application Controls")]
    public Button exitButton; // Tombol untuk keluar aplikasi

    [Header("Optimization")]
    [SerializeField] private bool performWarmup = true;
    [SerializeField] private bool showWarmupMessage = false;

    [Header("Maze Generator")]
    public TMP_Dropdown mazeSizeDropdown;
    public TMP_Dropdown mazeDensityDropdown;

    [Header("Automated Testing")]
    [SerializeField] private GameObject testerPanelPrefab;
    [SerializeField] private Transform testerPanelParent;
    private GameObject testerPanelInstance;

    // Konstanta untuk perhitungan CPU usage
    private const float TARGET_FRAME_TIME_MS = 16.67f; // 60 FPS = 16.67ms per frame

    // Add a flag to track if pathfinding is running
    private bool isPathfindingRunning = false;

    // Add a flag to track if visualization is running
    private bool isVisualizationRunning = false;

    // Add a flag to track if NPC is moving
    private bool isNpcMoving = false;

    private void Start()
    {
        // Initialize UI elements
        InitializeUI();

        // Add listeners
        applyGridSizeButton.onClick.AddListener(OnApplyGridSize);
        runPathfindingButton.onClick.AddListener(OnRunPathfinding);
        resetButton.onClick.AddListener(OnResetPathfinding);
        algorithmDropdown.onValueChanged.AddListener(OnAlgorithmChanged);
        saveButton.onClick.AddListener(OnSaveMap);
        loadButton.onClick.AddListener(OnLoadMap);

        // Add exit button listener if the button exists
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitApplication);

        // Add listener for maze generator
        if (generateMazeButton != null)
            generateMazeButton.onClick.AddListener(OnGenerateMaze);

        // Add visualization control listeners
        if (visualizationSpeedSlider != null)
            visualizationSpeedSlider.onValueChanged.AddListener(OnVisualizationSpeedChanged);

        if (visualizationBatchSlider != null)
            visualizationBatchSlider.onValueChanged.AddListener(OnVisualizationBatchChanged);

        //if (showVisualizationToggle != null)
        //{
        //    showVisualizationToggle.isOn = npc.showVisualization;
        //    showVisualizationToggle.onValueChanged.AddListener(OnShowVisualizationChanged);
        //}

        // Subscribe to NPC's pathfinding events
        npc.OnPathfindingComplete += UpdatePerformanceMetrics;
        npc.OnPathfindingComplete += OnPathfindingCompleted;

        // Subscribe to visualization completion event
        npc.OnVisualizationComplete += OnVisualizationCompleted;

        // Subscribe to movement completion event
        npc.OnMovementComplete += OnMovementCompleted;

        // Initialize performance metrics
        ClearPerformanceMetrics();

        // Initialize visualization controls
        InitializeVisualizationControls();

        // Perform algorithm warmup
        if (performWarmup)
        {
            StartCoroutine(WarmupPathfindingSystem());
        }

        // Tampilkan lokasi penyimpanan
        ShowSaveLocation();
    }

    private IEnumerator WarmupPathfindingSystem()
    {
        // Wait one frame to ensure everything is initialized
        yield return null;

        if (showWarmupMessage)
        {
            //Debug.Log("Performing pathfinding warmup...");
        }

        // Get current NPC position
        Vector3 npcPos = npc.transform.position;
        int startX = (int)(npcPos.x / gridMap.GridNodeWidth);
        int startY = (int)(npcPos.y / gridMap.GridNodeHeight);

        // Find destination node for warmup (try to use opposite corner)
        int destX = gridMap.NumX - 1;
        int destY = gridMap.NumY - 1;

        // Ensure destination is walkable
        GridNode destNode = gridMap.GetGridNode(destX, destY);
        if (destNode == null || !destNode.IsWalkable)
        {
            // Find any walkable node for warmup
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

        // Save current destination position
        Vector3 originalDestPos = gridMap.Destination.position;

        // Set temporary destination for warmup
        gridMap.SetDestination(destX, destY);

        // Run pathfinding quietly (without visualization)
        GridNode startNode = gridMap.GetGridNode(startX, startY);

        if (startNode != null && destNode != null && destNode.IsWalkable)
        {
            // Temporarily disable visualization for warmup
            float originalVisualizationSpeed = npc.visualizationSpeed;
            npc.visualizationSpeed = 0f;
            bool originalShowVisualization = npc.showVisualization;
            npc.showVisualization = false;

            // Do warmup for each algorithm type to JIT compile all code paths
            foreach (NPC.PathFinderType algoType in System.Enum.GetValues(typeof(NPC.PathFinderType)))
            {
                // Save current algorithm
                NPC.PathFinderType originalAlgorithm = npc.pathFinderType;

                // Change to this algorithm
                npc.ChangeAlgorithm(algoType);

                // Run silent pathfinding
                npc.MoveTo(destNode, true);

                // Wait a bit to ensure completion
                yield return new WaitForSeconds(0.05f);

                // Reset back to original algorithm
                npc.ChangeAlgorithm(originalAlgorithm);
            }

            // Restore visualization settings
            npc.visualizationSpeed = originalVisualizationSpeed;
            npc.showVisualization = originalShowVisualization;
        }

        // Restore original destination
        gridMap.Destination.position = originalDestPos;

        // Clear metrics from warmup
        ClearPerformanceMetrics();

        if (showWarmupMessage)
        {
            //Debug.Log("Pathfinding warmup complete");
        }


        // Reset grid colors
        gridMap.ResetGridNodeColours();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (npc != null)
        {
            npc.OnPathfindingComplete -= UpdatePerformanceMetrics;
            npc.OnPathfindingComplete -= OnPathfindingCompleted;
            npc.OnVisualizationComplete -= OnVisualizationCompleted;
            npc.OnMovementComplete -= OnMovementCompleted;
        }

        // Unsubscribe from UI events
        if (allowDiagonalToggle != null)
        {
            allowDiagonalToggle.onValueChanged.RemoveListener(OnDiagonalMovementChanged);
        }
    }

    private void InitializeUI()
    {
        // Set initial values
        gridSizeXInput.text = gridMap.NumX.ToString();
        gridSizeYInput.text = gridMap.NumY.ToString();

        // Set input fields to only accept integers
        gridSizeXInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        gridSizeYInput.contentType = TMP_InputField.ContentType.IntegerNumber;

        // Add input validation events
        gridSizeXInput.onValidateInput += ValidateNumberInput;
        gridSizeYInput.onValidateInput += ValidateNumberInput;

        // Setup algorithm dropdown
        algorithmDropdown.ClearOptions();
        algorithmDropdown.AddOptions(new System.Collections.Generic.List<string> {
            "A*",
            "Dijkstra",
            "Greedy BFS",
            "Backtracking",
            "BFS"
        });

        // Initialize diagonal movement toggle
        if (allowDiagonalToggle != null)
        {
            allowDiagonalToggle.isOn = gridMap.AllowDiagonalMovement;
            allowDiagonalToggle.onValueChanged.AddListener(OnDiagonalMovementChanged);
        }

        // Setup maze size dropdown
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

        // Setup maze density dropdown
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

    // Validation function to only allow numeric input
    private char ValidateNumberInput(string text, int charIndex, char addedChar)
    {
        // Only allow digits
        if (char.IsDigit(addedChar))
        {
            return addedChar;
        }
        else
        {
            return '\0'; // Return null character to reject the input
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

        // Hitung dan tampilkan CPU usage
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
            // Validate grid size limits
            const int MAX_GRID_SIZE = 200;
            const int MIN_GRID_SIZE = 2;

            if (newSizeX > MAX_GRID_SIZE || newSizeY > MAX_GRID_SIZE)
            {
                // Display an error message
                Debug.LogWarning($"Grid size cannot exceed {MAX_GRID_SIZE}x{MAX_GRID_SIZE}. Resize operation cancelled.");

                // Revert input fields to current grid size
                gridSizeXInput.text = gridMap.NumX.ToString();
                gridSizeYInput.text = gridMap.NumY.ToString();

                // Don't proceed with resize
                return;
            }

            // Check for minimum size
            if (newSizeX < MIN_GRID_SIZE || newSizeY < MIN_GRID_SIZE)
            {
                // Display an error message
                Debug.LogWarning($"Grid size cannot be less than {MIN_GRID_SIZE}x{MIN_GRID_SIZE}. Resize operation cancelled.");

                // Revert input fields to current grid size
                gridSizeXInput.text = gridMap.NumX.ToString();
                gridSizeYInput.text = gridMap.NumY.ToString();

                // Don't proceed with resize
                return;
            }

            // Apply the grid size (only if within limits)
            if (gridMap.ResizeGrid(newSizeX, newSizeY))
            {
                ClearPerformanceMetrics();
            }
        }
    }

    private void OnRunPathfinding()
    {
        // Get current NPC position
        Vector3 npcPos = npc.transform.position;
        int startX = (int)(npcPos.x / gridMap.GridNodeWidth);
        int startY = (int)(npcPos.y / gridMap.GridNodeHeight);

        // Get destination position
        Vector3 destPos = gridMap.Destination.position;
        int destX = (int)(destPos.x / gridMap.GridNodeWidth);
        int destY = (int)(destPos.y / gridMap.GridNodeHeight);

        // Run pathfinding
        GridNode startNode = gridMap.GetGridNode(startX, startY);
        GridNode endNode = gridMap.GetGridNode(destX, destY);

        if (startNode != null && endNode != null)
        {
            ClearPerformanceMetrics();

            // Set flags that pathfinding, visualization, and movement will happen
            isPathfindingRunning = true;
            isVisualizationRunning = true;
            isNpcMoving = true;  // assume movement will happen
            SetUIInteractivity(false);

            npc.MoveTo(endNode);
        }
    }

    private void OnResetPathfinding()
    {
        // Reset all flags to ensure UI will be enabled after reload
        isPathfindingRunning = false;
        isVisualizationRunning = false;
        isNpcMoving = false;

        // Force enable UI - this ensures buttons will be enabled
        // after reset regardless of editor/build status
        SetUIInteractivity(true);

        // Reload the current scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

        //Debug.Log("Reloading scene...");
    }

    private void OnAlgorithmChanged(int index)
    {
        NPC.PathFinderType newType = (NPC.PathFinderType)index;
        string algorithmName = GetAlgorithmName(newType);

        Debug.Log($"Algorithm changed - Index: {index}, Type: {newType}, Name: {algorithmName}");

        npc.ChangeAlgorithm(newType);
        ClearPerformanceMetrics();
    }

    // Helper method to get the readable name of the algorithm
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

        // Buat direktori jika belum ada
        string saveDirectory = Path.Combine(Application.persistentDataPath, "GridSaves");
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        string filePath = Path.Combine(saveDirectory, $"{mapNameInput.text}.json");
        gridMap.SaveGridState(filePath);

        Debug.Log($"Map saved to: {filePath} (includes grid state, NPC position, and destination position)");
    }

    /// <summary>
    /// Membuka folder penyimpanan di File Explorer
    /// </summary>
    public void OpenSaveFolder()
    {
        string saveDirectory = Path.Combine(Application.persistentDataPath, "GridSaves");

        // Buat direktori jika belum ada
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        // Buka folder di file explorer
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
            //Debug.LogWarning($"Map file not found: {filePath}");
            return;
        }

        gridMap.LoadGridState(filePath);
        ClearPerformanceMetrics();

        //Debug.Log($"Map loaded from: {filePath}");
    }

    /// <summary>
    /// Generates a random maze with the selected size and density
    /// </summary>
    private void OnGenerateMaze()
    {
        // Get selected maze size
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
                // If more options are added that exceed MAX_GRID_SIZE, they'll be rejected
        }

        // Check if size exceeds maximum or is below minimum - reject if it does
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

        // Resize grid if needed
        if (gridMap.NumX != sizeX || gridMap.NumY != sizeY)
        {
            if (!gridMap.ResizeGrid(sizeX, sizeY))
            {
                // Resize failed, abort maze generation
                return;
            }

            // Update grid size inputs
            gridSizeXInput.text = sizeX.ToString();
            gridSizeYInput.text = sizeY.ToString();
        }

        // Get selected density
        float density = 30f; // Default medium

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

        // Untuk grid besar, nonaktifkan visualisasi sementara untuk performa lebih baik
        bool originalShowVisualization = false;
        float originalVisualizationSpeed = 0f;

        if (isLargeGrid && npc != null)
        {
            // Simpan nilai asli
            originalShowVisualization = npc.showVisualization;
            originalVisualizationSpeed = npc.visualizationSpeed;

            // Nonaktifkan visualisasi untuk grid besar
            npc.showVisualization = false;
        }

        // Generate the maze with selected density
        gridMap.GenerateRandomMaze(density);

        // Kembalikan nilai visualisasi jika diubah
        if (isLargeGrid && npc != null)
        {
            npc.showVisualization = originalShowVisualization;
            npc.visualizationSpeed = originalVisualizationSpeed;
        }

        // Clear performance metrics
        ClearPerformanceMetrics();

        // Tampilkan pesan khusus untuk grid besar
        if (isLargeGrid)
        {
            //Debug.Log("Large maze generated. For best performance, consider disabling visualization during pathfinding.");
        }

        //Debug.Log($"Generated maze with size {sizeX}x{sizeY} and density {density}%");
    }

    /// <summary>
    /// Menampilkan lokasi penyimpanan file di konsol
    /// </summary>
    private void ShowSaveLocation()
    {
        string saveDirectory = Path.Combine(Application.persistentDataPath, "GridSaves");
    }

    /// <summary>
    /// Menutup aplikasi saat tombol exit ditekan
    /// </summary>
    public void OnExitApplication()
    {
#if UNITY_EDITOR
        // Jika di Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Jika di build
        Application.Quit();
#endif

        Debug.Log("Application exit requested");
    }

    private void InitializeVisualizationControls()
    {
        // Set initial slider values based on NPC settings
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
        // Pastikan nilai batch adalah integer
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

    /// <summary>
    /// Menangani perubahan toggle diagonal movement
    /// </summary>
    private void OnDiagonalMovementChanged(bool isOn)
    {
        gridMap.AllowDiagonalMovement = isOn;
        // Reset performance metrics
        ClearPerformanceMetrics();
    }

    // New method to handle pathfinding completion
    private void OnPathfindingCompleted(PathfindingMetrics metrics)
    {
        // Pathfinding is completed, but visualization might still be running
        isPathfindingRunning = false;

        // Check if pathfinding failed by looking at metrics or path length
        bool pathfindingFailed = (metrics.pathLength == 0) ||
                                  (npc.pathFinder != null &&
                                  npc.pathFinder.Status == PathFinding.PathFinderStatus.FAILURE);

        if (pathfindingFailed)
        {
            // If pathfinding failed, there won't be any visualization or movement
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

        // If pathfinding succeeded, continue with normal flow
        // Very important: Keep UI disabled regardless of visualization state to prevent
        // the brief window of interactivity between pathfinding completion and visualization start
        if (npc.showVisualization)
        {
            // If visualization is enabled in settings, assume it will start soon
            // Keep UI disabled by keeping isVisualizationRunning true
            isVisualizationRunning = true;
            // Do NOT enable UI here - wait for visualization to complete
        }
        else
        {
            // Only if visualization is completely disabled in settings, enable UI
            isVisualizationRunning = false;

            // Only re-enable in editor mode
#if UNITY_EDITOR
            SetUIInteractivity(true);
#else
            // In build, keep disabled
            SetUIInteractivity(false);
#endif
        }
    }

    // New method to handle visualization completion
    private void OnVisualizationCompleted()
    {
        // Visualization is completed, but NPC may start moving
        isVisualizationRunning = false;

        // Check if NPC is moving or will move
        if (npc.IsMoving || npc.wayPoints.Count > 0)
        {
            // Movement is starting or in progress
            isNpcMoving = true;
            // Leave UI disabled
        }
        else
        {
            // No movement expected
            isNpcMoving = false;

            // Only re-enable in editor mode
#if UNITY_EDITOR
            SetUIInteractivity(true);
#else
            // In build, keep disabled
            SetUIInteractivity(false);
#endif
        }
    }

    // New method to handle movement completion
    private void OnMovementCompleted()
    {
        // Movement is completed, re-enable UI buttons only in editor
        isNpcMoving = false;

        // Only re-enable in editor mode
#if UNITY_EDITOR
        SetUIInteractivity(true);
#else
        // In build, keep disabled
        Debug.Log("Movement complete but keeping buttons disabled in build mode");
        SetUIInteractivity(false);
#endif
    }

    // Method to enable/disable UI elements based on pathfinding, visualization, and movement state
    private void SetUIInteractivity(bool enabled)
    {
        // If any process is running, disable controls
        bool shouldEnable = enabled && !isPathfindingRunning && !isVisualizationRunning && !isNpcMoving;

        // In builds (not editor), once disabled, buttons stay disabled until reset
#if !UNITY_EDITOR
        if (shouldEnable && (isPathfindingRunning || isVisualizationRunning || isNpcMoving))
        {
            // In builds, once pathfinding started, keep buttons disabled regardless
            Debug.Log("In build - keeping buttons disabled even after completion");
            shouldEnable = false;
        }
#endif

        // Add debug logging
        Debug.Log($"SetUIInteractivity called with enabled={enabled}, pathfinding={isPathfindingRunning}, " +
                 $"visualization={isVisualizationRunning}, movement={isNpcMoving}, shouldEnable={shouldEnable}, " +
                 $"inEditor={Application.isEditor}");

        // Keep reset and exit buttons always enabled
        // Disable all other buttons when processes are running

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

        // Reset and exit buttons remain enabled
        // resetButton and exitButton stay interactable
    }

    private void Update()
    {
        // Continuously check for various states - this ensures buttons stay disabled
        if (npc != null)
        {
            // Check visualization
            if (npc.IsVisualizingPath && !isVisualizationRunning)
            {
                Debug.Log("Detected active visualization - updating UI state");
                isVisualizationRunning = true;
                SetUIInteractivity(false);
            }

            // Check movement
            if (npc.IsMoving && !isNpcMoving)
            {
                Debug.Log("Detected active NPC movement - updating UI state");
                isNpcMoving = true;
                SetUIInteractivity(false);
            }
        }
    }

}