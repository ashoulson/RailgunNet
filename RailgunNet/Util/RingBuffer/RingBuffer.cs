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
  public class RingBuffer<T>
    where T : class, IRingValue, IPoolable
  {
    private T[] data;

    public RingBuffer(int capacity)
    {
      this.data = new T[capacity];
      for (int i = 0; i < capacity; i++)
        this.data[i] = null;
    }

    public void Store(T value)
    {
      int index = value.Key % this.data.Length;
      if (this.data[index] != null)
        Pool.Free(this.data[index]);
      this.data[index] = value;
    }

    public T Get(int key)
    {
      T result = this.data[key % this.data.Length];
      if ((result != null) && (result.Key == key))
        return result;
      return null;
    }

    public bool Contains(int key)
    {
      T result = this.data[key % this.data.Length];
      if ((result != null) && (result.Key == key))
        return true;
      return false;
    }

    public bool TryGet(int key, out T value)
    {
      value = this.Get(key);
      return (value != null);
    }
  }
}
