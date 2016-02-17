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
  public class Image : Record, IPoolable
  {
    Pool IPoolable.Pool { get; set; }
    void IPoolable.Reset() { this.Reset(); }

    /// <summary>
    /// Deep-copies this Image, allocating from the pool in the process.
    /// </summary>
    internal Image Clone()
    {
      Image clone = ResourceManager.Instance.AllocateImage();
      clone.Id = this.Id;
      clone.State = this.State.Clone();
      return clone;
    }

    /// <summary>
    /// Creates an entity out of this image. The entity type instantiation
    /// is handled by the State itself.
    /// </summary>
    internal Entity CreateEntity()
    {
      Entity entity = this.State.CreateEntity();
      entity.Id = this.Id;
      entity.State = this.State.Clone();
      return entity;
    }

    protected void Reset()
    {
      this.Id = Record.INVALID_ID;
      if (this.State != null)
        Pool.Free(this.State);
      this.State = null;
    }
  }
}
