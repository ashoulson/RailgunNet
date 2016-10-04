/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016 - Alexander Shoulson - http://ashoulson.com
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *  
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
*/

#if SERVER
using System.Collections.Generic;

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
      Tick localTick,
      IEnumerable<RailEntity> entities)
    {
      this.entries.Clear();
      float priority; 

      foreach (RailEntity entity in entities)
        if (this.GetPriority(entity, localTick, out priority))
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
#endif