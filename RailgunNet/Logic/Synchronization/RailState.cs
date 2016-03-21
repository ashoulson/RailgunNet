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
  public abstract class RailState : IRailPoolable<RailState>, IRailTimedValue
  {
    #region Interface
    IRailPool<RailState> IRailPoolable<RailState>.Pool { get; set; }
    void IRailPoolable<RailState>.Reset() { this.Reset(); }
    Tick IRailTimedValue.Tick { get { return this.Tick; } }
    #endregion

    internal EntityId EntityId { get; private set; }  // Synchronized
    internal int EntityType { get; private set; }     // Synchronized
    internal Tick Tick { get; private set; }          // Synchronized

    // Client-only
    internal bool IsController { get; set; }          // Synchronized to client
    internal Tick DestroyedTick { get; private set; } // Synchronized to client
    internal bool IsDestroyed { get { return this.DestroyedTick.IsValid; } }

    /// <summary>
    /// The RailState maintains this container and will free it on reset.
    /// </summary>
    internal RailStateData DataContainer { get; set; }

    public RailState()
    {
      this.Tick = Tick.INVALID;
      this.EntityId = EntityId.INVALID;
      this.EntityType = -1;

      this.IsController = false;
      this.DestroyedTick = Tick.INVALID;

      this.DataContainer = null;
    }



    protected internal void Reset() 
    {
      this.Tick = Tick.INVALID;
      this.EntityId = EntityId.INVALID;
      this.EntityType = -1;

      this.IsController = false;
      this.DestroyedTick = Tick.INVALID;

      RailPool.Free(this.DataContainer);
      this.DataContainer = null;
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
      clone.DestroyedTick = this.DestroyedTick;
      clone.DataContainer = this.DataContainer.Clone();
      return clone;
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

    private void SetOnDecode(Tick tick, EntityId id)
    {
      this.Tick = tick;
      this.EntityId = id;
    }
    #region Encode/Decode
    internal static EntityId PeekId(ByteBuffer buffer)
    {
      return buffer.PeekEntityId();
    }

    internal bool ShouldEncode(
      RailState basis, 
      bool isController, 
      bool isFirst,
      Tick destroyed)
    {
      if (basis == null)
        return true;
      if (isController || isFirst || destroyed.IsValid)
        return true;
      uint flags = this.GetDirtyFlags(basis);
      return flags > 0;
    }

    internal void Encode(
      ByteBuffer buffer, 
      RailState basis,
      bool isController,
      bool isFirst,
      Tick destroyed)
    {
      // Write: [Id]
      buffer.WriteEntityId(this.EntityId);

      // Write: [Type]
      this.EncodeType(buffer, basis);

      // Write: [IsDestroyed]
      buffer.WriteBool(destroyed.IsValid);

      // Write: [IsController]
      buffer.WriteBool(isController);

      // Write: [IsFirst]
      buffer.WriteBool(isFirst);

      if (destroyed.IsValid)
      {
        // Write: [Destroyed Tick] (if applicable)
        buffer.WriteTick(destroyed);
      }
      else
      {
        // Write: [Mutable Data]
        this.EncodeMutable(buffer, basis);

        // Write: [Immutable Data] (if applicable)
        if (isFirst)
          this.EncodeImmutableData(buffer);

        // Write: [Controller Data] (if applicable)
        if (isController)
          this.EncodeControllerData(buffer);
      }
    }

    internal static RailState Decode(
      ByteBuffer buffer, 
      RailState basis,
      Tick latestTick,
      bool isDelta) // If false, the basis is the latest state received
    {
      // Read: [Id]
      EntityId id = buffer.ReadEntityId();

      // Read: [Type]
      int type = RailState.DecodeType(buffer, basis, isDelta);

      RailState state = RailState.CreateState(basis, latestTick, id, type);

      // Read: [IsDestroyed]
      bool isDestroyed = buffer.ReadBool();

      // Read: [IsController]
      state.IsController = buffer.ReadBool();

      // Read: [IsFirst]
      bool isFirst = buffer.ReadBool();

      if (isDestroyed)
      {
        // Read: [Destroyed Tick] (if applicable)
        state.DestroyedTick = buffer.ReadTick();
      }
      else
      {
        // Read: [Mutable Data]
        state.DecodeMutable(buffer, isDelta);

        // Read: [Immutable Data] (if applicable)
        if (isFirst)
          state.DecodeImmutableData(buffer);

        // Read: [Controller Data] (if applicable)
        if (state.IsController)
          state.DecodeControllerData(buffer);
      }

      return state;
    }

    private void EncodeType(
      ByteBuffer buffer, 
      RailState basis)
    {
      if (basis == null) // Full Encode
      {
        // Write: [Type]
        buffer.WriteInt(
          RailResource.Instance.EntityTypeCompressor, 
          this.EntityType);
      }
      else
      {
        // No [Type] for deltas
      }
    }

    private static int DecodeType(
      ByteBuffer buffer, 
      RailState basis, 
      bool isDelta)
    {
      if (isDelta == false) // Full Decode
      {
        // Read: [Type]
        return buffer.ReadInt(
          RailResource.Instance.EntityTypeCompressor);
      }
      else // Delta Decode
      {
        // No [Type] for deltas -- use the basis
        return basis.EntityType;
      }
    }

    private void EncodeMutable(
      ByteBuffer buffer, 
      RailState basis)
    {
      if (basis == null) // Full Encode
      {
        // Write: [Mutable Data] (full)
        this.EncodeMutableData(buffer, RailState.FLAGS_ALL);
      }
      else // Delta Encode
      {
        uint flags = this.GetDirtyFlags(basis);

        // Write: [Dirty Flags]
        buffer.Write(this.FlagBitsUsed, flags);

        // Write: [Mutable Data] (delta)
        this.EncodeMutableData(buffer, flags);
      }
    }

    private void DecodeMutable(
      ByteBuffer buffer, 
      bool isDelta)
    {
      if (isDelta == false)
      {
        // Read: [Mutable Data] (full)
        this.DecodeMutableData(buffer, RailState.FLAGS_ALL);
      }
      else
      {
        // Write: [Dirty Flags]
        uint flags = buffer.Read(this.FlagBitsUsed);

        // Write: [Mutable Data] (delta)
        this.DecodeMutableData(buffer, flags);
      }
    }

    private static RailState CreateState(
      RailState basis,
      Tick latestTick,
      EntityId id,
      int type)
    {
      RailState state = RailResource.Instance.AllocateState(type);
      state.SetOnDecode(latestTick, id);
      if (basis != null)
        state.SetDataFrom(basis); // Copy over mutable and immutable data
      return state;
    }
    #endregion

    #region DEBUG
    public virtual string DEBUG_FormatDebug() { return ""; }
    #endregion
  }
}
