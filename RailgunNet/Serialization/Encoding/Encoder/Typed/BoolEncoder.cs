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
  public class BoolEncoder : Encoder<bool>
  {
    internal override int GetCost(bool value)
    {
      return 1;
    }

    public BoolEncoder() {}

    internal override void Write(BitBuffer buffer, bool value)
    {
      buffer.Push(1, this.Pack(value));
    }

    internal override bool Read(BitBuffer buffer)
    {
      return this.Unpack(buffer.Pop(1));
    }

    internal override bool Peek(BitBuffer buffer)
    {
      return this.Unpack(buffer.Peek(1));
    }

    internal uint Pack(bool value)
    {
      return value ? 1u : 0u;
    }

    internal bool Unpack(uint data)
    {
      return (data == 0) ? false : true;
    }
  }
}