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

public class DemoEntity : RailEntity<DemoState>
{
  //private DemoObject demoObject = null;

  public DemoEntity() { }

  private float modifier;

  public void InitializeHost(int archetypeId)
  {
    this.modifier = 1.0f;
    this.State.ArchetypeId = archetypeId;
  }

  protected override void OnUpdateHost()
  {
    this.UpdatePosition();
    //this.ApplyPosition();
  }

  protected override void OnAddedToEnvironment()
  {
    DemoEvents.OnEntityAdded(this);
  }

  private void InitializeObject()
  {
    //if (this.IsMaster)
    //{
    //  Renderer renderer = this.demoObject.GetComponent<Renderer>();
    //  renderer.material = new Material(renderer.material);
    //  renderer.material.color = Color.red;
    //}
  }

  private void UpdatePosition()
  {
    this.State.X += 1.0f * Time.fixedDeltaTime * this.modifier;

    if (this.State.X > 5.0f)
      this.modifier *= -1.0f;
    if (this.State.X < -5.0f)
      this.modifier *= -1.0f;
  }

  //private void ApplyPosition()
  //{
  //  this.demoObject.transform.position = 
  //    new Vector2(
  //      this.State.X, 
  //      this.State.Y);
  //}
}
