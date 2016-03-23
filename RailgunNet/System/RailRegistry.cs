using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    {
      Type factoryType = typeof(RailPool<,>);
      Type specific = 
        factoryType.MakeGenericType(typeof(T), derivedType);
      ConstructorInfo ci = specific.GetConstructor(Type.EmptyTypes);
      return (IRailPool<T>)ci.Invoke(new object[] { });
    }

    public static IRailFactory<T> CreateFactory<T>(
      Type derivedType)
    {
      Type factoryType = typeof(RailFactory<,>);
      Type specific =
        factoryType.MakeGenericType(typeof(T), derivedType);
      ConstructorInfo ci = specific.GetConstructor(Type.EmptyTypes);
      return (IRailFactory<T>)ci.Invoke(new object[] { });
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
