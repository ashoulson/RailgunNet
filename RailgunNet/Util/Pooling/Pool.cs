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
  public abstract class Pool
  {
    protected abstract object AllocateGeneric();
    protected abstract void DeallocateGeneric(object item);

    public static void Free(IPoolable item)
    {
      item.Pool.DeallocateGeneric(item);
    }

    public static T CloneEmpty<T>(T item)
      where T : IPoolable
    {
      return (T)item.Pool.AllocateGeneric();
    }
  }

  public abstract class AbstractPool<T> : Pool
    where T : IPoolable
  {
    protected Stack<T> freeList;

    public AbstractPool()
    {
      this.freeList = new Stack<T>();
    }

    public abstract T Allocate();

    public void Deallocate(T value)
    {
      RailgunUtil.Assert(value.Pool == this);
      value.Reset();
      this.freeList.Push(value);
    }

    protected override object AllocateGeneric()
    {
      return this.Allocate();
    }

    protected override void DeallocateGeneric(object item)
    {
      this.Deallocate((T)item);
    }
  }

  public class Pool<T> : AbstractPool<T>
    where T : IPoolable, new()
  {
    public override T Allocate()
    {
      if (this.freeList.Count > 0)
        return this.freeList.Pop();

      T state = new T();
      state.Pool = this;
      return state;
    }
  }
}
