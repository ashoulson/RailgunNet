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
  /// An image is the stored state of an entity at a given point in time.
  /// </summary>
  public class RailImage : RailRecord, IPoolable
  {
    #region IPoolable Members
    Pool IPoolable.Pool { get; set; }
    void IPoolable.Reset() { this.Reset(); }
    #endregion

    /// <summary>
    /// Deep-copies this Image, allocating from the pool in the process.
    /// </summary>
    internal RailImage Clone()
    {
      RailImage clone = RailResource.Instance.AllocateImage();
      clone.Id = this.Id;
      clone.State = this.State.Clone();
      return clone;
    }

    /// <summary>
    /// Creates an entity out of this image. The entity type instantiation
    /// is handled by the State itself.
    /// </summary>
    internal RailEntity CreateEntity()
    {
      RailEntity entity = this.State.CreateEntity();
      entity.Id = this.Id;
      entity.State = this.State.Clone();
      return entity;
    }

    protected void Reset()
    {
      this.Id = RailRecord.INVALID_ID;
      if (this.State != null)
        Pool.Free(this.State);
      this.State = null;
    }

    #region Encode/Decode
    /// Image encoding order:
    /// If new: | ID | TYPE | ----- STATE DATA ----- |
    /// If old: | ID | ----- STATE DATA ----- |

    internal static int PeekId(
      BitBuffer buffer)
    {
      return buffer.Peek(Encoders.EntityId);
    }

    internal void Encode(
      BitBuffer buffer)
    {
      // Write: [State Data]
      this.State.Encode(buffer);

      // Write: [Type]
      buffer.Push(Encoders.StateType, this.State.Type);

      // Write: [Id]
      buffer.Push(Encoders.EntityId, this.Id);
    }

    internal bool Encode(
      BitBuffer buffer,
      RailImage basis)
    {
      // Write: [State Data] -- May not write anything if no change
      if (this.State.Encode(buffer, basis.State) == false)
        return false;

      // (No [Type] for delta images)

      // Write: [Id]
      buffer.Push(Encoders.EntityId, this.Id);
      return true;
    }

    internal static RailImage Decode(
      BitBuffer buffer)
    {
      // Read: [Id]
      int imageId = buffer.Pop(Encoders.EntityId);

      // Read: [Type]
      int stateType = buffer.Pop(Encoders.StateType);

      RailImage image = RailResource.Instance.AllocateImage();
      RailState state = RailResource.Instance.AllocateState(stateType);

      // Read: [State Data]
      state.Decode(buffer);

      image.Id = imageId;
      image.State = state;

      return image;
    }

    internal static RailImage Decode(
      BitBuffer buffer,
      RailImage basis)
    {
      // Read: [Id]
      int imageId = buffer.Pop(Encoders.EntityId);

      // (No [Type] for delta images)

      RailImage image = RailResource.Instance.AllocateImage();
      RailState state = RailResource.Instance.AllocateState(basis.State.Type);

      // Read: [State Data]
      state.Decode(buffer, basis.State);

      image.Id = imageId;
      image.State = state;
      return image;
    }
    #endregion
  }
}
