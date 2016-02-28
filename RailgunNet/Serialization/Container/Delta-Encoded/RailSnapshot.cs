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
  /// <summary>
  /// A snapshot is a collection of entity states representing a complete
  /// state of the world at a given frame.
  /// </summary>
  public class RailSnapshot 
    : IRailPoolable, IRailRingValue
  {
    RailPool IRailPoolable.Pool { get; set; }
    void IRailPoolable.Reset() { this.Reset(); }
    int IRailRingValue.Key { get { return this.Tick; } }

    public int Tick { get; internal protected set; }

    private Dictionary<int, RailState> states;

    public RailSnapshot()
    {
      this.states = new Dictionary<int, RailState>();
      this.Tick = RailClock.INVALID_TICK;
    }

    /// <summary>
    /// Deep-copies this Snapshot, allocating from the pool in the process.
    /// </summary>
    internal RailSnapshot Clone()
    {
      RailSnapshot clone = RailResource.Instance.AllocateSnapshot();
      clone.Tick = this.Tick;
      foreach (RailState state in this.Values)
        clone.Add(state.Clone());
      return clone;
    }

    protected virtual void Reset()
    {
      foreach (RailState state in this.Values)
        RailPool.Free(state);
      this.states.Clear();
    }

    #region State Access
    internal virtual void Add(RailState state)
    {
      this.states.Add(state.Id, state);
    }

    internal bool TryGet(int id, out RailState state)
    {
      return this.states.TryGetValue(id, out state);
    }

    internal bool Contains(int id)
    {
      return this.states.ContainsKey(id);
    }

    internal Dictionary<int, RailState>.ValueCollection Values
    {
      get { return this.states.Values; }
    }
    #endregion

    #region Encode/Decode
    /// Snapshot encoding order:
    /// | BASISTICK | TICK | STATE COUNT | STATE | STATE | STATE | ...

    internal static int PeekBasisTick(
      BitBuffer buffer)
    {
      return buffer.Peek(Encoders.Tick);
    }

    internal void Encode(
      BitBuffer buffer)
    {
      foreach (RailState state in this.Values)
      {
        // Write: [State Data]
        state.Encode(buffer);
      }

      // Write: [Count]
      buffer.Push(Encoders.EntityCount, this.states.Count);

      // Write: [Tick]
      buffer.Push(Encoders.Tick, this.Tick);

      // Write: [BasisTick]
      buffer.Push(Encoders.Tick, RailClock.INVALID_TICK);
    }

    internal void Encode(
      BitBuffer buffer,
      RailSnapshot basis)
    {
      int count = 0;

      foreach (RailState state in this.Values)
      {
        // Write: [State]
        RailState basisState;
        if (basis.TryGet(state.Id, out basisState))
        {
          // We may not write a state if nothing changed
          if (state.Encode(buffer, basisState))
            count++;
        }
        else
        {
          state.Encode(buffer);
          count++;
        }
      }

      // Write: [Count]
      buffer.Push(Encoders.EntityCount, count);

      // Write: [Tick]
      buffer.Push(Encoders.Tick, this.Tick);

      // Write: [BasisTick]
      buffer.Push(Encoders.Tick, basis.Tick);
    }

    internal static RailSnapshot Decode(
      BitBuffer buffer)
    {
      // Read: [Basis Tick] (discarded)
      buffer.Pop(Encoders.Tick);

      // Read: [Tick]
      int tick = buffer.Pop(Encoders.Tick);

      // Read: [Count]
      int count = buffer.Pop(Encoders.EntityCount);

      RailSnapshot snapshot = RailResource.Instance.AllocateSnapshot();
      snapshot.Tick = tick;

      for (int i = 0; i < count; i++)
      {
        // Read: [State] (full)
        snapshot.Add(RailState.Decode(buffer));
      }

      return snapshot;
    }

    internal static RailSnapshot Decode(
      BitBuffer buffer,
      RailSnapshot basis)
    {
      // Read: [BasisTick] (discarded)
      buffer.Pop(Encoders.Tick);

      // Read: [Tick]
      int tick = buffer.Pop(Encoders.Tick);

      // Read: [Count]
      int count = buffer.Pop(Encoders.EntityCount);

      RailSnapshot snapshot = RailResource.Instance.AllocateSnapshot();
      snapshot.Tick = tick;

      for (int i = 0; i < count; i++)
      {
        // Peek: [State.Id]
        int stateId = RailState.PeekId(buffer);

        // Read: [State] (either full or delta)
        RailState basisState;
        if (basis.TryGet(stateId, out basisState))
          snapshot.Add(RailState.Decode(buffer, basisState));
        else
          snapshot.Add(RailState.Decode(buffer));
      }

      RailSnapshot.ReconcileBasis(snapshot, basis);
      return snapshot;
    }

    /// <summary>
    /// Incorporates any non-updated entities from the basis snapshot into
    /// the newly-populated snapshot.
    /// </summary>
    internal static void ReconcileBasis(
      RailSnapshot snapshot, 
      RailSnapshot basis)
    {
      foreach (RailState basisState in basis.Values)
        if (snapshot.Contains(basisState.Id) == false)
          snapshot.Add(basisState.Clone());
    }
    #endregion
  }
}
