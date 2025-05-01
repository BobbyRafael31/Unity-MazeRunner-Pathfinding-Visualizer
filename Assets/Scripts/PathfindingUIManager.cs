using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
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

    // Konstanta untuk perhitungan CPU usage
    private const float TARGET_FRAME_TIME_MS = 16.67f; // 60 FPS = 16.67ms per frame

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
                "Big"
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
            gridMap.ResizeGrid(newSizeX, newSizeY);
            ClearPerformanceMetrics();
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
            npc.MoveTo(endNode);
        }
    }

    private void OnResetPathfinding()
    {
        // Reload the current scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

        //Debug.Log("Reloading scene...");
    }

    private void OnAlgorithmChanged(int index)
    {
        NPC.PathFinderType newType = (NPC.PathFinderType)index;
        npc.ChangeAlgorithm(newType);
        ClearPerformanceMetrics();
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

        Debug.Log($"Map saved to: {filePath}");
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

        switch (mazeSizeDropdown.value)
        {
            case 0: // Kecil
                sizeX = sizeY = 20;
                break;
            case 1: // Sedang
                sizeX = sizeY = 50;
                break;
            case 2: // Besar
                sizeX = sizeY = 100;
                isLargeGrid = true;
                break;
        }

        // Resize grid if needed
        if (gridMap.NumX != sizeX || gridMap.NumY != sizeY)
        {
            gridMap.ResizeGrid(sizeX, sizeY);

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
}