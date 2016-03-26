using System;
using System.Collections.Generic;

using UnityEngine;

public class TestBSearch : MonoBehaviour 
{
  // http://stackoverflow.com/questions/6436246/linq-to-find-the-closest-number-that-is-greater-less-than-an-input
	void Start () 
	{
    List<int> l = new List<int>() { 3, 5, 8, 11, 12, 13, 14, 21 };
    var found = l.BinarySearch(-1);
    if (found < 0) // the value 10 wasn't found
      found = (~found - 1);
    if (found < 0)
      found = 0;
    Debug.Log(found);
    var value = l[found];
    Debug.Log(value);
	}
	
	void Update () 
	{
	}
}
