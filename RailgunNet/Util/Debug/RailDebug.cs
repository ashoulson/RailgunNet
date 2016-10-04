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
using System.Diagnostics;

namespace Railgun
{
  public interface IRailDebugLogger
  {
    void LogMessage(object message);
    void LogWarning(object message);
    void LogError(object message);
  }

  internal class RailConsoleLogger : IRailDebugLogger
  {
    public void LogError(object message)
    {
      RailConsoleLogger.Log("ERROR: " + message, ConsoleColor.Red);
    }

    public void LogWarning(object message)
    {
      RailConsoleLogger.Log("WARNING: " + message, ConsoleColor.Yellow);
    }

    public void LogMessage(object message)
    {
      RailConsoleLogger.Log("INFO: " + message, ConsoleColor.Gray);
    }

    private static void Log(object message, ConsoleColor color)
    {
      ConsoleColor current = Console.ForegroundColor;
      Console.ForegroundColor = color;
      Console.WriteLine(message);
      Console.ForegroundColor = current;
    }
  }

  public static class RailDebug
  {
    public static IRailDebugLogger Logger = new RailConsoleLogger();

    [Conditional("DEBUG")]
    public static void LogMessage(object message)
    {
      if (RailDebug.Logger != null)
        lock (RailDebug.Logger)
          RailDebug.Logger.LogMessage(message);
    }

    [Conditional("DEBUG")]
    public static void LogWarning(object message)
    {
      if (RailDebug.Logger != null)
        lock (RailDebug.Logger)
          RailDebug.Logger.LogWarning(message);
    }

    [Conditional("DEBUG")]
    public static void LogError(object message)
    {
      if (RailDebug.Logger != null)
        lock (RailDebug.Logger)
          RailDebug.Logger.LogError(message);
    }

    [Conditional("DEBUG")]
    public static void Assert(bool condition)
    {
      if (condition == false)
        RailDebug.LogError("Assert Failed!");
    }

    [Conditional("DEBUG")]
    public static void Assert(bool condition, object message)
    {
      if (condition == false)
        RailDebug.LogError("Assert Failed: " + message);
    }
  }
}
