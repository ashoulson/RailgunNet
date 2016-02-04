using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public class GenericPool<T> : Pool<T>
    where T : IPoolable, new()
  {
    public T Allocate()
    {
      if (this.freeList.Count > 0)
        return this.freeList.Pop();

      T value = new T();
      value.Pool = this;
      return value;
    }
  }
}
