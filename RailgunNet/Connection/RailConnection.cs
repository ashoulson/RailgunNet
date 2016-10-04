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

namespace Railgun
{
  /// <summary>
  /// Server is the core executing class for communication. It is responsible
  /// for managing connection contexts and payload I/O.
  /// </summary>
  public abstract class RailConnection
  {
    public event Action Started;

    public static bool IsServer { get; protected set; }

    public RailRoom Room { get { return this.room; } }
    internal RailInterpreter Interpreter { get { return this.interpreter; } }

    private readonly RailRoom room;
    private readonly RailInterpreter interpreter;
    private bool hasStarted;

    public abstract void Update();

    protected RailConnection()
    {
      this.room = new RailRoom();
      this.interpreter = new RailInterpreter();
      this.hasStarted = false;
    }

    internal void OnEventReceived(RailEvent evnt, RailPeer sender)
    {
      if (evnt.EntityId.IsValid)
      {
        RailEntity entity = null;
        this.Room.TryGet(evnt.EntityId, out entity);

#if SERVER
        // Entity events can only be executed on controlled entities
        bool safeToExecute = (entity != null) && (entity.Controller == sender);
#elif CLIENT
        bool safeToExecute = (entity != null);
#endif

        if (safeToExecute)
          evnt.Invoke(this.room, sender, entity);
      }
      else
      {
        evnt.Invoke(this.room, sender);
      }
    }

    protected void DoStart()
    {
      if (this.hasStarted == false)
        if (this.Started != null)
          this.Started.Invoke();
      this.hasStarted = true;
    }
  }
}
