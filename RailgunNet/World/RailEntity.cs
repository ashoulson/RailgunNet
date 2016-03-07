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

namespace Railgun
{
  public abstract class RailEntity
  {
    public const int INVALID_ID = -1;

    internal RailController Controller { get; set; }
    internal RailStateBuffer StateBuffer { get; private set; }
    internal bool IsAwake { get; private set; }

    public RailStateDelta StateDelta { get; private set; }
    public int CurrentTick { get { return this.World.Tick; } }

    protected internal bool IsMaster { get; internal set; }
    protected internal RailWorld World { get; internal set; }

    protected internal int Id { get { return this.State.Id; } }
    protected internal int Type { get { return this.State.EntityType; } }
    protected internal RailState State { get; set; }

    /// <summary>
    /// The server tick this entity was created on -- not synchronized.
    /// </summary>
    internal int TickCreated { get; set; }

    protected virtual void OnAddedToWorld() { }
    protected virtual void OnControllerChanged() { }

    protected virtual void Simulate() { }
    internal virtual void SimulateCommand(RailCommand command) { }

    public bool IsPredicted
    {
      get { return ((this.Controller != null) && (this.IsMaster == false)); }
    }

    internal RailEntity()
    {
      this.Controller = null;
      this.World = null;
      this.StateBuffer = new RailStateBuffer();
      this.IsAwake = false;
    }

    internal void InitializeClient(RailState state)
    {
      this.State = state.Clone();
      this.State.Tick = RailClock.INVALID_TICK;

      this.IsMaster = false;
      this.StateDelta = new RailStateDelta();
    }

    internal void InitializeServer(RailState state)
    {
      this.State = state.Clone();
      this.State.Tick = RailClock.INVALID_TICK;

      this.IsMaster = true;
      this.StateDelta = null;
    }

    internal void UpdateServer()
    {
      if (this.Controller != null)
      {
        RailCommand command = this.Controller.LatestCommand;
        if (command != null)
          this.SimulateCommand(command);
      }
      this.Simulate();
    }

    internal void UpdateClient(int serverTick)
    {
      if (this.IsAwake)
      {
        if (this.Controller != null)
          this.ForwardSimulate();
        else
          this.ReplicaSimulate(serverTick);
      }
    }

    internal void ReplicaSimulate(int serverTick)
    {
      this.ClearDelta();

      this.StateDelta.Update(this.StateBuffer, serverTick);
      this.State.SetDataFrom(this.StateDelta.Latest);
    }

    internal bool HasLatest(int serverTick)
    {
      return (this.StateBuffer.GetLatest(serverTick) != null);
    }

    internal void AddedToWorld()
    {
      this.IsAwake = true;
      this.OnAddedToWorld();
      this.OnControllerChanged();    
    }

    internal void ControllerChanged()
    {
      if (this.IsAwake)
        this.OnControllerChanged();
    }

    internal void StoreState(int tick)
    {
      RailState state = this.State.Clone();
      state.Tick = tick;
      this.StateBuffer.Store(state);
    }

    #region Smoothing
    public T GetSmoothedValue<T>(
      float frameDelta, 
      RailSmoother<T> smoother)
    {
      if ((this.StateDelta == null) || (this.StateDelta.Latest == null))
        return default(T);

      // If we're predicting, advance to the prediction tick. This is
      // hacky in that it assumes that we'll only ever have a one-tick 
      // difference between any two states in the delta when doing prediction.
      int currentTick = this.CurrentTick;
      if (this.StateDelta.Latest.IsPredicted)
        currentTick = this.StateDelta.Latest.Tick;

      if (this.StateDelta.Next != null)
      {
        return smoother.Smooth(
          frameDelta,
          currentTick,
          this.StateDelta.Latest,
          this.StateDelta.Next);
      }
      else if (this.StateDelta.Prior != null)
      {
        return smoother.Smooth(
          frameDelta,
          currentTick,
          this.StateDelta.Prior,
          this.StateDelta.Latest);
      }
      else
      {
        return smoother.Access(this.StateDelta.Latest);
      }
    }
    #endregion

    #region Prediction
    private void ForwardSimulate()
    {
      if (this.StateDelta == null)
        return;

      this.ClearDelta();

      RailState latest = this.StateBuffer.Latest.Clone();
      latest.IsPredicted = true;
      this.StateDelta.Set(null, latest, null);
      this.State.SetDataFrom(latest);

      int count = 1;
      foreach (RailCommand command in this.Controller.OutgoingCommands)
      {
        this.SimulateCommand(command);
        this.PushDelta(latest.Tick + count);
        count++;
      }
    }

    private void ClearDelta()
    {
      RailState prior = this.StateDelta.Prior;
      RailState latest = this.StateDelta.Latest;
      RailState next = this.StateDelta.Next;

      if ((prior != null) && prior.IsPredicted)
        RailPool.Free(prior);
      if ((latest != null) && latest.IsPredicted)
        RailPool.Free(latest);
      if ((next != null) && next.IsPredicted)
        RailPool.Free(next);

      this.StateDelta.Clear();
    }

    private void PushDelta(int count)
    {
      RailState predicted = this.State.Clone();
      predicted.Tick += count;
      predicted.IsPredicted = true;

      RailState popped = this.StateDelta.Push(predicted);
      if ((popped != null) && popped.IsPredicted)
        RailPool.Free(popped);
    }
    #endregion

    #region DEBUG
    public virtual string DEBUG_FormatDebug() 
    {
      string output = "[";
      foreach (RailState state in this.StateBuffer.Values)
        output += state.Tick + ":" + state.DEBUG_FormatDebug() + ",";
      output = output.Remove(output.Length - 1, 1) + "] (";

      if (this.StateDelta != null)
      {
        if (this.StateDelta.Prior != null)
          output += this.StateDelta.Prior.Tick;
        output += ",";
        if (this.StateDelta.Latest != null)
          output += this.StateDelta.Latest.Tick;
        output += ",";
        if (this.StateDelta.Next != null)
          output += this.StateDelta.Next.Tick;
        output += ")";
      }

      return output;
    }
    #endregion
  }

  /// <summary>
  /// Handy shortcut class for auto-casting the internal state.
  /// </summary>
  public abstract class RailEntity<TState> : RailEntity
    where TState : RailState
  {
    public new TState State { get { return (TState)base.State; } }
  }

  /// <summary>
  /// Handy shortcut class for auto-casting the internal state and command.
  /// </summary>
  public abstract class RailEntity<TState, TCommand> : RailEntity<TState>
    where TState : RailState
    where TCommand : RailCommand
  {
    internal override void SimulateCommand(RailCommand command)
    {
      this.SimulateCommand((TCommand)command);
    }

    protected virtual void SimulateCommand(TCommand command) { }
  }
}
