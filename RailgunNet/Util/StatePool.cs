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
  public abstract class StatePool : Pool<State>
  {
    public int Type { get; private set; }

    public StatePool()
    {
      // Allocate and deallocate a dummy state to read and store its type
      State dummy = this.Allocate();
      this.Type = dummy.Type;
      this.Deallocate(dummy);
    }

    public abstract override State Allocate();
  }

  public class StatePool<T> : StatePool
    where T : State, IPoolable, new()
  {
    public override State Allocate()
    {
      if (this.freeList.Count > 0)
        return this.freeList.Pop();

      T value = new T();
      value.Pool = this;
      return value;
    }
  }
}
