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
  internal interface IRailPoolable<T>
  {
    IRailPool<T> Pool { get; set; }
    void Reset();
  }

  internal interface IRailPool<T>
  {
    T Allocate();
    void Deallocate(T obj);
    IRailPool<T> Clone();
  }

  internal static class RailPool
  {
    internal static void Free<T>(T obj)
      where T : IRailPoolable<T>
    {
      obj.Pool.Deallocate(obj);
    }

    // Special cases
    internal static void Free(
      RailState.Delta container)
    {
      RailPool.Free(container.State);
    }

    internal static void Free(
      RailState.Record container)
    {
      RailPool.Free(container.State);
    }
  }

  internal class RailPool<T> : IRailPool<T>
    where T : IRailPoolable<T>, new()
  {
    private readonly Stack<T> freeList;

    public RailPool()
    {
      this.freeList = new Stack<T>();
    }

    public T Allocate()
    {
      if (this.freeList.Count > 0)
        return this.freeList.Pop();

      T obj = new T();
      obj.Pool = this;
      obj.Reset();
      return obj;
    }

    public void Deallocate(T obj)
    {
      RailDebug.Assert(obj.Pool == this);
      
      obj.Reset();
      this.freeList.Push(obj);
    }

    public IRailPool<T> Clone()
    {
      return new RailPool<T>();
    }
  }

  internal class RailPool<TBase, TDerived> : IRailPool<TBase>
    where TBase : IRailPoolable<TBase>
    where TDerived : TBase, new()
  {
    private readonly Stack<TBase> freeList;

    public RailPool()
    {
      this.freeList = new Stack<TBase>();
    }

    public TBase Allocate()
    {
      if (this.freeList.Count > 0)
        return this.freeList.Pop();

      TBase obj = new TDerived();
      obj.Pool = this;
      obj.Reset();
      return obj;
    }

    public void Deallocate(TBase obj)
    {
      RailDebug.Assert(obj.Pool == this);
      
      obj.Reset();
      this.freeList.Push(obj);
    }

    public IRailPool<TBase> Clone()
    {
      return new RailPool<TBase, TDerived>();
    }
  }
}
