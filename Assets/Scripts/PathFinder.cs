using System;
using System.Collections.Generic;

namespace PathFinding
{
    #region PathFinderStatus Enumeration

    /// <summary>
    /// Enumerasi yang merepresentasikan berbagai status dari PathFinder.
    /// Digunakan untuk melacak progress dari pencarian jalur (pathfinding).
    /// </summary>
    public enum PathFinderStatus
    {
        NOT_INITIALISED,
        SUCCESS,
        FAILURE,
        RUNNING,
    }

    /// <summary>
    /// Kelas abstrak Node yang menjadi dasar untuk semua jenis vertex
    /// yang digunakan dalam algoritma pathfinding.
    /// </summary>
    /// <typeparam name="T">Tipe data nilai yang disimpan dalam node</typeparam>
    abstract public class Node<T>
    {
        public T Value { get; private set; }
        public Node(T value)
        {
            Value = value;
        }
        abstract public List<Node<T>> GetNeighbours();
    }

    /// <summary>
    /// Kelas abstrak PathFinder yang menjadi dasar untuk semua algoritma pencarian jalur.
    /// </summary>
    /// <typeparam name="T">Tipe data nilai yang disimpan dalam node</typeparam>
    public abstract class PathFinder<T>
    {
        #region Delegates for Cost Calculation.

        public delegate float CostFunction(T a, T b);
        public int ClosedListCount => closedList.Count;
        public int OpenListCount => openList.Count;
        public CostFunction HeuristicCost { get; set; }
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
            public PathFinderNode Parent { get; set; }
            public Node<T> Location { get; private set; }
            public GridMap Map { get; set; }

            public float FCost { get; private set; }
            public float GCost { get; private set; }
            public float HCost { get; private set; }

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

            public void SetGCost(float c)
            {
                GCost = c;
                FCost = GCost + HCost;
            }

            public void SetHCost(float h)
            {
                HCost = h;
                FCost = GCost + HCost;
            }

            public int CompareTo(PathFinderNode other)
            {
                if (other == null) return 1;
                return FCost.CompareTo(other.FCost);
            }
        }
        #endregion

        #region Properties

        public PathFinderStatus Status
        {
            get;
            protected set;
        } = PathFinderStatus.NOT_INITIALISED;

        public Node<T> Start { get; protected set; }
        public Node<T> Goal { get; protected set; }
        public PathFinderNode CurrentNode { get; protected set; }
        public GridMap Map { get; internal set; }

        #endregion

        #region Open and Closed Lists and Associated Functions.

        protected List<PathFinderNode> openList =
          new List<PathFinderNode>();

        protected List<PathFinderNode> closedList =
          new List<PathFinderNode>();
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

        public delegate void DelegatePathFinderNode(PathFinderNode node);

        public DelegatePathFinderNode onChangeCurrentNode;
        public DelegatePathFinderNode onAddToOpenList;
        public DelegatePathFinderNode onAddToClosedList;
        public DelegatePathFinderNode onDestinationFound;

        public delegate void DelegateNoArguments();

        public DelegateNoArguments onStarted;
        public DelegateNoArguments onRunning;
        public DelegateNoArguments onFailure;
        public DelegateNoArguments onSuccess;

        #endregion

        #region Pathfinding Search Related Functions
        public virtual void Reset()
        {
            if (Status == PathFinderStatus.RUNNING)
            {
                return;
            }

            CurrentNode = null;
            openList.Clear();
            closedList.Clear();

            Status = PathFinderStatus.NOT_INITIALISED;
        }

        public virtual PathFinderStatus Step()
        {
            closedList.Add(CurrentNode);
            onAddToClosedList?.Invoke(CurrentNode);

            if (openList.Count == 0)
            {
                Status = PathFinderStatus.FAILURE;
                onFailure?.Invoke();
                return Status;
            }

            CurrentNode = GetLeastCostNode(openList);

            onChangeCurrentNode?.Invoke(CurrentNode);

            openList.Remove(CurrentNode);

            if (EqualityComparer<T>.Default.Equals(
              CurrentNode.Location.Value, Goal.Value))
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

            Status = PathFinderStatus.RUNNING;
            onRunning?.Invoke();
            return Status;
        }
        abstract protected void AlgorithmSpecificImplementation(Node<T> cell);
        public virtual bool Initialise(Node<T> start, Node<T> goal)
        {
            if (Status == PathFinderStatus.RUNNING)
            {
                return false;
            }

            Reset();

            Start = start;
            Goal = goal;

            if (EqualityComparer<T>.Default.Equals(Start.Value, Goal.Value))
            {
                // Cost set to 0
                CurrentNode = new PathFinderNode(Start, null, 0.0f, 0.0f);

                onChangeCurrentNode?.Invoke(CurrentNode);
                onStarted?.Invoke();
                onDestinationFound?.Invoke(CurrentNode);

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

    #endregion

    #region Priority Queue
    /// <summary>
    /// Memprioritaskan item berdasarkan nilai komparatif mereka
    /// </summary>
    /// <typeparam name="T">Tipe item dalam antrian prioritas</typeparam>
    public class PriorityQueue<T> where T : IComparable<T>
    {
        private List<T> data;
        private IComparer<T> comparer;
        private Dictionary<T, int> elementIndexMap;

        // Cache untuk optimasi
        private T _lastDequeued;
        private int _count;

        public PriorityQueue() : this(Comparer<T>.Default) { }

        public PriorityQueue(IComparer<T> comparer)
        {
            this.data = new List<T>();
            this.comparer = comparer;
            this.elementIndexMap = new Dictionary<T, int>();
            this._count = 0;
        }

        public void Enqueue(T item)
        {
            data.Add(item);
            int childIndex = data.Count - 1;
            elementIndexMap[item] = childIndex;
            HeapifyUp(childIndex);
            _count = data.Count;
        }

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

        public bool Remove(T item)
        {
            if (!elementIndexMap.TryGetValue(item, out int index))
                return false;

            int lastIndex = data.Count - 1;

            if (index == lastIndex)
            {
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

                int parentIndex = (index - 1) / 2;
                if (index > 0 && comparer.Compare(data[index], data[parentIndex]) < 0)
                    HeapifyUp(index);
                else
                    HeapifyDown(index);
            }

            _count = data.Count;
            return true;
        }

        public void UpdatePriority(T item, float newPriority)
        {
            if (_lastDequeued != null && EqualityComparer<T>.Default.Equals(item, _lastDequeued))
                return;

            if (!elementIndexMap.TryGetValue(item, out int index))
                return;

            int parentIndex = (index - 1) / 2;
            if (index > 0 && comparer.Compare(data[index], data[parentIndex]) < 0)
                HeapifyUp(index);
            else
                HeapifyDown(index);
        }

        private void HeapifyUp(int index)
        {
            int parentIndex = (index - 1) / 2;
            while (index > 0 && comparer.Compare(data[index], data[parentIndex]) < 0)
            {
                Swap(index, parentIndex);
                index = parentIndex;
                parentIndex = (index - 1) / 2;
            }
        }

        private void HeapifyDown(int index)
        {
            int lastIndex = data.Count - 1;
            while (true)
            {
                int leftChildIndex = 2 * index + 1;
                if (leftChildIndex > lastIndex) break;

                int rightChildIndex = leftChildIndex + 1;
                int smallestChildIndex = leftChildIndex;

                if (rightChildIndex <= lastIndex && comparer.Compare(data[rightChildIndex], data[leftChildIndex]) < 0)
                    smallestChildIndex = rightChildIndex;

                if (comparer.Compare(data[index], data[smallestChildIndex]) <= 0) break;

                Swap(index, smallestChildIndex);
                index = smallestChildIndex;
            }
        }

        private void Swap(int index1, int index2)
        {
            T tmp = data[index1];
            data[index1] = data[index2];
            data[index2] = tmp;
            elementIndexMap[data[index1]] = index1;
            elementIndexMap[data[index2]] = index2;
        }

        public int Count => _count;

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
        private HashSet<T> closedSet;
        private Dictionary<T, PathFinderNode> openListMap;

        private bool isGridLarge = false;
        private int estimatedNodesCount = 0;

        public DijkstraPathFinder(int estimatedNodeCount = 0)
        {
            this.estimatedNodesCount = estimatedNodeCount;

            int initialCapacity = estimatedNodesCount > 0 ?
                Math.Min(estimatedNodesCount / 4, 256) : 16;

            isGridLarge = estimatedNodesCount > 2500;

            closedSet = new HashSet<T>(initialCapacity);
            openListMap = new Dictionary<T, PathFinderNode>(initialCapacity);
        }

        protected override void AlgorithmSpecificImplementation(Node<T> cell)
        {
            if (!closedSet.Contains(cell.Value))
            {
                float G = CurrentNode.GCost + NodeTraversalCost(
                  CurrentNode.Location.Value, cell.Value);

                float H = 0.0f;

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
        private new PriorityQueue<PathFinderNode> openList;
        private Dictionary<T, PathFinderNode> openListMap;
        private HashSet<T> closedSet;

        private bool processingBatch = false;
        private List<Node<T>> neighborBatch;

        private bool isGridLarge = false;
        private int estimatedNodesCount = 0;

        public AStarPathFinder(int estimatedNodeCount = 0)
        {
            this.estimatedNodesCount = estimatedNodeCount;

            int initialCapacity = estimatedNodesCount > 0 ?
                Math.Min(estimatedNodesCount / 4, 256) : 16;

            isGridLarge = estimatedNodesCount > 2500;

            openList = new PriorityQueue<PathFinderNode>(new FCostComparer());
            openListMap = new Dictionary<T, PathFinderNode>(initialCapacity);
            closedSet = new HashSet<T>(initialCapacity);

            if (isGridLarge)
            {
                neighborBatch = new List<Node<T>>(8);
            }
            else
            {
                neighborBatch = new List<Node<T>>(4); // Lebih kecil untuk grid kecil
            }
        }

        protected override void AlgorithmSpecificImplementation(Node<T> cell)
        {
            if (closedSet.Contains(cell.Value))
                return;

            float G = CurrentNode.GCost + NodeTraversalCost(CurrentNode.Location.Value, cell.Value);

            PathFinderNode existingNode = null;
            bool nodeExists = openListMap.TryGetValue(cell.Value, out existingNode);

            if (!nodeExists)
            {
                float H = HeuristicCost(cell.Value, Goal.Value);

                PathFinderNode n = new PathFinderNode(cell, CurrentNode, G, H);
                openList.Enqueue(n);
                openListMap[cell.Value] = n;

                if (!processingBatch || !isGridLarge)
                    onAddToOpenList?.Invoke(n);
            }
            else if (G < existingNode.GCost)
            {
                existingNode.Parent = CurrentNode;
                existingNode.SetGCost(G);

                openList.UpdatePriority(existingNode, existingNode.HCost);

                if ((!processingBatch || !isGridLarge) && onAddToOpenList != null)
                    onAddToOpenList.Invoke(existingNode);
            }
        }

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
            closedSet.Add(CurrentNode.Location.Value);
            onAddToClosedList?.Invoke(CurrentNode);

            List<Node<T>> neighbors;

            if (isGridLarge)
            {
                neighborBatch.Clear();
                neighborBatch.AddRange(CurrentNode.Location.GetNeighbours());
                neighbors = neighborBatch;

                processingBatch = neighbors.Count > 5;
            }
            else
            {
                neighbors = CurrentNode.Location.GetNeighbours();
                processingBatch = false;
            }

            foreach (Node<T> cell in neighbors)
            {
                AlgorithmSpecificImplementation(cell);
            }

            if (processingBatch && onAddToOpenList != null && isGridLarge)
            {
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

        public override void Reset()
        {
            base.Reset();
            openListMap.Clear();
            closedSet.Clear();
            if (isGridLarge && neighborBatch != null)
                neighborBatch.Clear();
            processingBatch = false;
        }

        private class FCostComparer : IComparer<PathFinderNode>
        {
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
        private new PriorityQueue<PathFinderNode> openList = new PriorityQueue<PathFinderNode>(new HeuristicComparer());
        private Dictionary<T, PathFinderNode> openSet = new Dictionary<T, PathFinderNode>(256);
        private HashSet<T> closedSet = new HashSet<T>(256);

        private bool processingBatch = false;
        private List<Node<T>> neighborBatch = new List<Node<T>>(4);

        protected override void AlgorithmSpecificImplementation(Node<T> cell)
        {
            if (closedSet.Contains(cell.Value))
                return;

            float G = CurrentNode.GCost + NodeTraversalCost(CurrentNode.Location.Value, cell.Value);
            float H;

            PathFinderNode existingNode = null;
            bool nodeExists = openSet.TryGetValue(cell.Value, out existingNode);

            if (!nodeExists)
            {
                if (EqualityComparer<T>.Default.Equals(cell.Value, Goal.Value))
                {
                    H = 0;
                }
                else
                {
                    H = HeuristicCost(cell.Value, Goal.Value);
                }

                PathFinderNode n = new PathFinderNode(cell, CurrentNode, G, H);
                openList.Enqueue(n);
                onAddToOpenList?.Invoke(n);
                openSet[cell.Value] = n;

            }
            else if (G < existingNode.GCost)
            {
                existingNode.Parent = CurrentNode;
                existingNode.SetGCost(G);

                openList.UpdatePriority(existingNode, existingNode.HCost);
                onAddToOpenList.Invoke(existingNode);
            }
        }

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

            neighborBatch.Clear();
            neighborBatch.AddRange(CurrentNode.Location.GetNeighbours());

            foreach (Node<T> cell in neighborBatch)
            {
                AlgorithmSpecificImplementation(cell);
            }

            if (processingBatch && onAddToOpenList != null)
            {
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

        public override void Reset()
        {
            base.Reset();
            openSet.Clear();
            closedSet.Clear();
            neighborBatch.Clear();
            processingBatch = false;
        }

        private class HeuristicComparer : IComparer<PathFinderNode>
        {
            public int Compare(PathFinderNode x, PathFinderNode y)
            {
                int result = x.HCost.CompareTo(y.HCost);
                if (result == 0)
                {
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
        private Stack<PathFinderNode> openStack = new Stack<PathFinderNode>();
        private HashSet<T> closedSet = new HashSet<T>();
        private HashSet<T> openSet = new HashSet<T>();

        protected override void AlgorithmSpecificImplementation(Node<T> cell)
        {
            if (!closedSet.Contains(cell.Value) && !openSet.Contains(cell.Value))
            {
                float G = CurrentNode.GCost + NodeTraversalCost(CurrentNode.Location.Value, cell.Value);
                float H = 0.0f;

                PathFinderNode n = new PathFinderNode(cell, CurrentNode, G, H);
                openList.Add(n);
                openStack.Push(n);
                openSet.Add(cell.Value);
                onAddToOpenList?.Invoke(n);
            }
        }

        public override PathFinderStatus Step()
        {
            if (CurrentNode == null)
            {
                if (openStack.Count == 0)
                {
                    Status = PathFinderStatus.FAILURE;
                    onFailure?.Invoke();
                    return Status;
                }

                CurrentNode = openStack.Pop();
                openList.Remove(CurrentNode);
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

            List<Node<T>> neighbours = CurrentNode.Location.GetNeighbours();
            foreach (Node<T> cell in neighbours)
            {
                AlgorithmSpecificImplementation(cell);
            }

            if (openStack.Count == 0)
            {
                Status = PathFinderStatus.FAILURE;
                onFailure?.Invoke();
                return Status;
            }

            CurrentNode = openStack.Pop();
            openList.Remove(CurrentNode);
            openSet.Remove(CurrentNode.Location.Value);
            onChangeCurrentNode?.Invoke(CurrentNode);

            Status = PathFinderStatus.RUNNING;
            onRunning?.Invoke();
            return Status;
        }

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
        private Queue<PathFinderNode> openQueue = new Queue<PathFinderNode>();
        private HashSet<T> closedSet = new HashSet<T>();
        private HashSet<T> openSet = new HashSet<T>();

        protected override void AlgorithmSpecificImplementation(Node<T> cell)
        {
            if (!closedSet.Contains(cell.Value) && !openSet.Contains(cell.Value))
            {
                float G = CurrentNode.GCost + NodeTraversalCost(
                  CurrentNode.Location.Value, cell.Value);
                float H = 0.0f;

                PathFinderNode n = new PathFinderNode(cell, CurrentNode, G, H);
                openList.Add(n);
                openQueue.Enqueue(n);
                openSet.Add(cell.Value);
                onAddToOpenList?.Invoke(n);
            }
        }

        public override PathFinderStatus Step()
        {
            if (CurrentNode == null)
            {
                if (openQueue.Count == 0)
                {
                    Status = PathFinderStatus.FAILURE;
                    onFailure?.Invoke();
                    return Status;
                }

                CurrentNode = openQueue.Dequeue();
                openList.Remove(CurrentNode);
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

            List<Node<T>> neighbours = CurrentNode.Location.GetNeighbours();
            foreach (Node<T> cell in neighbours)
            {
                AlgorithmSpecificImplementation(cell);
            }

            if (openQueue.Count == 0)
            {
                Status = PathFinderStatus.FAILURE;
                onFailure?.Invoke();
                return Status;
            }

            CurrentNode = openQueue.Dequeue();
            openList.Remove(CurrentNode);
            openSet.Remove(CurrentNode.Location.Value);
            onChangeCurrentNode?.Invoke(CurrentNode);

            Status = PathFinderStatus.RUNNING;
            onRunning?.Invoke();
            return Status;
        }

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
