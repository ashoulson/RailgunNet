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

namespace Railgun
{
  internal class RailStateDelta
    : IRailPoolable<RailStateDelta>
    , IRailTimedValue
  {
    #region Pooling
    IRailPool<RailStateDelta> IRailPoolable<RailStateDelta>.Pool { get; set; }
    void IRailPoolable<RailStateDelta>.Reset() { this.Reset(); }
    #endregion

    #region Interface
    Tick IRailTimedValue.Tick { get { return this.tick; } }
    #endregion

    internal static RailStateDelta CreateFrozen(
      RailResource resource,
      Tick tick, 
      EntityId entityId)
    {
      RailStateDelta delta = resource.CreateDelta();
      delta.Initialize(tick, entityId, null, Tick.INVALID, Tick.INVALID, true);
      return delta;
    }

    internal Tick Tick { get { return this.tick; } }
    internal EntityId EntityId { get { return this.entityId; } }
    internal RailState State { get { return this.state; } }
    internal bool IsFrozen { get; set; }

    internal bool HasControllerData { get { return this.state.HasControllerData; } }
    internal bool HasImmutableData { get { return this.state.HasImmutableData; } }
    internal bool IsRemoving { get { return this.RemovedTick.IsValid; } }
    internal Tick RemovedTick { get; private set; }
    internal Tick CommandAck { get; private set; } // Controller only

    private Tick tick;
    private EntityId entityId;
    private RailState state;

    internal RailEntity ProduceEntity(RailResource resource)
    {
      return this.state.ProduceEntity(resource);
    }

    public void Initialize(
      Tick tick,
      EntityId entityId,
      RailState state,
      Tick removedTick,
      Tick commandAck,
      bool isFrozen)
    {
      this.tick = tick;
      this.entityId = entityId;
      this.state = state;
      this.RemovedTick = removedTick;
      this.CommandAck = commandAck;
      this.IsFrozen = isFrozen;
    }

    public RailStateDelta()
    {
      this.Reset();
    }

    private void Reset()
    {
      this.tick = Tick.INVALID;
      this.entityId = EntityId.INVALID;
      RailPool.SafeReplace(ref this.state, null);
      this.IsFrozen = false;
    }
  }
}
