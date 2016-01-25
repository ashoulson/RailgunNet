using System;
using System.Collections.Generic;

using UnityEngine;

using Railgun;
using Reservoir;

//public class Example : IPoolable<Example>
//{
//  #region IPoolable Members
//  Pool<Example> IPoolable<Example>.Pool { get; set; }
//  Example INode<Example>.Next { get; set; }
//  Example INode<Example>.Previous { get; set; }
//  NodeList<Example> INode<Example>.List { get; set; }

//  void IPoolable<Example>.Initialize()
//  {

//  }

//  void IPoolable<Example>.Reset()
//  {

//  }
//  #endregion

//  // Your data and functions here
//  public int key;
//}

public class Test : MonoBehaviour
{
  //private Pool<Example> pool;
  //private NodeList<Example> list1;
  //private NodeList<Example> list2;

  void Start()
  {
    //this.pool = new Pool<Example>();
    //this.list1 = new NodeList<Example>();
    //this.list2 = new NodeList<Example>();
    RailgunUtil.RunTests();

  }

  void Update()
  {

    //this.RunTest();
  }

  //void RunTest()
  //{
  //  Debug.Log("Starting");
  //  Example a = this.pool.Allocate();
  //  Example b = this.pool.Allocate();
  //  Example c = this.pool.Allocate();
  //  Example d = this.pool.Allocate();
  //  Example e = this.pool.Allocate();
  //  Example f = this.pool.Allocate();

  //  a.key = 0;
  //  b.key = 1;
  //  c.key = 2;
  //  d.key = 3;
  //  e.key = 4;
  //  f.key = 5;

  //  this.list1.Add(a);
  //  this.list1.Add(b);
  //  this.list1.Add(c);
  //  this.list2.Add(d);
  //  this.list2.Add(e);
  //  this.list2.Add(f);

  //  this.PrintLists();

  //  this.list1.Remove(b);

  //  this.PrintLists();

  //  this.list1.Add(b);
  //  Example leaked = this.list1.RemoveFirst();

  //  this.PrintLists();

  //  this.list1.Append(list2);

  //  this.PrintLists();

  //  this.list1.Append(list2);

  //  this.PrintLists();

  //  this.list2.Append(list1);
  //  this.list1.Append(list1);

  //  this.PrintLists();

  //  Pool.FreeAll(list1);
  //  Pool.FreeAll(list2);
  //  Pool.Free(leaked);

  //  //Pool.Free(a);
  //  //Pool.Free(c);
  //  //Pool.Free(d);
  //  //Pool.Free(e);
  //  //Pool.Free(f);

  //  this.PrintLists();

  //  Debug.Log("Ending");
  //}

  //private void PrintLists()
  //{
  //  string output = "List 1:";
  //  foreach (Example e in this.list1)
  //    output += " " + e.key;
  //  Debug.Log(output);
  //  output = "List 2:";
  //  foreach (Example e in this.list2)
  //    output += " " + e.key;
  //  Debug.Log(output);
  //}
}
