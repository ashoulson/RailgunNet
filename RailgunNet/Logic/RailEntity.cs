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

using System;
using System.Collections;
using System.Collections.Generic;

using CommonTools;

namespace Railgun
{
  public abstract class RailEntity
  {
    internal static RailEntity Create(int factoryType)
    {
      // TODO: ALLOCATE
      RailEntity entity = null;
      entity.factoryType = factoryType;
      entity.OnAllocated(factoryType);
      return null;
    }

    protected internal RailWorld World { get; internal set; }
    public IRailController Controller { get { return this.controller; } }

    public EntityId Id { get; private set; }
    public RailState State { get; private set; }

    private RailDejitterBuffer<RailStateDelta> dejitterBuffer; // Client-only
    private RailHistoryBuffer<RailStateRecord> recordBuffer;   // Server-only
    private IRailControllerInternal controller;

    internal virtual void SimulateCommand(RailCommand command) { }
    protected virtual void Simulate() { }

    private int factoryType;

    internal RailEntity()
    {
      this.World = null;

      this.Id = EntityId.INVALID;
      this.State = null;

      this.controller = null;

      // TODO: Don't need this on the server!
      this.dejitterBuffer =
        new RailDejitterBuffer<RailStateDelta>(
          RailConfig.DEJITTER_BUFFER_LENGTH,
          RailConfig.NETWORK_SEND_RATE);

      // TODO: Don't need this on the client!
      this.recordBuffer = 
        new RailHistoryBuffer<RailStateRecord>(
          RailConfig.DEJITTER_BUFFER_LENGTH);
    }

    internal void AssignId(EntityId id)
    {
      this.Id = id;
    }

    internal void AssignController(IRailControllerInternal controller)
    {
      this.controller = controller;
    }

    private void OnAllocated(int factoryType)
    {
      this.State = RailState.Create(factoryType);
    }

    #region Server
    internal void UpdateServer(Tick serverTick)
    {
      if (this.controller != null)
        if (this.controller.LatestCommand != null)
          this.SimulateCommand(this.controller.LatestCommand);
      this.Simulate();
    }

    internal void StoreRecord(Tick tick)
    {
      RailStateRecord record = RailStateRecord.Create(tick, this.State);
      this.recordBuffer.Store(record);
    }

    internal RailStateDelta ProduceDelta(
      Tick basisTick, 
      IRailController destination)
    {
      bool includeImmutableData = (basisTick.IsValid == false);
      bool includeControllerData = (destination == this.controller);
      RailState current = this.State;
      RailState basis = null;

      if (basisTick.IsValid)
      {
        RailStateRecord record = this.recordBuffer.Latest(basisTick);
        if (record != null)
          basis = record.State;
      }

      return RailStateDelta.Create(
        this.Id,
        current,
        basis,
        includeImmutableData,
        includeControllerData);
    }
    #endregion

    #region Client
    internal void UpdateClient(Tick serverTick)
    {
      RailStateDelta delta = this.dejitterBuffer.GetLatestAt(serverTick);
      if (delta != null)
        delta.ApplyTo(this.State);
    }

    internal void ReceiveDelta(RailStateDelta delta)
    {
      this.dejitterBuffer.Store(delta);
    }

    internal bool HasLatest(Tick tick)
    {
      return (this.dejitterBuffer.GetLatestAt(tick) != null);
    }
    #endregion
  }

  /// <summary>
  /// Handy shortcut class for auto-casting the internal state.
  /// </summary>
  public abstract class RailEntity<TState> : RailEntity
    where TState : RailState, new()
  {
    public new TState State { get { return (TState)base.State; } }
  }

  /// <summary>
  /// Handy shortcut class for auto-casting the internal state and command.
  /// </summary>
  public abstract class RailEntity<TState, TCommand> : RailEntity<TState>
    where TState : RailState, new()
    where TCommand : RailCommand
  {
    internal override void SimulateCommand(RailCommand command)
    {
      this.SimulateCommand((TCommand)command);
    }

    protected virtual void SimulateCommand(TCommand command) { }
  }
}
