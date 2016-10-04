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
using System.Linq;
using System.Reflection;

namespace Railgun
{
  public class RegisterCommandAttribute : Attribute
  {
  }

  public class RegisterEntityAttribute : Attribute
  {
    public Type StateType { get { return this.stateType; } }

    private readonly Type stateType;

    public RegisterEntityAttribute(Type stateType)
    {
      this.stateType = stateType;
    }
  }

  public class RegisterEventAttribute : Attribute
  {
  }

  internal static class RailRegistry
  {
    public static IRailPool<T> CreatePool<T>(
      Type derivedType)
      where T : IRailPoolable<T>
    {
      Type factoryType = typeof(RailPool<,>);
      Type specific = 
        factoryType.MakeGenericType(typeof(T), derivedType);
      ConstructorInfo ci = specific.GetConstructor(Type.EmptyTypes);
      return (IRailPool<T>)ci.Invoke(new object[] { });
    }

    public static IList<KeyValuePair<Type, T>> FindAll<T>()
      where T : Attribute
    {
      List<KeyValuePair<Type, T>> found = new List<KeyValuePair<Type, T>>();

      foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        foreach (KeyValuePair<Type, T> pair in RailRegistry.Find<T>(assembly))
          found.Add(pair);

      // Sort by name in case different builds order the types differently
      found.OrderBy(x => x.Key.FullName);
      return found;
    }

    private static IEnumerable<KeyValuePair<Type, T>> Find<T>(
      Assembly assembly)
      where T : Attribute
    {
      foreach (Type type in assembly.GetTypes())
      {
        object[] attributes = type.GetCustomAttributes(typeof(T), false);
        if (attributes.Length > 0)
        {
          T attribute = (T)attributes[0];
          yield return new KeyValuePair<Type, T>(type, attribute);
        }
      }
    }
  }
}
