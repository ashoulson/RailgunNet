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
  /// States are attached to entities and contain user-defined data. They are
  /// responsible for encoding and decoding that data, and delta-compression.
  /// </summary>
  public abstract class RailState : 
    IRailPoolable<RailState>, IRailRingValue, IRailCloneable<RailState>
  {
    private const uint FLAGS_ALL = 0xFFFFFFFF;

    IRailPool<RailState> IRailPoolable<RailState>.Pool { get; set; }
    void IRailPoolable<RailState>.Reset() { this.Reset(); }
    Tick IRailRingValue.Tick { get { return this.Tick; } }

    public RailState()
    {
      this.Reset();
    }

    // Client/Server
    internal EntityId EntityId { get; private set; } // Synchronized
    internal int EntityType { get; private set; }    // Synchronized
    internal Tick Tick { get; private set; }         // Synchronized

    // Client-only -- always false on server
    internal bool IsController { get; private set; } // Synchronized (indirectly)
    internal bool IsPredicted { get; private set; }  // Not synchronized

    /// <summary>
    /// Compares this state against a basis and returns a bitfield of which
    /// properties are dirty.
    /// </summary>
    protected abstract uint GetDirtyFlags(RailState basis);
    protected internal abstract void SetDataFrom(RailState other);

    protected abstract int FlagBitsUsed { get; }
    protected abstract void ResetData();
    protected abstract void EncodeImmutable(BitBuffer buffer);
    protected abstract void DecodeImmutable(BitBuffer buffer);
    protected abstract void EncodeMutable(BitBuffer buffer, uint flags);
    protected abstract void DecodeMutable(BitBuffer buffer, uint flags);

    protected internal void Reset() 
    {
      this.Tick = Tick.INVALID;
      this.EntityId = EntityId.INVALID;
      this.EntityType = -1;

      this.IsPredicted = false;
      this.IsController = false;

      this.ResetData();
    }

    internal void Initialize(int type)
    {
      this.EntityType = type;
    }

    public RailState Clone()
    {
      RailState clone = RailResource.Instance.AllocateState(this.EntityType);
      clone.Tick = this.Tick;
      clone.EntityId = this.EntityId;
      clone.EntityType = this.EntityType;
      clone.IsController = this.IsController;
      clone.IsPredicted = this.IsPredicted;
      clone.SetDataFrom(this);
      return clone;
    }

    internal void SetOnPredict(Tick tick)
    {
      this.IsPredicted = true;
      this.Tick = tick;
    }

    internal void SetOnEntityCreate(EntityId id)
    {
      this.Tick = Tick.INVALID;
      this.EntityId = id;
    }

    internal void SetOnStore(Tick tick)
    {
      this.Tick = tick;
    }

    private void SetOnDecode(Tick tick, EntityId id, bool isController)
    {
      this.Tick = tick;
      this.EntityId = id;
      this.IsController = isController;
    }

    #region Encode/Decode
    internal static EntityId PeekId(BitBuffer buffer)
    {
      return buffer.Peek(RailEncoders.EntityId);
    }

    internal void Encode(
      BitBuffer buffer, 
      RailState basis,
      bool isController,
      bool isFirst)
    {
      // Write: [Id]
      buffer.Write(RailEncoders.EntityId, this.EntityId);

      // Write: [IsController]
      buffer.Write(RailEncoders.Bool, isController);

      // Write: [IsFirst]
      buffer.Write(RailEncoders.Bool, isFirst);

      // Write: [Type]
      this.EncodeType(buffer, basis);

      // Write: [Mutable Data]
      this.EncodeMutable(buffer, basis);

      // Write: [Immutable Data] (if applicable)
      if (isFirst)
        this.EncodeImmutable(buffer);
    }

    internal static RailState Decode(
      BitBuffer buffer, 
      RailState basis,
      Tick latestTick,
      bool isDelta) // If false, the basis is the latest state received
    {
      // Read: [Id]
      EntityId id = buffer.Read(RailEncoders.EntityId);

      // Read: [IsController]
      bool isController = buffer.Read(RailEncoders.Bool);

      // Read: [IsFirst]
      bool isFirst = buffer.Read(RailEncoders.Bool);

      // Read: [Type]
      int type = RailState.DecodeType(buffer, basis, isDelta);

      // Create the state
      RailState state = 
        RailState.CreateState(basis, latestTick, id, type, isController);
     
      // Read: [Mutable Data]
      state.DecodeMutable(buffer, isDelta);

      // Read: [Immutable Data] (if applicable)
      if (isFirst)
        state.DecodeImmutable(buffer);

      return state;
    }

    private void EncodeType(
      BitBuffer buffer, 
      RailState basis)
    {
      if (basis == null) // Full Encode
      {
        // Write: [Type]
        buffer.Write(RailEncoders.EntityType, this.EntityType);
      }
      else
      {
        // No [Type] for deltas
      }
    }

    private static int DecodeType(
      BitBuffer buffer, 
      RailState basis, 
      bool isDelta)
    {
      if (isDelta == false) // Full Decode
      {
        // Read: [Type]
        return buffer.Read(RailEncoders.EntityType);
      }
      else // Delta Decode
      {
        // No [Type] for deltas -- use the basis
        return basis.EntityType;
      }
    }

    private void EncodeMutable(
      BitBuffer buffer, 
      RailState basis)
    {
      if (basis == null) // Full Encode
      {
        // Write: [Mutable Data] (full)
        this.EncodeMutable(buffer, RailState.FLAGS_ALL);
      }
      else // Delta Encode
      {
        // Write: [Dirty Flags]
        uint flags = this.GetDirtyFlags(basis);
        buffer.Write(this.FlagBitsUsed, flags);

        // Write: [Mutable Data] (delta)
        this.EncodeMutable(buffer, flags);
      }
    }

    private void DecodeMutable(
      BitBuffer buffer, 
      bool isDelta)
    {
      if (isDelta == false)
      {
        // Read: [Mutable Data] (full)
        this.DecodeMutable(buffer, RailState.FLAGS_ALL);
      }
      else
      {
        // Write: [Dirty Flags]
        uint flags = buffer.Read(this.FlagBitsUsed);

        // Write: [Mutable Data] (delta)
        this.DecodeMutable(buffer, flags);
      }
    }

    private static RailState CreateState(
      RailState basis,
      Tick latestTick,
      EntityId id,
      int type,
      bool isController)
    {
      RailState state = RailResource.Instance.AllocateState(type);
      state.SetOnDecode(latestTick, id, isController);
      if (basis != null)
        state.SetDataFrom(basis); // Copy over mutable and immutable data
      return state;
    }
    #endregion

    #region DEBUG
    public virtual string DEBUG_FormatDebug() { return ""; }
    #endregion
  }

  /// <summary>
  /// This is the class to override to attach user-defined data to an entity.
  /// </summary>
  public abstract class RailState<T> : RailState
    where T : RailState<T>, new()
  {
    #region Casting Overrides
    protected override uint GetDirtyFlags(RailState basis)
    {
      return this.GetDirtyFlags((T)basis);
    }

    protected internal override void SetDataFrom(RailState other)
    {
      this.SetDataFrom((T)other);
    }
    #endregion

    protected abstract uint GetDirtyFlags(T basis);
    protected abstract void SetDataFrom(T other);
  }
}
