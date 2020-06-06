// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.07.07

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xtensive.Core;
using Xtensive.Orm.Providers;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Rse.Providers
{
  /// <summary>
  /// Abstract base class for any query provider that can be directly executed.
  /// </summary>
  [Serializable]
  public abstract class ExecutableProvider : Provider
  {
    /// <summary>
    /// Gets the provider this provider is compiled from.
    /// </summary>
    public CompilableProvider Origin { get; private set; }

    /// <exception cref="InvalidOperationException"><see cref="Origin"/> is <see langword="null" />.</exception>
    protected override RecordSetHeader BuildHeader() => Origin.Header;

    #region OnXxxEnumerate methods (to override)

    /// <summary>
    /// Called when enumerator is created on this provider.
    /// </summary>
    /// <param name="context">The enumeration context.</param>
    protected virtual void OnBeforeEnumerate(EnumerationContext context)
    {
      foreach (var source in Sources) {
        if (source is ExecutableProvider ep) {
          ep.OnBeforeEnumerate(context);
        }
      }
    }

    /// <summary>
    /// Called when enumeration is finished.
    /// </summary>
    /// <param name="context">The enumeration context.</param>
    protected virtual void OnAfterEnumerate(EnumerationContext context)
    {
      foreach (var source in Sources) {
        if (source is ExecutableProvider ep) {
          ep.OnAfterEnumerate(context);
        }
      }
    }

    protected abstract TupleEnumerator OnEnumerate(EnumerationContext context);

    protected virtual Task<TupleEnumerator> OnEnumerateAsync(EnumerationContext context, CancellationToken token)
    {
      //Default version is synchronous
      token.ThrowIfCancellationRequested();
      return Task.FromResult(OnEnumerate(context));
    }

    #endregion

    #region Caching related methods

    protected T GetValue<T>(EnumerationContext context, string name)
      where T : class =>
      context.GetValue<T>(this, name);

    protected void SetValue<T>(EnumerationContext context, string name, T value)
      where T : class =>
      context.SetValue(this, name, value);

    #endregion

    /// <summary>
    /// Gets <see cref="RecordSet"/> bound to the specified <paramref name="provider"/>.
    /// </summary>
    /// <param name="session">Session to bind.</param>
    /// <param name="parameterContext"><see cref="ParameterContext"/> instance with
    /// the values of query parameters.</param>
    /// <returns>New <see cref="RecordSet"/> bound to specified <paramref name="session"/>.</returns>
    public RecordSet GetRecordSet(Session session, ParameterContext parameterContext)
    {
      ArgumentValidator.EnsureArgumentNotNull(session, nameof(session));
      var enumerationContext = session.CreateEnumerationContext(parameterContext);
      return RecordSet.Create(enumerationContext, this);
    }

    /// <summary>
    /// Asynchronously gets <see cref="RecordSet"/> bound to the specified <paramref name="provider"/>.
    /// </summary>
    /// <param name="session">Session to bind.</param>
    /// <param name="parameterContext"><see cref="ParameterContext"/> instance with
    /// the values of query parameters.</param>
    /// <param name="token">Token to cancel operation.</param>
    /// <returns>Task performing this operation.</returns>
    public async Task<RecordSet> GetRecordSetAsync(
      Session session, ParameterContext parameterContext, CancellationToken token)
    {
      ArgumentValidator.EnsureArgumentNotNull(session, nameof(session));
      var enumerationContext =
        await session.CreateEnumerationContextAsync(parameterContext, token).ConfigureAwait(false);
      return await RecordSet.CreateAsync(enumerationContext, this, token);
    }

    public class RecordSet: IEnumerator<Tuple>, IAsyncEnumerator<Tuple>
    {
      private const string enumerationMarker = "Enumerated";
      private enum State
      {
        New,
        Prepared,
        Finished
      }

      private readonly EnumerationContext context;
      private readonly ExecutableProvider provider;
      private readonly CancellationToken token;
      private readonly bool isGreedy;

      private State state = State.New;
      private bool enumerated;
      private TupleEnumerator tupleEnumerator;
      private ICompletableScope enumerationScope;
      private bool hasNextTuple;

      public Tuple Current { get; private set; }

      object IEnumerator.Current => Current;

      void IEnumerator.Reset() => throw new NotSupportedException();

      public bool MoveNext()
      {
        switch (state) {
          case State.New:
            throw new InvalidOperationException("RecordSet is not prepared.");
          case State.Prepared:
            try {
              if (hasNextTuple) {
                Current = tupleEnumerator.Current;
                hasNextTuple = tupleEnumerator.MoveNext();
                return true;
              }
            }
            catch {
              FinishEnumeration(true);

              throw;
            }

            FinishEnumeration(false);

            state = State.Finished;
            goto case State.Finished;
          case State.Finished:
          default:
            return false;
        }
      }

      public async ValueTask<bool> MoveNextAsync()
      {
        switch (state) {
          case State.New:
            throw new InvalidOperationException("RecordSet is not prepared.");
          case State.Prepared:
            try {
              if (hasNextTuple) {
                Current = tupleEnumerator.Current;
                hasNextTuple = await tupleEnumerator.MoveNextAsync();
                return true;
              }
            }
            catch {
              FinishEnumeration(true);

              throw;
            }

            FinishEnumeration(false);
            state = State.Finished;
            goto case State.Finished;
          case State.Finished:
          default:
            return false;
        }
      }

      private async ValueTask Prepare(bool executeAsync)
      {
        enumerationScope = context.BeginEnumeration();
        enumerated = context.GetValue<bool>(provider, enumerationMarker);
        if (!enumerated) {
          provider.OnBeforeEnumerate(context);
          context.SetValue(provider, enumerationMarker, true);
        }

        try {
          tupleEnumerator = executeAsync
            ? await provider.OnEnumerateAsync(context, token)
            : provider.OnEnumerate(context);

          if (isGreedy && !tupleEnumerator.IsInMemory) {
            var tuples = new List<Tuple>();
            if (executeAsync) {
              await using (tupleEnumerator) {
                while (await tupleEnumerator.MoveNextAsync()) {
                  tuples.Add(tupleEnumerator.Current);
                }
              }
            }
            else {
              using (tupleEnumerator) {
                while (tupleEnumerator.MoveNext()) {
                  tuples.Add(tupleEnumerator.Current);
                }
              }
            }
            tupleEnumerator = new TupleEnumerator(tuples);
          }

          hasNextTuple = executeAsync ? await tupleEnumerator.MoveNextAsync() : tupleEnumerator.MoveNext();
        }
        catch {
          FinishEnumeration(true);
          throw;
        }
        state = State.Prepared;
      }

      private void FinishEnumeration(bool isError)
      {
        if (!enumerated) {
          provider.OnAfterEnumerate(context);
        }

        if (!isError) {
          enumerationScope?.Complete();
        }
      }

      public void Dispose()
      {
        if (state != State.New) {
          tupleEnumerator.Dispose();
        }
        enumerationScope?.Dispose();
      }

      public async ValueTask DisposeAsync()
      {
        if (state != State.New) {
          await tupleEnumerator.DisposeAsync();
        }
        enumerationScope?.Dispose();
      }

      private RecordSet(EnumerationContext context, ExecutableProvider provider, CancellationToken token = default)
      {
        this.context = context;
        this.provider = provider;
        this.token = token;
        isGreedy = context.CheckOptions(EnumerationContextOptions.GreedyEnumerator);
      }

      public static RecordSet Create(EnumerationContext context, ExecutableProvider provider)
      {
        var recordSet = new RecordSet(context, provider);
        _ = recordSet.Prepare(false);
        return recordSet;
      }

      public static async ValueTask<RecordSet> CreateAsync(
        EnumerationContext context, ExecutableProvider provider, CancellationToken token)
      {
        var recordSet = new RecordSet(context, provider, token);
        await recordSet.Prepare(true);
        return recordSet;
      }
    }

    // Constructors

    /// <summary>
    ///   Initializes a new instance of this class.
    /// </summary>
    /// <param name="origin">The <see cref="Origin"/> property value.</param>
    /// <param name="sources">The <see cref="Provider.Sources"/> property value.</param>
    protected ExecutableProvider(CompilableProvider origin, params ExecutableProvider[] sources)
      : base(origin.Type, sources)
    {
      Origin = origin;
    }
  }
}
