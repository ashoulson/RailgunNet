using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal class RailRingDelta<T>
    where T : class
  {
    public T Prior { get; private set; }
    public T Latest { get; private set; }
    public T Next { get; private set; }

    public RailRingDelta()
    {
      this.Prior = null;
      this.Latest = null;
      this.Next = null;
    }

    public void Set(T prior, T latest, T next)
    {
      this.Prior = prior;
      this.Latest = latest;
      this.Next = next;
    }
  }
}
