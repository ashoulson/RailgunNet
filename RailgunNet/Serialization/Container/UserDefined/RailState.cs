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
    IRailPoolable, IRailRingValue, IRailCloneable<RailState>
  {
    RailPool IRailPoolable.Pool { get; set; }
    void IRailPoolable.Reset() { this.Reset(); }
    Tick IRailRingValue.Tick { get { return this.Tick; } }

    public RailState()
    {
      this.Reset();
    }

    // Synchronized
    internal EntityId EntityId { get; private set; }
    internal int EntityType { get; private set; }
    internal Tick Tick { get; private set; }

    // Client-only -- always false on server
    internal bool IsController { get; private set; }
    internal bool IsPredicted { get; private set; }

    protected abstract void EncodeData(BitBuffer buffer);
    protected abstract void DecodeData(BitBuffer buffer);
    protected abstract void EncodeData(BitBuffer buffer, RailState basis);
    protected abstract void DecodeData(BitBuffer buffer, RailState basis);
    protected abstract void ResetData();

    internal abstract void SetDataFrom(RailState other);

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
      bool isController)
    {
      // Write: [Id]
      buffer.Write(RailEncoders.EntityId, this.EntityId);

      // Write: [IsController]
      buffer.Write(RailEncoders.Bool, isController);

      if (basis == null) // Full Encode
      {
        // Write: [Type]
        buffer.Write(RailEncoders.EntityType, this.EntityType);

        // Write: [Data]
        this.EncodeData(buffer);
      }
      else // Delta Encode
      {
        // No [Type] for deltas

        // Write: [Data]
        this.EncodeData(buffer, basis);
      }
    }

    internal static RailState Decode(
      BitBuffer buffer, 
      RailState basis,
      Tick latestTick)
    {
      RailState state = null;

      // Read: [Id]
      EntityId id = buffer.Read(RailEncoders.EntityId);

      // Read: [IsController]
      bool isController = buffer.Read(RailEncoders.Bool);

      if (basis == null)
      {
        // Read: [Type]
        int type = buffer.Read(RailEncoders.EntityType);

        // Create the state
        state = RailResource.Instance.AllocateState(type);
        state.SetOnDecode(latestTick, id, isController);

        // Read: [Data]
        state.DecodeData(buffer);
      }
      else
      {
        // No [Type] for deltas
        int type = basis.EntityType;

        // Create the state
        state = RailResource.Instance.AllocateState(type);
        state.SetOnDecode(latestTick, id, isController);

        // Read: [Data]
        state.DecodeData(buffer, basis);
      }

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
    internal override void SetDataFrom(RailState other)
    {
      this.SetDataFrom((T)other);
    }

    protected override void EncodeData(BitBuffer buffer, RailState basis)
    {
      this.EncodeData(buffer, (T)basis);
    }

    protected override void DecodeData(BitBuffer buffer, RailState basis)
    {
      this.DecodeData(buffer, (T)basis);
    }
    #endregion

    protected abstract void SetDataFrom(T other);
    protected abstract void EncodeData(BitBuffer buffer, T basis);
    protected abstract void DecodeData(BitBuffer buffer, T basis);
  }
}
