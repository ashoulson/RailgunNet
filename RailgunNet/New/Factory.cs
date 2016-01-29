using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Reservoir;

namespace Railgun
{
  /// <summary>
  /// Factories are responsible for creating boxes.
  /// </summary>
  public abstract class Factory
  {
    internal NodeList<Box> freeList;

    public Factory()
    {
      this.freeList = new NodeList<Box>();
    }

    internal abstract Box Allocate();

    internal void Deallocate(Box box)
    {
      if (box.Factory != this)
        throw new ArgumentException("Box must be from this factory");

      box.Reset();
      this.freeList.Add(box);
    }
  }

  /// <summary>
  /// Factories are responsible for creating boxes. For a given box class,
  /// you will need to instantiate and provide a typed box factory in order
  /// to provide the system with a way to create boxes of your type.
  /// 
  /// This is a pooled data structure. Unused boxes are freed and returned.
  /// </summary>
  public sealed class Factory<T> : Factory
    where T : Box<T>, new()
  {
    internal override Box Allocate()
    {
      Box box = null;
      if (this.freeList.Count == 0)
        box = new T();
      else
        box = this.freeList.RemoveLast();

      box.Factory = this;
      box.Initialize();
      return box;
    }
  }
}
