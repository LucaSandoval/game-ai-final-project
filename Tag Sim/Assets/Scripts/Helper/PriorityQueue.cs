using System;
using System.Collections.Generic;

public class PriorityQueue<T>
{
    private List<T> heap = new List<T>();
    private Dictionary<T, int> indexMap = new Dictionary<T, int>(); // For fast updates
    private readonly Comparison<T> compare; // Dynamic comparator

    public int Count => heap.Count;

    public PriorityQueue(Comparison<T> comparison)
    {
        this.compare = comparison;
    }

    public void Enqueue(T item)
    {
        heap.Add(item);
        int index = heap.Count - 1;
        indexMap[item] = index;
        HeapifyUp(index);
    }

    public T Dequeue()
    {
        if (heap.Count == 0) throw new InvalidOperationException("Priority queue is empty");

        T min = heap[0];
        RemoveAt(0);
        return min;
    }

    public void UpdatePriority(T item)
    {
        if (!indexMap.ContainsKey(item)) return;

        int index = indexMap[item];
        HeapifyUp(index);
        HeapifyDown(index);
    }

    private void RemoveAt(int index)
    {
        int lastIndex = heap.Count - 1;
        if (index == lastIndex)
        {
            indexMap.Remove(heap[index]);
            heap.RemoveAt(index);
            return;
        }

        heap[index] = heap[lastIndex];
        indexMap[heap[index]] = index;
        indexMap.Remove(heap[lastIndex]);
        heap.RemoveAt(lastIndex);

        HeapifyUp(index);
        HeapifyDown(index);
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (compare(heap[index], heap[parent]) >= 0) break;

            Swap(index, parent);
            index = parent;
        }
    }

    private void HeapifyDown(int index)
    {
        while (true)
        {
            int left = 2 * index + 1;
            int right = 2 * index + 2;
            int smallest = index;

            if (left < heap.Count && compare(heap[left], heap[smallest]) < 0)
                smallest = left;
            if (right < heap.Count && compare(heap[right], heap[smallest]) < 0)
                smallest = right;
            if (smallest == index) break;

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        (heap[a], heap[b]) = (heap[b], heap[a]);
        indexMap[heap[a]] = a;
        indexMap[heap[b]] = b;
    }

    public bool Contains(T item)
    {
        return indexMap.ContainsKey(item);
    }

}