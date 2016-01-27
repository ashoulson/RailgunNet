using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public abstract class State<T>
  {
    internal int Id { get; set; }

    protected internal abstract void Encode(BitPacker bitPacker);
    protected internal abstract bool Encode(BitPacker bitPacker, T basis);

    protected internal abstract void Decode(BitPacker bitPacker);
    protected internal abstract void Decode(BitPacker bitPacker, T basis);

    protected internal abstract void SetFrom(T other);
  }
}
