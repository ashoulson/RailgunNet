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
    private readonly RailView lastSent;
    private readonly RailView ackedByClient;

    // Pre-allocated reusable fill lists
    private readonly List<KeyValuePair<float, RailEntity>> entryList;
    private readonly List<RailStateDelta> activeList;
    private readonly List<RailStateDelta> frozenList;
    private readonly List<RailStateDelta> removedList;

    internal RailScope()
    {
      this.Evaluator = new RailScopeEvaluator();
      this.lastSent = new RailView();
      this.ackedByClient = new RailView();

      this.entryList = new List<KeyValuePair<float, RailEntity>>();
      this.activeList = new List<RailStateDelta>();
      this.frozenList = new List<RailStateDelta>();
      this.removedList = new List<RailStateDelta>();
    }

    internal bool EvaluateEvent(
      RailEvent evnt)
    {
      return this.Evaluator.Evaluate(evnt);
    }

    internal void PopulateDeltas(
      IRailController target,
      Tick serverTick,
      RailServerPacket packet,
      IEnumerable<RailEntity> activeEntities,
      IEnumerable<RailEntity> removedEntities)
    {
      this.ProduceScoped(target, serverTick, activeEntities);
      this.ProduceRemoved(target, removedEntities);

      packet.Populate(this.activeList, this.frozenList, this.removedList);

      this.removedList.Clear();
      this.frozenList.Clear();
      this.activeList.Clear();
    }

    internal void IntegrateAcked(RailView packetView)
    {
      this.ackedByClient.Integrate(packetView);
    }

    internal void RegisterSent(EntityId entityId, Tick tick, bool isFrozen)
    {
      this.lastSent.RecordUpdate(entityId, tick, isFrozen);
    }

    private bool GetPriority(
      RailEntity entity, 
      Tick current,
      out float priority)
    {
      RailViewEntry lastSent = this.lastSent.GetLatest(entity.Id);
      if (lastSent.IsValid)
        return this.EvaluateEntity(entity, current - lastSent.Tick, out priority);
      return this.EvaluateEntity(entity, int.MaxValue, out priority);
    }

    /// <summary>
    /// Divides the active entities into those that are in scope and those
    /// out of scope. If an entity is out of scope and hasn't been acked as
    /// such by the client, we will add it to the outgoing frozen delta list.
    /// Otherwise, if an entity is in scope we will add it to the sorted
    /// active delta list.
    /// </summary>
    private void ProduceScoped(
      IRailController target,
      Tick serverTick,
      IEnumerable<RailEntity> activeEntities)
    {
      this.entryList.Clear();
      float priority;

      foreach (RailEntity entity in activeEntities)
      {
        if (entity.IsRemoving)
        {
          continue;
        }
        // Controlled entities are always in scope to their controller
        else if (entity.Controller == target)
        {
          this.entryList.Add(
            new KeyValuePair<float, RailEntity>(float.MinValue, entity));
        }
        else if (this.GetPriority(entity, serverTick, out priority))
        {
          this.entryList.Add(
            new KeyValuePair<float, RailEntity>(priority, entity));
        }
        else
        {
          // We only want to send a freeze state if we aren't already frozen
          RailViewEntry latest = this.ackedByClient.GetLatest(entity.Id);
          if (latest.IsFrozen == false)
            this.frozenList.Add(
              RailStateDelta.CreateFrozen(serverTick, entity.Id));
        }
      }

      this.entryList.Sort(RailScope.Comparer);
      foreach (KeyValuePair<float, RailEntity> entry in this.entryList)
      { 
        RailViewEntry latest = this.ackedByClient.GetLatest(entry.Value.Id);
        RailStateDelta delta = entry.Value.ProduceDelta(latest.Tick, target);
        if (delta != null)
          this.activeList.Add(delta);
      }
    }

    /// <summary>
    /// Produces deltas for all non-acked destroyed entities.
    /// </summary>
    private void ProduceRemoved(
      IRailController target,
      IEnumerable<RailEntity> destroyedEntities)
    {
      foreach (RailEntity entity in destroyedEntities)
      {
        RailViewEntry latest = this.ackedByClient.GetLatest(entity.Id);
        if (latest.IsValid && (latest.Tick < entity.RemovedTick))
          // Note: Because the removed tick is valid, this should force-create
          this.removedList.Add(entity.ProduceDelta(latest.Tick, target));
      }
    }

    private bool EvaluateEntity(
      RailEntity entity, 
      int ticksSinceSend, 
      out float priority)
    {
      return this.Evaluator.Evaluate(entity, ticksSinceSend, out priority);
    }
  }
}
#endif