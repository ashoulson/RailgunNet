using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal class ResourceManager
  {
    // TODO: Make this thread-safe (like [ThreadStatic])
    internal static ResourceManager Instance { get; private set; }

    internal static void Initialize(params StatePool[] statePools)
    {
      ResourceManager.Instance = new ResourceManager(statePools);
    }

    private GenericPool<Snapshot> snapshotPool;
    private GenericPool<Image> imagePool;
    private Dictionary<int, StatePool> statePools;

    private ResourceManager(params StatePool[] statePools)
    {
      this.snapshotPool = new GenericPool<Snapshot>();
      this.imagePool = new GenericPool<Image>();
      this.statePools = new Dictionary<int, StatePool>();
      foreach (StatePool statePool in statePools)
        this.statePools[statePool.Type] = statePool;
    }

    internal Snapshot AllocateSnapshot()
    {
      return this.snapshotPool.Allocate();
    }

    internal Image AllocateImage()
    {
      return this.imagePool.Allocate();
    }

    internal State AllocateState(int type)
    {
      return this.statePools[type].Allocate();
    }
  }
}
