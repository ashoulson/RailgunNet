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

using Railgun;
using UnityEngine;

[RegisterEntity(typeof(DemoState))]
public class DemoDummy : RailEntity<DemoState>
{
  public event Action Frozen;
  public event Action Unfrozen;

  private int ticks = 0;

  private float startX = 0.0f;
  private float direction = 1.0f;
  private float speed = 0.1f;

  protected override void OnStart()
  {
    DemoEvents.OnDummyAdded(this);

    this.startX = this.State.X;
  }

  protected override void OnSimulate()
  {
    this.ticks++;
    if (this.ticks >= 40)
    {
      this.direction *= -1.0f;
      this.ticks = 0;
    }

    this.State.X += this.speed * this.direction;
    this.State.Y += this.speed * this.direction;
  }

  protected override void OnFrozen()
  {
    if (this.Frozen != null)
      this.Frozen.Invoke();
  }

  protected override void OnUnfrozen()
  {
    if (this.Unfrozen != null)
      this.Unfrozen.Invoke();
  }
}
