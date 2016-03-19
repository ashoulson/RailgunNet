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

using CommonTools;

namespace Railgun
{
  public abstract class RailEntity
  {
    public static void RegisterEntityType<TEntity, TState>(int type)
      where TEntity : RailEntity<TState>, new()
      where TState : RailState, new()
    {
      RailResource.Instance.RegisterEntityType<TEntity, TState>(type);
    }

    internal IRailController Controller { get {return this.controller; } }
    internal RailStateBuffer StateBuffer { get; private set; }
    internal RailStateDelta StateDelta { get; private set; }

    private IRailControllerInternal controller;

    /// <summary>
    /// A unique network ID assigned to this entity.
    /// </summary>
    public EntityId Id { get; private set; }

    /// <summary>
    /// The int index for the type of entity this state applies to.
    /// Set by the resource manager when creating this entity.
    /// </summary>
    internal int Type { get; private set; }

    protected internal RailWorld World { get; internal set; }
    public RailState State { get; private set; }

    /// <summary>
    /// The last tick this entity was updated on.
    /// </summary>
    protected Tick Tick { get; private set; }

    /// <summary>
    /// SERVER: Called when the entity has a new controller. 
    /// CLIENT: Called when local control is granted or revoked.
    /// 
    /// Always called once right after OnStart().
    /// </summary>
    protected virtual void OnControllerChanged() { }

    /// <summary>
    /// SERVER: Called before the first update tick.
    /// CLIENT: Called before the first update tick.
    /// </summary>
    protected virtual void Start() { }

    /// <summary>
    /// SERVER: Called every tick.
    /// CLIENT: Called during prediction (multiple times per frame).
    /// 
    /// Includes the most recent command available from the controller.
    /// </summary>
    internal virtual void SimulateCommand(RailCommand command) { }

    /// <summary>
    /// SERVER: Called every update tick.
    /// CLIENT: Called during prediction (multiple times per frame).
    /// 
    /// Called after SimulateCommand().
    /// </summary>
    protected virtual void Simulate() { }

    private bool hadFirstTick;

    public bool IsPredicted
    {       
      get 
      { 
        return 
          (this.Controller != null) && 
          (RailConnection.IsServer == false);
      }
    }

    internal RailEntity()
    {
      this.controller = null;

      this.Id = EntityId.INVALID;

      this.StateBuffer = new RailStateBuffer();
      this.StateDelta = new RailStateDelta();

      this.World = null;
      this.State = null;

      this.hadFirstTick = false;
    }

    internal void Initialize(int type)
    {
      this.Type = type;
      this.State = RailResource.Instance.AllocateState(type);
    }

    internal void AssignId(EntityId id)
    {
      CommonDebug.Assert(this.Id.IsValid == false);
      CommonDebug.Assert(id.IsValid);

      this.Id = id;
      this.State.SetOnEntityCreate(this.Id);
    }

    internal void SetController(IRailControllerInternal controller)
    {
      this.controller = controller;
      this.ControllerChanged();
    }

    internal void UpdateServer(
      Tick serverTick)
    {
      if (this.World != null)
      {
        this.Tick = serverTick;

        if (this.hadFirstTick == false)
        {
          this.hadFirstTick = true;
          this.Start();
          this.ControllerChanged();
        }

        if (this.controller != null)
          if (this.controller.LatestCommand != null)
            this.SimulateCommand(this.controller.LatestCommand);

        this.Simulate();
      }
    }

    internal void UpdateClient(
      Tick serverTick)
    {
      if (this.World != null)
      {
        this.Tick = serverTick;

        if (this.Controller == null)
          this.ReplicaSimulate(serverTick);
        else
          this.ForwardSimulate();
      }
    }

    internal void ReplicaSimulate(Tick serverTick)
    {
      this.ClearDelta();

      this.StateDelta.Update(this.StateBuffer, serverTick);
      if (this.StateDelta.Latest != null)
      {
        this.State.SetDataFrom(this.StateDelta.Latest);

        if (this.hadFirstTick == false)
        {
          this.hadFirstTick = true;
          this.Start();
          this.ControllerChanged();
        }
      }
    }

    internal bool HasLatest(Tick serverTick)
    {
      return (this.StateBuffer.GetLatestAt(serverTick) != null);
    }

    internal void ControllerChanged()
    {
      if ((this.World != null) && this.hadFirstTick)
        this.OnControllerChanged();
    }

    internal void StoreState(Tick tick)
    {
      RailState state = this.State.Clone();
      state.SetOnStore(tick);
      this.StateBuffer.Store(state);
    }

    #region Encoding/Decoding
    internal void EncodeState(
      BitBuffer buffer, 
      IRailController destination,
      Tick latestTick, 
      Tick basisTick)
    {
      CommonDebug.Assert(destination != null);
      CommonDebug.Assert(latestTick.IsValid);

      RailState basis = null;
      TickSpan span = TickSpan.OUT_OF_RANGE;
      bool isController = (destination == this.Controller);
      bool isFirst = false;

      if (basisTick.IsValid)
      {
        basis = this.StateBuffer.Get(basisTick);
        if (basis != null)
          span = TickSpan.Create(latestTick, basisTick);
      }
      else
      {
        isFirst = true;
      }

      // Write: [TickSpan]
      buffer.Write(RailEncoders.TickSpan, span);

      // Write: [State]
      this.State.Encode(buffer, basis, isController, isFirst);
    }

    /// <summary>
    /// Decodes the latest state for a RailEntity and returns it. 
    /// May return null if we received bad data and need to discard the state.
    /// 
    /// Throws a BasisNotFoundException if decoding is impossible.
    /// </summary>
    internal static RailState DecodeState(
      BitBuffer buffer,
      IDictionary<EntityId, RailEntity> knownEntities,
      Tick latestTick)
    {
      // Read: [TickSpan]
      TickSpan span = buffer.Read(RailEncoders.TickSpan);
      CommonDebug.Assert(span.IsValid);

      // Peek: [State.Id]
      EntityId id = RailState.PeekId(buffer);

      // There are three different situations that may occur with respect to
      // the basis being null or not null and the value of isDelta:
      //
      // #  Description                         Basis       IsDelta
      // 1  First received state                null        false
      // 2  Full decode                         latest      false
      // 3  Delta decode                        real basis  true
      // 4  Bad/failed delta decode recovery    latest      true
      //
      // Case 4 can happen if something went wrong. In this case we try to
      // recover by still decoding, but trash the result and move on. The 
      // reason for this split between basis and isDelta is so we can copy
      // over immutable values from previous states even if full decoding.

      RailEntity entity = null;
      RailState basis = null;
      bool isDelta = true;
      bool canStore = true;

      if (knownEntities.TryGetValue(id, out entity) == false)
        entity = null;

      if (span.IsInRange)
      {
        basis = RailEntity.GetBasis(entity, latestTick, span, out canStore);
      }
      else
      {
        // If there is no basis, try to substitute the latest state to get the
        // immutable values since they will never change after initialization
        if (entity != null)
          basis = entity.StateBuffer.Latest;
        isDelta = false;
      }

      // Read: [State]
      RailState state = RailState.Decode(buffer, basis, latestTick, isDelta);

      if (canStore)
        return state;
      return null;
    }

    private static RailState GetBasis(
      RailEntity entity,
      Tick latestTick,
      TickSpan span,
      out bool isValid)
    {
      if (entity == null)
        throw new BasisNotFoundException("No entity found");

      isValid = true;
      Tick basisTick = Tick.Create(latestTick, span);
      RailState basis = entity.StateBuffer.Get(basisTick);

      if (basis == null)
      {
        basis = entity.StateBuffer.Latest;
        if (basis == null)
        {
          throw new BasisNotFoundException(
            "No basis or latest for id " +
            entity.Id +
            " on tick " +
            basisTick);
        }

        CommonDebug.LogWarning(
          "Missing basis, using latest and discarding for id " +
          entity.Id +
          " on tick " +
          basisTick);
        isValid = false;
      }

      return basis;
    }
    #endregion

    #region Smoothing
    public T GetSmoothedValue<T>(
      float frameDelta, 
      RailSmoother<T> smoother)
    {
      if ((this.StateDelta == null) || (this.StateDelta.Latest == null))
        return default(T);

      // If we're predicting, advance to the prediction tick. This is
      // hacky in that it assumes that we'll only ever have a one-tick 
      // difference between any two states in the delta when doing prediction.
      Tick currentTick = this.World.Tick;
      if (this.StateDelta.Latest.IsPredicted)
        currentTick = this.StateDelta.Latest.Tick;

      if (this.StateDelta.Next != null)
      {
        return smoother.Smooth(
          frameDelta,
          currentTick,
          this.StateDelta.Latest,
          this.StateDelta.Next);
      }
      else if (this.StateDelta.Prior != null)
      {
        return smoother.Smooth(
          frameDelta,
          currentTick,
          this.StateDelta.Prior,
          this.StateDelta.Latest);
      }
      else
      {
        return smoother.Access(this.StateDelta.Latest);
      }
    }
    #endregion

    #region Prediction
    private void ForwardSimulate()
    {
      if (this.StateDelta == null)
        return;

      this.ClearDelta();

      RailState latest = this.StateBuffer.Latest.Clone();
      latest.SetOnPredict(this.World.Tick);

      this.StateDelta.Set(null, latest, null);
      this.State.SetDataFrom(latest);

      if (this.hadFirstTick == false)
      {
        this.hadFirstTick = true;
        this.Start();
        this.ControllerChanged();
      }

      this.ApplyCommands();
    }

    private void ClearDelta()
    {
      RailState prior = this.StateDelta.Prior;
      RailState latest = this.StateDelta.Latest;
      RailState next = this.StateDelta.Next;

      if ((prior != null) && prior.IsPredicted)
        RailPool.Free(prior);
      if ((latest != null) && latest.IsPredicted)
        RailPool.Free(latest);
      if ((next != null) && next.IsPredicted)
        RailPool.Free(next);

      this.StateDelta.Clear();
    }

    private void ApplyCommands()
    {
      int offset = 1;
      foreach (RailCommand command in this.controller.PendingCommands)
      {
        this.SimulateCommand(command);
        this.Simulate();
        this.PushDelta(offset);

        offset++;
      }
    }

    private void PushDelta(int offset)
    {
      RailState predicted = this.State.Clone();
      predicted.SetOnPredict(this.World.Tick + offset);

      RailState popped = this.StateDelta.Push(predicted);
      if ((popped != null) && popped.IsPredicted)
        RailPool.Free(popped);
    }
    #endregion

    #region DEBUG
    public virtual string DEBUG_FormatDebug() 
    {
      string output = "[";
      foreach (RailState state in this.StateBuffer.Values)
        output += state.Tick + ":" + state.DEBUG_FormatDebug() + ",";
      output = output.Remove(output.Length - 1, 1) + "] (";

      if (this.StateDelta != null)
      {
        if (this.StateDelta.Prior != null)
          output += this.StateDelta.Prior.Tick;
        output += ",";
        if (this.StateDelta.Latest != null)
          output += this.StateDelta.Latest.Tick;
        output += ",";
        if (this.StateDelta.Next != null)
          output += this.StateDelta.Next.Tick;
        output += ")";
      }

      return output;
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
