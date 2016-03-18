using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal class RailView
  {
    private class ViewComparer :
      Comparer<KeyValuePair<EntityId, Tick>>
    {
      private static readonly Comparer<Tick> Comparer = Tick.Comparer;

      public override int Compare(
        KeyValuePair<EntityId, Tick> x, 
        KeyValuePair<EntityId, Tick> y)
      {
        return ViewComparer.Comparer.Compare(x.Value, y.Value);
      }
    }

    private static readonly ViewComparer Comparer = new ViewComparer();

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

    /// <summary>
    /// Views sort in descending tick order. When sending a view to the server
    /// we send the most recent updated entities since they're the most likely
    /// to actually matter to the server/client scope.
    /// </summary>
    public IEnumerable<KeyValuePair<EntityId, Tick>> GetOrdered()
    {
      List<KeyValuePair<EntityId, Tick>> values =
        new List<KeyValuePair<EntityId, Tick>>(this.latestUpdates);
      values.Sort(RailView.Comparer);
      values.Reverse();
      return values;
    }
  }
}
