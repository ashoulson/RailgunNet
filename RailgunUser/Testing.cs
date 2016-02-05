///*
// *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
// *  Copyright (c) 2016 - Alexander Shoulson - http://ashoulson.com
// *
// *  This software is provided 'as-is', without any express or implied
// *  warranty. In no event will the authors be held liable for any damages
// *  arising from the use of this software.
// *  Permission is granted to anyone to use this software for any purpose,
// *  including commercial applications, and to alter it and redistribute it
// *  freely, subject to the following restrictions:
// *  
// *  1. The origin of this software must not be misrepresented; you must not
// *     claim that you wrote the original software. If you use this software
// *     in a product, an acknowledgment in the product documentation would be
// *     appreciated but is not required.
// *  2. Altered source versions must be plainly marked as such, and must not be
// *     misrepresented as being the original software.
// *  3. This notice may not be removed or altered from any source distribution.
//*/

//using System;
//using System.Collections;
//using System.Collections.Generic;

//using Railgun;

//using UnityEngine;

//public static class Testing
//{

//  public static void RunTests()
//  {
//    Railgun.Initialize();
//    DemoEncoders.Initialize();

//    Testing.TestBitBuffer(50, 400);
//    //Testing.TestIntEncoder(200, 200);
//    //Testing.TestFloatEncoder(200, 200);
//    Testing.TestEntityState(100);
//    Testing.TestHostPacketTransmission(30, 10, 20);
//    Debug.Log("Done Tests");
//  }

//  #region EntityState
//  public static void TestEntityState(int iterations)
//  {
//    BitBuffer buffer = new BitBuffer();

//    // Normally these are pooled, but we'll just allocate some here
//    DemoState basis = new DemoState();
//    DemoState current = new DemoState();
//    DemoState decoded = new DemoState();
//    basis.SetData(0, 0, 0.0f, 0.0f, 0.0f, 0);

//    int maxBitsUsed = 0;
//    float sum = 0.0f;

//    for (int i = 0; i < iterations; i++)
//    {
//      Testing.MutateState(basis, current);

//      float probability = UnityEngine.Random.Range(0.0f, 1.0f);
//      if (probability > 0.5f)
//      {
//        current.Encode(buffer);
//        maxBitsUsed = buffer.BitsUsed;
//        sum += (float)buffer.BitsUsed;
//        decoded.Decode(buffer);
//        Testing.TestCompare(current, decoded);
//      }
//      else
//      {
//        if (current.Encode(buffer, basis))
//        {
//          sum += (float)buffer.BitsUsed;
//          decoded.Decode(buffer, basis);
//          Testing.TestCompare(current, decoded);
//        }
//      }

//      basis.SetFrom(current);
//    }

//    Debug.Log("EntityState Max: " + maxBitsUsed + "b, Avg: " + (int)((sum / (float)iterations) + 0.5f) + "b");
//  }

//  internal static void TestCompare(DemoState a, DemoState b)
//  {
//    RailgunUtil.Assert(a.ArchetypeId == b.ArchetypeId, "ArchetypeId mismatch: " + (a.ArchetypeId - b.ArchetypeId));
//    RailgunUtil.Assert(a.UserId == b.UserId, "UserId mismatch: " + (a.UserId - b.UserId));
//    RailgunUtil.Assert(DemoMath.CoordinatesEqual(a.X, b.X), "X mismatch: " + (a.X - b.X));
//    RailgunUtil.Assert(DemoMath.CoordinatesEqual(a.Y, b.Y), "Y mismatch: " + (a.Y - b.Y));
//    RailgunUtil.Assert(DemoMath.AnglesEqual(a.Angle, b.Angle), "Angle mismatch: " + (a.Angle - b.Angle));
//    RailgunUtil.Assert(a.Status == b.Status, "Status mismatch: " + (a.Status - b.Status));
//  }

//  internal static void PopulateState(DemoState state)
//  {
//    state.SetData(
//      UnityEngine.Random.Range(DemoEncoders.ArchetypeId.MinValue, DemoEncoders.ArchetypeId.MaxValue),
//      UnityEngine.Random.Range(DemoEncoders.UserId.MinValue, DemoEncoders.UserId.MaxValue),
//      UnityEngine.Random.Range(DemoEncoders.Coordinate.MinValue, DemoEncoders.Coordinate.MaxValue),
//      UnityEngine.Random.Range(DemoEncoders.Coordinate.MinValue, DemoEncoders.Coordinate.MaxValue),
//      UnityEngine.Random.Range(DemoEncoders.Angle.MinValue, DemoEncoders.Angle.MaxValue),
//      UnityEngine.Random.Range(DemoEncoders.Status.MinValue, DemoEncoders.Status.MaxValue));
//  }

//  internal static void MutateState(DemoState state, DemoState basis)
//  {
//    state.SetData(
//      UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DemoEncoders.ArchetypeId.MinValue, DemoEncoders.ArchetypeId.MaxValue) : basis.ArchetypeId,
//      UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DemoEncoders.UserId.MinValue, DemoEncoders.UserId.MaxValue) : basis.UserId,
//      UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DemoEncoders.Coordinate.MinValue, DemoEncoders.Coordinate.MaxValue) : basis.X,
//      UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DemoEncoders.Coordinate.MinValue, DemoEncoders.Coordinate.MaxValue) : basis.Y,
//      UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DemoEncoders.Angle.MinValue, DemoEncoders.Angle.MaxValue) : basis.Angle,
//      UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DemoEncoders.Status.MinValue, DemoEncoders.Status.MaxValue) : basis.Status);
//  }
//  #endregion

//  //#region Snapshot/Interpreter

//  //private static void TestHostPacketTransmission(int numEntities, int innerIter, int outerIter)
//  //{
//  //  int deltaCount = 0;
//  //  int deltaSum = 0;
//  //  int complete = 0;

//  //  for (int i = 0; i < outerIter; i++)
//  //  {
//  //    PoolContext poolContext = new PoolContext(new Factory<DemoState>());
//  //    Interpreter interpreter = new Interpreter(poolContext);
//  //    Environment environment = 
//  //      Testing.CreateEnvironment(
//  //        poolContext, 
//  //        interpreter, 
//  //        numEntities - 5);

//  //    RingBuffer<Snapshot> receivedBuffer = new RingBuffer<Snapshot>(60);
//  //    Snapshot lastSent = null;

//  //    for (int j = 0; j < innerIter; j++)
//  //    {
//  //      environment.Frame++;

//  //      Snapshot sending = environment.CreateSnapshot(poolContext);
//  //      byte[] payload = null;
        
//  //      // SEND
//  //      if (lastSent != null)
//  //      {
//  //        payload = interpreter.Encode(sending, lastSent);
//  //        deltaSum += payload.Length;
//  //        deltaCount++;
//  //      }
//  //      else
//  //      {
//  //        payload = interpreter.Encode(sending);
//  //        int bitsUsed = payload.Length;
//  //        if (bitsUsed > complete)
//  //          complete = bitsUsed;
//  //      }

//  //      // RECEIVE
//  //      Snapshot receiving =
//  //        interpreter.Decode(
//  //          payload, 
//  //          receivedBuffer);
//  //      receivedBuffer.Store(receiving);

//  //      Testing.FakeUpdateState(environment);
//  //      if (environment.Count < numEntities)
//  //        if (UnityEngine.Random.Range(0.0f, 1.0f) > 0.8f)
//  //          Testing.FakeAddEntity(poolContext, interpreter, environment);

//  //      TestCompare(sending, receiving);
//  //      lastSent = sending;
//  //    }
//  //  }

//  //  Debug.Log("Snapshot Max: " + complete + "B, Avg: " + (int)(((float)deltaSum / (float)deltaCount) + 0.5f) + "B");
//  //}

//  //private static void TestCompare(Snapshot a, Snapshot b)
//  //{
//  //  RailgunUtil.Assert(a.Count == b.Count);
//  //  foreach (Image iA in a.GetValues())
//  //  {
//  //    DemoState stateA = (DemoState)iA.State;
//  //    DemoState stateB = (DemoState)b.Get(iA.Id).State;
//  //    Testing.TestCompare(stateA, stateB);
//  //  }
//  //}

//  //private static void FakeUpdateState(Environment environment)
//  //{
//  //  foreach (Entity entity in environment.GetValues())
//  //  {
//  //    DemoState state = (DemoState)entity.State;
//  //    state.SetData(
//  //      UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DemoEncoders.ArchetypeId.MinValue, DemoEncoders.ArchetypeId.MaxValue) : state.ArchetypeId,
//  //      UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DemoEncoders.UserId.MinValue, DemoEncoders.UserId.MaxValue) : state.UserId,
//  //      UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DemoEncoders.Coordinate.MinValue, DemoEncoders.Coordinate.MaxValue) : state.X,
//  //      UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DemoEncoders.Coordinate.MinValue, DemoEncoders.Coordinate.MaxValue) : state.Y,
//  //      UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DemoEncoders.Angle.MinValue, DemoEncoders.Angle.MaxValue) : state.Angle,
//  //      UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DemoEncoders.Status.MinValue, DemoEncoders.Status.MaxValue) : state.Status);
//  //  }
//  //}

//  //private static void FakeAddEntity(
//  //  Context context,
//  //  Interpreter interpreter, 
//  //  Environment environment)
//  //{
//  //  Entity entity = new DemoEntity();

//  //  DemoState state =
//  //    (DemoState)context.AllocateState(
//  //      DemoTypes.TYPE_DEMO);

//  //  entity.Id = environment.Count;
//  //  entity.State = state;

//  //  Testing.PopulateState(state);
//  //  environment.Add(entity);
//  //}

//  //private static Environment CreateEnvironment(
//  //  PoolContext poolContext,
//  //  Interpreter interpreter, 
//  //  int numEntities)
//  //{
//  //  Environment environment = new Environment();
     
//  //  for (int i = 0; i < numEntities; i++)
//  //    Testing.FakeAddEntity(poolContext, interpreter, environment);

//  //  return environment;
//  //}
//  //#endregion

//  #region Debug
//  #endregion
//}
