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
  public class RailController
  {
    /// <summary>
    /// A history of received packets from the client. Used on the server.
    /// </summary>
    private readonly RailRingBuffer<RailCommand> incomingBuffer;

    /// <summary>
    /// A history of sent commands to the server. Used on the client.
    /// </summary>
    private readonly Queue<RailCommand> outgoingBuffer;

    /// <summary>
    /// The entities controlled by this controller.
    /// </summary>
    private readonly HashSet<RailEntity> controlledEntities;

    /// <summary>
    /// The latest usable command in the dejitter buffer.
    /// </summary>
    internal RailCommand LatestCommand { get; private set; }

    internal IEnumerable<RailEntity> ControlledEntities 
    { 
      get { return this.controlledEntities; } 
    }

    internal IEnumerable<RailCommand> OutgoingCommands
    {
      get { return this.outgoingBuffer; }
    }

    internal RailController() : this(null) { }

    internal RailController(RailClock controllerClock)
    {
      // We use no divisor for storing commands because commands are sent in
      // batches that we can use to fill in the holes between send frames
      this.incomingBuffer =
        new RailRingBuffer<RailCommand>(RailConfig.DEJITTER_BUFFER_LENGTH);
      this.outgoingBuffer = new Queue<RailCommand>();
      this.controlledEntities = new HashSet<RailEntity>();

      this.LatestCommand = null;
    }

    internal void AddControlled(RailEntity entity)
    {
      CommonDebug.Assert(entity.Controller == null);
      this.controlledEntities.Add(entity);
      entity.Controller = this;
    }

    internal void RemoveController(RailEntity entity)
    {
      CommonDebug.Assert(entity.Controller == this);
      this.controlledEntities.Remove(entity);
      entity.Controller = null;
    }

    internal void Update(int tick)
    {
      this.LatestCommand = this.incomingBuffer.GetLatest(tick);
    }

    internal void StoreIncoming(IEnumerable<RailCommand> commands)
    {
      foreach (RailCommand command in commands)
        this.incomingBuffer.Store(command);
    }

    internal void QueueOutgoing(RailCommand command)
    {
      if (this.outgoingBuffer.Count < RailConfig.COMMAND_BUFFER_COUNT)
        this.outgoingBuffer.Enqueue(command);
    }

    internal void CleanCommands(int lastReceivedTick)
    {
      while (true)
      {
        if (this.outgoingBuffer.Count == 0)
          break;
        if (this.outgoingBuffer.Peek().Tick > lastReceivedTick)
          break;
        RailPool.Free(this.outgoingBuffer.Dequeue());
      }
    }
  }
}
