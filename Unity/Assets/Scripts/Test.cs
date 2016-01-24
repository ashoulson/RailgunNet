using System;
using System.Collections.Generic;

using UnityEngine;

using Railgun;

public class Test : MonoBehaviour
{
  void Start()
  {
    IntEncoder.Test(100, 100);
    FloatEncoder.Test(100, 100);
    Debug.Log("Done");
  }

  void Update()
  {
  }
}
