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
    internal static void Initialize()
    {
      RailResource.instance = new RailResource();
    }

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

    internal IntCompressor EventTypeCompressor { get { return this.eventTypeCompressor; } }
    internal IntCompressor EntityTypeCompressor { get { return this.entityTypeCompressor; } }

    private IntCompressor eventTypeCompressor = null;
    private IntCompressor entityTypeCompressor = null;

    private IRailPool<RailCommand> masterCommandPool;
    private Dictionary<int, IRailPool<RailState>> masterStatePools;
    private Dictionary<int, IRailPool<RailEvent>> masterEventPools;
    private Dictionary<int, IRailFactory<RailEntity>> masterEntityFactories;

    private Dictionary<Type, int> entityTypeToKey;
    private Dictionary<Type, int> eventTypeToKey;

    private RailResource()
    {
      this.masterCommandPool = null;
      this.masterStatePools = new Dictionary<int, IRailPool<RailState>>();
      this.masterEventPools = new Dictionary<int, IRailPool<RailEvent>>();
      this.masterEntityFactories = new Dictionary<int, IRailFactory<RailEntity>>();

      this.entityTypeToKey = new Dictionary<Type, int>();
      this.eventTypeToKey = new Dictionary<Type, int>();

      this.RegisterEntities();
      this.RegisterEvents();
      this.RegisterCommand();
    }

    internal int EntityTypeToKey<T>() where T : RailEntity
    {
      return this.entityTypeToKey[typeof(T)];
    }

    internal int EventTypeToKey<T>() where T : RailEvent
    {
      return this.eventTypeToKey[typeof(T)];
    }

    internal IRailPool<RailCommand> CloneCommandPool()
    {
      return this.masterCommandPool.Clone();
    }

    internal Dictionary<int, IRailPool<RailState>> CloneStatePools()
    {
      var pools = new Dictionary<int, IRailPool<RailState>>();
      foreach (var pair in this.masterStatePools)
        pools.Add(pair.Key, pair.Value.Clone());
      return pools;
    }

    internal Dictionary<int, IRailPool<RailEvent>> CloneEventPools()
    {
      var pools = new Dictionary<int, IRailPool<RailEvent>>();
      foreach (var pair in this.masterEventPools)
        pools.Add(pair.Key, pair.Value.Clone());
      return pools;
    }

    internal Dictionary<int, IRailFactory<RailEntity>> CloneEntityFactories()
    {
      var factories = new Dictionary<int, IRailFactory<RailEntity>>();
      foreach (var pair in this.masterEntityFactories)
        factories.Add(pair.Key, pair.Value.Clone());
      return factories;
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

        int typeKey = this.masterStatePools.Count + 1; // 0 is an invalid type
        this.masterStatePools.Add(typeKey, statePool);
        this.masterEntityFactories.Add(typeKey, entityFactory);
        this.entityTypeToKey.Add(entityType, typeKey);
      }

      this.entityTypeCompressor = 
        new IntCompressor(0, this.masterEntityFactories.Count + 1);
    }

    private void RegisterEvents()
    {
      var eventTypes = RailRegistry.FindAll<RegisterEventAttribute>();
      foreach (var pair in eventTypes)
      {
        Type eventType = pair.Key;
        IRailPool<RailEvent> statePool =
          RailRegistry.CreatePool<RailEvent>(eventType);

        int typeKey = this.masterEventPools.Count + 1; // 0 is an invalid type
        this.masterEventPools.Add(typeKey, statePool);
        this.eventTypeToKey.Add(eventType, typeKey);
      }

      this.eventTypeCompressor =
        new IntCompressor(0, this.masterEventPools.Count + 1);
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
      this.masterCommandPool = RailRegistry.CreatePool<RailCommand>(commandType);
    }
  }
}
