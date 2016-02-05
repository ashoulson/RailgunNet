using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal class ResourceManager
  {
    // TODO: Make this thread-safe (like [ThreadStatic])
    public static ResourceManager Instance { get; private set; }

    public static void Initialize(params StatePool[] factories)
    {
      ResourceManager.Instance = new ResourceManager(factories);
    }

    private GenericPool<Snapshot> snapshotPool;
    private GenericPool<Image> imagePool;
    private Dictionary<int, StatePool> factories;

    private ResourceManager(params StatePool[] factories)
    {
      this.snapshotPool = new GenericPool<Snapshot>();
      this.imagePool = new GenericPool<Image>();
      this.factories = new Dictionary<int, StatePool>();
      foreach (StatePool factory in factories)
        this.factories[factory.Type] = factory;
    }

    public Snapshot AllocateSnapshot()
    {
      return this.snapshotPool.Allocate();
    }

    public Image AllocateImage()
    {
      return this.imagePool.Allocate();
    }

    internal State AllocateState(int type)
    {
      return this.factories[type].Allocate();
    }
  }
}
