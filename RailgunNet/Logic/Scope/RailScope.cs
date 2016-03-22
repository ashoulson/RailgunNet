using System;
using System.Collections.Generic;
using System.Linq;

namespace Railgun
{
  internal class RailScope
  {
    private class EntityPriorityComparer : 
      Comparer<KeyValuePair<float, RailEntity>>
    {
      private static readonly Comparer<float> Comparer = 
        Comparer<float>.Default;

      public override int Compare(
        KeyValuePair<float, RailEntity> x, 
        KeyValuePair<float, RailEntity> y)
      {
        return EntityPriorityComparer.Comparer.Compare(x.Key, y.Key);
      }
    }

    private static readonly EntityPriorityComparer Comparer = 
      new EntityPriorityComparer();

    internal RailScopeEvaluator Evaluator { private get; set; }

    private readonly List<KeyValuePair<float, RailEntity>> entries;
    private readonly RailView lastSent;

    internal RailScope()
    {
      this.Evaluator = new RailScopeEvaluator();

      this.entries = new List<KeyValuePair<float, RailEntity>>();
      this.lastSent = new RailView();
    }

    internal bool Evaluate(RailEvent evnt)
    {
      return this.Evaluator.Evaluate(evnt);
    }

    internal IEnumerable<RailEntity> Evaluate(
      IEnumerable<RailEntity> entities,
      Tick latestTick)
    {
      this.entries.Clear();
      float priority; 

      foreach (RailEntity entity in entities)
        if (this.GetPriority(entity, latestTick, out priority))
          this.entries.Add(new KeyValuePair<float, RailEntity>(priority, entity));

      this.entries.Sort(RailScope.Comparer);
      foreach (KeyValuePair<float, RailEntity> entry in this.entries)
        yield return entry.Value;
    }

    internal void RegisterSent(EntityId entityId, Tick sent)
    {
      this.lastSent.RecordUpdate(entityId, sent);
    }

    private bool GetPriority(
      RailEntity entity, 
      Tick current,
      out float priority)
    {
      Tick lastSent = this.lastSent.GetLatest(entity.Id);
      if (lastSent.IsValid)
        return this.Evaluator.Evaluate(entity, current - lastSent, out priority);
      return this.Evaluator.Evaluate(entity, int.MaxValue, out priority);
    }
  }
}
