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
  internal class RailResource
  {
    // TODO: Make this thread-safe (like [ThreadStatic])
    internal static RailResource Instance { get; private set; }

    internal static void Initialize()
    {
      RailResource.Instance = new RailResource();
    }

    private RailPool<RailServerPacket> serverPacketPool;
    private RailPool<RailClientPacket> clientPacketPool;

    private RailPool<RailCommand> commandPool;

    private Dictionary<int, RailPool<RailState>> statePools;
    private Dictionary<int, RailPool<RailEvent>> eventPools;

    private Dictionary<int, RailFactory<RailEntity>> entityFactories;

    private RailResource()
    {
      this.serverPacketPool = 
        new RailPool<RailServerPacket, RailServerPacket>();
      this.clientPacketPool = 
        new RailPool<RailClientPacket, RailClientPacket>();

      this.commandPool = null;
      this.statePools = new Dictionary<int, RailPool<RailState>>();
      this.eventPools = new Dictionary<int, RailPool<RailEvent>>();
      this.entityFactories = new Dictionary<int, RailFactory<RailEntity>>();
    }

    internal void RegisterEntityType<TEntity, TState>(int type)
      where TEntity : RailEntity<TState>, new()
      where TState : RailState, new()
    {
      this.entityFactories[type] = new RailFactory<RailEntity, TEntity>();
      this.statePools[type] = new RailPool<RailState, TState>();
    }

    internal void RegisterEventType<TEvent>(int type)
      where TEvent : RailEvent, new()
    {
      this.eventPools[type] = new RailPool<RailEvent, TEvent>();
    }

    internal void RegisterCommandType<TCommand>()
      where TCommand : RailCommand, new()
    {
      CommonDebug.Assert(this.commandPool == null);
      this.commandPool = new RailPool<RailCommand, TCommand>();
    }

    internal RailEntity CreateEntity(int type)
    {
      RailEntity entity = this.entityFactories[type].Create();
      entity.Initialize(type);
      return entity;
    }

    internal RailState AllocateState(int type)
    {
      RailState state = this.statePools[type].Allocate();
      state.Initialize(type);
      return state;
    }

    internal RailServerPacket AllocateServerPacket()
    {
      return this.serverPacketPool.Allocate();
    }

    internal RailClientPacket AllocateClientPacket()
    {
      return this.clientPacketPool.Allocate();
    }

    internal RailCommand AllocateCommand()
    {
      return this.commandPool.Allocate();
    }

    internal RailEvent AllocateEvent(int type)
    {
      return this.eventPools[type].Allocate();
    }
  }
}
