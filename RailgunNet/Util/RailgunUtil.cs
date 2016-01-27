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

using UnityEngine;

namespace Railgun
{
  public static class RailgunUtil
  {
    public static void Swap<T>(ref T a, ref T b)
    {
      T temp = b;
      b = a;
      a = temp;
    }

    internal static void ExpandArray<T>(ref T[] oldArray)
    {
      // TODO: Revisit this using next-largest primes like built-in lists do
      int newCapacity = oldArray.Length * 2;
      T[] newArray = new T[newCapacity];
      Array.Copy(oldArray, newArray, oldArray.Length);
      oldArray = newArray;
    }

    #region Debug
    public static void RunTests()
    {
      Encoder.Initialize();
      //BitPacker.Test(50, 400);
      //IntEncoder.Test(200, 200);
      //FloatEncoder.Test(200, 200);
      //EntityState.Test(100);
      StateBag<EntityState>.Test(100, 10);
      Debug.Log("Done Tests");
    }

    internal static void Assert(bool condition)
    {
      System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
      if (condition == false)
        Debug.LogError("Assert failed\n" + t);
    }

    internal static void Assert(bool condition, object message)
    {
      System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
      if (condition == false)
        Debug.LogError(message + "\n" + t);
    }
    #endregion
  }
}
