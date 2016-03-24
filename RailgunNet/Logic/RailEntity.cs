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
      RailEntity entity = RailResource.Instance.CreateEntity(factoryType);
      entity.factoryType = factoryType;
      entity.State = RailState.Create(factoryType);
      return entity;
    }

    internal static T Create<T>()
      where T : RailEntity
    {
      int factoryType = RailResource.Instance.GetEntityFactoryType<T>();
      return (T)RailEntity.Create(factoryType);
    }

    // Settings
    protected virtual bool ForceUpdates { get { return true; } }

    // Simulation info
    protected internal RailWorld World { get; internal set; }
    public IRailController Controller { get { return this.controller; } }

    // Synchronization info
    public EntityId Id { get; private set; }
    public RailState State { get; private set; }

    private RailDejitterBuffer<IRailStateDelta> dejitterBuffer; // Client-only
    private RailHistoryBuffer<IRailStateRecord> recordBuffer;   // Server-only
    private IRailControllerInternal controller;

    internal virtual void SimulateCommand(RailCommand command) { }
    protected virtual void Simulate() { }
    protected virtual void Start() { }

    private int factoryType;
    private bool hasStarted;

    internal RailEntity()
    {
      this.World = null;

      this.Id = EntityId.INVALID;
      this.State = null;

      this.controller = null;
      this.hasStarted = false;

      // TODO: Don't need this on the server!
      this.dejitterBuffer =
        new RailDejitterBuffer<IRailStateDelta>(
          RailConfig.DEJITTER_BUFFER_LENGTH,
          RailConfig.NETWORK_SEND_RATE);

      // TODO: Don't need this on the client!
      this.recordBuffer =
        new RailHistoryBuffer<IRailStateRecord>(
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

    private void DoStart()
    {
      if (this.hasStarted == false)
        this.Start();
      this.hasStarted = true;
    }

    #region Server
    internal void UpdateServer(Tick serverTick)
    {
      this.DoStart();
      if (this.controller != null)
        if (this.controller.LatestCommand != null)
          this.SimulateCommand(this.controller.LatestCommand);
      this.Simulate();
    }

    internal void StoreRecord(
      Tick currentTick)
    {
      IRailStateRecord record =
        RailState.CreateRecord(
          currentTick, 
          this.State, 
          this.recordBuffer.Latest);
      if (record != null)
        this.recordBuffer.Store(record);
    }

    internal IRailStateDelta ProduceDelta(
      Tick currentTick,
      Tick basisTick, 
      IRailController destination)
    {
      IRailStateRecord basis = null;
      if (basisTick.IsValid)
        basis = this.recordBuffer.LatestAt(basisTick);

      return RailState.CreateDelta(
        currentTick,
        this.Id,
        this.State,
        basis,
        (destination == this.controller),
        (basisTick.IsValid == false),
        this.ForceUpdates);
    }
    #endregion

    #region Client
    internal void UpdateClient(Tick serverTick)
    {
      IRailStateDelta delta = this.dejitterBuffer.GetLatestAt(serverTick);
      if (delta != null)
        this.State.ApplyDelta(delta);
      this.DoStart();
    }

    internal void ReceiveDelta(IRailStateDelta delta)
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
