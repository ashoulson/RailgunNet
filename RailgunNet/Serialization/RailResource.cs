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
using CommonTools;

namespace Railgun
{
  internal class RailResource
  {
    // TODO: Make this thread-safe (like [ThreadStatic])
    internal static RailResource Instance { get; private set; }

    internal static void Initialize(
      RailCommand commandToRegister, 
      RailState[] statestoRegister)
    {
      RailResource.Instance = 
        new RailResource(commandToRegister, statestoRegister);
    }

    private RailPoolGeneric<RailSnapshot> snapshotPool;
    private RailPoolGeneric<RailPacketC2S> inputPool;

    private RailPoolCommand commandPool;

    private Dictionary<int, RailPoolState> statePools;

    private RailResource(
      RailCommand commandToRegister, 
      params RailState[] statestoRegister)
    {
      this.snapshotPool = new RailPoolGeneric<RailSnapshot>();
      this.inputPool = new RailPoolGeneric<RailPacketC2S>();

      this.commandPool = commandToRegister.CreatePool();

      this.statePools = new Dictionary<int, RailPoolState>();
      foreach (RailState state in statestoRegister)
        this.statePools[state.Type] = state.CreatePool();
    }

    internal RailSnapshot AllocateSnapshot()
    {
      return this.snapshotPool.Allocate();
    }

    internal RailPacketC2S AllocateInput()
    {
      return this.inputPool.Allocate();
    }

    internal RailCommand AllocateCommand()
    {
      return this.commandPool.Allocate();
    }

    internal RailState AllocateState(int type)
    {
      return this.statePools[type].Allocate();
    }
  }
}
