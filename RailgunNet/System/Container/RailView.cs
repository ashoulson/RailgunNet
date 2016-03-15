using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal class RailView
  {
    private Dictionary<EntityId, Tick> latestUpdates;

    public RailView()
    {
      this.latestUpdates = new Dictionary<EntityId, Tick>();
    }

    public Tick GetLatest(EntityId id)
    {
      Tick result;
      if (this.latestUpdates.TryGetValue(id, out result))
        return result;
      return Tick.INVALID;
    }

    public void Clear()
    {
      this.latestUpdates.Clear();
    }

    public void RecordUpdate(EntityId id, Tick tick)
    {
      Tick currentTick;
      if (this.latestUpdates.TryGetValue(id, out currentTick))
        if (currentTick > tick)
          return;
      this.latestUpdates[id] = tick;
    }

    public void Integrate(RailView other)
    {
      foreach (KeyValuePair<EntityId, Tick> pair in other.latestUpdates)
        this.RecordUpdate(pair.Key, pair.Value);
    }

    public void Encode(BitBuffer buffer)
    {
      // Write: [Count]
      buffer.Write(RailEncoders.EntityCount, this.latestUpdates.Count);

      foreach (KeyValuePair<EntityId, Tick> pair in this.latestUpdates)
      {
        // Write: [EntityId]
        buffer.Write(RailEncoders.EntityId, pair.Key);

        // Write: [Tick]
        buffer.Write(RailEncoders.Tick, pair.Value);
      }
    }

    public void Decode(BitBuffer buffer)
    {
      // Read: [Count]
      int count = buffer.Read(RailEncoders.EntityCount);

      for (int i = 0; i < count; i++)
      {
        this.RecordUpdate(
          // Read: [EntityId]
          buffer.Read(RailEncoders.EntityId),

          // Read: [Tick]
          buffer.Read(RailEncoders.Tick));
      }
    }
  }
}
