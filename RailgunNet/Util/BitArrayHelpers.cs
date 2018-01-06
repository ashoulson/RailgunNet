/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016-2018 - Alexander Shoulson - http://ashoulson.com
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

using System.Collections.Generic;

namespace Railgun
{
  internal static class BitArrayHelpers
  {
    internal static IEnumerable<int> GetValues(ulong bits)
    {
      int offset = 0;

      while (bits > 0)
      {
        // Skip 3 bits if possible
        if ((bits & 0x7UL) == 0)
        {
          bits >>= 3;
          offset += 3;
        }

        if ((bits & 0x1UL) > 0)
          yield return offset;

        bits >>= 1;
        offset++;
      }
    }

    internal static bool Contains(int value, ulong bits, int length)
    {
      if (value < 0)
        return false;
      if (value >= length)
        return false;

      ulong bit = 1UL << value;
      return (bits & bit) > 0;
    }
  }
}
