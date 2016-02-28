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
    public delegate void UpdateEvent(int tick);

    public event UpdateEvent StateUpdated;

    internal RailPeerClient Owner { get; set; }

    protected internal bool IsMaster { get; internal set; }
    protected internal RailWorld World { get; internal set; }

    protected internal int Id { get { return this.State.Id; } }
    protected internal int Type { get { return this.State.Type; } }
    protected internal RailState State { get; set; }

    protected internal virtual void OnUpdateHost() { }
    protected internal virtual void OnAddedToWorld() { }
    protected internal virtual void OnStateUpdated(int tick) { }

    public void AssignOwner(RailPeerClient owner)
    {
      this.Owner = owner;
    }

    internal void NotifyStateUpdated(int tick)
    {
      this.OnStateUpdated(tick);
      if (this.StateUpdated != null)
        this.StateUpdated.Invoke(tick);
    }

    protected T GetLatestCommand<T>()
      where T : RailCommand<T>, new()
    {
      if (this.Owner != null)
        if (this.Owner.latestInput != null)
          return (T)this.Owner.latestInput.Command;
      return null;
    }

    internal RailState CreateState()
    {
      return this.State.Clone();
    }
  }

  /// <summary>
  /// Handy shortcut class for auto-casting the internal state.
  /// </summary>
  public abstract class RailEntity<T> : RailEntity
    where T : RailState
  {
    private T typedState = null;
    public new T State 
    { 
      get 
      { 
        if (this.typedState == null)
          this.typedState = (T)base.State;
        return this.typedState;
      }
      set
      {
        this.typedState = value;
        base.State = value;
      }
    }
  }
}
