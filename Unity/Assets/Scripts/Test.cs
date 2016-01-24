using System;
using System.Collections.Generic;

using UnityEngine;

using RailgunNet;

public class Test : MonoBehaviour 
{
  void Start () 
  {
    bool failed = false;
    for (int i = 0; i < 50; i++)
    {
      if (BitPacker.TestBitPacker(100, 500) == false)
        failed = true;
    }
    Debug.Log(failed);
  }
  
  void Update () 
  {
  }
}
