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

namespace Railgun
{
  public class RailRegistry
  {
    internal Type CommandType { get { return this.commandType; } }
    internal IEnumerable<Type> EventTypes { get { return this.eventTypes; } }
    internal IEnumerable<KeyValuePair<Type, Type>> EntityTypes
    {
      get { return this.entityTypes; }
    }

    private Type commandType;
    private List<Type> eventTypes;
    private List<KeyValuePair<Type, Type>> entityTypes;

    public RailRegistry()
    {
      this.commandType = null;
      this.eventTypes = new List<Type>();
      this.entityTypes = new List<KeyValuePair<Type, Type>>();
    }

    public void SetCommandType<TCommand>()
      where TCommand : RailCommand
    {
      this.commandType = typeof(TCommand);
    }

    public void AddEventType<TEvent>()
      where TEvent : RailEvent
    {
      this.eventTypes.Add(typeof(TEvent));
    }

    public void AddEntityType<TEntity, TState>()
      where TEntity : RailEntity
      where TState : RailState
    {
      this.entityTypes.Add(
        new KeyValuePair<Type, Type>(typeof(TEntity), typeof(TState)));
    }
  }
}
