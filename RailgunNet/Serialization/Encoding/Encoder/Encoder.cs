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

namespace Railgun
{
  public abstract class Encoder<T>
  {
    internal abstract int RequiredBits { get; }

    internal abstract uint Pack(T value);
    internal abstract T Unpack(uint data);

    internal virtual void Write(BitBuffer buffer, T value)
    {
      buffer.Write(this.RequiredBits, this.Pack(value));
    }

    internal virtual T Read(BitBuffer buffer)
    {
      return this.Unpack(buffer.Read(this.RequiredBits));
    }

    internal virtual T Peek(BitBuffer buffer)
    {
      return this.Unpack(buffer.Peek(this.RequiredBits));
    }

    internal virtual void Reserve(BitBuffer buffer)
    {
      buffer.SetReserved(this.RequiredBits);
    }

    internal virtual void WriteReserved(BitBuffer buffer, T value)
    {
      buffer.WriteReserved(this.RequiredBits, this.Pack(value));
    }
  }
}
