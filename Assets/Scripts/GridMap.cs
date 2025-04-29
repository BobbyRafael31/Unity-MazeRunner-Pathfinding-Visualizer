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
    // Ukuran grid pada sumbu X
    [SerializeField]
    int numX;
    // Ukuran grid pada sumbu Y
    [SerializeField]
    int numY;

    // Prefab untuk visualisasi node grid
    [SerializeField]
    GameObject gridNodeViewPrefab;

    // Mengizinkan atau melarang pergerakan diagonal pada pathfinding
    [SerializeField]
    bool allowDiagonalMovement = false;

    // Properti publik untuk mengakses allowDiagonalMovement dari luar
    public bool AllowDiagonalMovement
    {
        get { return allowDiagonalMovement; }
        set { allowDiagonalMovement = value; }
    }

    // Warna-warna untuk representasi visual berbagai status node
    public Color COLOR_WALKABLE = new Color(0.4f, 0.4f, 0.8f, 1.0f);   // Warna untuk node yang dapat dilalui
    public Color COLOR_NONWALKABLE = Color.black;                       // Warna untuk node yang tidak dapat dilalui
    public Color COLOR_CURRENT_NODE = Color.cyan;                       // Warna untuk node yang sedang diproses
    public Color COLOR_ADD_TO_OPENLIST = Color.green;                   // Warna untuk node yang ditambahkan ke open list
    public Color COLOR_ADD_TO_CLOSEDLIST = Color.grey;                  // Warna untuk node yang ditambahkan ke closed list
    public Color COLOR_PATH = Color.blue;                               // Warna untuk node yang menjadi bagian dari jalur final

    // Getter untuk ukuran grid
    public int NumX { get { return numX; } }
    public int NumY { get { return numY; } }

    // Referensi ke NPC yang akan menggunakan path
    [SerializeField]
    NPC npc;

    // Referensi ke Transform tujuan untuk visualisasi
    [SerializeField]
    Transform destination;

    public Transform Destination { get { return destination; } }

    // Ukuran fisik setiap node dalam grid
    float gridNodeWidth = 1.0f;
    float gridNodeHeight = 1.0f;

    // Getter untuk ukuran node
    public float GridNodeWidth { get { return gridNodeWidth; } }
    public float GridNodeHeight { get { return gridNodeHeight; } }

    // Array 2D untuk menyimpan semua GridNodeView
    private GridNodeView[,] gridNodeViews = null;

    // Posisi mouse terakhir untuk tracking perubahan
    private Vector2 lastMousePosition;
    // Node terakhir yang statusnya diubah
    private GridNodeView lastToggledNode = null;

    /// <summary>
    /// Inisialisasi grid pada saat permainan dimulai
    /// </summary>
    void Start()
    {
        // Membuat array untuk menyimpan semua node view
        gridNodeViews = new GridNodeView[NumX, NumY];
        for (int i = 0; i < NumX; i++)
        {
            for (int j = 0; j < NumY; j++)
            {
                // Membuat instance dari prefab node grid di posisi yang sesuai
                GameObject obj = Instantiate(
                  gridNodeViewPrefab,
                  new Vector3(
                    i * GridNodeWidth,
                    j * GridNodeHeight,
                    0.0f),
                  Quaternion.identity);

                // Memberi nama pada objek grid node untuk identifikasi
                obj.name = "GridNode_" + i.ToString() + "_" + j.ToString();
                GridNodeView gnv = obj.GetComponent<GridNodeView>();
                gridNodeViews[i, j] = gnv;
                gnv.Node = new GridNode(new Vector2Int(i, j), this);

                // Menjadikan node sebagai child dari GridMap
                obj.transform.SetParent(transform);
            }
        }

        // Mengatur posisi kamera agar dapat melihat seluruh grid
        SetCameraPosition();
        // Menetapkan referensi grid map pada NPC
        npc.Map = this;
        // Menetapkan posisi awal NPC pada node (0,0)
        npc.SetStartNode(gridNodeViews[0, 0].Node);
    }

    /// <summary>
    /// Mengatur posisi kamera agar dapat menampilkan seluruh grid
    /// </summary>
    void SetCameraPosition()
    {
        // Calculate the center of the grid
        float gridCenterX = ((numX - 1) * GridNodeWidth) / 2;
        float gridCenterY = ((numY - 1) * GridNodeHeight) / 2;

        // Calculate dynamic offset based on grid size
        float gridWidth = (numX - 1) * GridNodeWidth;
        float gridHeight = (numY - 1) * GridNodeHeight;

        // For 20x20 grid: x=5.6, y=10.7, orthoSize=12.4
        float baseX = 5.6f;
        float baseY = 10.7f;
        float baseOrthoSize = 12.4f;
        float baseGridSize = 20f;

        // Scale position and ortho size based on current grid size relative to 20x20
        float scaleFactor = Mathf.Max(numX, numY) / baseGridSize;
        float xPos = baseX * scaleFactor;
        float yPos = baseY * scaleFactor;
        float orthoSize = baseOrthoSize * scaleFactor;

        // Position camera
        Camera.main.transform.position = new Vector3(
            xPos,
            yPos,
            -100.0f);

        // Set orthographic size
        Camera.main.orthographicSize = orthoSize;
    }

    // Tabel offset arah untuk pathfinding
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

    /// <summary>
    /// Mendapatkan daftar node tetangga yang dapat dilalui dari node yang diberikan
    /// </summary>
    /// <param name="loc">Node saat ini</param>
    /// <returns>Daftar node tetangga yang dapat dilalui</returns>
    public List<PathFinding.Node<Vector2Int>> GetNeighbours(PathFinding.Node<Vector2Int> loc)
    {
        // Pre-alokasi dengan kapasitas maksimum untuk menghindari resize
        List<PathFinding.Node<Vector2Int>> neighbours = new List<PathFinding.Node<Vector2Int>>(8);

        int x = loc.Value.x;
        int y = loc.Value.y;

        // Periksa arah kardinal terlebih dahulu (lebih cepat dan selalu ada)
        for (int dir = 0; dir < 4; dir++)
        {
            int nx = x + directions[dir, 0];
            int ny = y + directions[dir, 1];

            // Periksa bounds dan walkability dalam satu kondisi
            if (nx >= 0 && nx < numX && ny >= 0 && ny < numY && gridNodeViews[nx, ny].Node.IsWalkable)
            {
                neighbours.Add(gridNodeViews[nx, ny].Node);
            }
        }

        // Jika diagonal movement diizinkan, periksa 4 arah diagonal
        if (allowDiagonalMovement)
        {
            for (int dir = 4; dir < 8; dir++)
            {
                int nx = x + directions[dir, 0];
                int ny = y + directions[dir, 1];

                // Periksa bounds dan walkability dalam satu kondisi
                if (nx >= 0 && nx < numX && ny >= 0 && ny < numY && gridNodeViews[nx, ny].Node.IsWalkable)
                {
                    neighbours.Add(gridNodeViews[nx, ny].Node);
                }
            }
        }

        return neighbours;
    }

    /// <summary>
    /// Mendeteksi node pada posisi klik mouse dan mengubah status walkable-nya
    /// </summary>
    public void RayCastAndToggleWalkable()
    {
        // Konversi posisi mouse ke koordinat dunia
        Vector2 rayPos = new Vector2(
          Camera.main.ScreenToWorldPoint(Input.mousePosition).x,
          Camera.main.ScreenToWorldPoint(Input.mousePosition).y);

        // Melakukan raycast untuk mendeteksi objek pada posisi mouse
        RaycastHit2D hit = Physics2D.Raycast(rayPos, Vector2.zero, 0f);

        if (hit)
        {
            // Mendapatkan objek yang terkena raycast
            GameObject obj = hit.transform.gameObject;
            GridNodeView gnv = obj.GetComponent<GridNodeView>();

            // Memastikan node adalah valid dan belum diubah sebelumnya
            if (gnv != null && gnv != lastToggledNode)
            {
                //Debug.Log($"Toggling walkable state for node at position: {gnv.Node.Value}");
                ToggleWalkable(gnv);
                lastToggledNode = gnv;
            }
        }
        else
        {
            lastToggledNode = null;
        }
    }

    /// <summary>
    /// Mendeteksi node pada posisi klik mouse dan menetapkannya sebagai tujuan
    /// </summary>
    public void RayCastAndSetDestination()
    {
        // Konversi posisi mouse ke koordinat dunia
        Vector2 rayPos = new Vector2(
          Camera.main.ScreenToWorldPoint(Input.mousePosition).x,
          Camera.main.ScreenToWorldPoint(Input.mousePosition).y);
        // Melakukan raycast untuk mendeteksi objek pada posisi mouse
        RaycastHit2D hit = Physics2D.Raycast(rayPos, Vector2.zero, 0f);

        if (hit)
        {
            // Mendapatkan objek yang terkena raycast
            GameObject obj = hit.transform.gameObject;
            GridNodeView gnv = obj.GetComponent<GridNodeView>();

            // Memindahkan objek destination ke posisi node yang dipilih
            Vector3 pos = destination.position;
            pos.x = gnv.Node.Value.x * gridNodeWidth;
            pos.y = gnv.Node.Value.y * gridNodeHeight;
            destination.position = pos;
        }
    }

    /// <summary>
    /// Mengubah status walkable dari suatu node dan memperbarui warnanya
    /// </summary>
    /// <param name="gnv">GridNodeView yang akan diubah status walkable-nya</param>
    public void ToggleWalkable(GridNodeView gnv)
    {
        if (gnv == null)
            return;

        int x = gnv.Node.Value.x;
        int y = gnv.Node.Value.y;

        // Membalik status walkable node
        gnv.Node.IsWalkable = !gnv.Node.IsWalkable;

        // Memperbarui warna node berdasarkan status walkable-nya
        if (gnv.Node.IsWalkable)
        {
            gnv.SetInnerColor(COLOR_WALKABLE);
        }
        else
        {
            gnv.SetInnerColor(COLOR_NONWALKABLE);
        }
    }

    /// <summary>
    /// Mendeteksi node pada posisi klik mouse dan menetapkannya sebagai posisi NPC
    /// </summary>
    public void RayCastAndSetNPCPosition()
    {
        // Konversi posisi mouse ke koordinat dunia
        Vector2 rayPos = new Vector2(
          Camera.main.ScreenToWorldPoint(Input.mousePosition).x,
          Camera.main.ScreenToWorldPoint(Input.mousePosition).y);
        // Melakukan raycast untuk mendeteksi objek pada posisi mouse
        RaycastHit2D hit = Physics2D.Raycast(rayPos, Vector2.zero, 0f);

        if (hit)
        {
            // Mendapatkan objek yang terkena raycast
            GameObject obj = hit.transform.gameObject;
            GridNodeView gnv = obj.GetComponent<GridNodeView>();

            if (gnv != null && gnv.Node.IsWalkable)
            {
                // Set posisi NPC ke node yang dipilih
                npc.SetStartNode(gnv.Node);
                //Debug.Log($"Setting NPC position to: {gnv.Node.Value}");
            }
        }
    }

    /// <summary>
    /// Update dipanggil setiap frame untuk menangani input pengguna
    /// </summary>
    void Update()
    {
        // Handle camera panning with middle mouse button
        if (Input.GetMouseButton(2)) // Middle mouse button
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            Vector3 moveDirection = new Vector3(-mouseX, -mouseY, 0) * Camera.main.orthographicSize * 0.15f;
            Camera.main.transform.position += moveDirection;
        }

        // Mengubah status walkable node saat Shift + tombol kiri mouse ditekan
        if (Input.GetMouseButton(0))
        {
            Vector2 currentMousePosition = new Vector2(
                Camera.main.ScreenToWorldPoint(Input.mousePosition).x,
                Camera.main.ScreenToWorldPoint(Input.mousePosition).y);

            if (currentMousePosition != lastMousePosition)
            {
                // Menggambar dinding dengan Shift+Left Click
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    RayCastAndToggleWalkable();
                }
                // Set NPC position dengan Left Click biasa
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

        // Menetapkan tujuan baru saat tombol kanan mouse ditekan
        if (Input.GetMouseButtonDown(1))
        {
            RayCastAndSetDestination();
        }

        // Menyesuaikan ukuran kamera dengan scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            // Increase max zoom out to 200 and keep min zoom in at 1
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - scroll * Camera.main.orthographicSize, 1.0f, 200.0f);
        }
    }

    /// <summary>
    /// Mendapatkan GridNode pada koordinat (x,y)
    /// </summary>
    /// <param name="x">Koordinat X</param>
    /// <param name="y">Koordinat Y</param>
    /// <returns>GridNode pada koordinat tersebut atau null jika tidak valid</returns>
    public GridNode GetGridNode(int x, int y)
    {
        if (x >= 0 && x < numX && y >= 0 && y < numY)
        {
            return gridNodeViews[x, y].Node;
        }
        return null;
    }

    /// <summary>
    /// Mendapatkan GridNodeView pada koordinat (x,y)
    /// </summary>
    /// <param name="x">Koordinat X</param>
    /// <param name="y">Koordinat Y</param>
    /// <returns>GridNodeView pada koordinat tersebut atau null jika tidak valid</returns>
    public GridNodeView GetGridNodeView(int x, int y)
    {
        if (x >= 0 && x < numX && y >= 0 && y < numY)
        {
            return gridNodeViews[x, y];
        }
        return null;
    }

    // Berbagai fungsi penghitungan jarak untuk algoritma pathfinding

    /// <summary>
    /// Menghitung jarak Manhattan (jarak grid) antara dua titik
    /// </summary>
    public static float GetManhattanCost(
      Vector2Int a,
      Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    /// <summary>
    /// Menghitung jarak Euclidean (jarak garis lurus) antara dua titik
    /// </summary>
    public static float GetCostBetweenTwoCells(
      Vector2Int a,
      Vector2Int b)
    {
        return Mathf.Sqrt(
          (a.x - b.x) * (a.x - b.x) +
          (a.y - b.y) * (a.y - b.y)
          );
    }

    /// <summary>
    /// Alias untuk GetCostBetweenTwoCells (jarak Euclidean)
    /// </summary>
    public static float GetEuclideanCost(
      Vector2Int a,
      Vector2Int b)
    {
        return GetCostBetweenTwoCells(a, b);
    }

    /// <summary>
    /// Callback yang dipanggil saat algoritma pathfinding mengubah current node
    /// </summary>
    public void OnChangeCurrentNode(PathFinding.PathFinder<Vector2Int>.PathFinderNode node)
    {
        int x = node.Location.Value.x;
        int y = node.Location.Value.y;
        GridNodeView gnv = gridNodeViews[x, y];
        gnv.SetInnerColor(COLOR_CURRENT_NODE);
    }

    /// <summary>
    /// Callback yang dipanggil saat node ditambahkan ke open list dalam algoritma pathfinding
    /// </summary>
    public void OnAddToOpenList(PathFinding.PathFinder<Vector2Int>.PathFinderNode node)
    {
        int x = node.Location.Value.x;
        int y = node.Location.Value.y;
        GridNodeView gnv = gridNodeViews[x, y];
        gnv.SetInnerColor(COLOR_ADD_TO_OPENLIST);
    }

    /// <summary>
    /// Callback yang dipanggil saat node ditambahkan ke closed list dalam algoritma pathfinding
    /// </summary>
    public void OnAddToClosedList(PathFinding.PathFinder<Vector2Int>.PathFinderNode node)
    {
        int x = node.Location.Value.x;
        int y = node.Location.Value.y;
        GridNodeView gnv = gridNodeViews[x, y];
        gnv.SetInnerColor(COLOR_ADD_TO_CLOSEDLIST);
    }

    /// <summary>
    /// Mengatur ulang warna semua node grid ke warna default berdasarkan status walkable-nya
    /// </summary>
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

    /// <summary>
    /// Kelas untuk menyimpan status grid untuk keperluan save/load
    /// </summary>
    [System.Serializable]
    public class GridState
    {
        public bool[,] walkableStates;
    }

    /// <summary>
    /// Menyimpan status walkable dari semua node grid ke file
    /// </summary>
    /// <param name="filePath">Path file untuk menyimpan data</param>
    public void SaveGridState(string filePath)
    {
        try
        {
            // Pastikan direktori ada
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            GridState gridState = new GridState();
            gridState.walkableStates = new bool[numX, numY];

            // Menyimpan status walkable dari setiap node
            for (int i = 0; i < numX; i++)
            {
                for (int j = 0; j < numY; j++)
                {
                    gridState.walkableStates[i, j] = gridNodeViews[i, j].Node.IsWalkable;
                }
            }

            // Mengkonversi ke JSON dan menyimpan ke file
            string json = JsonConvert.SerializeObject(gridState);
            File.WriteAllText(filePath, json);

            //Debug.Log($"Grid state saved to {filePath}");
        }
        catch (System.Exception e)
        {
            //Debug.LogError($"Error saving grid state: {e.Message}");
        }
    }

    /// <summary>
    /// Memuat status walkable dari semua node grid dari file
    /// </summary>
    /// <param name="filePath">Path file untuk memuat data</param>
    public void LoadGridState(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                //Debug.LogError($"Save file not found: {filePath}");
                return;
            }

            // Membaca dan mengkonversi data dari file JSON
            string json = File.ReadAllText(filePath);
            GridState gridState = JsonConvert.DeserializeObject<GridState>(json);

            // Periksa apakah ukuran grid dalam file sesuai dengan grid saat ini
            if (gridState.walkableStates.GetLength(0) != numX ||
                gridState.walkableStates.GetLength(1) != numY)
            {
                //Debug.LogWarning($"Grid size mismatch. File: {gridState.walkableStates.GetLength(0)}x{gridState.walkableStates.GetLength(1)}, Current: {numX}x{numY}. Resizing grid...");
                ResizeGrid(gridState.walkableStates.GetLength(0), gridState.walkableStates.GetLength(1));
            }

            // Menerapkan status walkable ke setiap node
            for (int i = 0; i < numX; i++)
            {
                for (int j = 0; j < numY; j++)
                {
                    gridNodeViews[i, j].Node.IsWalkable = gridState.walkableStates[i, j];
                    gridNodeViews[i, j].SetInnerColor(gridState.walkableStates[i, j] ? COLOR_WALKABLE : COLOR_NONWALKABLE);
                }
            }

            //Debug.Log($"Grid state loaded from {filePath}");
        }
        catch (System.Exception e)
        {
            //Debug.LogError($"Error loading grid state: {e.Message}");
        }
    }

    /// <summary>
    /// Resizes the grid to the specified dimensions
    /// </summary>
    public void ResizeGrid(int newSizeX, int newSizeY)
    {
        // Clean up existing grid
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

        // Update dimensions
        numX = newSizeX;
        numY = newSizeY;

        // Create new grid
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

        // Update camera position
        SetCameraPosition();

        // Reset NPC position if needed
        if (npc != null)
        {
            npc.SetStartNode(gridNodeViews[0, 0].Node);
        }
    }

    /// <summary>
    /// Sets the destination position for pathfinding
    /// </summary>
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

    /// <summary>
    /// Membuat maze dengan algoritma Recursive Backtracking
    /// </summary>
    /// <param name="wallDensity">Kepadatan dinding dalam persen (0-100), mempengaruhi rasio jalur terhadap ruang terbuka</param>
    public void GenerateRandomMaze(float density = 35f)
    {
        // Use the recursive backtracking maze generation
        GenerateRecursiveBacktrackingMaze(density);
    }

    /// <summary>
    /// Membuat maze menggunakan algoritma recursive backtracking dengan kontrol densitas dinding.
    /// </summary>
    /// <param name="density">Persentase dinding dalam maze (0-100)</param>
    public void GenerateRecursiveBacktrackingMaze(float density = 30f)
    {
        // Inisialisasi semua tile sebagai dinding (bukan jalur)
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

        // Hitung jumlah total tile dan target dinding
        int totalTiles = numX * numY;
        int targetWallCount = Mathf.RoundToInt((density / 100f) * totalTiles);

        // Buat sistem random
        System.Random random = new System.Random();

        // Arah: Atas (0), Kanan (1), Bawah (2), Kiri (3)
        int[] dx = { 0, 1, 0, -1 };
        int[] dy = { 1, 0, -1, 0 };

        // Mulai dari posisi acak
        int startX = random.Next(0, numX);
        int startY = random.Next(0, numY);

        // Buat cell awal menjadi jalur
        MakeCellWalkable(startX, startY);

        // Stack untuk recursive backtracking
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(startX, startY));

        // Jalankan recursive backtracking
        while (stack.Count > 0)
        {
            // Ambil posisi saat ini
            Vector2Int current = stack.Peek();

            // Daftar untuk menyimpan arah yang valid (belum dikunjungi)
            List<int> directions = new List<int>();

            // Cek semua arah
            for (int i = 0; i < 4; i++)
            {
                // Periksa tetangga 2 langkah (untuk memastikan kita tidak membuat jalur yang bersebelahan)
                int nx = current.x + dx[i] * 2;
                int ny = current.y + dy[i] * 2;

                // Jika tetangga dalam batas grid dan belum dikunjungi
                if (nx >= 0 && nx < numX && ny >= 0 && ny < numY)
                {
                    GridNode neighborNode = GetGridNode(nx, ny);
                    if (neighborNode != null && !neighborNode.IsWalkable)
                    {
                        // Tambahkan arah ke daftar valid
                        directions.Add(i);
                    }
                }
            }

            // Jika ada arah yang valid
            if (directions.Count > 0)
            {
                // Pilih arah secara acak
                int direction = directions[random.Next(0, directions.Count)];

                // Hitung posisi tetangga dan dinding di antaranya
                int nx = current.x + dx[direction] * 2;
                int ny = current.y + dy[direction] * 2;
                int wallX = current.x + dx[direction];
                int wallY = current.y + dy[direction];

                // Buat jalur di tetangga dan dinding di antaranya
                MakeCellWalkable(nx, ny);
                MakeCellWalkable(wallX, wallY);

                // Tambahkan tetangga ke stack
                stack.Push(new Vector2Int(nx, ny));
            }
            else
            {
                // Tidak ada arah yang valid, backtrack
                stack.Pop();
            }
        }

        // Hitung jumlah dinding saat ini
        int currentWallCount = CountNonWalkableCells();

        // Jika kita memiliki terlalu banyak dinding (kepadatan terlalu tinggi)
        if (currentWallCount > targetWallCount)
        {
            // Hapus dinding secara acak hingga mencapai target
            RemoveRandomWalls(currentWallCount - targetWallCount);
        }
        // Jika kita memiliki terlalu sedikit dinding (kepadatan terlalu rendah)
        else if (currentWallCount < targetWallCount)
        {
            // Tambahkan dinding secara acak hingga mencapai target
            AddRandomWalls(targetWallCount - currentWallCount);
        }

        // Pastikan posisi NPC dan tujuan dapat dilalui
        EnsureNodeAndNeighborsWalkable((int)(npc.transform.position.x / gridNodeWidth),
                                      (int)(npc.transform.position.y / gridNodeHeight));

        EnsureNodeAndNeighborsWalkable((int)(destination.position.x / gridNodeWidth),
                                      (int)(destination.position.y / gridNodeHeight));
    }

    /// <summary>
    /// Menghitung jumlah sel yang tidak dapat dilalui (dinding) di grid
    /// </summary>
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

    /// <summary>
    /// Membuat sebuah sel menjadi dapat dilalui (jalur)
    /// </summary>
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

    /// <summary>
    /// Membuat sebuah sel menjadi tidak dapat dilalui (dinding)
    /// </summary>
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

    /// <summary>
    /// Menghapus dinding secara acak dari maze
    /// </summary>
    private void RemoveRandomWalls(int count)
    {
        System.Random random = new System.Random();

        // Buat daftar semua dinding
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

        // Acak daftarnya
        for (int i = 0; i < walls.Count; i++)
        {
            int j = random.Next(i, walls.Count);
            Vector2Int temp = walls[i];
            walls[i] = walls[j];
            walls[j] = temp;
        }

        // Hapus dinding hingga mencapai target
        for (int i = 0; i < count && i < walls.Count; i++)
        {
            Vector2Int wall = walls[i];
            MakeCellWalkable(wall.x, wall.y);
        }
    }

    /// <summary>
    /// Menambahkan dinding secara acak ke maze
    /// </summary>
    private void AddRandomWalls(int count)
    {
        System.Random random = new System.Random();

        // Buat daftar semua jalur
        List<Vector2Int> paths = new List<Vector2Int>();
        for (int x = 0; x < numX; x++)
        {
            for (int y = 0; y < numY; y++)
            {
                GridNode node = GetGridNode(x, y);
                if (node != null && node.IsWalkable)
                {
                    // Jangan tambahkan dinding di posisi NPC atau tujuan
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

        // Acak daftarnya
        for (int i = 0; i < paths.Count; i++)
        {
            int j = random.Next(i, paths.Count);
            Vector2Int temp = paths[i];
            paths[i] = paths[j];
            paths[j] = temp;
        }

        // Tambahkan dinding hingga mencapai target atau hingga daftar jalur habis
        int added = 0;
        for (int i = 0; i < paths.Count && added < count; i++)
        {
            Vector2Int path = paths[i];

            // Pastikan menambahkan dinding tidak memotong jalur penting
            if (!IsPathCritical(path.x, path.y))
            {
                MakeCellNonWalkable(path.x, path.y);
                added++;
            }
        }
    }

    /// <summary>
    /// Memeriksa apakah sebuah sel merupakan jalur kritis yang tidak boleh diblokir
    /// </summary>
    private bool IsPathCritical(int x, int y)
    {
        // Hindari memblokir jalur satu-satunya
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

        // Jika hanya memiliki satu atau dua tetangga yang dapat dilalui, ini mungkin jalur penting
        return walkableNeighbors <= 2;
    }

    /// <summary>
    /// Memastikan bahwa node dan tetangganya dapat dilalui
    /// </summary>
    private void EnsureNodeAndNeighborsWalkable(int x, int y)
    {
        // Pastikan node utama dapat dilalui
        MakeCellWalkable(x, y);

        // Pastikan setidaknya satu tetangga dapat dilalui agar tidak terjebak
        int[] dx = { 0, 1, 0, -1 };
        int[] dy = { 1, 0, -1, 0 };

        bool hasWalkableNeighbor = false;

        // Cek jika sudah ada tetangga yang dapat dilalui
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

        // Jika tidak ada tetangga yang dapat dilalui, buat salah satu tetangga dapat dilalui
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
}