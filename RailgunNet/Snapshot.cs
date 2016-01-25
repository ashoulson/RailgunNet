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

using UnityEngine;

using Reservoir;

namespace Railgun
{
  internal class Snapshot : IPoolable<Snapshot>
  {
    internal const int INVALID_FRAME    = -1;
    internal const int DEFAULT_CAPACITY = 16;

    #region IPoolable Members
    NodeList<Snapshot> INode<Snapshot>.List { get; set; }
    Snapshot INode<Snapshot>.Next { get; set; }
    Snapshot INode<Snapshot>.Previous { get; set; }

    Pool<Snapshot> IPoolable<Snapshot>.Pool { get; set; }

    void IPoolable<Snapshot>.Initialize()
    {
 	    // Do nothing, see parameterized Initialize() below
    }
    #endregion

    private NodeList<EntityState> stateList;
    private Dictionary<int, EntityState> entityLookup;

    // Used for allocating new EntityStates, shared with others
    private Pool<EntityState> statePool;

    #region Local Read/Write Access
    /// <summary>
    /// Adds an entity state to the buffer.
    /// </summary>
    public void AddEntityState(EntityState state)
    {
      this.stateList.Add(state);
      this.entityLookup[state.EntityId] = state;
    }

    public EntityState GetEntityState(int entityId)
    {
      return this.entityLookup[entityId];
    }
    #endregion

    public Snapshot()
    {
      this.stateList = new NodeList<EntityState>();
      this.entityLookup = new Dictionary<int, EntityState>();
    }

    public void Initialize(Pool<EntityState> statePool)
    {
      this.statePool = statePool;
    }

    public void Reset()
    {
      Pool.FreeAll(this.stateList);
      this.entityLookup.Clear();
    }

    /// <summary>
    /// Not all entities will be updated in every incoming snapshot. This
    /// routine looks at a previous snapshot and pulls in any entities that 
    /// weren't sent over the network this time around.
    /// </summary>
    private void MigrateSkippedEntities(Snapshot basis)
    {
      foreach (KeyValuePair<int, EntityState> pair in basis.entityLookup)
        if (this.entityLookup.ContainsKey(pair.Key) == false)
          this.AddEntityState(pair.Value);
    }

    #region Serialization
    /// <summary>
    /// Full-encodes the snapshot with no delta compression.
    /// </summary>
    private void Encode(BitPacker bitPacker)
    {
      foreach (EntityState state in this.stateList)
        state.Encode(bitPacker);
      bitPacker.Push(Encoders.EntityCount, this.stateList.Count);
    }

    /// <summary>
    /// Delta-encodes the snapshot relative to a prior snapshot.
    /// </summary>
    private void Encode(BitPacker bitPacker, Snapshot basis)
    {
      int numWritten = 0;
      foreach (EntityState state in this.stateList)
        if (this.EncodeState(bitPacker, state, basis))
          numWritten++;
      bitPacker.Push(Encoders.EntityCount, numWritten);
    }

    /// <summary>
    /// Full-decodes the state with no delta compression
    /// </summary>
    /// <param name="bitPacker"></param>
    private void Decode(BitPacker bitPacker)
    {
      this.Reset();

      int numEntities = bitPacker.Pop(Encoders.EntityCount);
      for (int i = 0; i < numEntities; i++)
        this.DecodeState(bitPacker);
    }

    /// <summary>
    /// Delta-decodes the snapshot relative to a prior snapshot.
    /// </summary>
    private void Decode(BitPacker bitPacker, Snapshot basis)
    {
      this.Reset();

      int numEntities = bitPacker.Pop(Encoders.EntityCount);
      for (int i = 0; i < numEntities; i++)
        this.DecodeState(bitPacker, basis);
      this.MigrateSkippedEntities(basis);
    }

    #region State Serialization
    /// <summary>
    /// Full-encodes an entity state.
    /// </summary>
    public void EncodeState(
      BitPacker bitPacker, 
      EntityState toEncode)
    {
      toEncode.Encode(bitPacker);
    }

    /// <summary>
    /// Delta-encodes an entity state. Returns true iff anything was written.
    /// </summary>
    public bool EncodeState(
      BitPacker bitPacker, 
      EntityState toEncode, 
      Snapshot basis)
    {
      int entityId = toEncode.EntityId;

      // See if the basis snapshot contains this entity
      EntityState fromBasis = null;
      if (basis.entityLookup.TryGetValue(entityId, out fromBasis))
        return toEncode.Encode(bitPacker, fromBasis);

      // Do a full encoding since this is a new entity
      toEncode.Encode(bitPacker);
      return true;
    }

    /// <summary>
    /// Full-decodes an entity state.
    /// </summary>
    /// <param name="bitPacker"></param>
    public void DecodeState(
      BitPacker bitPacker)
    {
      int entityId = EntityState.PeekId(bitPacker);
      EntityState toStore = this.statePool.Allocate();

      toStore.Decode(bitPacker);

      this.AddEntityState(toStore);
    }

    /// <summary>
    /// Delta-decodes an entity state against another Snapshot.
    /// </summary>
    public void DecodeState(
      BitPacker bitPacker, 
      Snapshot basis)
    {
      int entityId = EntityState.PeekId(bitPacker);
      EntityState toStore = this.statePool.Allocate();

      // See if the basis snapshot contains this entity
      EntityState fromBasis = null;
      if (basis.entityLookup.TryGetValue(entityId, out fromBasis))
        toStore.Decode(bitPacker, fromBasis);
      else
        toStore.Decode(bitPacker); // Hopefully it's a new entity

      this.AddEntityState(toStore);
    }
    #endregion

    #endregion

    #region Debug
    //public static void Test(int iterations, int numEntities)
    //{
    //  // Populate the entity pool
    //  List<EntityState> entityPool = CreateEntityPool(numEntities);
    //  Snapshot.TestFullSerialize(entityPool);

    //  Snapshot first = new Snapshot();
    //  for (int i = 0; i < entityPool.Count; i++)
    //  {
    //    EntityState toAdd = entityPool[i];
    //    first.AddEntityState(ref toAdd);
    //  }

    //  Snapshot.UpdateEntityPool(ref entityPool);
    //  Snapshot second = new Snapshot();
    //  for (int i = 0; i < entityPool.Count; i++)
    //  {
    //    EntityState toAdd = entityPool[i];
    //    first.AddEntityState(ref toAdd);
    //  }
    //}

    //private static void TestFullSerialize(List<EntityState> entityPool)
    //{
    //  int frame = 0;
    //  Snapshot toSend = new Snapshot();
    //  toSend.Initialize();

    //  for (int i = 0; i < entityPool.Count; i++)
    //  {
    //    EntityState toAdd = entityPool[i];
    //    toSend.AddEntityState(ref toAdd);
    //  }

    //  // Test serialization and deserialization
    //  BitPacker packer = new BitPacker();
    //  toSend.Encode(packer);

    //  Debug.Log("Snapshot used: " + packer.BitsUsed + " bits.");

    //  Snapshot toReceive = new Snapshot();
    //  toReceive.Decode(packer);

    //  TestCompare(toSend, toReceive);
    //}

    //private static void TestCompare(Snapshot a, Snapshot b)
    //{
    //  RailgunUtil.Assert(a.EntitiesCount == b.EntitiesCount);
    //  for (int i = 0; i < a.EntitiesCount; i++)
    //  {
    //    EntityState entityA = a.stateBuffer[i];
    //    EntityState entityB = b.GetEntityState(entityA.EntityId);
    //    EntityState.TestCompare(entityA, entityB);
    //  }
    //}

    //private static List<EntityState> CreateEntityPool(int count)
    //{
    //  List<EntityState> entities = new List<EntityState>();
    //  for (int i = 0; i < count; i++)
    //  {
    //    // Normally these should be pooled, but we'll shortcut
    //    entities[i] = new EntityState();
    //    EntityState.GenerateState(i, entities[i]);
    //  }
    //  return entities;
    //}

    //private static void UpdateEntityPool(ref List<EntityState> entityPool)
    //{
    //  for (int i = 0; i < entityPool.Count; i++)
    //    if (UnityEngine.Random.Range(0.0f, 1.0f) > 0.4f)
    //      EntityState.GenerateState(entityPool[i], entityPool[i]);
    //}
    #endregion
  }
}