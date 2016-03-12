using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal struct Tuple<T1, T2>
  {
    internal T1 Item1 { get { return this.item1; } }
    internal T2 Item2 { get { return this.item2; } }

    private readonly T1 item1;
    private readonly T2 item2;

    internal Tuple(T1 item1, T2 item2)
    {
      this.item1 = item1;
      this.item2 = item2;
    }
  }
}
