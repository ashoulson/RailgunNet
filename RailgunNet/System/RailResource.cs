/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016-2018 - Alexander Shoulson - http://ashoulson.com
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
using System.Reflection;

namespace Railgun
{
  internal class RailResource
  {
    private static IRailPool<T> CreatePool<T>(
      Type derivedType)
      where T : IRailPoolable<T>
    {
      Type factoryType = typeof(RailPool<,>);
      Type specific =
        factoryType.MakeGenericType(typeof(T), derivedType);
      ConstructorInfo ci = specific.GetConstructor(Type.EmptyTypes);
      return (IRailPool<T>)ci.Invoke(new object[] { });
    }

    internal RailIntCompressor EventTypeCompressor { get { return this.eventTypeCompressor; } }
    internal RailIntCompressor EntityTypeCompressor { get { return this.entityTypeCompressor; } }

    private readonly RailIntCompressor eventTypeCompressor;
    private readonly RailIntCompressor entityTypeCompressor;

    private readonly Dictionary<Type, int> entityTypeToKey;
    private readonly Dictionary<Type, int> eventTypeToKey;

    private readonly IRailPool<RailCommand> commandPool;
    private readonly Dictionary<int, IRailPool<RailEntity>> entityPools;
    private readonly Dictionary<int, IRailPool<RailState>> statePools;
    private readonly Dictionary<int, IRailPool<RailEvent>> eventPools;

    private readonly IRailPool<RailStateDelta> deltaPool;
    private readonly IRailPool<RailCommandUpdate> commandUpdatePool;
#if SERVER
    private readonly IRailPool<RailStateRecord> recordPool;
#endif

    internal RailResource(RailRegistry registry)
    {
      this.entityTypeToKey = new Dictionary<Type, int>();
      this.eventTypeToKey = new Dictionary<Type, int>();

      this.commandPool = this.CreateCommandPool(registry);
      this.entityPools = new Dictionary<int, IRailPool<RailEntity>>();
      this.statePools = new Dictionary<int, IRailPool<RailState>>();
      this.eventPools = new Dictionary<int, IRailPool<RailEvent>>();

      this.RegisterEvents(registry);
      this.RegisterEntities(registry);

      this.eventTypeCompressor =
        new RailIntCompressor(0, this.eventPools.Count + 1);
      this.entityTypeCompressor = 
        new RailIntCompressor(0, this.entityPools.Count + 1);

      this.deltaPool = new RailPool<RailStateDelta>();
      this.commandUpdatePool = new RailPool<RailCommandUpdate>();
#if SERVER
      this.recordPool = new RailPool<RailStateRecord>();
#endif
    }

    private IRailPool<RailCommand> CreateCommandPool(
      RailRegistry registry)
    {
      return
        RailResource.CreatePool<RailCommand>(
          registry.CommandType);
    }

    private void RegisterEvents(
      RailRegistry registry)
    {
      foreach (Type eventType in registry.EventTypes)
      {
        IRailPool<RailEvent> statePool =
          RailResource.CreatePool<RailEvent>(eventType);

        int typeKey = this.eventPools.Count + 1; // 0 is an invalid type
        this.eventPools.Add(typeKey, statePool);
        this.eventTypeToKey.Add(eventType, typeKey);
      }
    }

    private void RegisterEntities(
      RailRegistry registry)
    {
      foreach (KeyValuePair<Type, Type> pair in registry.EntityTypes)
      {
        Type entityType = pair.Key;
        Type stateType = pair.Value;

        IRailPool<RailState> statePool =
          RailResource.CreatePool<RailState>(stateType);
        IRailPool<RailEntity> entityPool =
          RailResource.CreatePool<RailEntity>(entityType);

        int typeKey = this.statePools.Count + 1; // 0 is an invalid type
        this.statePools.Add(typeKey, statePool);
        this.entityPools.Add(typeKey, entityPool);
        this.entityTypeToKey.Add(entityType, typeKey);
      }
    }

    #region Allocation
    public RailCommand CreateCommand()
    {
      return this.commandPool.Allocate();
    }

    public RailEntity CreateEntity(int factoryType)
    {
      return this.entityPools[factoryType].Allocate();
    }

    public RailState CreateState(int factoryType)
    {
      return this.statePools[factoryType].Allocate();
    }

    public RailEvent CreateEvent(int factoryType)
    {
      return this.eventPools[factoryType].Allocate();
    }

    public RailStateDelta CreateDelta()
    {
      return this.deltaPool.Allocate();
    }

    public RailCommandUpdate CreateCommandUpdate()
    {
      return this.commandUpdatePool.Allocate();
    }

#if SERVER
    public RailStateRecord CreateRecord()
    {
      return this.recordPool.Allocate();
    }
#endif

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
