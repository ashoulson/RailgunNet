using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal class PoolContext
  {
    private Pool<Snapshot> snapshotPool;
    private Pool<Image> imagePool;
    private Dictionary<int, Factory> factories;

    public PoolContext(params Factory[] factories)
    {
      this.snapshotPool = new Pool<Snapshot>();
      this.imagePool = new Pool<Image>();
      this.factories = new Dictionary<int, Factory>();
      foreach (Factory factory in factories)
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
