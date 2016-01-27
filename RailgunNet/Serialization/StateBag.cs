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
  /// <summary>
  /// A synchronizing, delta-encodable collection of states. 
  /// Compares and updates state contents, and is responsible for 
  /// addition and removal of state entries across snapshots.
  /// </summary>
  internal class StateBag<T> : IPoolable<StateBag<T>>
    where T : State<T>, IPoolable<T>, new()
  {
    #region IPoolable Members
    NodeList<StateBag<T>> INode<StateBag<T>>.List { get; set; }
    StateBag<T> INode<StateBag<T>>.Next { get; set; }
    StateBag<T> INode<StateBag<T>>.Previous { get; set; }
    Pool<StateBag<T>> IPoolable<StateBag<T>>.Pool { get; set; }

    void IPoolable<StateBag<T>>.Initialize() 
    {
      // Do nothing
    }

    void IPoolable<StateBag<T>>.Reset() 
    {
      this.ClearStates();
      this.statePool = null;
    }
    #endregion

    private NodeList<T> stateList;
    private Dictionary<int, T> stateLookup;
    private Pool<T> statePool;

    #region Local Read/Write Access
    /// <summary>
    /// Adds an entity state to the buffer.
    /// </summary>
    public void Add(T state)
    {
      this.stateList.Add(state);
      this.stateLookup[state.Id] = state;
    }

    public T Get(int id)
    {
      return this.stateLookup[id];
    }

    public int Count { get { return this.stateList.Count; } }
    #endregion

    public StateBag()
    {
      this.stateList = new NodeList<T>();
      this.stateLookup = new Dictionary<int, T>();
    }

    public void AssignPool(Pool<T> statePool)
    {
      this.statePool = statePool;
    }

    public void ClearStates()
    {
      Pool.FreeAll(this.stateList);
      this.stateLookup.Clear();
    }

    /// <summary>
    /// Not all entities will be updated in every incoming snapshot. This
    /// routine looks at a previous snapshot and pulls in any entities that 
    /// weren't sent over the network this time around.
    /// </summary>
    private void MigrateSkippedEntities(StateBag<T> basis)
    {
      foreach (KeyValuePair<int, T> pair in basis.stateLookup)
      {
        if (this.stateLookup.ContainsKey(pair.Key) == false)
        {
          T state = this.statePool.Allocate();
          state.Id = pair.Key;
          state.SetFrom(pair.Value);
          this.Add(state);
        }
      }
    }

    #region Serialization
    /// <summary>
    /// Full-encodes the snapshot with no delta compression.
    /// </summary>
    private void Encode(BitPacker bitPacker)
    {
      foreach (T state in this.stateList)
        this.EncodeState(bitPacker, state);
      bitPacker.Push(Encoder.StateCount, this.stateList.Count);
    }

    /// <summary>
    /// Delta-encodes the snapshot relative to a prior snapshot.
    /// </summary>
    private void Encode(BitPacker bitPacker, StateBag<T> basis)
    {
      int numWritten = 0;
      foreach (T state in this.stateList)
        if (this.EncodeState(bitPacker, state, basis))
          numWritten++;
      bitPacker.Push(Encoder.StateCount, numWritten);
    }

    /// <summary>
    /// Full-decodes the state with no delta compression
    /// </summary>
    private void Decode(BitPacker bitPacker)
    {
      this.ClearStates();

      int numStates = bitPacker.Pop(Encoder.StateCount);
      for (int i = 0; i < numStates; i++)
        this.DecodeState(bitPacker);
    }

    /// <summary>
    /// Delta-decodes the snapshot relative to a prior snapshot.
    /// </summary>
    private void Decode(BitPacker bitPacker, StateBag<T> basis)
    {
      this.ClearStates();

      int numStates = bitPacker.Pop(Encoder.StateCount);
      for (int i = 0; i < numStates; i++)
        this.DecodeState(bitPacker, basis);
      this.MigrateSkippedEntities(basis);
    }

    #region State Serialization
    /// <summary>
    /// Full-encodes an entity state.
    /// </summary>
    public void EncodeState(
      BitPacker bitPacker, 
      T state)
    {
      // Write the state data and the state's id
      state.Encode(bitPacker);
      bitPacker.Push(Encoder.StateId, state.Id);
    }

    /// <summary>
    /// Delta-encodes a state. Returns true iff anything was written.
    /// </summary>
    public bool EncodeState(
      BitPacker bitPacker, 
      T state, 
      StateBag<T> basisBag)
    {
      // See if the basis snapshot contains this state
      T basis = null;
      if (basisBag.stateLookup.TryGetValue(state.Id, out basis))
      {
        // If we wrote the data, also write the Id
        if (state.Encode(bitPacker, basis))
        {
          bitPacker.Push(Encoder.StateId, state.Id);
          return true;
        }
        return false;
      }

      // It's a new state, write the full thing
      state.Encode(bitPacker);
      bitPacker.Push(Encoder.StateId, state.Id);
      return true;
    }

    /// <summary>
    /// Full-decodes a state.
    /// </summary>
    /// <param name="bitPacker"></param>
    public void DecodeState(
      BitPacker bitPacker)
    {
      RailgunUtil.Assert(this.statePool != null, "No pool assigned");

      T state = this.statePool.Allocate();
      int id = bitPacker.Pop(Encoder.StateId);
      state.Id = id;

      state.Decode(bitPacker);

      this.Add(state);
    }

    /// <summary>
    /// Delta-decodes a state against another Bag's version.
    /// </summary>
    public void DecodeState(
      BitPacker bitPacker, 
      StateBag<T> basisBag)
    {
      RailgunUtil.Assert(this.statePool != null, "No pool assigned");

      T state = this.statePool.Allocate();
      int id = bitPacker.Pop(Encoder.StateId);
      state.Id = id;

      // See if the basis bag contains this state
      T basis = null;
      if (basisBag.stateLookup.TryGetValue(id, out basis))
        state.Decode(bitPacker, basis);
      else
        state.Decode(bitPacker); // It's a new state

      this.Add(state);
    }
    #endregion

    #endregion

    #region Debug
    public static void Test(int iterations, int numEntities)
    {
      // Populate the entity pool
      Pool<EntityState> pool = new Pool<EntityState>();
      TestFullSerialize(numEntities, pool);
      TestDeltaSerialize(numEntities, pool);

    //  StateBag<EntityState> first = new StateBag<EntityState>();
    //  first.AssignPool(pool);
    //  for (int i = 0; i < entityList.Count; i++)
    //  {
    //    EntityState toAdd = entityList[i];
    //    first.Add(ref toAdd);
    //  }

    ////  Snapshot.UpdateEntityPool(ref entityPool);
    //  Snapshot second = new Snapshot();
    //  for (int i = 0; i < entityList.Count; i++)
    //  {
    //    EntityState toAdd = entityList[i];
    //    first.AddEntityState(ref toAdd);
    //  }
    }

    private static void TestFullSerialize(int numEntities, Pool<EntityState> pool)
    {
      List<EntityState> stateCollection = CreateStateCollection(numEntities, pool);
      StateBag<EntityState> toSend = new StateBag<EntityState>();
      toSend.AssignPool(pool);
      foreach (EntityState state in stateCollection)
        toSend.Add(state);

      BitPacker packer = new BitPacker();
      toSend.Encode(packer);
      Debug.Log("Bag used: " + packer.BitsUsed + " bits.");

      StateBag<EntityState> toReceive = new StateBag<EntityState>();
      toReceive.AssignPool(pool);
      toReceive.Decode(packer);

      TestCompare(toSend, toReceive);
    }

    private static void TestDeltaSerialize(int numEntities, Pool<EntityState> pool)
    {
      // Create the basis bag (we won't be sending this)
      List<EntityState> stateCollection = CreateStateCollection(numEntities, pool);
      StateBag<EntityState> basis = new StateBag<EntityState>();
      basis.AssignPool(pool);
      foreach (EntityState state in stateCollection)
        basis.Add(state);

      // Create the mutated bag (we will be sending this)
      List<EntityState> mutatedStateCollection = CreateMutatedStateCollection(stateCollection, pool);
      StateBag<EntityState> toSend = new StateBag<EntityState>();
      toSend.AssignPool(pool);
      foreach(EntityState state in mutatedStateCollection)
        toSend.Add(state);

      BitPacker packer = new BitPacker();
      toSend.Encode(packer, basis);
      Debug.Log("Delta bag used: " + packer.BitsUsed + " bits.");

      StateBag<EntityState> toReceive = new StateBag<EntityState>();
      toReceive.AssignPool(pool);
      toReceive.Decode(packer, basis);

      TestCompare(toSend, toReceive);
    }

    private static void TestCompare(StateBag<EntityState> a, StateBag<EntityState> b)
    {
      RailgunUtil.Assert(a.Count == b.Count);
      foreach (EntityState stateA in a.stateList)
      {
        EntityState found;
        if (b.stateLookup.TryGetValue(stateA.Id, out found))
        {
          EntityState.TestCompare(stateA, b.Get(stateA.Id));
        }
        else
        {
          RailgunUtil.Assert(false, "Not found in b: " + stateA.Id);
        }
      }
    }

    private static List<EntityState> CreateStateCollection(int count, Pool<EntityState> pool)
    {
      List<EntityState> stateCollection = new List<EntityState>();
      for (int i = 0; i < count; i++)
      {
        // Normally these should be pooled, but we'll shortcut
        EntityState state = pool.Allocate();
        state.Id = i;
        EntityState.PopulateState(state);
        stateCollection.Add(state);
      }
      return stateCollection;
    }

    private static List<EntityState> CreateMutatedStateCollection(List<EntityState> basisCollection, Pool<EntityState> pool)
    {
      List<EntityState> stateCollection = new List<EntityState>();
      for (int i = 0; i < basisCollection.Count; i++)
      {
        EntityState basis = basisCollection[i];
        EntityState state = pool.Allocate();
        state.Id = basis.Id;
        EntityState.MutateState(state, basis);
        stateCollection.Add(state);
      }
      return stateCollection;
    }

    private static void UpdateEntityPool(ref List<EntityState> entityPool)
    {
      for (int i = 0; i < entityPool.Count; i++)
        if (UnityEngine.Random.Range(0.0f, 1.0f) > 0.4f)
          EntityState.MutateState(entityPool[i], entityPool[i]);
    }
    #endregion
  }
}