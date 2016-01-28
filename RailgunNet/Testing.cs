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

using Reservoir;
using UnityEngine;

using Railgun.Domain;

namespace Railgun
{
  public static class Testing
  {

    public static void RunTests()
    {
      Railgun.Initialize();
      DomainEncoders.Initialize();

      Testing.TestBitPacker(50, 400);
      Testing.TestIntEncoder(200, 200);
      Testing.TestFloatEncoder(200, 200);
      Testing.TestEntityState(100);
      Testing.TestStateBag(100, 10);
      Debug.Log("Done Tests");
    }

    #region EntityState
    public static void TestEntityState(int iterations)
    {
      BitPacker bitPacker = new BitPacker();

      // Normally these are pooled, but we'll just allocate some here
      PawnState basis = new PawnState();
      PawnState current = new PawnState();
      PawnState decoded = new PawnState();
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

    internal static void TestCompare(PawnState a, PawnState b)
    {
      RailgunUtil.Assert(a.ArchetypeId == b.ArchetypeId, "ArchetypeId mismatch: " + (a.ArchetypeId - b.ArchetypeId));
      RailgunUtil.Assert(a.UserId == b.UserId, "UserId mismatch: " + (a.UserId - b.UserId));
      RailgunUtil.Assert(RailgunMath.CoordinatesEqual(a.X, b.X), "X mismatch: " + (a.X - b.X));
      RailgunUtil.Assert(RailgunMath.CoordinatesEqual(a.Y, b.Y), "Y mismatch: " + (a.Y - b.Y));
      RailgunUtil.Assert(RailgunMath.AnglesEqual(a.Angle, b.Angle), "Angle mismatch: " + (a.Angle - b.Angle));
      RailgunUtil.Assert(a.Status == b.Status, "Status mismatch: " + (a.Status - b.Status));
    }

    internal static void PopulateState(PawnState state)
    {
      state.SetData(
        UnityEngine.Random.Range(DomainEncoders.ArchetypeId.MinValue, DomainEncoders.ArchetypeId.MaxValue),
        UnityEngine.Random.Range(DomainEncoders.UserId.MinValue, DomainEncoders.UserId.MaxValue),
        UnityEngine.Random.Range(DomainEncoders.Coordinate.MinValue, DomainEncoders.Coordinate.MaxValue),
        UnityEngine.Random.Range(DomainEncoders.Coordinate.MinValue, DomainEncoders.Coordinate.MaxValue),
        UnityEngine.Random.Range(DomainEncoders.Angle.MinValue, DomainEncoders.Angle.MaxValue),
        UnityEngine.Random.Range(DomainEncoders.Status.MinValue, DomainEncoders.Status.MaxValue));
    }

    internal static void MutateState(PawnState state, PawnState basis)
    {
      state.SetData(
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DomainEncoders.ArchetypeId.MinValue, DomainEncoders.ArchetypeId.MaxValue) : basis.ArchetypeId,
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DomainEncoders.UserId.MinValue, DomainEncoders.UserId.MaxValue) : basis.UserId,
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DomainEncoders.Coordinate.MinValue, DomainEncoders.Coordinate.MaxValue) : basis.X,
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DomainEncoders.Coordinate.MinValue, DomainEncoders.Coordinate.MaxValue) : basis.Y,
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DomainEncoders.Angle.MinValue, DomainEncoders.Angle.MaxValue) : basis.Angle,
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(DomainEncoders.Status.MinValue, DomainEncoders.Status.MaxValue) : basis.Status);
    }
    #endregion

    #region StateBag
    private static void TestStateBag(int iterations, int numEntities)
    {
      // Populate the entity pool
      Pool<PawnState> pool = new Pool<PawnState>();
      TestFullSerialize(numEntities, pool);
      TestDeltaSerialize(numEntities, pool);

      //  StateBag<EntityState> first = new StateBag<EntityState>();
      //  first.AssignPool(pool);
      //  for (int i = 0; i < entityList.Count; i++)
      //  {
      //    EntityState toAdd = entityList[i];
      //    first.Add(ref toAdd);
      //  }

      ////  Snapshot.UpdateEntityPool(ref entityPool);
      //  Snapshot second = new Snapshot();
      //  for (int i = 0; i < entityList.Count; i++)
      //  {
      //    EntityState toAdd = entityList[i];
      //    first.AddEntityState(ref toAdd);
      //  }
    }

    private static void TestFullSerialize(int numEntities, Pool<PawnState> pool)
    {
      List<PawnState> stateCollection = CreateStateCollection(numEntities, pool);
      StateBag<PawnState> toSend = new StateBag<PawnState>();
      toSend.AssignPool(pool);
      foreach (PawnState state in stateCollection)
        toSend.Add(state);

      BitPacker packer = new BitPacker();
      toSend.Encode(packer);
      Debug.Log("Bag used: " + packer.BitsUsed + " bits.");

      StateBag<PawnState> toReceive = new StateBag<PawnState>();
      toReceive.AssignPool(pool);
      toReceive.Decode(packer);

      TestCompare(toSend, toReceive);
    }

    private static void TestDeltaSerialize(int numEntities, Pool<PawnState> pool)
    {
      // Create the basis bag (we won't be sending this)
      List<PawnState> stateCollection = CreateStateCollection(numEntities, pool);
      StateBag<PawnState> basis = new StateBag<PawnState>();
      basis.AssignPool(pool);
      foreach (PawnState state in stateCollection)
        basis.Add(state);

      // Create the mutated bag (we will be sending this)
      List<PawnState> mutatedStateCollection = CreateMutatedStateCollection(stateCollection, pool);
      StateBag<PawnState> toSend = new StateBag<PawnState>();
      toSend.AssignPool(pool);
      foreach (PawnState state in mutatedStateCollection)
        toSend.Add(state);

      BitPacker packer = new BitPacker();
      toSend.Encode(packer, basis);
      Debug.Log("Delta bag used: " + packer.BitsUsed + " bits.");

      StateBag<PawnState> toReceive = new StateBag<PawnState>();
      toReceive.AssignPool(pool);
      toReceive.Decode(packer, basis);

      TestCompare(toSend, toReceive);
    }

    private static void TestCompare(StateBag<PawnState> a, StateBag<PawnState> b)
    {
      RailgunUtil.Assert(a.Count == b.Count);
      foreach (PawnState stateA in a.stateList)
      {
        PawnState found;
        if (b.stateLookup.TryGetValue(stateA.Id, out found))
        {
          Testing.TestCompare(stateA, b.Get(stateA.Id));
        }
        else
        {
          RailgunUtil.Assert(false, "Not found in b: " + stateA.Id);
        }
      }
    }

    private static List<PawnState> CreateStateCollection(int count, Pool<PawnState> pool)
    {
      List<PawnState> stateCollection = new List<PawnState>();
      for (int i = 0; i < count; i++)
      {
        // Normally these should be pooled, but we'll shortcut
        PawnState state = pool.Allocate();
        state.Id = i;
        Testing.PopulateState(state);
        stateCollection.Add(state);
      }
      return stateCollection;
    }

    private static List<PawnState> CreateMutatedStateCollection(List<PawnState> basisCollection, Pool<PawnState> pool)
    {
      List<PawnState> stateCollection = new List<PawnState>();
      for (int i = 0; i < basisCollection.Count; i++)
      {
        PawnState basis = basisCollection[i];
        PawnState state = pool.Allocate();
        state.Id = basis.Id;
        Testing.MutateState(state, basis);
        stateCollection.Add(state);
      }
      return stateCollection;
    }

    private static void UpdateEntityPool(ref List<PawnState> entityPool)
    {
      for (int i = 0; i < entityPool.Count; i++)
        if (UnityEngine.Random.Range(0.0f, 1.0f) > 0.4f)
          Testing.MutateState(entityPool[i], entityPool[i]);
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
