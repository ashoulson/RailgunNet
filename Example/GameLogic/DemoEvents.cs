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

public class DemoEvents
{
  public static event Action<DemoControlled> ControlledCreated;
  public static event Action<DemoDummy> DummyCreated;
  public static event Action<DemoMimic> MimicCreated;

  public static event Action<DemoActionEvent> DemoActionEvent;

  public static void OnControlledAdded(DemoControlled entity)
  {
    if (DemoEvents.ControlledCreated != null)
      DemoEvents.ControlledCreated.Invoke(entity);
  }

  public static void OnDummyAdded(DemoDummy entity)
  {
    if (DemoEvents.DummyCreated != null)
      DemoEvents.DummyCreated.Invoke(entity);
  }

  public static void OnMimicAdded(DemoMimic entity)
  {
    if (DemoEvents.DummyCreated != null)
      DemoEvents.MimicCreated.Invoke(entity);
  }

  public static void OnDemoActionEvent(DemoActionEvent evnt)
  {
    if (DemoEvents.DemoActionEvent != null)
      DemoEvents.DemoActionEvent.Invoke(evnt);
  }
}
