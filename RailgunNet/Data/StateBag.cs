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

using Reservoir;

namespace Railgun
{
  /// <summary>
  /// A synchronizing, delta-encodable collection of states. 
  /// Compares and updates state contents, and is responsible for 
  /// addition and removal of state entries across snapshots.
  /// </summary>
  internal class StateBag<T> : Poolable<StateBag<T>>
    where T : State<T>, new()
  {
    internal NodeList<T> stateList;
    internal Dictionary<int, T> stateLookup;
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

    public T GetEmpty(int id)
    {
      T state = this.statePool.Allocate();
      state.Id = id;
      return state;
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

    protected override void Initialize()
    {
      this.ClearStates();
      this.statePool = null;
    }

    public void ClearStates()
    {
      Pool.FreeAll(this.stateList);
      this.stateLookup.Clear();
    }

    #region Serialization
    /// <summary>
    /// Full-encodes the snapshot with no delta compression.
    /// </summary>
    internal void Encode(BitPacker bitPacker)
    {
      foreach (T state in this.stateList)
        this.EncodeState(bitPacker, state);
      bitPacker.Push(InternalEncoders.StateCount, this.stateList.Count);
    }

    /// <summary>
    /// Delta-encodes the snapshot relative to a prior snapshot.
    /// </summary>
    internal void Encode(BitPacker bitPacker, StateBag<T> basis)
    {
      int numWritten = 0;
      foreach (T state in this.stateList)
        if (this.EncodeState(bitPacker, state, basis))
          numWritten++;
      bitPacker.Push(InternalEncoders.StateCount, numWritten);
    }

    /// <summary>
    /// Full-decodes the state with no delta compression
    /// </summary>
    internal void Decode(BitPacker bitPacker)
    {
      this.ClearStates();

      int numStates = bitPacker.Pop(InternalEncoders.StateCount);
      for (int i = 0; i < numStates; i++)
        this.DecodeState(bitPacker);
    }

    /// <summary>
    /// Delta-decodes the snapshot relative to a prior snapshot.
    /// </summary>
    internal void Decode(BitPacker bitPacker, StateBag<T> basis)
    {
      this.ClearStates();

      int numStates = bitPacker.Pop(InternalEncoders.StateCount);
      for (int i = 0; i < numStates; i++)
        this.DecodeState(bitPacker, basis);
      this.MigrateSkippedEntities(basis);
    }

    #region State Serialization
    /// <summary>
    /// Full-encodes an entity state.
    /// </summary>
    private void EncodeState(
      BitPacker bitPacker, 
      T state)
    {
      // Write the state data and the state's id
      state.Encode(bitPacker);
      bitPacker.Push(InternalEncoders.StateId, state.Id);
    }

    /// <summary>
    /// Delta-encodes a state. Returns true iff anything was written.
    /// </summary>
    private bool EncodeState(
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
          bitPacker.Push(InternalEncoders.StateId, state.Id);
          return true;
        }
        return false;
      }

      // It's a new state, write the full thing
      state.Encode(bitPacker);
      bitPacker.Push(InternalEncoders.StateId, state.Id);
      return true;
    }

    /// <summary>
    /// Full-decodes a state.
    /// </summary>
    /// <param name="bitPacker"></param>
    private void DecodeState(
      BitPacker bitPacker)
    {
      RailgunUtil.Assert(this.statePool != null, "No pool assigned");

      T state = this.statePool.Allocate();
      int id = bitPacker.Pop(InternalEncoders.StateId);
      state.Id = id;

      state.Decode(bitPacker);

      this.Add(state);
    }

    /// <summary>
    /// Delta-decodes a state against another Bag's version.
    /// </summary>
    private void DecodeState(
      BitPacker bitPacker, 
      StateBag<T> basisBag)
    {
      RailgunUtil.Assert(this.statePool != null, "No pool assigned");

      T state = this.statePool.Allocate();
      int id = bitPacker.Pop(InternalEncoders.StateId);
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

    #endregion
  }
}