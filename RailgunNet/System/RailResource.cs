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
    internal static RailResource Instance 
    { 
      get
      {
        if (RailResource.instance == null)
          RailResource.instance = new RailResource();
        return RailResource.instance;
      }
    }

    private static RailResource instance = null;

    private IRailPool<RailServerPacket> serverPacketPool;
    private IRailPool<RailClientPacket> clientPacketPool;

    private IRailPool<RailCommand> commandPool;
    private Dictionary<int, IRailPool<RailState>> statePools;
    private Dictionary<int, IRailPool<RailEvent>> eventPools;
    private Dictionary<int, IRailFactory<RailEntity>> entityFactories;

    /// <summary>
    /// Used for directly creating an entity type without the type Id.
    /// </summary>
    private Dictionary<Type, int> entityTypeToKey;
    private Dictionary<Type, int> eventTypeToKey;

    private RailResource()
    {
      this.serverPacketPool = new RailPool<RailServerPacket>();
      this.clientPacketPool = new RailPool<RailClientPacket>();

      this.commandPool = null;
      this.statePools = new Dictionary<int, IRailPool<RailState>>();
      this.eventPools = new Dictionary<int, IRailPool<RailEvent>>();
      this.entityFactories = new Dictionary<int, IRailFactory<RailEntity>>();

      this.entityTypeToKey = new Dictionary<Type, int>();
      this.eventTypeToKey = new Dictionary<Type, int>();

      // TODO: When there's a thread-safe pass on this, make sure to clone
      // the pools and factories rather than recreating them from searching
      this.Initialize();
    }

    public void Initialize()
    {
      this.RegisterEntities();
      this.RegisterEvents();
      this.RegisterCommand();

      this.CreateEncoder(this.statePools.Keys, ref RailEncoders.EntityType);
      this.CreateEncoder(this.eventPools.Keys, ref RailEncoders.EventType);
    }

    private void RegisterEntities()
    {
      var entityTypes = RailRegistry.FindAll<RegisterEntityAttribute>();
      foreach (var pair in entityTypes)
      {
        Type entityType = pair.Key;
        Type stateType = pair.Value.StateType;

        IRailPool<RailState> statePool =
          RailRegistry.CreatePool<RailState>(stateType);
        IRailFactory<RailEntity> entityFactory = 
          RailRegistry.CreateFactory<RailEntity>(entityType);

        int typeKey = this.statePools.Count;
        this.statePools.Add(typeKey, statePool);
        this.entityFactories.Add(typeKey, entityFactory);
        this.entityTypeToKey.Add(entityType, typeKey);
      }
    }

    private void RegisterEvents()
    {
      var eventTypes = RailRegistry.FindAll<RegisterEventAttribute>();
      foreach (var pair in eventTypes)
      {
        Type eventType = pair.Key;
        IRailPool<RailEvent> statePool =
          RailRegistry.CreatePool<RailEvent>(eventType);

        int typeKey = this.eventPools.Count;
        this.eventPools.Add(typeKey, statePool);
        this.eventTypeToKey.Add(eventType, typeKey);
      }
    }

    private void RegisterCommand()
    {
      var commandTypes = RailRegistry.FindAll<RegisterCommandAttribute>();
      if (commandTypes.Count < 1)
        throw new ApplicationException("No command type registerred");
      else if (commandTypes.Count > 1)
        throw new ApplicationException("Too many command types registerred");

      var pair = commandTypes[0];
      Type commandType = pair.Key;
      this.commandPool = RailRegistry.CreatePool<RailCommand>(commandType);
    }

    internal RailEntity CreateEntity(int type)
    {
      RailEntity entity = this.entityFactories[type].Instantiate();
      entity.Initialize(type);
      return entity;
    }

    internal RailState AllocateState(int type)
    {
      RailState state = this.statePools[type].Allocate();
      state.Initialize(type);
      return state;
    }

    internal RailEvent AllocateEvent(int type)
    {
      RailEvent evnt = this.eventPools[type].Allocate();
      evnt.Initialize(type);
      return evnt;
    }

    internal T CreateEntity<T>()
      where T : RailEntity
    {
      int key = this.entityTypeToKey[typeof(T)];
      return (T)this.CreateEntity(key);
    }

    internal T AllocateEvent<T>()
      where T : RailEvent
    {
      int key = this.eventTypeToKey[typeof(T)];
      return (T)this.AllocateEvent(key);
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

    private void CreateEncoder(
      IEnumerable<int> keys, 
      ref IntEncoder destination)
    {
      int lowest = 0;
      int highest = 0;
      foreach (int type in keys)
      {
        if (lowest > type)
          lowest = type;
        if (highest < type)
          highest = type;
      }
      destination = new IntEncoder(lowest, highest);
    }
  }
}
