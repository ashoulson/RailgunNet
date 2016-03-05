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
    internal RailPeerClient Owner { get; set; }

    internal RailStateBuffer StateBuffer { get; private set; }

    public RailStateDelta StateDelta { get; private set; }
    public int CurrentTick { get { return this.World.Tick; } }

    protected internal bool IsMaster { get; internal set; }
    protected internal RailWorld World { get; internal set; }

    protected internal int Id { get { return this.State.Id; } }
    protected internal int Type { get { return this.State.Type; } }
    protected internal RailState State { get; set; }

    protected virtual void OnAddedToWorld() { }

    protected virtual void Simulate() { }
    internal virtual void SimulateCommand(RailCommand command) { }

    internal RailEntity()
    {
      this.Owner = null;
      this.World = null;
      this.StateBuffer = new RailStateBuffer();
    }

    internal void InitializeClient(RailState state)
    {
      this.State = state.Clone(RailClock.INVALID_TICK);
      this.IsMaster = false;
      this.StateDelta = new RailStateDelta();
    }

    internal void InitializeServer(RailState state)
    {
      this.State = state.Clone(RailClock.INVALID_TICK);
      this.IsMaster = true;
      this.StateDelta = null;
    }

    internal void UpdateServer()
    {
      if (this.Owner != null)
      {
        RailCommand command = this.Owner.GetLatestCommand();
        if (command != null)
          this.SimulateCommand(command);
      }
      this.Simulate();
    }

    internal void UpdateClient(int serverTick)
    {
      this.StateDelta.Update(this.StateBuffer, serverTick);
      this.State.SetDataFrom(this.StateDelta.Latest);
    }

    internal void SetToLatest()
    {
      this.State.SetDataFrom(this.StateBuffer.Latest);
    }

    internal bool HasLatest(int serverTick)
    {
      this.StateDelta.Update(this.StateBuffer, serverTick);
      return (this.StateDelta.Latest != null);
    }

    internal void AddedToWorld()
    {
      this.OnAddedToWorld();
    }

    public void AssignOwner(RailPeerClient owner)
    {
      this.Owner = owner;
    }

    internal void StoreState(int tick)
    {
      this.StateBuffer.Store(this.State.Clone(tick));
    }
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
