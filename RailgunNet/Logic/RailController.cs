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
using System.Collections.Generic;

namespace Railgun
{
  public class RailController
  {
    public object UserData { get; set; }

    public virtual Tick EstimatedRemoteTick
    {
      get
      {
        throw new InvalidOperationException(
          "Local controller has no remote tick");
      }
    }

    public IEnumerable<RailEntity> ControlledEntities
    {
      get { return this.controlledEntities; }
    }

    public IRailNetPeer NetPeer
    {
      get { return this.netPeer; }
    }

#if SERVER
    /// <summary>
    /// Used for setting the scope evaluator heuristics.
    /// </summary>
    public RailScopeEvaluator ScopeEvaluator
    {
      set { this.scope.Evaluator = value; }
    }

    /// <summary>
    /// Used for determining which entity updates to send.
    /// </summary>
    internal RailScope Scope { get { return this.scope; } }

    private readonly RailScope scope;
#endif

    /// <summary>
    /// The entities controlled by this controller.
    /// </summary>
    private readonly HashSet<RailEntity> controlledEntities;

    /// <summary>
    /// The network I/O peer for sending/receiving data.
    /// </summary>
    protected readonly IRailNetPeer netPeer;

    internal RailController(
      RailResource resource, 
      IRailNetPeer netPeer = null)
    {
      this.controlledEntities = new HashSet<RailEntity>();
      this.netPeer = netPeer;

#if SERVER
      this.scope = new RailScope(this, resource);
#endif

      if (netPeer != null)
        netPeer.BindController(this);
    }

    /// <summary>
    /// Queues an event to send directly to this peer.
    /// Caller should call Free() on the event when done sending.
    /// </summary>
    public virtual void SendEvent(RailEvent evnt, ushort attempts = 3)
    {
      throw new InvalidOperationException(
        "Cannot send event to local controller");
    }

#if SERVER
    public void GrantControl(RailEntity entity)
    {
      this.GrantControlInternal(entity);
    }

    public void RevokeControl(RailEntity entity)
    {
      this.RevokeControlInternal(entity);
    }
#endif

    #region Controller
    /// <summary>
    /// Detaches the controller from all controlled entities.
    /// </summary>
    internal void Shutdown()
    {
      foreach (RailEntity entity in this.controlledEntities)
        entity.AssignController(null);
      this.controlledEntities.Clear();
    }

    /// <summary>
    /// Adds an entity to be controlled by this peer.
    /// </summary>
    internal void GrantControlInternal(RailEntity entity)
    {
      RailDebug.Assert(entity.IsRemoving == false);
      if (entity.Controller == this)
        return;
      RailDebug.Assert(entity.Controller == null);

      this.controlledEntities.Add(entity);
      entity.AssignController(this);
    }

    /// <summary>
    /// Remove an entity from being controlled by this peer.
    /// </summary>
    internal void RevokeControlInternal(RailEntity entity)
    {
      RailDebug.Assert(entity.Controller == this);

      this.controlledEntities.Remove(entity);
      entity.AssignController(null);
    }
#endregion
  }
}
