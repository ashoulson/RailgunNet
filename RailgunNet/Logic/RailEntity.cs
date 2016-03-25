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
    private class Smoother
    {
      private IRailStateDelta prior;
      private IRailStateDelta current;
      private IRailStateDelta next;

      private IRailStateRecord priorState;

      internal Smoother()
      {
        this.priorState = null;
        this.prior = null;
        this.current = null;
        this.next = null;
      }

      internal IRailStateDelta Update(
        Tick currentTick,
        RailState currentState,
        RailDejitterBuffer<IRailStateDelta> buffer)
      {
        buffer.GetRange(
          currentTick,
          out this.prior,
          out this.current,
          out this.next);

        if (this.priorState != null)
          RailPool.Free(this.priorState);

        this.priorState = RailState.CreateRecord(currentTick, currentState);

        return this.current;
      }

      internal RailState GetSmoothedState(
        Tick realTick,
        RailState currentState,
        float frameDelta)
      {
        RailState clone = currentState.Clone();
        float realTime = realTick.Time + frameDelta;
        //if (this.next != null)
        //  clone.ApplySmoothed(this.current, this.next, realTime);
        //else if (prior != null)
        if (this.priorState != null)
          clone.ApplySmoothed(this.priorState, currentState, realTime);
        
        return clone;
      }
    }

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
    protected virtual bool ForceUpdates { get { return true; } } // Server-only

    // Simulation info
    protected internal RailWorld World { get; internal set; }
    public IRailController Controller { get { return this.controller; } }
    public RailState State { get; private set; }

    // Synchronization info
    public EntityId Id { get; private set; }

    private RailDejitterBuffer<IRailStateDelta> incoming;  // Client-only
    private RailQueueBuffer<IRailStateRecord> outgoing;    // Server-only
    private IRailControllerInternal controller;

    internal virtual void SimulateCommand(RailCommand command) { }
    protected virtual void Simulate() { }
    protected virtual void Start() { }

    private int factoryType;
    private bool hasStarted;

    // Client-only
    private readonly Smoother smoother;
    private RailState smoothedState;

    internal RailEntity()
    {
      this.World = null;

      this.Id = EntityId.INVALID;
      this.State = null;

      this.controller = null;
      this.hasStarted = false;

      // TODO: Don't need this on the server!
      this.incoming =
        new RailDejitterBuffer<IRailStateDelta>(
          RailConfig.DEJITTER_BUFFER_LENGTH,
          RailConfig.NETWORK_SEND_RATE);
      this.smoother = new Smoother();
      this.smoothedState = null;

      // TODO: Don't need this on the client!
      this.outgoing =
        new RailQueueBuffer<IRailStateRecord>(
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
    internal void UpdateServer()
    {
      this.DoStart();
      if (this.controller != null)
        if (this.controller.LatestCommand != null)
          this.SimulateCommand(this.controller.LatestCommand);
      this.Simulate();
    }

    internal void StoreRecord()
    {
      IRailStateRecord record =
        RailState.CreateRecord(
          this.World.Tick,
          this.State, 
          this.outgoing.Latest);
      if (record != null)
        this.outgoing.Store(record);
    }

    internal IRailStateDelta ProduceDelta(
      Tick basisTick, 
      IRailController destination)
    {
      IRailStateRecord basis = null;
      if (basisTick.IsValid)
        basis = this.outgoing.LatestAt(basisTick);

      return RailState.CreateDelta(
        this.World.Tick,
        this.Id,
        this.State,
        basis,
        (destination == this.controller),
        (basisTick.IsValid == false),
        this.ForceUpdates);
    }
    #endregion

    #region Client
    internal void UpdateClient()
    {
      IRailStateDelta current =
        this.smoother.Update(this.World.Tick, this.State, this.incoming);
      if (current != null)
      {
        this.State.ApplyDelta(current);
        this.DoStart();
      }
    }

    internal void ReceiveDelta(IRailStateDelta delta)
    {
      this.incoming.Store(delta);
    }

    internal bool HasReadyState(Tick tick)
    {
      return (this.incoming.GetLatestAt(tick) != null);
    }

    internal RailState GetSmoothedState(float frameDelta)
    {
      if (this.smoothedState == null)
        this.smoothedState = this.State.Clone();

      RailState result = 
        this.smoother.GetSmoothedState(
          this.World.Tick,
          this.State,
          frameDelta);

      if (result != null)
      {
        this.smoothedState.OverwriteFrom(result);
        RailPool.Free(result);
      }
      else
      {
        this.smoothedState.OverwriteFrom(this.State);
      }

      return this.smoothedState;
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

    public new TState GetSmoothedState(float frameDelta) 
    { 
      return (TState)base.GetSmoothedState(frameDelta); 
    }
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
