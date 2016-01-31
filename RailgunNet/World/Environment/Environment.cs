using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public class Environment : Snapshot
  {
    private Dictionary<int, Entity> idToEntity;

    public Environment()
    {
      this.idToEntity = new Dictionary<int, Entity>();
    }

    public void Update()
    {
      foreach (Entity entity in this.idToEntity.Values)
        entity.Update();
    }

    public void Add(Entity entity)
    {
      base.Add(entity);
      this.idToEntity.Add(entity.Id, entity);
    }

    public void Remove(Entity entity)
    {
      base.Remove(entity);
      this.idToEntity.Remove(entity.Id);
    }

    protected override void Reset()
    {
      base.Reset();
      this.idToEntity.Clear();
    }
  }
}
