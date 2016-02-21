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
  /// <summary>
  /// Host is the core executing class on the server. It is responsible for
  /// managing connection contexts and payload I/O.
  /// </summary>
  public abstract class RailConnection
  {
    public RailWorld World { get { return this.world; } }
    protected RailWorld world;
    internal Interpreter interpreter;

    /// <summary>
    /// A complete snapshot history of all sent/received snapshots. Used for
    /// delta encoding either on send or on receive.
    /// </summary>
    internal readonly RingBuffer<RailSnapshot> snapshots;

    public abstract void Update();

    public RailConnection(params RailStateFactory[] factories)
    {
      RailResource.Initialize(factories);

      this.world = new RailWorld();
      this.interpreter = new Interpreter();
      this.snapshots = 
        new RingBuffer<RailSnapshot>(
          RailConfig.DEJITTER_BUFFER_LENGTH,
          RailConfig.NETWORK_SEND_RATE);
    }

    protected bool ShouldSend(int tick)
    {
      return (tick % RailConfig.NETWORK_SEND_RATE) == 0;
    }
  }
}
