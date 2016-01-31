using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  /// <summary>
  /// Payload packet produced and sent by the host
  /// </summary>
  class HostPacket : IPoolable
  {
    Pool IPoolable.Pool { get; set; }
    void IPoolable.Reset() { this.Reset(); }

    public Snapshot Snapshot { get; set; }

    public HostPacket()
    {
      this.Snapshot = null;
    }

    public void Reset()
    {
      if (this.Snapshot != null)
        Pool.Free(this.Snapshot);
    }
  }
}
