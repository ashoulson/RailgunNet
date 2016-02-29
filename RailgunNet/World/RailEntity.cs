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

    protected virtual void OnUpdateServer() { }
    protected virtual void OnUpdateClient() { }
    protected virtual void OnAddedToWorld() { }

    internal RailEntity()
    {
      this.Owner = null;
      this.World = null;
    }

    internal void InitializeClient()
    {
      this.State = null;
      this.IsMaster = false;
      this.StateBuffer = new RailStateBuffer();
      this.StateDelta = new RailStateDelta();
    }

    internal void InitializeServer(RailState state)
    {
      this.State = state;
      this.IsMaster = true;
      this.StateBuffer = null;
      this.StateDelta = null;
    }

    internal void UpdateServer()
    {
      this.OnUpdateServer();
    }

    internal void UpdateClient(int serverTick)
    {
      this.StateDelta.Update(this.StateBuffer, serverTick);
      this.State = this.StateDelta.Latest;
      this.OnUpdateClient();
    }

    internal bool CheckDelta(int serverTick)
    {
      this.StateDelta.Update(this.StateBuffer, serverTick);
      this.State = this.StateDelta.Latest;
      return (this.State != null);
    }

    internal void AddedToWorld()
    {
      this.OnAddedToWorld();
    }

    public void AssignOwner(RailPeerClient owner)
    {
      this.Owner = owner;
    }

    protected T GetLatestCommand<T>()
      where T : RailCommand<T>, new()
    {
      if (this.Owner != null)
        return this.Owner.GetLatestCommand<T>();
      return null;
    }

    internal RailState CloneForSnapshot(int tick)
    {
      RailState clone = this.State.Clone(tick);
      return clone;
    }
  }

  /// <summary>
  /// Handy shortcut class for auto-casting the internal state.
  /// </summary>
  public abstract class RailEntity<T> : RailEntity
    where T : RailState
  {
    public new T State { get { return (T)base.State; } }
  }
}
