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
  internal class RailResource
  {
    private class MasterInstance
    {
      // Read-only data structures, don't need to be thread-local
      internal Dictionary<int, IRailPool<RailEntity>> EntityFactories { get; private set; }
      internal Dictionary<Type, int> EntityTypeToKey { get; private set; }
      internal Dictionary<Type, int> EventTypeToKey { get; private set; }
      internal RailIntCompressor EventTypeCompressor { get; private set; }
      internal RailIntCompressor EntityTypeCompressor { get; private set; }

      // Mutable pools, need to be cloned per-thread
      private IRailPool<RailCommand> commandPool;
      private Dictionary<int, IRailPool<RailState>> statePools;
      private Dictionary<int, IRailPool<RailEvent>> eventPools;

      internal MasterInstance()
      {
        this.EntityFactories = new Dictionary<int, IRailPool<RailEntity>>();
        this.EntityTypeToKey = new Dictionary<Type, int>();
        this.EventTypeToKey = new Dictionary<Type, int>();
        this.EntityTypeCompressor = null;
        this.EntityTypeCompressor = null;

        this.commandPool = null;
        this.statePools = new Dictionary<int, IRailPool<RailState>>();
        this.eventPools = new Dictionary<int, IRailPool<RailEvent>>();

        this.RegisterCommand();
        this.RegisterEntities();
        this.RegisterEvents();
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
          IRailPool<RailEntity> entityPool =
            RailRegistry.CreatePool<RailEntity>(entityType);

          int typeKey = this.statePools.Count + 1; // 0 is an invalid type
          this.statePools.Add(typeKey, statePool);
          this.EntityFactories.Add(typeKey, entityPool);
          this.EntityTypeToKey.Add(entityType, typeKey);
        }

        this.EntityTypeCompressor =
          new RailIntCompressor(0, this.EntityFactories.Count + 1);
      }

      private void RegisterEvents()
      {
        var eventTypes = RailRegistry.FindAll<RegisterEventAttribute>();
        foreach (var pair in eventTypes)
        {
          Type eventType = pair.Key;
          IRailPool<RailEvent> statePool =
            RailRegistry.CreatePool<RailEvent>(eventType);

          int typeKey = this.eventPools.Count + 1; // 0 is an invalid type
          this.eventPools.Add(typeKey, statePool);
          this.EventTypeToKey.Add(eventType, typeKey);
        }

        this.EventTypeCompressor =
          new RailIntCompressor(0, this.eventPools.Count + 1);
      }

      private void RegisterCommand()
      {
        var commandTypes = RailRegistry.FindAll<RegisterCommandAttribute>();
        if (commandTypes.Count < 1)
          throw new ApplicationException("No command type registerred");
        else if (commandTypes.Count > 1)
          throw new ApplicationException("Too many command types registerred");

        var pair = commandTypes[0];
        this.commandPool = RailRegistry.CreatePool<RailCommand>(pair.Key);
      }

      internal IRailPool<RailCommand> CloneCommandPool()
      {
        return this.commandPool.Clone();
      }

      internal Dictionary<int, IRailPool<RailState>> CloneStatePools()
      {
        var pools = new Dictionary<int, IRailPool<RailState>>();
        foreach (var pair in this.statePools)
          pools.Add(pair.Key, pair.Value.Clone());
        return pools;
      }

      internal Dictionary<int, IRailPool<RailEvent>> CloneEventPools()
      {
        var pools = new Dictionary<int, IRailPool<RailEvent>>();
        foreach (var pair in this.eventPools)
          pools.Add(pair.Key, pair.Value.Clone());
        return pools;
      }
    }

    private static MasterInstance Master
    {
      get
      {
        lock (RailResource.masterLock)
        {
          if (RailResource.master == null)
            RailResource.master = new MasterInstance();
          return RailResource.master;
        }
      }
    }

    // Used for locking when creating thread-local instances
    private static object masterLock = new object();

    // Master resource holder, used for read-only data and creating instances
    private static MasterInstance master;

    [ThreadStatic]
    private static RailResource instance;

    internal static RailResource Instance
    {
      get
      {
        if (RailResource.instance == null)
          RailResource.instance = new RailResource();
        return RailResource.instance;
      }
    }

    // Taken from the master
    public RailIntCompressor EventTypeCompressor { get; private set; }
    public RailIntCompressor EntityTypeCompressor { get; private set; }
    private Dictionary<int, IRailPool<RailEntity>> entityFactories;
    private Dictionary<Type, int> entityTypeToKey;
    private Dictionary<Type, int> eventTypeToKey;

    // Mutable pools, need to be cloned per-thread
    private IRailPool<RailCommand> commandPool;
    private Dictionary<int, IRailPool<RailState>> statePools;
    private Dictionary<int, IRailPool<RailEvent>> eventPools;

    private IRailPool<RailServerPacket> serverPacketPool;
    private IRailPool<RailClientPacket> clientPacketPool;

    private IRailPool<RailStateDelta> deltaPool;
    private IRailPool<RailStateRecord> recordPool;
    private IRailPool<RailCommandUpdate> commandUpdatePool;

    private RailResource()
    {
      MasterInstance master = RailResource.Master;

      // Copy references to read-only stuff from the master
      this.EventTypeCompressor = master.EventTypeCompressor;
      this.EntityTypeCompressor = master.EntityTypeCompressor;
      this.entityFactories = master.EntityFactories;
      this.entityTypeToKey = master.EntityTypeToKey;
      this.eventTypeToKey = master.EventTypeToKey;

      // Clone or instantiate the rest
      this.commandPool = master.CloneCommandPool();
      this.statePools = master.CloneStatePools();
      this.eventPools = master.CloneEventPools();

      this.serverPacketPool = new RailPool<RailServerPacket>();
      this.clientPacketPool = new RailPool<RailClientPacket>();

      this.deltaPool = new RailPool<RailStateDelta>();
      this.recordPool = new RailPool<RailStateRecord>();
      this.commandUpdatePool = new RailPool<RailCommandUpdate>();
    }

    #region Allocation
    public RailEntity CreateEntity(int factoryType)
    {
      return this.entityFactories[factoryType].Allocate();
    }

    public RailCommand CreateCommand()
    {
      return this.commandPool.Allocate();
    }

    public RailState CreateState(int factoryType)
    {
      return this.statePools[factoryType].Allocate();
    }

    public RailEvent CreateEvent(int factoryType)
    {
      return this.eventPools[factoryType].Allocate();
    }

    public RailServerPacket CreateServerPacket()
    {
      return this.serverPacketPool.Allocate();
    }

    public RailClientPacket CreateClientPacket()
    {
      return this.clientPacketPool.Allocate();
    }

    public RailStateDelta CreateDelta()
    {
      return this.deltaPool.Allocate();
    }

    public RailStateRecord CreateRecord()
    {
      return this.recordPool.Allocate();
    }

    public RailCommandUpdate CreateCommandUpdate()
    {
      return this.commandUpdatePool.Allocate();
    }

    #region Typed
    public int GetEntityFactoryType<T>() 
      where T : RailEntity
    {
      return this.entityTypeToKey[typeof(T)];
    }

    public int GetEventFactoryType<T>() 
      where T : RailEvent
    {
      return this.eventTypeToKey[typeof(T)];
    }
    #endregion
    #endregion
  }
}
