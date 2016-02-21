using System;
using System.Collections.Generic;

namespace Example
{
  class Program
  {
    static void Main(string[] args)
    {
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
