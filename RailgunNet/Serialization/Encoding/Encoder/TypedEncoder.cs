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
  public class TypedEncoder<T> : Encoder<T>
    where T : struct, IEncodableType<T>
  {
    private T dummy = default(T);

    internal override int GetCost(T value)
    {
      return value.GetCost();
    }

    internal override void Write(BitBuffer buffer, T value)
    {
      value.Write(buffer);
    }

    internal override T Read(BitBuffer buffer)
    {
      return dummy.Read(buffer);
    }

    internal override T Peek(BitBuffer buffer)
    {
      return dummy.Peek(buffer);
    }
  }
}
