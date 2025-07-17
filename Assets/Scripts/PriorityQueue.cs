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
    private List<Tuple<TElement, TPriority>> elements = new List<Tuple<TElement, TPriority>>();
    private Dictionary<TElement, int> elementIndexMap = new Dictionary<TElement, int>();
    public int Count => elements.Count;

    public void Enqueue(TElement element, TPriority priority)
    {
        elements.Add(Tuple.Create(element, priority));
        elementIndexMap[element] = elements.Count - 1;
        HeapifyUp(elements.Count - 1);
    }

    public TElement Dequeue()
    {
        if (elements.Count == 0)
            throw new InvalidOperationException("The priority queue is empty.");

        var element = elements[0].Item1;
        var last = elements[elements.Count - 1];
        elements.RemoveAt(elements.Count - 1);

        if (elements.Count > 0)
        {
            elements[0] = last;
            elementIndexMap[last.Item1] = 0;
            HeapifyDown(0);
        }

        elementIndexMap.Remove(element);
        return element;
    }

    public void UpdatePriority(TElement element, TPriority newPriority)
    {
        if (!elementIndexMap.ContainsKey(element))
            throw new InvalidOperationException("Element not found in priority queue.");

        var index = elementIndexMap[element];
        var oldPriority = elements[index].Item2;
        elements[index] = Tuple.Create(element, newPriority);

        if (newPriority.CompareTo(oldPriority) < 0)
        {
            HeapifyUp(index);
        }
        else
        {
            HeapifyDown(index);
        }
    }

    private void HeapifyUp(int index)
    {
        var parentIndex = (index - 1) / 2;
        if (index > 0 && elements[index].Item2.CompareTo(elements[parentIndex].Item2) < 0)
        {
            Swap(index, parentIndex);
            HeapifyUp(parentIndex);
        }
    }

    private void HeapifyDown(int index)
    {
        var leftChildIndex = 2 * index + 1;
        var rightChildIndex = 2 * index + 2;
        var smallest = index;

        if (leftChildIndex < elements.Count && elements[leftChildIndex].Item2.CompareTo(elements[smallest].Item2) < 0)
        {
            smallest = leftChildIndex;
        }

        if (rightChildIndex < elements.Count && elements[rightChildIndex].Item2.CompareTo(elements[smallest].Item2) < 0)
        {
            smallest = rightChildIndex;
        }

        if (smallest != index)
        {
            Swap(index, smallest);
            HeapifyDown(smallest);
        }
    }

    private void Swap(int i, int j)
    {
        var temp = elements[i];
        elements[i] = elements[j];
        elements[j] = temp;

        elementIndexMap[elements[i].Item1] = i;
        elementIndexMap[elements[j].Item1] = j;
    }
}
