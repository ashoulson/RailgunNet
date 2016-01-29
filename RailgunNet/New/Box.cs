using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Reservoir;

namespace Railgun
{
  /// <summary>
  /// Boxes are attached to entities and contain user-defined data. They are
  /// responsible for encoding and decoding that data, and delta-compression.
  /// </summary>
  public abstract class Box : INode<Box>
  {
    #region INode<Box> Members
    NodeList<Box> INode<Box>.List { get; set; }
    Box INode<Box>.Next { get; set; }
    Box INode<Box>.Previous { get; set; }
    #endregion

    #region Factory-Related
    internal Factory Factory { get; set; }
    internal void Free() { this.Factory.Deallocate(this); }

    internal Box Clone()
    {
      Box box = this.Factory.Allocate();
      this.Populate(box);
      return box;
    }
    #endregion

    internal abstract void Encode(BitPacker bitPacker, Box basis);
    internal abstract void Decode(BitPacker bitPacker, Box basis);
    internal abstract void Populate(Box clone);

    protected internal abstract byte Type { get; }
    protected internal abstract void Encode(BitPacker bitPacker);
    protected internal abstract void Decode(BitPacker bitPacker);

    protected internal virtual void Initialize() { }
    protected internal virtual void Reset() { }
  }

  /// <summary>
  /// This is the class to override to attach user-defined data to an entity.
  /// </summary>
  public abstract class Box<T> : Box
    where T : Box<T>
  {
    #region Casting Overrides
    internal override void Encode(BitPacker bitPacker, Box basis)
    {
      this.Encode(bitPacker, (T)basis);
    }

    internal override void Decode(BitPacker bitPacker, Box basis)
    {
      this.Decode(bitPacker, (T)basis);
    }

    internal override void Populate(Box clone)
    {
      this.Populate((T)clone);
    }
    #endregion

    protected internal abstract void Populate(T clone);
    protected internal abstract bool Encode(BitPacker bitPacker, T basis);
    protected internal abstract void Decode(BitPacker bitPacker, T basis);
    protected internal abstract void SetFrom(T other);
  }
}
