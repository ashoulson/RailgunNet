using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public class RingBuffer<T>
    where T : class, IRingValue, IPoolable
  {
    private T[] data;

    public RingBuffer(int capacity)
    {
      this.data = new T[capacity];
      for (int i = 0; i < capacity; i++)
        this.data[i] = null;
    }

    public void Store(T value)
    {
      int index = value.Key % this.data.Length;
      if (this.data[index] != null)
        Pool.Free(this.data[index]);
      this.data[index] = value;
    }

    public T Get(int key)
    {
      T result = this.data[key % this.data.Length];
      if ((result != null) && (result.Key == key))
        return result;
      return null;
    }

    public bool Contains(int key)
    {
      T result = this.data[key % this.data.Length];
      if ((result != null) && (result.Key == key))
        return true;
      return false;
    }

    public bool TryGet(int key, out T value)
    {
      value = this.Get(key);
      return (value != null);
    }
  }
}
