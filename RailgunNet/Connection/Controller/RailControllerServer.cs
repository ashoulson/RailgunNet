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
  public class RailControllerServer : RailController
  {
    /// <summary>
    /// A history of received packets from the client. Used on the server.
    /// </summary>
    private readonly RailRingBuffer<RailCommand> incomingBuffer;

    /// <summary>
    /// The latest usable command in the dejitter buffer.
    /// </summary>
    internal override RailCommand LatestCommand
    {
      get { return this.latestCommand; }
    }

    private RailCommand latestCommand;

    internal RailControllerServer() : base()
    {
      // We use no divisor for storing commands because commands are sent in
      // batches that we can use to fill in the holes between send frames
      this.incomingBuffer =
        new RailRingBuffer<RailCommand>(RailConfig.DEJITTER_BUFFER_LENGTH);
      this.latestCommand = null;
    }

    internal void Update(int tick)
    {
      this.latestCommand = this.incomingBuffer.GetLatest(tick);
    }

    /// <summary>
    /// Stores incoming commands to be buffered in the dejitter buffer.
    /// </summary>
    internal void StoreIncoming(IEnumerable<RailCommand> commands)
    {
      foreach (RailCommand command in commands)
        this.incomingBuffer.Store(command);
    }

    /// <summary>
    /// Adds an entity to be controlled by this controller.
    /// </summary>
    internal void AddEntity(RailEntity entity, int tick)
    {
      if (entity.Controller == this)
        return;

      CommonDebug.Assert(entity.Controller == null);
      this.controlledEntities.Add(entity);

      entity.Controller = this;
      entity.ControllerChanged();

      this.QueueControlEvent(entity.Id, true, tick);
    }

    /// <summary>
    /// Remove an entity from being controlled by this controller.
    /// </summary>
    internal void RemoveEntity(RailEntity entity, int tick)
    {
      CommonDebug.Assert(entity.Controller == this);
      this.controlledEntities.Remove(entity);

      entity.Controller = null;
      entity.ControllerChanged();

      this.QueueControlEvent(entity.Id, false, tick);
    }

    internal override void Shutdown()
    {
      foreach (RailEntity entity in this.controlledEntities)
      {
        entity.Controller = null;
        entity.ControllerChanged();
      }
      this.controlledEntities.Clear();
    }

    private void QueueControlEvent(int entityId, bool granted, int tick)
    {
      RailControlEvent controlEvent =
        RailResource.Instance.AllocateControlEvent();
      controlEvent.EntityId = entityId;
      controlEvent.Granted = granted;
      this.QueueReliable(controlEvent, tick);
      RailPool.Free(controlEvent);
    }
  }
}
