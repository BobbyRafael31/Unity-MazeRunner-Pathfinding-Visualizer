using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Utility class to analyze pathfinding test results and display statistics
/// </summary>
public class PathfindingTestAnalyzer : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text resultsText;
    public TMP_Dropdown algorithmFilterDropdown;
    public TMP_Dropdown statisticTypeDropdown;
    public RectTransform graphContainer;
    public GameObject barPrefab;
    
    [Header("File Settings")]
    public string resultsFileName = "pathfinding_tests.csv";

    // Data structures
    private List<TestResult> allResults = new List<TestResult>();
    private Dictionary<string, Color> algorithmColors = new Dictionary<string, Color>()
    {
        { "ASTAR", Color.blue },
        { "DIJKSTRA", Color.green },
        { "GREEDY", Color.red },
        { "BACKTRACKING", Color.yellow },
        { "BFS", Color.magenta }
    };

    // Struct to hold a single test result
    private struct TestResult
    {
        public string algorithm;
        public int gridSizeX;
        public int gridSizeY;
        public float density;
        public bool diagonalMovement;
        public float timeTaken;
        public int pathLength;
        public int nodesExplored;
        public long memoryUsed;
        public bool pathFound;
        public int testIndex;
    }

    void Start()
    {
        SetupDropdowns();
        LoadResults();
        
        // Set default visualization
        if (allResults.Count > 0)
        {
            GenerateStatistics();
        }
    }

    private void SetupDropdowns()
    {
        // Set up algorithm filter dropdown
        algorithmFilterDropdown.ClearOptions();
        algorithmFilterDropdown.AddOptions(new List<string> {
            "All Algorithms",
            "A*",
            "Dijkstra",
            "Greedy BFS",
            "Backtracking",
            "BFS"
        });
        algorithmFilterDropdown.onValueChanged.AddListener(OnFilterChanged);

        // Set up statistic type dropdown
        statisticTypeDropdown.ClearOptions();
        statisticTypeDropdown.AddOptions(new List<string> {
            "Execution Time",
            "Path Length",
            "Nodes Explored",
            "Memory Used",
            "Success Rate"
        });
        statisticTypeDropdown.onValueChanged.AddListener(OnFilterChanged);
    }

    private void LoadResults()
    {
        string directory = Path.Combine(Application.persistentDataPath, "TestResults");
        string filePath = Path.Combine(directory, resultsFileName);
        
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Results file not found: {filePath}");
            if (resultsText != null)
            {
                resultsText.text = "No test results found. Run tests first.";
            }
            return;
        }

        // Read all lines and skip header
        string[] lines = File.ReadAllLines(filePath);
        if (lines.Length <= 1)
        {
            Debug.LogWarning("Results file is empty or only contains a header");
            return;
        }

        // Clear previous results
        allResults.Clear();

        // Skip header row and parse data rows
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] values = line.Split(',');
            if (values.Length < 10)
            {
                Debug.LogWarning($"Invalid data in line {i}: {line}");
                continue;
            }

            TestResult result = new TestResult
            {
                algorithm = values[0],
                gridSizeX = int.Parse(values[1]),
                gridSizeY = int.Parse(values[2]),
                density = float.Parse(values[3]),
                diagonalMovement = bool.Parse(values[4]),
                timeTaken = float.Parse(values[5]),
                pathLength = int.Parse(values[6]),
                nodesExplored = int.Parse(values[7]),
                memoryUsed = long.Parse(values[8]),
                pathFound = bool.Parse(values[9]),
                testIndex = int.Parse(values[10])
            };

            allResults.Add(result);
        }

        Debug.Log($"Loaded {allResults.Count} test results");
    }

    public void OnFilterChanged(int value)
    {
        GenerateStatistics();
    }

    private void GenerateStatistics()
    {
        if (allResults.Count == 0)
            return;

        // Clear previous graph
        foreach (Transform child in graphContainer)
        {
            Destroy(child.gameObject);
        }

        // Get selected algorithm filter
        string algorithmFilter = "All";
        switch (algorithmFilterDropdown.value)
        {
            case 0: algorithmFilter = "All"; break;
            case 1: algorithmFilter = "ASTAR"; break;
            case 2: algorithmFilter = "DIJKSTRA"; break;
            case 3: algorithmFilter = "GREEDY"; break;
            case 4: algorithmFilter = "BACKTRACKING"; break;
            case 5: algorithmFilter = "BFS"; break;
        }

        // Filter results by algorithm if not "All"
        List<TestResult> filteredResults = allResults;
        if (algorithmFilter != "All")
        {
            filteredResults = allResults.Where(r => r.algorithm == algorithmFilter).ToList();
        }

        if (filteredResults.Count == 0)
        {
            resultsText.text = "No data for selected filters.";
            return;
        }

        // Generate statistics based on selected metric
        switch (statisticTypeDropdown.value)
        {
            case 0: // Execution Time
                GenerateTimeStatistics(filteredResults);
                break;
            case 1: // Path Length
                GeneratePathLengthStatistics(filteredResults);
                break;
            case 2: // Nodes Explored
                GenerateNodesExploredStatistics(filteredResults);
                break;
            case 3: // Memory Used
                GenerateMemoryStatistics(filteredResults);
                break;
            case 4: // Success Rate
                GenerateSuccessRateStatistics(filteredResults);
                break;
        }
    }

    private void GenerateTimeStatistics(List<TestResult> results)
    {
        var averageTimeByAlgorithm = results
            .GroupBy(r => r.algorithm)
            .Select(g => new {
                Algorithm = g.Key,
                AverageTime = g.Average(r => r.timeTaken)
            })
            .OrderByDescending(x => x.AverageTime)
            .ToList();

        // Generate graph bars
        CreateBarsForData(averageTimeByAlgorithm.Select(x => x.Algorithm).ToList(),
                         averageTimeByAlgorithm.Select(x => (float)x.AverageTime).ToList(),
                         "ms");

        // Generate text summary
        resultsText.text = "Average Execution Time by Algorithm:\n\n";
        foreach (var stat in averageTimeByAlgorithm)
        {
            resultsText.text += $"{GetAlgorithmName(stat.Algorithm)}: {stat.AverageTime:F2} ms\n";
        }
    }

    private void GeneratePathLengthStatistics(List<TestResult> results)
    {
        var averagePathByAlgorithm = results
            .Where(r => r.pathFound) // Only include successful paths
            .GroupBy(r => r.algorithm)
            .Select(g => new {
                Algorithm = g.Key,
                AveragePath = g.Average(r => r.pathLength)
            })
            .OrderByDescending(x => x.AveragePath)
            .ToList();

        // Generate graph bars
        CreateBarsForData(averagePathByAlgorithm.Select(x => x.Algorithm).ToList(),
                         averagePathByAlgorithm.Select(x => (float)x.AveragePath).ToList(),
                         "nodes");

        // Generate text summary
        resultsText.text = "Average Path Length by Algorithm:\n\n";
        foreach (var stat in averagePathByAlgorithm)
        {
            resultsText.text += $"{GetAlgorithmName(stat.Algorithm)}: {stat.AveragePath:F2} nodes\n";
        }
    }

    private void GenerateNodesExploredStatistics(List<TestResult> results)
    {
        var averageNodesExploredByAlgorithm = results
            .GroupBy(r => r.algorithm)
            .Select(g => new {
                Algorithm = g.Key,
                AverageNodes = g.Average(r => r.nodesExplored)
            })
            .OrderByDescending(x => x.AverageNodes)
            .ToList();

        // Generate graph bars
        CreateBarsForData(averageNodesExploredByAlgorithm.Select(x => x.Algorithm).ToList(),
                         averageNodesExploredByAlgorithm.Select(x => (float)x.AverageNodes).ToList(),
                         "nodes");

        // Generate text summary
        resultsText.text = "Average Nodes Explored by Algorithm:\n\n";
        foreach (var stat in averageNodesExploredByAlgorithm)
        {
            resultsText.text += $"{GetAlgorithmName(stat.Algorithm)}: {stat.AverageNodes:F0} nodes\n";
        }
    }

    private void GenerateMemoryStatistics(List<TestResult> results)
    {
        var averageMemoryByAlgorithm = results
            .GroupBy(r => r.algorithm)
            .Select(g => new {
                Algorithm = g.Key,
                AverageMemory = g.Average(r => r.memoryUsed) / 1024.0f // Convert to KB
            })
            .OrderByDescending(x => x.AverageMemory)
            .ToList();

        // Generate graph bars
        CreateBarsForData(averageMemoryByAlgorithm.Select(x => x.Algorithm).ToList(),
                         averageMemoryByAlgorithm.Select(x => (float)x.AverageMemory).ToList(),
                         "KB");

        // Generate text summary
        resultsText.text = "Average Memory Usage by Algorithm:\n\n";
        foreach (var stat in averageMemoryByAlgorithm)
        {
            resultsText.text += $"{GetAlgorithmName(stat.Algorithm)}: {stat.AverageMemory:F2} KB\n";
        }
    }

    private void GenerateSuccessRateStatistics(List<TestResult> results)
    {
        var successRateByAlgorithm = results
            .GroupBy(r => r.algorithm)
            .Select(g => new {
                Algorithm = g.Key,
                SuccessRate = g.Count(r => r.pathFound) * 100.0f / g.Count()
            })
            .OrderByDescending(x => x.SuccessRate)
            .ToList();

        // Generate graph bars
        CreateBarsForData(successRateByAlgorithm.Select(x => x.Algorithm).ToList(),
                         successRateByAlgorithm.Select(x => (float)x.SuccessRate).ToList(),
                         "%");

        // Generate text summary
        resultsText.text = "Success Rate by Algorithm:\n\n";
        foreach (var stat in successRateByAlgorithm)
        {
            resultsText.text += $"{GetAlgorithmName(stat.Algorithm)}: {stat.SuccessRate:F1}%\n";
        }
    }

    private void CreateBarsForData(List<string> labels, List<float> values, string unit)
    {
        if (barPrefab == null || graphContainer == null || labels.Count == 0)
            return;

        float graphWidth = graphContainer.rect.width;
        float graphHeight = graphContainer.rect.height;
        float maxValue = values.Max();
        float barWidth = graphWidth / (labels.Count + 1);

        for (int i = 0; i < labels.Count; i++)
        {
            // Create bar
            GameObject barObj = Instantiate(barPrefab, graphContainer);
            RectTransform barRect = barObj.GetComponent<RectTransform>();
            Image barImage = barObj.GetComponent<Image>();
            
            // Calculate height based on value
            float normalizedValue = values[i] / maxValue;
            float barHeight = graphHeight * normalizedValue * 0.8f; // 80% of graph height max
            
            // Position and size bar
            barRect.anchoredPosition = new Vector2((i + 0.5f) * barWidth, barHeight / 2);
            barRect.sizeDelta = new Vector2(barWidth * 0.8f, barHeight);
            
            // Set bar color
            string algorithm = labels[i];
            if (algorithmColors.ContainsKey(algorithm))
                barImage.color = algorithmColors[algorithm];
            
            // Add label
            GameObject labelObj = new GameObject($"Label_{labels[i]}");
            labelObj.transform.SetParent(barObj.transform);
            
            TMP_Text labelText = labelObj.AddComponent<TMP_Text>();
            labelText.text = $"{values[i]:F1}{unit}";
            labelText.fontSize = 12;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.black;
            
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(0, barHeight + 10);
            labelRect.sizeDelta = new Vector2(barWidth, 20);
            
            // Add algorithm name label at bottom
            GameObject nameObj = new GameObject($"Name_{labels[i]}");
            nameObj.transform.SetParent(barObj.transform);
            
            TMP_Text nameText = nameObj.AddComponent<TMP_Text>();
            nameText.text = GetAlgorithmName(labels[i]);
            nameText.fontSize = 10;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.black;
            
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchoredPosition = new Vector2(0, -10);
            nameRect.sizeDelta = new Vector2(barWidth, 20);
        }
    }

    private string GetAlgorithmName(string algorithm)
    {
        switch (algorithm)
        {
            case "ASTAR": return "A*";
            case "DIJKSTRA": return "Dijkstra";
            case "GREEDY": return "Greedy";
            case "BACKTRACKING": return "Backtracking";
            case "BFS": return "BFS";
            default: return algorithm;
        }
    }

    // Utility function to format memory size
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
} 