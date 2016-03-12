using System;
using System.Collections.Generic;

using UnityEngine;

using Railgun;

public class TestBitBuffer : MonoBehaviour 
{
  public static int COUNT = 12;

	void Start () 
	{
    BitBuffer buffer = new BitBuffer();
    byte[] output = null;
    string show = "";

    buffer.Push(0, 0xFFFFFFFF);

    buffer.SetRollback();

    buffer.Push(32, 0xFFFFFFFF);
    buffer.Push(32, 0xFFFFFFFF);
    buffer.Push(32, 0xFFFFFFFF);

    buffer.Rollback();

    Debug.Log(buffer.Position);

    //buffer.Push(5, 0x11);
    //buffer.Push(3, 0x5);    

    output = new byte[COUNT];
    buffer.StoreBytes(output);

    show = "";
    for (int i = COUNT - 1; i >= 0; i--)
      show += Convert.ToString(output[i], 2).PadLeft(8, '0') + " ";
    Debug.Log(show);

    Debug.Log(buffer.Position);
    Debug.Log(buffer.ByteSize);

    //buffer.Rollback();

    //output = new byte[COUNT];
    //buffer.StoreBytes(output);

    //show = "";
    //for (int i = COUNT - 1; i >= 0; i--)
    //  show += Convert.ToString(output[i], 2).PadLeft(8, '0'); ;
    //Debug.Log(show);
	}
	
	void Update () 
	{
	}
}
