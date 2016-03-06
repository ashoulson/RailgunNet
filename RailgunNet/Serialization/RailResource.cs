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
  internal class RailResource
  {
    // TODO: Make this thread-safe (like [ThreadStatic])
    internal static RailResource Instance { get; private set; }

    internal static void Initialize(
      RailCommand commandToRegister, 
      RailState[] statestoRegister,
      RailEvent[] eventsToRegister)
    {
      RailResource.Instance =
        new RailResource(
          commandToRegister, 
          statestoRegister,
          eventsToRegister);
    }

    private RailPoolGeneric<RailServerPacket> serverPacketPool;
    private RailPoolGeneric<RailClientPacket> clientPacketPool;

    private RailPoolCommand commandPool;

    private Dictionary<int, RailPoolState> statePools;
    private Dictionary<int, RailPoolEvent> eventPools;

    private RailResource(
      RailCommand commandToRegister, 
      RailState[] statesToRegister,
      RailEvent[] eventsToRegister)
    {
      this.serverPacketPool = new RailPoolGeneric<RailServerPacket>();
      this.clientPacketPool = new RailPoolGeneric<RailClientPacket>();

      this.commandPool = commandToRegister.CreatePool();
      this.statePools = new Dictionary<int, RailPoolState>();
      this.eventPools = new Dictionary<int, RailPoolEvent>();

      foreach (RailState state in statesToRegister)
        this.statePools[state.EntityType] = state.CreatePool();
      foreach (RailEvent evnt in eventsToRegister)
        this.eventPools[evnt.EventType] = evnt.CreatePool();

      this.CreateStandardEventPools();
    }

    internal RailServerPacket AllocateServerPacket()
    {
      return this.serverPacketPool.Allocate();
    }

    internal RailClientPacket AllocateClientPacket()
    {
      return this.clientPacketPool.Allocate();
    }

    internal RailCommand AllocateCommand()
    {
      return this.commandPool.Allocate();
    }

    internal RailState AllocateState(int type)
    {
      return this.statePools[type].Allocate();
    }

    internal RailEvent AllocateEvent(int type)
    {
      return this.eventPools[type].Allocate();
    }

    #region Event Shorthand
    private void CreateStandardEventPools()
    {
      this.eventPools[RailEventTypes.TYPE_CONTROL] = new RailControlEvent().CreatePool();
    }

    internal RailControlEvent AllocateControlEvent()
    {
      return (RailControlEvent)this.eventPools[RailEventTypes.TYPE_CONTROL].Allocate();
    }
    #endregion
  }
}
