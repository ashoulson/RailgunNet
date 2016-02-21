using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CommonTools;

namespace Railgun
{
  internal class RailResource
  {
    // TODO: Make this thread-safe (like [ThreadStatic])
    internal static RailResource Instance { get; private set; }

    internal static void Initialize(params RailFactory[] factories)
    {
      RailResource.Instance = new RailResource(factories);
    }

    private GenericPool<RailSnapshot> snapshotPool;
    private GenericPool<RailImage> imagePool;
    private Dictionary<int, RailStatePool> statePools;

    private RailResource(params RailFactory[] factories)
    {
      this.snapshotPool = new GenericPool<RailSnapshot>();
      this.imagePool = new GenericPool<RailImage>();
      this.statePools = new Dictionary<int, RailStatePool>();
      foreach (RailFactory factory in factories)
        this.statePools[factory.StatePool.Type] = factory.StatePool;
    }

    internal RailSnapshot AllocateSnapshot()
    {
      return this.snapshotPool.Allocate();
    }

    internal RailImage AllocateImage()
    {
      return this.imagePool.Allocate();
    }

    internal RailState AllocateState(int type)
    {
      return this.statePools[type].Allocate();
    }
  }
}
