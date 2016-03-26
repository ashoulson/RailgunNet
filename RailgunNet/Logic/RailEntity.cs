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

using System.Linq;

namespace Railgun
{
  public abstract class RailEntity
  {
    private static void ComputeSmoothed(
      Tick realTick,
      float frameDelta,
      RailState.Record first,
      RailState.Record second,
      RailState destination)
    {
      destination.ApplySmoothed(
        first.State,
        second.State,
        RailMath.ComputeInterp(
          first.Tick.Time,
          second.Tick.Time,
          realTick.Time + frameDelta));
    }

    private class SmoothingBuffer
    {
      private RailState.Record prevRecord;
      private RailState.Record curRecord;
      private RailState.Record nextRecord;

      private readonly RailDejitterBuffer<RailState.Delta> buffer;
      private RailState cachedOutput;

      internal SmoothingBuffer(RailDejitterBuffer<RailState.Delta> buffer)
      {
        this.buffer = buffer;
        this.curRecord = null;
        this.cachedOutput = null;
      }

      /// <summary>
      /// Repopulates the state buffer with new values based on the delta
      /// dejitter buffer provided by the entity.
      /// </summary>
      internal RailState Update(Tick current)
      {
        this.ClearRecords();

        RailState.Delta curDelta;
        RailState.Delta nextDelta;

        this.buffer.GetRangeAt(
          current,
          out curDelta,
          out nextDelta);

        if (curDelta != null)
        {
          if (this.curRecord == null)
            this.Initialize(curDelta);

          // Apply the delta to the current auth record and push it
          // into the current-past double buffer
          if (this.curRecord.Tick < curDelta.Tick)
          {
            if (this.prevRecord != null)
              RailPool.Free(this.prevRecord);
            this.prevRecord = this.curRecord;
            this.curRecord = this.CreateRecord(curDelta);
          }

          // Try to create a record for the next state as well, but make sure
          // the next record is actually in the future (no time jumps)
          if ((nextDelta != null) && (nextDelta.Tick > this.curRecord.Tick))
          {
            this.nextRecord = this.CreateRecord(nextDelta);
          }
        }

        return this.curRecord.State;
      }

      /// <summary>
      /// Interpolates or extrapolates to get a smooth state.
      /// </summary>
      internal RailState GetSmoothed(
        float frameDelta,
        Tick curTick)
      {
        this.cachedOutput.OverwriteFrom(this.curRecord.State);

        if (this.nextRecord != null)
        {
          RailEntity.ComputeSmoothed(
            curTick,
            frameDelta,
            this.curRecord,
            this.nextRecord,
            this.cachedOutput);
        }
        else if (this.prevRecord != null)
        {
          RailEntity.ComputeSmoothed(
            curTick,
            frameDelta,
            this.prevRecord,
            this.curRecord,
            this.cachedOutput);
        }

        return this.cachedOutput;
      }

      /// <summary>
      /// Initialzes the double buffer with the first delta we
      /// receive from the server.
      /// </summary>
      private void Initialize(RailState.Delta firstDelta)
      {
        RailDebug.Assert(firstDelta.HasImmutableData);
        this.cachedOutput = firstDelta.State.Clone();
        this.curRecord = 
          RailState.CreateRecord(
            firstDelta.Tick, 
            firstDelta.State.Clone());
      }

      private void ClearRecords()
      {
        // Clear out the prior next record since it may now be invalid
        if (this.nextRecord != null)
          RailPool.Free(this.nextRecord);
        this.nextRecord = null;
      }

      /// <summary>
      /// Copies the current record and applies the delta to it.
      /// </summary>
      private RailState.Record CreateRecord(RailState.Delta delta)
      {
        RailState.Record result =
          RailState.CreateRecord(
            delta.Tick,
            this.curRecord.State);
        result.State.ApplyDelta(delta);
        return result;
      }
    }

    private class PredictionBuffer
    {
      private RailState.Record prevRecord;
      private RailState.Record curRecord;

      private readonly RailDejitterBuffer<RailState.Delta> buffer;
      private RailState cachedOutput;

      internal PredictionBuffer(RailDejitterBuffer<RailState.Delta> buffer)
      {
        this.buffer = buffer;
      }

      internal RailState Start(
        Tick current,
        RailState currentState)
      {
        this.ClearRecords();

        // Bring us up to the last received state in the buffer
        RailState lastState = currentState.Clone();

        // Need to apply all the deltas in order
        foreach (RailState.Delta delta in this.buffer.GetLatestFrom(current))
          lastState.ApplyDelta(delta);
        this.curRecord =
          RailState.CreateRecord(
            this.buffer.Latest.Tick,
            lastState);

        if (this.cachedOutput == null)
          this.cachedOutput = lastState.Clone();
        else
          this.cachedOutput.OverwriteFrom(lastState);

        return this.curRecord.State;
      }

      internal void Update(
        RailState currentState)
      {
        if (this.prevRecord != null)
          RailPool.Free(this.prevRecord);
        this.prevRecord = this.curRecord;

        this.curRecord =
          RailState.CreateRecord(
            this.curRecord.Tick + 1,
            currentState);
      }

      /// <summary>
      /// Interpolates if possible to get a smoothed state.
      /// </summary>
      internal RailState GetSmoothed(
        float frameDelta)
      {
        this.cachedOutput.OverwriteFrom(this.curRecord.State);

        if (this.prevRecord != null)
        {
          RailEntity.ComputeSmoothed(
            this.prevRecord.Tick,
            frameDelta,
            this.prevRecord,
            this.curRecord,
            this.cachedOutput);
        }

        return this.cachedOutput;
      }

      private void ClearRecords()
      {
        if (this.prevRecord != null)
          RailPool.Free(this.prevRecord);
        if (this.curRecord != null)
          RailPool.Free(this.curRecord);

        this.prevRecord = null;
        this.prevRecord = null;
      }
    }

    internal static RailEntity Create(int factoryType)
    {
      RailEntity entity = RailResource.Instance.CreateEntity(factoryType);
      entity.factoryType = factoryType;
      entity.State = RailState.Create(factoryType);
      return entity;
    }

    internal static T Create<T>()
      where T : RailEntity
    {
      int factoryType = RailResource.Instance.GetEntityFactoryType<T>();
      return (T)RailEntity.Create(factoryType);
    }

    // Settings
    protected virtual bool ForceUpdates { get { return true; } } // Server-only

    // Simulation info
    protected internal RailWorld World { get; internal set; }
    public IRailController Controller { get { return this.controller; } }
    public RailState State { get; private set; }

    // Synchronization info
    public EntityId Id { get; private set; }

    private RailDejitterBuffer<RailState.Delta> incoming;  // Client-only
    private RailQueueBuffer<RailState.Record> outgoing;    // Server-only
    private IRailControllerInternal controller;

    internal virtual void SimulateCommand(RailCommand command) { }
    protected virtual void Simulate() { }
    protected virtual void Start() { }

    private int factoryType;
    private bool hasStarted;

    // Client-only
    private readonly SmoothingBuffer smoothBuffer;
    private readonly PredictionBuffer predictBuffer;

    internal RailEntity()
    {
      this.World = null;

      this.Id = EntityId.INVALID;
      this.State = null;

      this.controller = null;
      this.hasStarted = false;

      // TODO: Don't need this on the server!
      this.incoming =
        new RailDejitterBuffer<RailState.Delta>(
          RailConfig.DEJITTER_BUFFER_LENGTH,
          RailConfig.NETWORK_SEND_RATE);
      this.smoothBuffer = new SmoothingBuffer(this.incoming);
      this.predictBuffer = new PredictionBuffer(this.incoming);

      // TODO: Don't need this on the client!
      this.outgoing =
        new RailQueueBuffer<RailState.Record>(
          RailConfig.DEJITTER_BUFFER_LENGTH);
    }

    internal void AssignId(EntityId id)
    {
      this.Id = id;
    }

    internal void AssignController(IRailControllerInternal controller)
    {
      this.controller = controller;
    }

    private void DoStart()
    {
      if (this.hasStarted == false)
        this.Start();
      this.hasStarted = true;
    }

    #region Server
    internal void UpdateServer()
    {
      this.DoStart();
      IRailControllerInternal controller = this.controller;
      if ((controller != null) && (controller.LatestCommand != null))
        this.SimulateCommand(this.controller.LatestCommand);
      this.Simulate();
    }

    internal void StoreRecord()
    {
      RailState.Record record =
        RailState.CreateRecord(
          this.World.Tick,
          this.State, 
          this.outgoing.Latest);
      if (record != null)
        this.outgoing.Store(record);
    }

    internal RailState.Delta ProduceDelta(
      Tick basisTick, 
      IRailController destination)
    {
      RailState.Record basis = null;
      if (basisTick.IsValid)
        basis = this.outgoing.LatestAt(basisTick);

      return RailState.CreateDelta(
        this.World.Tick,
        this.Id,
        this.State,
        basis,
        (destination == this.controller),
        (basisTick.IsValid == false),
        this.ForceUpdates);
    }
    #endregion

    #region Client
    internal void UpdateClient()
    {
      this.UpdateSmoothing();
      this.DoStart();
      if (this.controller != null)
        this.UpdatePrediction();
    }

    internal void ReceiveDelta(RailState.Delta delta)
    {
      this.incoming.Store(delta);
    }

    internal bool HasReadyState(Tick tick)
    {
      return (this.incoming.GetLatestAt(tick) != null);
    }

    internal RailState GetSmoothedState(float frameDelta)
    {
      if (this.controller == null)
        return this.smoothBuffer.GetSmoothed(frameDelta, this.World.Tick);
      else
        return this.predictBuffer.GetSmoothed(frameDelta);
    }

    private void UpdateSmoothing()
    {
      RailState currentState =
        this.smoothBuffer.Update(this.World.Tick);
      this.State.OverwriteFrom(currentState);
    }

    private void UpdatePrediction()
    {
      RailState latestState =
        this.predictBuffer.Start(this.World.Tick, this.State);
      this.State.OverwriteFrom(latestState);

      foreach (RailCommand command in this.controller.PendingCommands)
      {
        this.SimulateCommand(command);
        this.Simulate();
        this.predictBuffer.Update(this.State);
      }
    }
    #endregion
  }

  /// <summary>
  /// Handy shortcut class for auto-casting the internal state.
  /// </summary>
  public abstract class RailEntity<TState> : RailEntity
    where TState : RailState, new()
  {
    public new TState State { get { return (TState)base.State; } }

    public new TState GetSmoothedState(float frameDelta) 
    { 
      return (TState)base.GetSmoothedState(frameDelta); 
    }
  }

  /// <summary>
  /// Handy shortcut class for auto-casting the internal state and command.
  /// </summary>
  public abstract class RailEntity<TState, TCommand> : RailEntity<TState>
    where TState : RailState, new()
    where TCommand : RailCommand
  {
    internal override void SimulateCommand(RailCommand command)
    {
      this.SimulateCommand((TCommand)command);
    }

    protected virtual void SimulateCommand(TCommand command) { }
  }
}
