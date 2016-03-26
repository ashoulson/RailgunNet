using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  // http://stackoverflow.com/questions/102398/priority-queue-in-net
  internal abstract class Heap
  {
    protected static int GetParent(int i)
    {
      return (i + 1) / 2 - 1;
    }

    protected static int GetYoungChild(int i)
    {
      return (i + 1) * 2 - 1;
    }

    protected static int GetOldChild(int i)
    {
      return Heap.GetYoungChild(i) + 1;
    }
  }

  internal abstract class Heap<T> : Heap, IEnumerable<T>
  {
    private const int INITIAL_CAPACITY = 0;
    private const int GROW_FACTOR = 2;
    private const int MIN_GROW = 1;

    private int capacity = Heap<T>.INITIAL_CAPACITY;
    private T[] heap = new T[Heap<T>.INITIAL_CAPACITY];
    private int tail = 0;

    public int Count { get { return this.tail; } }
    public int Capacity { get { return this.capacity; } }

    protected Comparer<T> Comparer { get; private set; }
    protected abstract bool Dominates(T x, T y);

    protected Heap()
      : this(Comparer<T>.Default)
    {
    }

    protected Heap(Comparer<T> comparer)
      : this(Enumerable.Empty<T>(), comparer)
    {
    }

    protected Heap(IEnumerable<T> collection)
      : this(collection, Comparer<T>.Default)
    {
    }

    protected Heap(IEnumerable<T> collection, Comparer<T> comparer)
    {
      if (collection == null) 
        throw new ArgumentNullException("collection");
      if (comparer == null) 
        throw new ArgumentNullException("comparer");

      this.Comparer = comparer;

      foreach (T item in collection)
      {
        if (this.Count == this.Capacity)
          this.Grow();
        this.heap[this.tail++] = item;
      }

      for (int i = Heap.GetParent(this.tail - 1); i >= 0; i--)
        this.BubbleDown(i);
    }

    public void Add(T item)
    {
      if (this.Count == this.Capacity)
        this.Grow();

      this.heap[tail++] = item;
      this.BubbleUp(this.tail - 1);
    }

    private void BubbleUp(int i)
    {
      if (i == 0)
        return;
      if (this.Dominates(this.heap[Heap.GetParent(i)], this.heap[i]))
        return;

      this.Swap(i, Heap.GetParent(i));
      this.BubbleUp(Heap.GetParent(i));
    }

    public T PeekFirst()
    {
      if (this.Count == 0) 
        throw new InvalidOperationException("Heap is empty");
      return this.heap[0];
    }

    public T PopFirst()
    {
      if (this.Count == 0) 
        throw new InvalidOperationException("Heap is empty");

      T ret = this.heap[0];
      this.tail--;
      this.Swap(this.tail, 0);
      this.BubbleDown(0);
      return ret;
    }

    private void BubbleDown(int i)
    {
      int dominatingNode = this.Dominating(i);
      if (dominatingNode == i) 
        return;
      this.Swap(i, dominatingNode);
      this.BubbleDown(dominatingNode);
    }

    private int Dominating(int i)
    {
      int dominatingNode = i;
      dominatingNode = 
        this.GetDominating(Heap.GetYoungChild(i), dominatingNode);
      dominatingNode = 
        this.GetDominating(Heap.GetOldChild(i), dominatingNode);
      return dominatingNode;
    }

    private int GetDominating(int newNode, int dominatingNode)
    {
      if (newNode < tail)
      {
        bool dominates = 
          this.Dominates(this.heap[dominatingNode], this.heap[newNode]);
        if (dominates)
          return dominatingNode;
        return newNode;
      }
      
      return dominatingNode;
    }

    private void Swap(int i, int j)
    {
      T temp = this.heap[i];
      this.heap[i] = this.heap[j];
      this.heap[j] = temp;
    }

    private void Grow()
    {
      int newCapacity = this.capacity * GROW_FACTOR + MIN_GROW;
      var newHeap = new T[newCapacity];
      Array.Copy(this.heap, newHeap, this.capacity);
      this.heap = newHeap;
      this.capacity = newCapacity;
    }

    public IEnumerator<T> GetEnumerator()
    {
      return heap.Take(Count).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }

  internal class MaxHeap<T> : Heap<T>
  {
    public MaxHeap()
      : this(Comparer<T>.Default)
    {
    }

    public MaxHeap(Comparer<T> comparer)
      : base(comparer)
    {
    }

    public MaxHeap(IEnumerable<T> collection, Comparer<T> comparer)
      : base(collection, comparer)
    {
    }

    public MaxHeap(IEnumerable<T> collection)
      : base(collection)
    {
    }

    protected override bool Dominates(T x, T y)
    {
      return this.Comparer.Compare(x, y) >= 0;
    }
  }

  internal class MinHeap<T> : Heap<T>
  {
    public MinHeap()
      : this(Comparer<T>.Default)
    {
    }

    public MinHeap(Comparer<T> comparer)
      : base(comparer)
    {
    }

    public MinHeap(IEnumerable<T> collection)
      : base(collection)
    {
    }

    public MinHeap(IEnumerable<T> collection, Comparer<T> comparer)
      : base(collection, comparer)
    {
    }

    protected override bool Dominates(T x, T y)
    {
      return this.Comparer.Compare(x, y) <= 0;
    }
  }
}
