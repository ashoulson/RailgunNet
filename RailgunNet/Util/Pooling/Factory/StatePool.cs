using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public abstract class StatePool : Pool<State>
  {
    public int Type { get; private set; }

    public StatePool()
    {
      // Allocate and deallocate a dummy state to read and store its type
      State dummy = this.Allocate();
      this.Type = dummy.Type;
      this.Deallocate(dummy);
    }

    public abstract State Allocate();
  }

  public class StatePool<T> : StatePool
    where T : State, IPoolable, new()
  {
    public override State Allocate()
    {
      if (this.freeList.Count > 0)
        return this.freeList.Pop();

      T value = new T();
      value.Pool = this;
      return value;
    }
  }
}
