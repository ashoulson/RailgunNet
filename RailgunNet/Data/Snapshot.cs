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

using Reservoir;

namespace Railgun
{
  internal class Snapshot : Poolable<Snapshot>
  {
    internal int Frame { get; set; }

    internal NodeList<Image> images;
    private Dictionary<int, Image> idToImage;

    public void Clear()
    {
      Pool.FreeAll(this.images);
      this.idToImage.Clear();
    }

    public Snapshot()
    {
      this.images = new NodeList<Image>();
      this.idToImage = new Dictionary<int, Image>();
    }

    protected override void Reset()
    {
      this.Clear();
    }

    internal void Add(Image image)
    {
      this.images.Add(image);
      this.idToImage[image.Id] = image;
    }

    internal bool TryGet(int id, out Image image)
    {
      return this.idToImage.TryGetValue(id, out image);
    }

    internal bool Contains(int id)
    {
      return this.idToImage.ContainsKey(id);
    }
  }
}
