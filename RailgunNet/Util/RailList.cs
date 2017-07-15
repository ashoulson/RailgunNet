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

namespace Railgun
{
  public interface IRailListNode<T>
    where T : class, IRailListNode<T>
  {
    T Next { get; set; }
    T Prev { get; set; }
    RailList<T> List { get; set; }
  }

  public struct RailListIterator<T>
    where T : class, IRailListNode<T>
  {
    T iter;

    internal RailListIterator(T first)
    {
      this.iter = first;
    }

    public bool Next(out T value)
    {
      if (this.iter != null)
      {
        value = this.iter;
        this.iter = this.iter.Next;
        return true;
      }

      value = null;
      return false;
    }
  }

  public class RailList<T>
    where T : class, IRailListNode<T>
  {
    public int Count { get; private set; }
    public T First { get; private set; }
    public T Last { get; private set; }

    public RailList()
    {
      this.First = null;
      this.Last = null;
      this.Count = 0;
    }

    public RailListIterator<T> GetIterator()
    {
      return new RailListIterator<T>(this.First);
    }

    public RailListIterator<T> GetIterator(T startAfter)
    {
#if DEBUG
      if (startAfter.List != this)
        throw new ArgumentException("Node is not in this list");
#endif
      if (startAfter == null)
        throw new ArgumentException();
      return new RailListIterator<T>(startAfter.Next);
    }

    /// <summary>
    /// Adds a node to the end of the list. O(1)
    /// </summary>
    public void Add(T value)
    {
#if DEBUG
      if (value.List != null)
        throw new InvalidOperationException("Value is already in a list");
#endif

      if (this.First == null)
        this.First = value;
      value.Prev = this.Last;

      if (this.Last != null)
        this.Last.Next = value;
      value.Next = null;

      this.Last = value;

#if DEBUG
      value.List = this;
#endif
      this.Count++;
    }

    /// <summary>
    /// Adds a node to the beginning of the list. O(1)
    /// </summary>
    public void InsertFirst(T value)
    {
#if DEBUG
      if (value.List != null)
        throw new InvalidOperationException("Value is already in a list");
#endif

      value.Prev = null;
      value.Next = this.First;

      if (this.First != null)
        this.First.Prev = value;
      this.First = value;

#if DEBUG
      value.List = this;
#endif
      this.Count++;
    }

    /// <summary>
    /// Adds a node before the given one. O(1)
    /// </summary>
    public void InsertBefore(T node, T value)
    {
#if DEBUG
      if (node.List != this)
        throw new ArgumentException("Node is not in this list");
      if (value.List != null)
        throw new InvalidOperationException("Value is already in a list");
#endif

      if (this.First == node)
        this.First = value;
      if (node.Prev != null)
        node.Prev.Next = value;

      value.Prev = node.Prev;
      value.Next = node;
      node.Prev = value;

#if DEBUG
      value.List = this;
#endif
      this.Count++;
    }

    /// <summary>
    /// Adds a node after the given one. O(1)
    /// </summary>
    public void InsertAfter(T node, T value)
    {
#if DEBUG
      if (node.List != this)
        throw new ArgumentException("Node is not in this list");
      if (value.List != null)
        throw new InvalidOperationException("Value is already in a list");
#endif

      if (this.Last == node)
        this.Last = value;
      if (node.Next != null)
        node.Next.Prev = value;

      value.Next = node.Next;
      value.Prev = node;
      node.Next = value;

#if DEBUG
      value.List = this;
#endif
      this.Count++;
    }

    /// <summary>
    /// Removes and returns a node from the list. O(1)
    /// </summary>
    public T Remove(T node)
    {
#if DEBUG
      if (node.List != this)
        throw new ArgumentException("Node is not in this list");
#endif

      if (this.First == node)
        this.First = node.Next;
      if (this.Last == node)
        this.Last = node.Prev;

      if (node.Prev != null)
        node.Prev.Next = node.Next;
      if (node.Next != null)
        node.Next.Prev = node.Prev;

      node.Next = null;
      node.Prev = null;

#if DEBUG
      node.List = null;
#endif
      this.Count--;
      return node;
    }

    /// <summary>
    /// Removes and returns the first element. O(1)
    /// </summary>
    public T RemoveFirst()
    {
      if (this.First == null)
        throw new InvalidOperationException();

      T result = this.First;
      if (result.Next != null)
        result.Next.Prev = null;
      this.First = result.Next;
      if (this.Last == result)
        this.Last = null;

      result.Next = null;
      result.Prev = null;

#if DEBUG
      result.List = null;
#endif
      this.Count--;
      return result;
    }

    /// <summary>
    /// Removes and returns the last element. O(1)
    /// </summary>
    public T RemoveLast()
    {
      if (this.Last == null)
        throw new InvalidOperationException();

      T result = this.Last;
      if (result.Prev != null)
        result.Prev.Next = null;
      this.Last = result.Prev;
      if (this.First == result)
        this.First = null;

      result.Next = null;
      result.Prev = null;

#if DEBUG
      result.List = null;
#endif
      this.Count--;
      return result;
    }

    /// <summary>
    /// Gets the node after a given one. O(1)
    /// </summary>
    public T GetNext(T node)
    {
#if DEBUG
      if (node.List != this)
        throw new ArgumentException("Node is not in this list");
#endif
      return node.Next;
    }

    /// <summary>
    /// Returns all of the values in the list. Slower due to foreach. O(n)
    /// </summary>
    public IEnumerable<T> GetValues()
    {
      T iter = this.First;
      while (iter != null)
      {
        yield return iter;
        iter = iter.Next;
      }
    }

    /// <summary>
    /// Applies an action to every member of the list. O(n)
    /// </summary>
    public void ForEach(Action<T> action)
    {
      T iter = this.First;
      while (iter != null)
      {
        action.Invoke(iter);
        iter = iter.Next;
      }
    }

    /// <summary>
    /// Clears the list. Does not free or modify values. O(1)
    /// </summary>
    public void Clear()
    {
      this.First = null;
      this.Last = null;
      this.Count = 0;
    }
  }
}
