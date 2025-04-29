using System;
using System.Collections.Generic;

/// <summary>
/// Implementasi generic Priority Queue (Antrean Prioritas) menggunakan min-heap.
/// Memungkinkan operasi enqueue, dequeue, dan update priority dengan efisien.
/// </summary>
/// <typeparam name="TElement">Tipe elemen yang disimpan dalam antrean</typeparam>
/// <typeparam name="TPriority">Tipe prioritas yang digunakan, harus implementasi IComparable</typeparam>
public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
{
    // Menyimpan pasangan elemen dan prioritasnya dalam struktur heap
    private List<Tuple<TElement, TPriority>> elements = new List<Tuple<TElement, TPriority>>();
    // Menyimpan indeks setiap elemen dalam heap untuk akses cepat
    private Dictionary<TElement, int> elementIndexMap = new Dictionary<TElement, int>();

    /// <summary>
    /// Mendapatkan jumlah elemen dalam antrean prioritas
    /// </summary>
    public int Count => elements.Count;

    /// <summary>
    /// Menambahkan elemen baru ke dalam antrean prioritas dengan prioritas tertentu
    /// </summary>
    /// <param name="element">Elemen yang akan ditambahkan</param>
    /// <param name="priority">Prioritas dari elemen</param>
    public void Enqueue(TElement element, TPriority priority)
    {
        elements.Add(Tuple.Create(element, priority));
        elementIndexMap[element] = elements.Count - 1;
        HeapifyUp(elements.Count - 1);
    }

    /// <summary>
    /// Mengambil dan menghapus elemen dengan prioritas tertinggi (nilai terkecil)
    /// dari antrean prioritas
    /// </summary>
    /// <returns>Elemen dengan prioritas tertinggi</returns>
    /// <exception cref="InvalidOperationException">Dilempar jika antrean kosong</exception>
    public TElement Dequeue()
    {
        if (elements.Count == 0)
            throw new InvalidOperationException("The priority queue is empty.");

        var element = elements[0].Item1;
        var last = elements[elements.Count - 1];
        elements.RemoveAt(elements.Count - 1);

        if (elements.Count > 0)
        {
            // Pindahkan elemen terakhir ke root, lalu atur ulang heap
            elements[0] = last;
            elementIndexMap[last.Item1] = 0;
            HeapifyDown(0);
        }

        elementIndexMap.Remove(element);
        return element;
    }

    /// <summary>
    /// Memperbarui prioritas elemen yang sudah ada dalam antrean
    /// </summary>
    /// <param name="element">Elemen yang akan diperbarui prioritasnya</param>
    /// <param name="newPriority">Nilai prioritas baru</param>
    /// <exception cref="InvalidOperationException">Dilempar jika elemen tidak ditemukan</exception>
    public void UpdatePriority(TElement element, TPriority newPriority)
    {
        if (!elementIndexMap.ContainsKey(element))
            throw new InvalidOperationException("Element not found in priority queue.");

        var index = elementIndexMap[element];
        var oldPriority = elements[index].Item2;
        elements[index] = Tuple.Create(element, newPriority);

        // Jika prioritas baru lebih tinggi (nilai lebih kecil), heapify up
        if (newPriority.CompareTo(oldPriority) < 0)
        {
            HeapifyUp(index);
        }
        // Jika prioritas baru lebih rendah (nilai lebih besar), heapify down
        else
        {
            HeapifyDown(index);
        }
    }

    /// <summary>
    /// Mempertahankan properti heap dengan memindahkan elemen ke atas jika
    /// prioritasnya lebih tinggi dari parent
    /// </summary>
    /// <param name="index">Indeks elemen yang akan dipindahkan ke atas</param>
    private void HeapifyUp(int index)
    {
        var parentIndex = (index - 1) / 2;
        if (index > 0 && elements[index].Item2.CompareTo(elements[parentIndex].Item2) < 0)
        {
            Swap(index, parentIndex);
            HeapifyUp(parentIndex);
        }
    }

    /// <summary>
    /// Mempertahankan properti heap dengan memindahkan elemen ke bawah jika
    /// prioritasnya lebih rendah dari child
    /// </summary>
    /// <param name="index">Indeks elemen yang akan dipindahkan ke bawah</param>
    private void HeapifyDown(int index)
    {
        var leftChildIndex = 2 * index + 1;
        var rightChildIndex = 2 * index + 2;
        var smallest = index;

        // Cari child dengan prioritas tertinggi (nilai terkecil)
        if (leftChildIndex < elements.Count && elements[leftChildIndex].Item2.CompareTo(elements[smallest].Item2) < 0)
        {
            smallest = leftChildIndex;
        }

        if (rightChildIndex < elements.Count && elements[rightChildIndex].Item2.CompareTo(elements[smallest].Item2) < 0)
        {
            smallest = rightChildIndex;
        }

        // Jika child memiliki prioritas lebih tinggi, tukar dan lanjutkan heapify down
        if (smallest != index)
        {
            Swap(index, smallest);
            HeapifyDown(smallest);
        }
    }

    /// <summary>
    /// Menukar posisi dua elemen dalam heap dan memperbarui elementIndexMap
    /// </summary>
    /// <param name="i">Indeks elemen pertama</param>
    /// <param name="j">Indeks elemen kedua</param>
    private void Swap(int i, int j)
    {
        var temp = elements[i];
        elements[i] = elements[j];
        elements[j] = temp;

        // Perbarui elementIndexMap untuk mencerminkan posisi baru
        elementIndexMap[elements[i].Item1] = i;
        elementIndexMap[elements[j].Item1] = j;
    }
}
