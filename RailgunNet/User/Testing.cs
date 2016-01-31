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
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Railgun.User;

namespace Railgun
{
  public static class Testing
  {

    public static void RunTests()
    {
      Railgun.Initialize();
      UserEncoders.Initialize();

      Testing.TestBitPacker(50, 400);
      Testing.TestIntEncoder(200, 200);
      Testing.TestFloatEncoder(200, 200);
      Testing.TestEntityState(100);
      Testing.TestSnapshotTransmission(30, 10, 20);
      Debug.Log("Done Tests");
    }

    #region EntityState
    public static void TestEntityState(int iterations)
    {
      BitPacker bitPacker = new BitPacker();

      // Normally these are pooled, but we'll just allocate some here
      UserState basis = new UserState();
      UserState current = new UserState();
      UserState decoded = new UserState();
      basis.SetData(0, 0, 0.0f, 0.0f, 0.0f, 0);

      int maxBitsUsed = 0;
      float sum = 0.0f;

      for (int i = 0; i < iterations; i++)
      {
        Testing.MutateState(basis, current);

        float probability = UnityEngine.Random.Range(0.0f, 1.0f);
        if (probability > 0.5f)
        {
          current.Encode(bitPacker);
          maxBitsUsed = bitPacker.BitsUsed;
          sum += (float)bitPacker.BitsUsed;
          decoded.Decode(bitPacker);
          Testing.TestCompare(current, decoded);
        }
        else
        {
          if (current.Encode(bitPacker, basis))
          {
            sum += (float)bitPacker.BitsUsed;
            decoded.Decode(bitPacker, basis);
            Testing.TestCompare(current, decoded);
          }
        }

        basis.SetFrom(current);
      }

      Debug.Log("EntityState Max: " + maxBitsUsed + ", Avg: " + (sum / (float)iterations));
    }

    internal static void TestCompare(UserState a, UserState b)
    {
      RailgunUtil.Assert(a.ArchetypeId == b.ArchetypeId, "ArchetypeId mismatch: " + (a.ArchetypeId - b.ArchetypeId));
      RailgunUtil.Assert(a.UserId == b.UserId, "UserId mismatch: " + (a.UserId - b.UserId));
      RailgunUtil.Assert(UserMath.CoordinatesEqual(a.X, b.X), "X mismatch: " + (a.X - b.X));
      RailgunUtil.Assert(UserMath.CoordinatesEqual(a.Y, b.Y), "Y mismatch: " + (a.Y - b.Y));
      RailgunUtil.Assert(UserMath.AnglesEqual(a.Angle, b.Angle), "Angle mismatch: " + (a.Angle - b.Angle));
      RailgunUtil.Assert(a.Status == b.Status, "Status mismatch: " + (a.Status - b.Status));
    }

    internal static void PopulateState(UserState state)
    {
      state.SetData(
        UnityEngine.Random.Range(UserEncoders.ArchetypeId.MinValue, UserEncoders.ArchetypeId.MaxValue),
        UnityEngine.Random.Range(UserEncoders.UserId.MinValue, UserEncoders.UserId.MaxValue),
        UnityEngine.Random.Range(UserEncoders.Coordinate.MinValue, UserEncoders.Coordinate.MaxValue),
        UnityEngine.Random.Range(UserEncoders.Coordinate.MinValue, UserEncoders.Coordinate.MaxValue),
        UnityEngine.Random.Range(UserEncoders.Angle.MinValue, UserEncoders.Angle.MaxValue),
        UnityEngine.Random.Range(UserEncoders.Status.MinValue, UserEncoders.Status.MaxValue));
    }

    internal static void MutateState(UserState state, UserState basis)
    {
      state.SetData(
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(UserEncoders.ArchetypeId.MinValue, UserEncoders.ArchetypeId.MaxValue) : basis.ArchetypeId,
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(UserEncoders.UserId.MinValue, UserEncoders.UserId.MaxValue) : basis.UserId,
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(UserEncoders.Coordinate.MinValue, UserEncoders.Coordinate.MaxValue) : basis.X,
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(UserEncoders.Coordinate.MinValue, UserEncoders.Coordinate.MaxValue) : basis.Y,
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(UserEncoders.Angle.MinValue, UserEncoders.Angle.MaxValue) : basis.Angle,
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(UserEncoders.Status.MinValue, UserEncoders.Status.MaxValue) : basis.Status);
    }
    #endregion

    #region Snapshot/Interpreter

    private static void TestSnapshotTransmission(int numEntities, int innerIter, int outerIter)
    {
      int deltaCount = 0;
      int deltaSum = 0;
      int complete = 0;

      for (int i = 0; i < outerIter; i++)
      {
        Interpreter interpreter =
          new Interpreter(new Factory<UserState>());
        Environment environment = Testing.CreateEnvironment(interpreter, numEntities - 5);
        BitPacker bitPacker = new BitPacker();

        Snapshot lastSent = null;
        Snapshot lastReceived = null;

        for (int j = 0; j < innerIter; j++)
        {
          Snapshot sending = environment.Clone();
        
          if (lastSent != null)
          {
            interpreter.Encode(bitPacker, sending, lastSent);
            deltaSum += bitPacker.BitsUsed;
            deltaCount++;
          }
          else
          {
            interpreter.Encode(bitPacker, sending);
            int bitsUsed = bitPacker.BitsUsed;
            if (bitsUsed > complete)
              complete = bitsUsed;
          }

          Snapshot receiving;

          if (lastReceived != null)
          {
            receiving = interpreter.Decode(bitPacker, lastReceived);
          }
          else
          {
            receiving = interpreter.Decode(bitPacker);
          }

          RailgunUtil.Assert(bitPacker.BitsUsed == 0);
          Testing.FakeUpdateState(environment);
          if (environment.Count < numEntities)
            if (UnityEngine.Random.Range(0.0f, 1.0f) > 0.8f)
              Testing.FakeAddEntity(interpreter, environment);

          TestCompare(sending, receiving);
          lastSent = sending;
          lastReceived = receiving;
        }
      }

      Debug.Log("Snapshot Max: " + complete + ", Avg: " + ((float)deltaSum / (float)deltaCount));
    }

    private static void TestCompare(Snapshot a, Snapshot b)
    {
      RailgunUtil.Assert(a.Count == b.Count);
      foreach (Image iA in a.GetImages())
      {
        UserState stateA = (UserState)iA.State;
        UserState stateB = (UserState)b.Get(iA.Id).State;
        Testing.TestCompare(stateA, stateB);
      }
    }

    private static void FakeUpdateState(Environment environment)
    {
      foreach (Image image in environment.GetImages())
      {
        UserState state = (UserState)image.State;
        state.SetData(
          UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(UserEncoders.ArchetypeId.MinValue, UserEncoders.ArchetypeId.MaxValue) : state.ArchetypeId,
          UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(UserEncoders.UserId.MinValue, UserEncoders.UserId.MaxValue) : state.UserId,
          UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(UserEncoders.Coordinate.MinValue, UserEncoders.Coordinate.MaxValue) : state.X,
          UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(UserEncoders.Coordinate.MinValue, UserEncoders.Coordinate.MaxValue) : state.Y,
          UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(UserEncoders.Angle.MinValue, UserEncoders.Angle.MaxValue) : state.Angle,
          UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(UserEncoders.Status.MinValue, UserEncoders.Status.MaxValue) : state.Status);
      }
    }

    private static void FakeAddEntity(
      Interpreter interpreter, 
      Environment environment)
    {
      Entity entity = new Entity();
      interpreter.Link(entity);

      UserState state =
        (UserState)interpreter.CreateEmptyState(
          UserTypes.TYPE_USER_STATE);

      entity.Id = environment.Count;
      entity.State = state;

      Testing.PopulateState(state);
      environment.Add(entity);
    }

    private static Environment CreateEnvironment(
      Interpreter interpreter, 
      int numEntities)
    {
      Environment environment = new Environment();
      interpreter.Link(environment);
     
      for (int i = 0; i < numEntities; i++)
        Testing.FakeAddEntity(interpreter, environment);

      return environment;
    }
    #endregion

    #region IntEncoder
    public static void TestIntEncoder(int outerIter, int innerIter)
    {
      for (int i = 0; i < outerIter; i++)
      {
        int a = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        int b = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        if (a > b)
          RailgunUtil.Swap(ref a, ref b);
        IntEncoder serializer = new IntEncoder(a, b);

        for (int j = 0; j < innerIter; j++)
        {
          int random = UnityEngine.Random.Range(a, b);
          uint packed = serializer.Pack(random);
          int unpacked = serializer.Unpack(packed);

          RailgunUtil.Assert(random == unpacked,
            random +
            " " +
            unpacked +
            " " +
            (int)Mathf.Abs(random - unpacked) +
            " Min: " + a +
            " Max: " + b);
        }
      }

      // Test extreme cases
      IntEncoder extreme1 = new IntEncoder(0, 0);
      RailgunUtil.Assert(extreme1.Unpack(extreme1.Pack(0)) == 0, "A " + extreme1.Unpack(extreme1.Pack(0)));
      RailgunUtil.Assert(extreme1.Unpack(extreme1.Pack(1)) == 0, "B " + extreme1.Unpack(extreme1.Pack(1)));

      IntEncoder extreme2 = new IntEncoder(int.MinValue, int.MaxValue);
      RailgunUtil.Assert(extreme2.Unpack(extreme2.Pack(0)) == 0, "C " + extreme2.Unpack(extreme2.Pack(0)));
      RailgunUtil.Assert(extreme2.Unpack(extreme2.Pack(1024)) == 1024, "D " + extreme2.Unpack(extreme2.Pack(1024)));
      RailgunUtil.Assert(extreme2.Unpack(extreme2.Pack(int.MaxValue)) == int.MaxValue, "E " + extreme2.Unpack(extreme2.Pack(int.MaxValue)));
      RailgunUtil.Assert(extreme2.Unpack(extreme2.Pack(int.MinValue)) == int.MinValue, "F " + extreme2.Unpack(extreme2.Pack(int.MinValue)));
    }
    #endregion

    #region FloatEncoder
    public static void TestFloatEncoder(int outerIter, int innerIter)
    {
      for (int i = 0; i < outerIter; i++)
      {
        float a = UnityEngine.Random.Range(-10000000.0f, 10000000.0f);
        float b = UnityEngine.Random.Range(-10000000.0f, 10000000.0f);
        float precision = UnityEngine.Random.Range(0.0001f, 1.0f);

        if (a < b)
          RailgunUtil.Swap(ref a, ref b);
        FloatEncoder serializer = new FloatEncoder(a, b, precision);

        for (int j = 0; j < innerIter; j++)
        {
          float random = UnityEngine.Random.Range(a, b);
          uint packed = serializer.Pack(random);
          float unpacked = serializer.Unpack(packed);

          RailgunUtil.Assert(Mathf.Abs(random - unpacked) > precision,
            random +
            " " +
            unpacked +
            " " +
            Mathf.Abs(random - unpacked));
        }
      }
    }
    #endregion

    #region Debug
    /// <summary>
    /// Unit test for functionality.
    /// </summary>
    public static void TestBitPacker(int maxValues, int iterations)
    {
      BitPacker buffer = new BitPacker(1);
      Stack<uint> values = new Stack<uint>(maxValues);
      Stack<int> bits = new Stack<int>(maxValues);

      bool push = true;
      for (int i = 0; i < iterations; i++)
      {
        if (values.Count <= 0)
        {
          push = true; // Must push
        }
        else if (values.Count >= maxValues)
        {
          push = false; // Must pop
        }
        else
        {
          float probability = UnityEngine.Random.Range(0.0f, 1.0f);
          if (probability > 0.95f)
          {
            buffer.Clear();
            values.Clear();
            bits.Clear();
            continue;
          }
          else if (probability > 0.4f)
          {
            push = true;
          }
          else
          {
            push = false;
          }
        }

        if (values.Count > 0)
          RailgunUtil.Assert(buffer.Peek(bits.Peek()) == values.Peek());

        if (push)
        {
          uint randVal = 0;
          unchecked
          {
            uint randNum =
              (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            randVal = randNum;
          }
          int randBits = UnityEngine.Random.Range(0, 32);
          uint trimmedVal = randVal & (uint)((1 << randBits) - 1);

          values.Push(trimmedVal);
          bits.Push(randBits);
          buffer.Push(trimmedVal, randBits);
        }
        else
        {
          uint expectedVal = values.Pop();
          int expectedBits = bits.Pop();
          uint retrievedVal = buffer.Pop(expectedBits);

          if (expectedVal != retrievedVal)
            Debug.LogWarning(
              "Expected: " +
              expectedVal +
              " Got: " +
              retrievedVal);
        }
      }
    }
    #endregion
  }
}
