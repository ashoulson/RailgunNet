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
  /// Factories are responsible for creating states.
  /// </summary>
  public abstract class Factory
  {
    internal NodeList<State> freeList;
    internal byte StateType { get; private set; }

    public Factory()
    {
      this.freeList = new NodeList<State>();

      // Make one dummy state to get the type (we'll just pool it for later)
      State dummy = this.Allocate();
      this.StateType = dummy.Type;
      this.Deallocate(dummy);
    }

    internal abstract State Allocate();

    internal void Deallocate(State state)
    {
      if (state.Factory != this)
        throw new ArgumentException("State must be from this factory");

      state.Reset();
      this.freeList.Add(state);
    }
  }

  /// <summary>
  /// Factories are responsible for creating states. For a given state class,
  /// you will need to instantiate and provide a typed state factory in order
  /// to provide the system with a way to create states of your type.
  /// 
  /// This is a pooled data structure. Unused states are freed and returned.
  /// </summary>
  public sealed class Factory<T> : Factory
    where T : State<T>, new()
  {
    internal override State Allocate()
    {
      State state = null;
      if (this.freeList.Count == 0)
        state = new T();
      else
        state = this.freeList.RemoveLast();

      state.Factory = this;
      state.Initialize();
      return state;
    }
  }
}
