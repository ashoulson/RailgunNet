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
  /// An image is a frozen snapshot of an entity for a particular frame.
  /// It contains a recording of that entity's state with that frame's data.
  /// </summary>
  internal class Image : Poolable<Image>
  {
    public int Id { get; internal set; }
    public State State { get; internal set; }

    public Image() 
    {
      this.Reset();
    }

    protected override void Reset()
    {
      this.Id = Entity.INVALID_ID;
      this.State = null;
    }

    internal void Encode(BitPacker bitPacker)
    {
      this.State.Encode(bitPacker);
    }

    internal void Encode(BitPacker bitPacker, Image basis)
    {
      this.State.Encode(bitPacker, basis.State);
    }

    internal void Decode(BitPacker bitPacker)
    {
      // We assume this image is already populated with a state before decoding
      this.State.Decode(bitPacker);
    }

    internal void Decode(BitPacker bitPacker, Image basis)
    {
      // We assume this image is already populated with a state before decoding
      this.State.Decode(bitPacker, basis.State);
    }
  }
}
