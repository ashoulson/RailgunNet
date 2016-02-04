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
  internal class Context
  {
    private GenericPool<Snapshot> snapshotPool;
    private GenericPool<Image> imagePool;
    private Dictionary<int, StatePool> factories;

    public Context(params StatePool[] factories)
    {
      this.snapshotPool = new GenericPool<Snapshot>();
      this.imagePool = new GenericPool<Image>();
      this.factories = new Dictionary<int, StatePool>();
      foreach (StatePool factory in factories)
        this.factories[factory.Type] = factory;
    }

    public Snapshot AllocateSnapshot()
    {
      return this.snapshotPool.Allocate();
    }

    public Image AllocateImage()
    {
      return this.imagePool.Allocate();
    }

    internal State AllocateState(int type)
    {
      return this.factories[type].Allocate();
    }
  }
}
