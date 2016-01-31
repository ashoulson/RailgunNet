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
  public class Snapshot : IPoolable
  {
    Pool IPoolable.Pool { get; set; }
    void IPoolable.Reset() { this.Reset(); }

    public int Frame { get; internal protected set; }
    private Dictionary<int, Image> idToImage;

    public Snapshot()
    {
      this.Frame = Clock.INVALID_FRAME;
      this.idToImage = new Dictionary<int, Image>();
    }

    /// <summary>
    /// Deep-copies this Snapshot, allocating from the pool in the process.
    /// </summary>
    public Snapshot Clone()
    {
      Snapshot clone = Pool.CloneEmpty(this);
      clone.Frame = this.Frame;
      foreach (Image image in this.idToImage.Values)
        clone.Add(image.Clone());
      return clone;
    }

    public void Add(Image image)
    {
      this.idToImage.Add(image.Id, image);
    }

    protected void Remove(Image image)
    {
      this.idToImage.Remove(image.Id);
    }

    public bool TryGet(int id, out Image image)
    {
      return this.idToImage.TryGetValue(id, out image);
    }

    public bool Contains(int id)
    {
      return this.idToImage.ContainsKey(id);
    }

    public Dictionary<int, Image>.ValueCollection GetImages()
    {
      return this.idToImage.Values;
    }

    protected virtual void Reset()
    {
      foreach (Image image in this.idToImage.Values)
        Pool.Free(image);
      this.idToImage.Clear();
    }
  }
}
