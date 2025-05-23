using System;
using System.Collections.Generic;

namespace PathFinding
{
    /// <summary>
    /// Enumerasi yang merepresentasikan berbagai status dari PathFinder.
    /// Digunakan untuk melacak progress dari pencarian jalur (pathfinding).
    /// </summary>
    public enum PathFinderStatus
    {
        NOT_INITIALISED, // PathFinder belum diinisialisasi
        SUCCESS,         // Pencarian jalur berhasil menemukan tujuan
        FAILURE,         // Pencarian jalur gagal (tidak ada jalur ditemukan)
        RUNNING,         // Proses pencarian jalur sedang berjalan
    }

    /// <summary>
    /// Kelas abstrak Node yang menjadi dasar untuk semua jenis vertex
    /// yang digunakan dalam algoritma pathfinding.
    /// </summary>
    /// <typeparam name="T">Tipe data nilai yang disimpan dalam node</typeparam>
    abstract public class Node<T>
    {
        /// <summary>
        /// Nilai yang disimpan dalam node
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Konstruktor untuk membuat node baru dengan nilai tertentu
        /// </summary>
        /// <param name="value">Nilai yang akan disimpan dalam node</param>
        public Node(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Mendapatkan daftar tetangga dari node ini.
        /// Metode ini harus diimplementasikan oleh kelas turunan.
        /// </summary>
        /// <returns>Daftar tetangga dari node ini</returns>
        abstract public List<Node<T>> GetNeighbours();
    }

    /// <summary>
    /// Kelas abstrak PathFinder yang menjadi dasar untuk semua algoritma pencarian jalur.
    /// </summary>
    /// <typeparam name="T">Tipe data nilai yang disimpan dalam node</typeparam>
    public abstract class PathFinder<T>
    {
        #region Delegates for Cost Calculation.
        /// <summary>
        /// Delegate untuk menghitung biaya perjalanan antara dua node
        /// </summary>
        /// <param name="a">Node asal</param>
        /// <param name="b">Node tujuan</param>
        /// <returns>Biaya perjalanan dari a ke b</returns>
        public delegate float CostFunction(T a, T b);

        /// <summary>
        /// Mendapatkan jumlah node dalam daftar tertutup (closedList)
        /// </summary>
        public int ClosedListCount => closedList.Count;

        public int OpenListCount => openList.Count;

        /// <summary>
        /// Fungsi untuk menghitung biaya heuristik antara dua node
        /// Digunakan dalam algoritma informasi seperti A* dan Greedy Best-First
        /// </summary>
        public CostFunction HeuristicCost { get; set; }

        /// <summary>
        /// Fungsi untuk menghitung biaya perjalanan antara dua node yang bertetangga
        /// </summary>
        public CostFunction NodeTraversalCost { get; set; }
        #endregion

        #region PathFinderNode
        /// <summary>
        /// Kelas PathFinderNode.
        /// Merepresentasikan node dalam proses pencarian jalur.
        /// Node ini mengenkapsulasi Node<T> dan informasi tambahan untuk algoritma pencarian jalur.
        /// </summary>
        public class PathFinderNode : System.IComparable<PathFinderNode>
        {
            /// <summary>
            /// Node induk dalam jalur pencarian
            /// </summary>
            public PathFinderNode Parent { get; set; }

            /// <summary>
            /// Lokasi node dalam struktur graph
            /// </summary>
            public Node<T> Location { get; private set; }

            /// <summary>
            /// Referensi ke peta grid
            /// </summary>
            public GridMap Map { get; set; }

            /// <summary>
            /// Total biaya (F = G + H)
            /// </summary>
            public float FCost { get; private set; }

            /// <summary>
            /// Biaya dari node awal ke node saat ini (cost so far)
            /// </summary>
            public float GCost { get; private set; }

            /// <summary>
            /// Biaya heuristik dari node saat ini ke tujuan (estimated cost)
            /// </summary>
            public float HCost { get; private set; }

            /// <summary>
            /// Konstruktor untuk PathFinderNode
            /// </summary>
            /// <param name="location">Lokasi node dalam struktur graph</param>
            /// <param name="parent">Node induk dalam jalur pencarian</param>
            /// <param name="gCost">Biaya dari node awal ke node saat ini</param>
            /// <param name="hCost">Biaya heuristik dari node saat ini ke tujuan</param>
            public PathFinderNode(Node<T> location,
                                  PathFinderNode parent,
                                  float gCost,
                                  float hCost)
            {
                Location = location;
                Parent = parent;
                HCost = hCost;
                SetGCost(gCost);
            }

            /// <summary>
            /// Mengatur biaya G dan menghitung ulang biaya F
            /// </summary>
            /// <param name="c">Nilai baru untuk biaya G</param>
            public void SetGCost(float c)
            {
                GCost = c;
                FCost = GCost + HCost;
            }

            /// <summary>
            /// Mengatur biaya H dan menghitung ulang biaya F
            /// </summary>
            /// <param name="h">Nilai baru untuk biaya H</param>
            public void SetHCost(float h)
            {
                HCost = h;
                FCost = GCost + HCost;
            }

            /// <summary>
            /// Membandingkan node berdasarkan biaya F
            /// Digunakan untuk menyortir prioritas node dalam pencarian
            /// </summary>
            /// <param name="other">Node lain yang dibandingkan</param>
            /// <returns>Hasil perbandingan nilai FCost</returns>
            public int CompareTo(PathFinderNode other)
            {
                if (other == null) return 1;
                return FCost.CompareTo(other.FCost);
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Status saat ini dari pathfinder
        /// Nilai default-nya adalah NOT_INITIALISED
        /// </summary>
        public PathFinderStatus Status
        {
            get;
            protected set;
        } = PathFinderStatus.NOT_INITIALISED;

        /// <summary>
        /// Node awal pencarian jalur
        /// </summary>
        public Node<T> Start { get; protected set; }

        /// <summary>
        /// Node tujuan pencarian jalur
        /// </summary>
        public Node<T> Goal { get; protected set; }

        /// <summary>
        /// Node yang sedang diproses saat ini oleh pathfinder
        /// </summary>
        public PathFinderNode CurrentNode { get; protected set; }

        /// <summary>
        /// Referensi ke peta grid yang digunakan untuk pencarian jalur
        /// </summary>
        public GridMap Map { get; internal set; }
        #endregion

        #region Open and Closed Lists and Associated Functions.
        /// <summary>
        /// Daftar node yang belum diperiksa (open list)
        /// Node dalam daftar ini akan diproses di langkah berikutnya
        /// </summary>
        protected List<PathFinderNode> openList =
          new List<PathFinderNode>();

        /// <summary>
        /// Daftar node yang sudah diperiksa (closed list)
        /// Node dalam daftar ini sudah dievaluasi
        /// </summary>
        protected List<PathFinderNode> closedList =
          new List<PathFinderNode>();

        /// <summary>
        /// Mendapatkan node dengan biaya terendah dari suatu daftar
        /// Digunakan untuk memilih node berikutnya yang akan diperiksa
        /// </summary>
        /// <param name="myList">Daftar node yang akan diperiksa</param>
        /// <returns>Node dengan biaya terendah</returns>
        protected PathFinderNode GetLeastCostNode(
          List<PathFinderNode> myList)
        {
            int best_index = 0;
            float best_priority = myList[0].FCost;
            for (int i = 1; i < myList.Count; i++)
            {
                if (best_priority > myList[i].FCost)
                {
                    best_priority = myList[i].FCost;
                    best_index = i;
                }
            }
            PathFinderNode n = myList[best_index];
            return n;
        }

        /// <summary>
        /// Memeriksa apakah suatu cell (nilai T) ada dalam daftar node
        /// </summary>
        /// <param name="myList">Daftar node yang akan diperiksa</param>
        /// <param name="cell">Cell yang dicari</param>
        /// <returns>Indeks cell dalam daftar jika ditemukan, -1 jika tidak</returns>
        protected int IsInList(List<PathFinderNode> myList, T cell)
        {
            for (int i = 0; i < myList.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(myList[i].Location.Value, cell))
                    return i;
            }
            return -1;
        }
        #endregion

        #region Delegates for Action Callbacks
        /// <summary>
        /// Delegate untuk menangani event terkait node selama proses pathfinding
        /// Dapat digunakan untuk visualisasi atau debugging
        /// </summary>
        /// <param name="node">Node yang terlibat dalam event</param>
        public delegate void DelegatePathFinderNode(PathFinderNode node);

        /// <summary>
        /// Event dipanggil ketika current node berubah
        /// </summary>
        public DelegatePathFinderNode onChangeCurrentNode;

        /// <summary>
        /// Event dipanggil ketika node ditambahkan ke open list
        /// </summary>
        public DelegatePathFinderNode onAddToOpenList;

        /// <summary>
        /// Event dipanggil ketika node ditambahkan ke closed list
        /// </summary>
        public DelegatePathFinderNode onAddToClosedList;

        /// <summary>
        /// Event dipanggil ketika node tujuan ditemukan
        /// </summary>
        public DelegatePathFinderNode onDestinationFound;

        /// <summary>
        /// Delegate untuk menangani event tanpa parameter
        /// </summary>
        public delegate void DelegateNoArguments();

        /// <summary>
        /// Event dipanggil ketika pencarian jalur dimulai
        /// </summary>
        public DelegateNoArguments onStarted;

        /// <summary>
        /// Event dipanggil ketika pencarian jalur sedang berjalan
        /// </summary>
        public DelegateNoArguments onRunning;

        /// <summary>
        /// Event dipanggil ketika pencarian jalur gagal
        /// </summary>
        public DelegateNoArguments onFailure;

        /// <summary>
        /// Event dipanggil ketika pencarian jalur berhasil
        /// </summary>
        public DelegateNoArguments onSuccess;
        #endregion

        #region Pathfinding Search Related Functions
        /// <summary>
        /// Mereset variabel internal untuk pencarian baru
        /// </summary>
        public virtual void Reset()
        {
            if (Status == PathFinderStatus.RUNNING)
            {
                // Tidak bisa reset karena pathfinding sedang berlangsung
                return;
            }

            CurrentNode = null;
            openList.Clear();
            closedList.Clear();

            Status = PathFinderStatus.NOT_INITIALISED;
        }

        /// <summary>
        /// Melakukan satu langkah pencarian jalur
        /// Harus dipanggil berulang kali sampai Status menjadi SUCCESS atau FAILURE
        /// </summary>
        /// <returns>Status pathfinder setelah langkah ini selesai</returns>
        public virtual PathFinderStatus Step()
        {
            closedList.Add(CurrentNode);
            onAddToClosedList?.Invoke(CurrentNode);

            if (openList.Count == 0)
            {
                // Pencarian telah selesai tanpa menemukan jalur
                Status = PathFinderStatus.FAILURE;
                onFailure?.Invoke();
                return Status;
            }

            // Dapatkan node dengan biaya terendah dari openList
            CurrentNode = GetLeastCostNode(openList);

            onChangeCurrentNode?.Invoke(CurrentNode);

            openList.Remove(CurrentNode);

            // Periksa apakah node ini mengandung cell tujuan
            if (EqualityComparer<T>.Default.Equals(
              CurrentNode.Location.Value, Goal.Value))
            {
                Status = PathFinderStatus.SUCCESS;
                onDestinationFound?.Invoke(CurrentNode);
                onSuccess?.Invoke();
                return Status;
            }

            // Dapatkan tetangga dari node saat ini
            List<Node<T>> neighbours = CurrentNode.Location.GetNeighbours();

            // Proses setiap tetangga untuk kemungkinan ekspansi pencarian
            foreach (Node<T> cell in neighbours)
            {
                AlgorithmSpecificImplementation(cell);
            }

            Status = PathFinderStatus.RUNNING;
            onRunning?.Invoke();
            return Status;
        }

        /// <summary>
        /// Implementasi algoritma spesifik untuk memproses node tetangga
        /// Metode ini akan diimplementasikan berbeda untuk setiap algoritma pathfinding
        /// </summary>
        /// <param name="cell">Node tetangga yang akan diproses</param>
        abstract protected void AlgorithmSpecificImplementation(Node<T> cell);

        /// <summary>
        /// Inisialisasi pathfinder dengan node awal dan tujuan
        /// </summary>
        /// <param name="start">Node awal pencarian</param>
        /// <param name="goal">Node tujuan pencarian</param>
        /// <returns>True jika inisialisasi berhasil, False jika gagal</returns>
        public virtual bool Initialise(Node<T> start, Node<T> goal)
        {
            if (Status == PathFinderStatus.RUNNING)
            {
                // Pathfinding sedang berlangsung, tidak bisa diinisialisasi ulang
                return false;
            }

            Reset();

            Start = start;
            Goal = goal;

            // Deteksi dini jika start dan goal sama
            if (EqualityComparer<T>.Default.Equals(Start.Value, Goal.Value))
            {
                // Pada kasus start=goal, tidak perlu menjalankan algoritma pencarian
                // Cukup atur CurrentNode dan Status secara langsung
                CurrentNode = new PathFinderNode(Start, null, 0.0f, 0.0f);

                // Panggil callback sesuai urutan normal
                onChangeCurrentNode?.Invoke(CurrentNode);
                onStarted?.Invoke();
                onDestinationFound?.Invoke(CurrentNode);

                // Atur status ke SUCCESS
                Status = PathFinderStatus.SUCCESS;
                onSuccess?.Invoke();

                return true;
            }

            float H = HeuristicCost(Start.Value, Goal.Value);

            PathFinderNode root = new PathFinderNode(Start, null, 0.0f, H);

            openList.Add(root);
            onAddToOpenList?.Invoke(root);

            CurrentNode = root;

            onChangeCurrentNode?.Invoke(CurrentNode);
            onStarted?.Invoke();

            Status = PathFinderStatus.RUNNING;

            return true;
        }
        #endregion
    }

    #region Priority Queue
    /// <summary>
    /// Memprioritaskan item berdasarkan nilai komparatif mereka
    /// </summary>
    /// <typeparam name="T">Tipe item dalam antrian prioritas</typeparam>
    public class PriorityQueue<T> where T : IComparable<T>
    {
        /// <summary>
        /// Data yang disimpan dalam priority queue
        /// </summary>
        private List<T> data;

        /// <summary>
        /// Pembanding untuk menentukan prioritas item
        /// </summary>
        private IComparer<T> comparer;

        /// <summary>
        /// Menyimpan indeks setiap elemen dalam data untuk akses cepat
        /// </summary>
        private Dictionary<T, int> elementIndexMap;

        // Cache untuk optimasi
        private T _lastDequeued;
        private int _count;

        /// <summary>
        /// Konstruktor default menggunakan pembanding default untuk tipe T
        /// </summary>
        public PriorityQueue() : this(Comparer<T>.Default) { }

        /// <summary>
        /// Konstruktor dengan custom comparer
        /// </summary>
        /// <param name="comparer">Pembanding untuk menentukan prioritas item</param>
        public PriorityQueue(IComparer<T> comparer)
        {
            this.data = new List<T>();
            this.comparer = comparer;
            this.elementIndexMap = new Dictionary<T, int>();
            this._count = 0;
        }

        /// <summary>
        /// Menambahkan item ke dalam antrian prioritas
        /// </summary>
        /// <param name="item">Item yang akan ditambahkan</param>
        public void Enqueue(T item)
        {
            data.Add(item);
            int childIndex = data.Count - 1;
            elementIndexMap[item] = childIndex;
            HeapifyUp(childIndex);
            _count = data.Count;
        }

        /// <summary>
        /// Mengambil dan menghapus item dengan prioritas tertinggi
        /// </summary>
        /// <returns>Item dengan prioritas tertinggi</returns>
        public T Dequeue()
        {
            if (data.Count == 0)
                throw new InvalidOperationException("The priority queue is empty.");

            int lastIndex = data.Count - 1;
            T frontItem = data[0];
            _lastDequeued = frontItem;

            data[0] = data[lastIndex];
            data.RemoveAt(lastIndex);
            elementIndexMap.Remove(frontItem);

            if (data.Count > 0)
            {
                elementIndexMap[data[0]] = 0;
                HeapifyDown(0);
            }

            _count = data.Count;
            return frontItem;
        }

        /// <summary>
        /// Menghapus item tertentu dari antrian
        /// </summary>
        /// <param name="item">Item yang akan dihapus</param>
        /// <returns>True jika berhasil dihapus, False jika tidak</returns>
        public bool Remove(T item)
        {
            if (!elementIndexMap.TryGetValue(item, out int index))
                return false;

            int lastIndex = data.Count - 1;

            if (index == lastIndex)
            {
                // Item yang dihapus adalah item terakhir
                data.RemoveAt(lastIndex);
                elementIndexMap.Remove(item);
                _count = data.Count;
                return true;
            }

            data[index] = data[lastIndex];
            data.RemoveAt(lastIndex);
            elementIndexMap.Remove(item);

            if (index < data.Count)
            {
                elementIndexMap[data[index]] = index;

                // Tentukan apakah perlu heapify up atau down
                int parentIndex = (index - 1) / 2;
                if (index > 0 && comparer.Compare(data[index], data[parentIndex]) < 0)
                    HeapifyUp(index);
                else
                    HeapifyDown(index);
            }

            _count = data.Count;
            return true;
        }

        /// <summary>
        /// Memperbarui prioritas item dalam antrian
        /// </summary>
        /// <param name="item">Item yang akan diperbarui</param>
        /// <param name="newPriority">Nilai prioritas baru</param>
        public void UpdatePriority(T item, float newPriority)
        {
            // Fast check - jika item adalah yang terakhir dihapus, jangan lakukan apa-apa
            if (_lastDequeued != null && EqualityComparer<T>.Default.Equals(item, _lastDequeued))
                return;

            if (!elementIndexMap.TryGetValue(item, out int index))
                return;

            // Lakukan heapify - lebih efisien untuk A* pada grid kecil
            int parentIndex = (index - 1) / 2;
            if (index > 0 && comparer.Compare(data[index], data[parentIndex]) < 0)
                HeapifyUp(index);
            else
                HeapifyDown(index);
        }

        /// <summary>
        /// Menjaga sifat heap dengan pergerakan ke atas
        /// </summary>
        /// <param name="index">Indeks node yang akan diperbaiki posisinya</param>
        private void HeapifyUp(int index)
        {
            // Iteratif lebih cepat daripada rekursif untuk grid kecil
            int parentIndex = (index - 1) / 2;
            while (index > 0 && comparer.Compare(data[index], data[parentIndex]) < 0)
            {
                Swap(index, parentIndex);
                index = parentIndex;
                parentIndex = (index - 1) / 2;
            }
        }

        /// <summary>
        /// Menjaga sifat heap dengan pergerakan ke bawah
        /// </summary>
        /// <param name="index">Indeks node yang akan diperbaiki posisinya</param>
        private void HeapifyDown(int index)
        {
            int lastIndex = data.Count - 1;
            while (true)
            {
                int leftChildIndex = 2 * index + 1;
                if (leftChildIndex > lastIndex) break;

                int rightChildIndex = leftChildIndex + 1;
                int smallestChildIndex = leftChildIndex;

                // Fast check untuk rightChildIndex
                if (rightChildIndex <= lastIndex && comparer.Compare(data[rightChildIndex], data[leftChildIndex]) < 0)
                    smallestChildIndex = rightChildIndex;

                if (comparer.Compare(data[index], data[smallestChildIndex]) <= 0) break;

                Swap(index, smallestChildIndex);
                index = smallestChildIndex;
            }
        }

        /// <summary>
        /// Menukar posisi dua item dalam heap
        /// </summary>
        /// <param name="index1">Indeks item pertama</param>
        /// <param name="index2">Indeks item kedua</param>
        private void Swap(int index1, int index2)
        {
            T tmp = data[index1];
            data[index1] = data[index2];
            data[index2] = tmp;
            elementIndexMap[data[index1]] = index1;
            elementIndexMap[data[index2]] = index2;
        }

        /// <summary>
        /// Jumlah item dalam antrian
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Mendapatkan enumerator untuk data
        /// </summary>
        /// <returns>Enumerator untuk data</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return data.GetEnumerator();
        }
    }
    #endregion

    #region Dijkstra Implementation
    /// <summary>
    /// Implementasi algoritma Dijkstra yang melakukan pencarian secara merata
    /// ke semua arah untuk menemukan jalur terpendek
    /// </summary>
    /// <typeparam name="T">Tipe data nilai yang disimpan dalam node</typeparam>
    public class DijkstraPathFinder<T> : PathFinder<T>
    {
        /// <summary>
        /// HashSet untuk pemeriksaan closed list dengan cepat (O(1) complexity)
        /// </summary>
        private HashSet<T> closedSet;

        /// <summary>
        /// Dictionary untuk mengakses node di open list dengan cepat berdasarkan nilai mereka
        /// </summary>
        private Dictionary<T, PathFinderNode> openListMap;

        // Flag untuk mengontrol level optimisasi berdasarkan ukuran grid
        private bool isGridLarge = false;
        private int estimatedNodesCount = 0;

        /// <summary>
        /// Constructor baru dengan estimasi jumlah node
        /// </summary>
        public DijkstraPathFinder(int estimatedNodeCount = 0)
        {
            // Estimasi ukuran grid untuk optimisasi memory
            this.estimatedNodesCount = estimatedNodeCount;

            // Tentukan kapasitas awal berdasarkan ukuran grid
            int initialCapacity = estimatedNodesCount > 0 ?
                Math.Min(estimatedNodesCount / 4, 256) : 16;

            // Grid dianggap besar jika memiliki > 2500 node (50x50)
            isGridLarge = estimatedNodesCount > 2500;

            // Alokasi dengan kapasitas yang sesuai
            closedSet = new HashSet<T>(initialCapacity);
            openListMap = new Dictionary<T, PathFinderNode>(initialCapacity);
        }

        /// <summary>
        /// Implementasi spesifik algoritma Dijkstra untuk memproses node tetangga
        /// </summary>
        /// <param name="cell">Node tetangga yang akan diproses</param>
        protected override void AlgorithmSpecificImplementation(Node<T> cell)
        {
            // Pemeriksaan O(1) dengan HashSet
            // Melakukan pemeriksaan apakah node sudah ada di closed list
            if (!closedSet.Contains(cell.Value))
            {
                float G = CurrentNode.GCost + NodeTraversalCost(
                  CurrentNode.Location.Value, cell.Value);

                // Biaya heuristik untuk Dijkstra adalah 0
                float H = 0.0f;

                // Pemeriksaan O(1) dengan Dictionary
                // Melakukan pemeriksaan apakah node sudah ada di open list
                if (!openListMap.TryGetValue(cell.Value, out PathFinderNode existingNode))
                {
                    PathFinderNode n = new PathFinderNode(cell, CurrentNode, G, H);
                    openList.Add(n);
                    openListMap[cell.Value] = n;
                    onAddToOpenList?.Invoke(n);
                }
                else
                {
                    float oldG = existingNode.GCost;
                    if (G < oldG)
                    {
                        existingNode.Parent = CurrentNode;
                        existingNode.SetGCost(G);
                        onAddToOpenList?.Invoke(existingNode);
                    }
                }
            }
        }

        /// <summary>
        /// Melakukan satu langkah pencarian jalur dengan algoritma Dijkstra
        /// </summary>
        /// <returns>Status pathfinder setelah langkah ini selesai</returns>
        public override PathFinderStatus Step()
        {
            if (CurrentNode == null)
            {
                if (openList.Count == 0)
                {
                    Status = PathFinderStatus.FAILURE;
                    onFailure?.Invoke();
                    return Status;
                }

                CurrentNode = GetLeastCostNode(openList);
                openList.Remove(CurrentNode);
                openListMap.Remove(CurrentNode.Location.Value);
                onChangeCurrentNode?.Invoke(CurrentNode);
            }

            // Pindahkan pemeriksaan duplikasi ke sini
            if (!closedSet.Contains(CurrentNode.Location.Value))
            {
                closedList.Add(CurrentNode);
                closedSet.Add(CurrentNode.Location.Value);
                onAddToClosedList?.Invoke(CurrentNode);
            }

            if (EqualityComparer<T>.Default.Equals(CurrentNode.Location.Value, Goal.Value))
            {
                Status = PathFinderStatus.SUCCESS;
                onDestinationFound?.Invoke(CurrentNode);
                onSuccess?.Invoke();
                return Status;
            }

            List<Node<T>> neighbours = CurrentNode.Location.GetNeighbours();
            foreach (Node<T> cell in neighbours)
            {
                AlgorithmSpecificImplementation(cell);
            }

            if (openList.Count == 0)
            {
                Status = PathFinderStatus.FAILURE;
                onFailure?.Invoke();
                return Status;
            }

            CurrentNode = GetLeastCostNode(openList);
            openList.Remove(CurrentNode);
            openListMap.Remove(CurrentNode.Location.Value);
            onChangeCurrentNode?.Invoke(CurrentNode);

            Status = PathFinderStatus.RUNNING;
            onRunning?.Invoke();
            return Status;
        }

        /// <summary>
        /// Reset state pathfinder
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            closedSet.Clear();
            openListMap.Clear();
        }
    }
    #endregion

    #region A* Implementation
    /// <summary>
    /// Implementasi algoritma A* (A-Star) yang menggunakan informasi heuristik
    /// untuk menemukan jalur terpendek dari titik awal ke titik akhir
    /// </summary>
    /// <typeparam name="T">Tipe data nilai yang disimpan dalam node</typeparam>
    public class AStarPathFinder<T> : PathFinder<T>
    {
        /// <summary>
        /// Open list diimplementasikan sebagai priority queue untuk efisiensi
        /// </summary>
        private new PriorityQueue<PathFinderNode> openList;

        /// <summary>
        /// Peta untuk mengakses node dalam open list dengan cepat
        /// </summary>
        private Dictionary<T, PathFinderNode> openListMap;

        /// <summary>
        /// HashSet untuk memeriksa closed list dengan cepat (O(1) complexity)
        /// </summary>
        private HashSet<T> closedSet;

        // Flag untuk batch processing - hanya diaktifkan untuk grid besar
        private bool processingBatch = false;
        private List<Node<T>> neighborBatch;

        // Flag untuk mengontrol level optimisasi
        private bool isGridLarge = false;
        private int estimatedNodesCount = 0;

        /// <summary>
        /// Constructor baru dengan estimasi jumlah node
        /// </summary>
        public AStarPathFinder(int estimatedNodeCount = 0)
        {
            // Estimasi ukuran grid untuk optimisasi memory
            this.estimatedNodesCount = estimatedNodeCount;

            // Tentukan kapasitas awal berdasarkan ukuran grid
            int initialCapacity = estimatedNodesCount > 0 ?
                Math.Min(estimatedNodesCount / 4, 256) : 16;

            // Grid dianggap besar jika memiliki > 2500 node (50x50)
            isGridLarge = estimatedNodesCount > 2500;

            // Alokasi dengan kapasitas yang sesuai
            openList = new PriorityQueue<PathFinderNode>(new FCostComparer());
            openListMap = new Dictionary<T, PathFinderNode>(initialCapacity);
            closedSet = new HashSet<T>(initialCapacity);

            // Alokasi neighborBatch hanya jika diperlukan
            if (isGridLarge)
            {
                neighborBatch = new List<Node<T>>(8);
            }
            else
            {
                neighborBatch = new List<Node<T>>(4); // Lebih kecil untuk grid kecil
            }
        }

        /// <summary>
        /// Implementasi spesifik algoritma A* untuk memproses node tetangga
        /// </summary>
        /// <param name="cell">Node tetangga yang akan diproses</param>
        protected override void AlgorithmSpecificImplementation(Node<T> cell)
        {
            // Fast-reject: node sudah di closed list
            if (closedSet.Contains(cell.Value))
                return;

            // Hitung biaya G yang sebenarnya (jarak dari start)
            float G = CurrentNode.GCost + NodeTraversalCost(CurrentNode.Location.Value, cell.Value);

            // Periksa apakah node sudah ada di open list
            PathFinderNode existingNode = null;
            bool nodeExists = openListMap.TryGetValue(cell.Value, out existingNode);

            if (!nodeExists)
            {
                // Hitung heuristik dengan normal
                float H = HeuristicCost(cell.Value, Goal.Value);

                // Buat node baru dan tambahkan ke open list
                PathFinderNode n = new PathFinderNode(cell, CurrentNode, G, H);
                openList.Enqueue(n);
                openListMap[cell.Value] = n;

                // Callback hanya jika tidak dalam batch processing
                if (!processingBatch || !isGridLarge)
                    onAddToOpenList?.Invoke(n);
            }
            else if (G < existingNode.GCost)
            {
                // Path lebih baik ditemukan, update nilai G
                existingNode.Parent = CurrentNode;
                existingNode.SetGCost(G);

                // Jika kita menggunakan G sebagai tie-breaker, maka perlu update prioritas
                // karena G yang lebih rendah sekarang bisa mempengaruhi urutan priority queue
                openList.UpdatePriority(existingNode, existingNode.HCost);

                // Callback untuk UI
                if ((!processingBatch || !isGridLarge) && onAddToOpenList != null)
                    onAddToOpenList.Invoke(existingNode);
            }
        }

        /// <summary>
        /// Melakukan satu langkah pencarian jalur dengan algoritma A*
        /// </summary>
        /// <returns>Status pathfinder setelah langkah ini selesai</returns>
        public override PathFinderStatus Step()
        {
            if (CurrentNode == null)
            {
                if (openList.Count == 0)
                {
                    Status = PathFinderStatus.FAILURE;
                    onFailure?.Invoke();
                    return Status;
                }

                CurrentNode = openList.Dequeue();
                openListMap.Remove(CurrentNode.Location.Value);
                onChangeCurrentNode?.Invoke(CurrentNode);
            }

            if (EqualityComparer<T>.Default.Equals(CurrentNode.Location.Value, Goal.Value))
            {
                Status = PathFinderStatus.SUCCESS;
                onDestinationFound?.Invoke(CurrentNode);
                onSuccess?.Invoke();
                return Status;
            }

            closedList.Add(CurrentNode);
            closedSet.Add(CurrentNode.Location.Value); // Tambahkan ke hashset untuk pemeriksaan O(1)
            onAddToClosedList?.Invoke(CurrentNode);

            // Ambil semua tetangga
            List<Node<T>> neighbors;

            // Untuk grid besar, gunakan batch processing
            if (isGridLarge)
            {
                neighborBatch.Clear();
                neighborBatch.AddRange(CurrentNode.Location.GetNeighbours());
                neighbors = neighborBatch;

                // Gunakan batch hanya jika cukup banyak tetangga dan grid besar
                processingBatch = neighbors.Count > 5;
            }
            else
            {
                // Untuk grid kecil, tidak perlu batch processing
                neighbors = CurrentNode.Location.GetNeighbours();
                processingBatch = false;
            }

            // Proses semua tetangga
            foreach (Node<T> cell in neighbors)
            {
                AlgorithmSpecificImplementation(cell);
            }

            // Jika dalam batch mode, invoke callback sekali untuk semua tetangga
            if (processingBatch && onAddToOpenList != null && isGridLarge)
            {
                // Notify UI untuk refresh (optional)
                onAddToOpenList.Invoke(CurrentNode);
                processingBatch = false;
            }

            if (openList.Count == 0)
            {
                Status = PathFinderStatus.FAILURE;
                onFailure?.Invoke();
                return Status;
            }

            CurrentNode = openList.Dequeue();
            openListMap.Remove(CurrentNode.Location.Value);
            onChangeCurrentNode?.Invoke(CurrentNode);

            Status = PathFinderStatus.RUNNING;
            onRunning?.Invoke();
            return Status;
        }

        /// <summary>
        /// Reset state pathfinder
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            openListMap.Clear();
            closedSet.Clear();
            if (isGridLarge && neighborBatch != null)
                neighborBatch.Clear();
            processingBatch = false;
        }

        /// <summary>
        /// Pembanding F-Cost untuk priority queue dalam algoritma A*
        /// </summary>
        private class FCostComparer : IComparer<PathFinderNode>
        {
            /// <summary>
            /// Membandingkan dua node berdasarkan FCost mereka
            /// Jika FCost sama, gunakan HCost sebagai tie-breaker
            /// </summary>
            /// <param name="x">Node pertama</param>
            /// <param name="y">Node kedua</param>
            /// <returns>Hasil perbandingan</returns>
            public int Compare(PathFinderNode x, PathFinderNode y)
            {
                int result = x.FCost.CompareTo(y.FCost);
                if (result == 0)
                {
                    result = x.HCost.CompareTo(y.HCost); // Tie-breaking dengan H cost
                }
                return result;
            }
        }
    }
    #endregion

    #region Greedy Best-First Search
    /// <summary>
    /// Implementasi algoritma Greedy Best-First Search
    /// Algoritma ini hanya mempertimbangkan biaya heuristik (H) ke tujuan
    /// </summary>
    /// <typeparam name="T">Tipe data nilai yang disimpan dalam node</typeparam>
    public class GreedyPathFinder<T> : PathFinder<T>
    {
        /// <summary>
        /// Open list diimplementasikan sebagai priority queue untuk performa yang lebih baik
        /// </summary>
        private new PriorityQueue<PathFinderNode> openList = new PriorityQueue<PathFinderNode>(new HeuristicComparer());

        /// <summary>
        /// Dictionary untuk akses node open list yang cepat
        /// </summary>
        private Dictionary<T, PathFinderNode> openSet = new Dictionary<T, PathFinderNode>(256);

        /// <summary>
        /// HashSet untuk pemeriksaan closed list O(1)
        /// </summary>
        private HashSet<T> closedSet = new HashSet<T>(256);

        // Flag untuk batch processing
        private bool processingBatch = false;
        private List<Node<T>> neighborBatch = new List<Node<T>>(4);

        /// <summary>
        /// Implementasi spesifik algoritma Greedy untuk memproses node tetangga
        /// </summary>
        /// <param name="cell">Node tetangga yang akan diproses</param>
        protected override void AlgorithmSpecificImplementation(Node<T> cell)
        {
            // Fast-reject: node sudah diproses
            if (closedSet.Contains(cell.Value))
                return;

            // Hitung biaya G yang sebenarnya (jarak dari start)
            float G = CurrentNode.GCost + NodeTraversalCost(CurrentNode.Location.Value, cell.Value);
            float H;

            // Periksa apakah node sudah ada di open list
            PathFinderNode existingNode = null;
            bool nodeExists = openSet.TryGetValue(cell.Value, out existingNode);

            if (!nodeExists)
            {
                // Hitung heuristik dengan normal

                if (EqualityComparer<T>.Default.Equals(cell.Value, Goal.Value))
                {
                    // Langsung ke tujuan - prioritaskan dengan H = 0
                    H = 0;
                }
                else
                {
                    H = HeuristicCost(cell.Value, Goal.Value);
                }

                // Buat node dan tambahkan ke open list - gunakan G yang benar
                PathFinderNode n = new PathFinderNode(cell, CurrentNode, G, H);
                openList.Enqueue(n);
                onAddToOpenList?.Invoke(n);
                openSet[cell.Value] = n;

                //if (!processingBatch && onAddToOpenList != null)
                //    onAddToOpenList.Invoke(n);
            }
            else if (G < existingNode.GCost)
            {
                // Path lebih baik ditemukan, update nilai G
                // Meskipun Greedy hanya menggunakan H untuk prioritas,
                // G cost yang benar tetap penting untuk rekonstruksi jalur
                existingNode.Parent = CurrentNode;
                existingNode.SetGCost(G);

                // Jika kita menggunakan G sebagai tie-breaker, maka perlu update prioritas
                // karena G yang lebih rendah sekarang bisa mempengaruhi urutan priority queue
                openList.UpdatePriority(existingNode, existingNode.HCost);
                onAddToOpenList.Invoke(existingNode);

                // Callback untuk UI
                //if (!processingBatch && onAddToOpenList != null)

            }
        }

        /// <summary>
        /// Melakukan satu langkah pencarian jalur dengan algoritma Greedy
        /// </summary>
        /// <returns>Status pathfinder setelah langkah ini selesai</returns>
        public override PathFinderStatus Step()
        {
            if (CurrentNode == null)
            {
                if (openList.Count == 0)
                {
                    Status = PathFinderStatus.FAILURE;
                    onFailure?.Invoke();
                    return Status;
                }

                CurrentNode = openList.Dequeue();
                openSet.Remove(CurrentNode.Location.Value);
                onChangeCurrentNode?.Invoke(CurrentNode);
            }

            closedList.Add(CurrentNode);
            closedSet.Add(CurrentNode.Location.Value);
            onAddToClosedList?.Invoke(CurrentNode);

            if (EqualityComparer<T>.Default.Equals(CurrentNode.Location.Value, Goal.Value))
            {
                Status = PathFinderStatus.SUCCESS;
                onDestinationFound?.Invoke(CurrentNode);
                onSuccess?.Invoke();
                return Status;
            }

            // Dapatkan semua tetangga sekaligus untuk mengurangi panggilan fungsi
            neighborBatch.Clear();
            neighborBatch.AddRange(CurrentNode.Location.GetNeighbours());

            // Proses tetangga dalam batch untuk mengurangi callback overhead
            // processingBatch = neighborBatch.Count > 5; // Gunakan batch hanya jika cukup banyak tetangga

            foreach (Node<T> cell in neighborBatch)
            {
                AlgorithmSpecificImplementation(cell);
            }

            // Jika dalam batch mode, invoke callback sekali untuk semua tetangga
            if (processingBatch && onAddToOpenList != null)
            {
                // Notify UI untuk refresh (optional)
                onAddToOpenList.Invoke(CurrentNode);
                processingBatch = false;
            }

            if (openList.Count == 0)
            {
                Status = PathFinderStatus.FAILURE;
                onFailure?.Invoke();
                return Status;
            }

            CurrentNode = openList.Dequeue();
            openSet.Remove(CurrentNode.Location.Value);
            onChangeCurrentNode?.Invoke(CurrentNode);

            Status = PathFinderStatus.RUNNING;
            onRunning?.Invoke();
            return Status;
        }

        /// <summary>
        /// Reset state pathfinder
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            openSet.Clear();
            closedSet.Clear();
            neighborBatch.Clear();
            processingBatch = false;
        }

        /// <summary>
        /// Pembanding Heuristik untuk priority queue dalam algoritma Greedy
        /// </summary>
        private class HeuristicComparer : IComparer<PathFinderNode>
        {
            /// <summary>
            /// Membandingkan dua node berdasarkan nilai heuristik mereka.
            /// PENTING: Ini adalah kunci perbedaan dari algoritma Greedy - prioritas
            /// hanya didasarkan pada nilai H cost (jarak ke tujuan), tidak seperti
            /// A* yang menggunakan FCost (G+H). G cost tetap dilacak untuk 
            /// rekonstruksi jalur yang benar, tetapi tidak memengaruhi prioritas.
            /// </summary>
            /// <param name="x">Node pertama</param>
            /// <param name="y">Node kedua</param>
            /// <returns>Hasil perbandingan</returns>
            public int Compare(PathFinderNode x, PathFinderNode y)
            {
                int result = x.HCost.CompareTo(y.HCost);
                if (result == 0)
                {
                    // Jika H cost sama, gunakan G cost sebagai tie-breaker
                    // Pilih jalur dengan G cost lebih rendah (lebih dekat dari start)
                    result = x.GCost.CompareTo(y.GCost);
                }
                return result;
            }
        }
    }
    #endregion

    #region Backtracking Algorithm
    /// <summary>
    /// Implementasi algoritma Backtracking untuk pencarian jalur
    /// Menggunakan pendekatan depth-first dengan backtracking
    /// </summary>
    /// <typeparam name="T">Tipe data nilai yang disimpan dalam node</typeparam>
    public class BacktrackingPathFinder<T> : PathFinder<T>
    {
        /// <summary>
        /// Stack untuk menyimpan node yang akan dikunjungi (open list)
        /// </summary>
        private Stack<PathFinderNode> openStack = new Stack<PathFinderNode>();

        /// <summary>
        /// HashSet untuk pemeriksaan cepat apakah node tertentu sudah dikunjungi
        /// </summary>
        private HashSet<T> closedSet = new HashSet<T>();

        /// <summary>
        /// HashSet untuk pemeriksaan cepat apakah node tertentu sudah ada di open list
        /// </summary>
        private HashSet<T> openSet = new HashSet<T>();

        /// <summary>
        /// Implementasi spesifik algoritma Backtracking untuk memproses node tetangga
        /// </summary>
        /// <param name="cell">Node tetangga yang akan diproses</param>
        protected override void AlgorithmSpecificImplementation(Node<T> cell)
        {
            if (!closedSet.Contains(cell.Value) && !openSet.Contains(cell.Value))
            {
                // Backtracking tidak memperhitungkan cost, tetapi tetap menghitungnya untuk visualisasi
                float G = CurrentNode.GCost + NodeTraversalCost(CurrentNode.Location.Value, cell.Value);
                float H = 0.0f;

                PathFinderNode n = new PathFinderNode(cell, CurrentNode, G, H);
                openList.Add(n);      // Tambahkan ke list untuk tujuan kompatibilitas
                openStack.Push(n);    // Tambahkan ke stack untuk DFS
                openSet.Add(cell.Value);
                onAddToOpenList?.Invoke(n);
            }
        }

        /// <summary>
        /// Melakukan satu langkah pencarian jalur dengan algoritma Backtracking
        /// </summary>
        /// <returns>Status pathfinder setelah langkah ini selesai</returns>
        public override PathFinderStatus Step()
        {
            // Jika currentNode belum diinisialisasi
            if (CurrentNode == null)
            {
                // Jika tidak ada node dalam open stack, berarti pencarian gagal
                if (openStack.Count == 0)
                {
                    Status = PathFinderStatus.FAILURE;
                    onFailure?.Invoke();
                    return Status;
                }

                // Ambil node berikutnya dari stack (LIFO - kedalaman pertama)
                CurrentNode = openStack.Pop();
                openList.Remove(CurrentNode); // Hapus dari list untuk kompatibilitas
                openSet.Remove(CurrentNode.Location.Value);
                onChangeCurrentNode?.Invoke(CurrentNode);
            }

            // Tambahkan node saat ini ke closed list
            closedList.Add(CurrentNode);
            closedSet.Add(CurrentNode.Location.Value);
            onAddToClosedList?.Invoke(CurrentNode);

            // Cek apakah kita telah mencapai tujuan
            if (EqualityComparer<T>.Default.Equals(CurrentNode.Location.Value, Goal.Value))
            {
                Status = PathFinderStatus.SUCCESS;
                onDestinationFound?.Invoke(CurrentNode);
                onSuccess?.Invoke();
                return Status;
            }

            // Proses semua tetangga dari node saat ini
            List<Node<T>> neighbours = CurrentNode.Location.GetNeighbours();
            foreach (Node<T> cell in neighbours)
            {
                AlgorithmSpecificImplementation(cell);
            }

            // Jika tidak ada lagi node yang dapat dikunjungi, berarti pencarian gagal
            if (openStack.Count == 0)
            {
                Status = PathFinderStatus.FAILURE;
                onFailure?.Invoke();
                return Status;
            }

            // Ambil node berikutnya dari stack
            CurrentNode = openStack.Pop();
            openList.Remove(CurrentNode); // Hapus dari list untuk kompatibilitas
            openSet.Remove(CurrentNode.Location.Value);
            onChangeCurrentNode?.Invoke(CurrentNode);

            Status = PathFinderStatus.RUNNING;
            onRunning?.Invoke();
            return Status;
        }

        /// <summary>
        /// Reset state pathfinder
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            openStack.Clear();
            closedSet.Clear();
            openSet.Clear();
        }
    }
    #endregion

    #region Breath-First Search Algorithm
    /// <summary>
    /// Implementasi algoritma Breadth-First Search (BFS)
    /// Algoritma ini menjelajahi semua node pada jarak yang sama dari 
    /// titik awal sebelum bergerak ke node yang lebih jauh
    /// </summary>
    /// <typeparam name="T">Tipe data nilai yang disimpan dalam node</typeparam>
    public class BFSPathFinder<T> : PathFinder<T>
    {
        /// <summary>
        /// Queue sebagai open list untuk BFS
        /// </summary>
        private Queue<PathFinderNode> openQueue = new Queue<PathFinderNode>();

        /// <summary>
        /// HashSet untuk pemeriksaan nilai node dalam closed list (O(1) complexity)
        /// </summary>
        private HashSet<T> closedSet = new HashSet<T>();

        /// <summary>
        /// HashSet untuk pemeriksaan nilai node dalam open list (O(1) complexity)
        /// </summary>
        private HashSet<T> openSet = new HashSet<T>();

        /// <summary>
        /// Implementasi spesifik algoritma BFS untuk memproses node tetangga
        /// </summary>
        /// <param name="cell">Node tetangga yang akan diproses</param>
        protected override void AlgorithmSpecificImplementation(Node<T> cell)
        {
            if (!closedSet.Contains(cell.Value) && !openSet.Contains(cell.Value))
            {
                // BFS tidak memperhitungkan cost, tapi kita tetap menghitungnya untuk visualisasi
                float G = CurrentNode.GCost + NodeTraversalCost(
                  CurrentNode.Location.Value, cell.Value);
                float H = 0.0f;

                PathFinderNode n = new PathFinderNode(cell, CurrentNode, G, H);
                openList.Add(n);      // Tambahkan ke list untuk tujuan kompatibilitas
                openQueue.Enqueue(n); // Tambahkan ke queue untuk BFS
                openSet.Add(cell.Value);
                onAddToOpenList?.Invoke(n);
            }
        }

        /// <summary>
        /// Melakukan satu langkah pencarian jalur dengan algoritma BFS
        /// </summary>
        /// <returns>Status pathfinder setelah langkah ini selesai</returns>
        public override PathFinderStatus Step()
        {
            // Jika currentNode belum diinisialisasi
            if (CurrentNode == null)
            {
                // Jika tidak ada node dalam open queue, berarti pencarian gagal
                if (openQueue.Count == 0)
                {
                    Status = PathFinderStatus.FAILURE;
                    onFailure?.Invoke();
                    return Status;
                }

                // Ambil node berikutnya dari queue (FIFO)
                CurrentNode = openQueue.Dequeue();
                openList.Remove(CurrentNode); // Hapus dari list untuk kompatibilitas
                openSet.Remove(CurrentNode.Location.Value);
                onChangeCurrentNode?.Invoke(CurrentNode);
            }

            // Tambahkan node saat ini ke closed list
            closedList.Add(CurrentNode);
            closedSet.Add(CurrentNode.Location.Value);
            onAddToClosedList?.Invoke(CurrentNode);

            // Cek apakah kita telah mencapai tujuan
            if (EqualityComparer<T>.Default.Equals(CurrentNode.Location.Value, Goal.Value))
            {
                Status = PathFinderStatus.SUCCESS;
                onDestinationFound?.Invoke(CurrentNode);
                onSuccess?.Invoke();
                return Status;
            }

            // Proses semua tetangga dari node saat ini
            List<Node<T>> neighbours = CurrentNode.Location.GetNeighbours();
            foreach (Node<T> cell in neighbours)
            {
                AlgorithmSpecificImplementation(cell);
            }

            // Jika tidak ada lagi node yang dapat dikunjungi, berarti pencarian gagal
            if (openQueue.Count == 0)
            {
                Status = PathFinderStatus.FAILURE;
                onFailure?.Invoke();
                return Status;
            }

            // Ambil node berikutnya dari queue
            CurrentNode = openQueue.Dequeue();
            openList.Remove(CurrentNode); // Hapus dari list untuk kompatibilitas
            openSet.Remove(CurrentNode.Location.Value);
            onChangeCurrentNode?.Invoke(CurrentNode);

            Status = PathFinderStatus.RUNNING;
            onRunning?.Invoke();
            return Status;
        }

        /// <summary>
        /// Reset state pathfinder
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            openQueue.Clear();
            closedSet.Clear();
            openSet.Clear();
        }
    }
    #endregion
}
