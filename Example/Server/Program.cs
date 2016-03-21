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

namespace Example
{
  class Program
  {

    static void Main(string[] args)
    {
      //ByteBuffer buffer = new ByteBuffer();
      
      //Random random = new Random();

      //byte[] data = new byte[100];

      //for (int i = 0; i < 100000000; i++)
      //{
      //  buffer.Clear();
      //  uint val1 = (uint)random.Next(int.MinValue, int.MaxValue);
      //  uint val2 = (uint)random.Next(int.MinValue, int.MaxValue);
      //  uint val3 = (uint)random.Next(int.MinValue, int.MaxValue);
      //  buffer.WriteUInt(val1);
      //  buffer.WriteUInt(val2);
      //  buffer.WriteUInt(val3);

      //  int length = buffer.Store(data);
      //  buffer.Load(data, length);

      //  uint read1 = buffer.ReadUInt();
      //  uint read2 = buffer.ReadUInt();
      //  uint read3 = buffer.ReadUInt();
      //  if (val1 != read1)
      //    Console.WriteLine("BAD");
      //  if (val2 != read2)
      //    Console.WriteLine("BAD");
      //  if (val3 != read3)
      //    Console.WriteLine("BAD");
      //  if (buffer.IsFinished == false)
      //    Console.WriteLine("NOT DONE");
      //}


      //Console.WriteLine("Done");
      //Console.ReadLine();

      Server room = new Server(44325, 0.02f);
      room.Start();

      while (true)
      {
        room.Update();

        if (Console.KeyAvailable)
        {
          ConsoleKeyInfo key = Console.ReadKey(true);
          switch (key.Key)
          {
            case ConsoleKey.F1:
              room.Stop();
              return;

            default:
              break;
          }
        }
      }
    }
  }
}
