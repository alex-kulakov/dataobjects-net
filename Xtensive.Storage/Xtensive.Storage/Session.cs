// Copyright (C) 2007 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2007.08.10

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xtensive.Core;
using Xtensive.Core.Aspects;
using Xtensive.Core.Collections;
using Xtensive.Core.Helpers;
using Xtensive.Core.Internals.DocTemplates;
using Xtensive.Core.Tuples;
using Xtensive.Storage.Aspects;
using Xtensive.Storage.Configuration;
using Xtensive.Storage.Internals;
using Xtensive.Storage.Model;
using Xtensive.Storage.Providers;
using Xtensive.Storage.Rse;

namespace Xtensive.Storage
{
  [Storage]
  public class Session : DomainBound,
    IContext<SessionScope>,
    IResource
  {
    private volatile bool isDisposed = false;
    private readonly Set<object> consumers = new Set<object>();
    private object _lock = new object();

    #region Private \ internal properties

    [DebuggerHidden]
    internal HandlerAccessor HandlerAccessor { get; private set; }

    [DebuggerHidden]
    internal SessionHandler Handler { get; private set; }

    [DebuggerHidden]
    internal EntityDataCache DataCache { get; private set; }

    [DebuggerHidden]
    internal FlagRegistry<PersistenceState, EntityData> DirtyData { get; private set; }

    #endregion

    /// <summary>
    /// Gets the configuration of the <see cref="Session"/>.
    /// </summary>
    [DebuggerHidden]
    public SessionConfiguration Configuration { get; private set; }

    /// <summary>
    /// Persists all modified instances immediately.
    /// </summary>
    /// <remarks>
    /// This method should be called to ensure that all delayed
    /// updates are flushed to the storage. Note, that this method 
    /// is called automatically when it's necessary - e.g.
    /// before beginning\committing\rolling back a transaction,
    /// establishing a save point or rolling back to it, performing a
    /// query and so further. So generally you should not worry
    /// about calling this method.
    /// </remarks>
    public void Persist()
    {
      if (DirtyData.GetCount()==0)
        return;

      Handler.Persist(DirtyData);

      HashSet<EntityData> @new = DirtyData.GetItems(PersistenceState.New);
      HashSet<EntityData> modified = DirtyData.GetItems(PersistenceState.Modified);
      HashSet<EntityData> removed = DirtyData.GetItems(PersistenceState.Removed);

      foreach (EntityData data in @new.Union(modified).Except(removed))
        data.PersistenceState = PersistenceState.Persisted;

      DirtyData.Clear();
    }

    public RecordSet Select(IndexInfo index)
    {
      Persist();
      return Handler.Select(index);
    }

    public IEnumerable<T> All<T>() where T : Entity
    {
      Persist();
      TypeInfo type = Domain.Model.Types[typeof (T)];
      RecordSet result = Handler.Select(type.Indexes.PrimaryIndex);
      foreach (Tuple tuple in result) {
        Key key = Domain.KeyManager.Get(type.Hierarchy, tuple);
        DataCache.Update(key, tuple);
        yield return (T)key.Resolve();
      }
    }

    #region IResource members

    /// <inheritdoc/>
    void IResource.AddConsumer(object consumer)
    {
      consumers.Add(consumer);
    }

    /// <inheritdoc/>
    void IResource.RemoveConsumer(object consumer)
    {
      consumers.Remove(consumer);
      if (!(this as IResource).HasConsumers)
        Dispose();
    }

    /// <inheritdoc/>
    bool IResource.HasConsumers
    {
      get { return consumers.Count > 0; }
    }

    #endregion

    #region IContext<SessionScope> Members

    /// <summary>
    /// Gets the current active <see cref="Session"/> instance.
    /// </summary>
    [DebuggerHidden]
    public static Session Current
    {
      get { return SessionScope.Current==null ? null : SessionScope.Current.Session; }
    }

    /// <inheritdoc/>
    public SessionScope Activate()
    {
      if (IsActive)
        return null;
      return new SessionScope(this);
    }

    /// <inheritdoc/>
    IDisposable IContext.Activate()
    {
      return Activate();
    }

    /// <inheritdoc/>
    public bool IsActive
    {
      get { return SessionScope.Current.Session==this; }
    }

    #endregion


    // Constructors

    internal Session(Domain domain, SessionConfiguration configuration)
      : base(domain)
    {
      // Both Domain and Configuration are valid references here;
      // Configuration is already locked
      Configuration = configuration;
      HandlerAccessor = domain.HandlerAccessor;
      Handler = HandlerAccessor.HandlerFactory.CreateHandler<SessionHandler>();
      Handler.Session = this;
      DataCache = new EntityDataCache(Configuration.CacheSize);
    }

    #region Dispose pattern

    /// <see cref="DisposableDocTemplate.Dispose()" copy="true"/>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <see cref="DisposableDocTemplate.Dtor()" copy="true"/>
    ~Session()
    {
      Dispose(false);
    }

    /// <see cref="DisposableDocTemplate.Dispose(bool)" copy="true"/>
    protected virtual void Dispose(bool disposing)
    {
      if (isDisposed)
        return;
      lock (_lock) {
        if (isDisposed)
          return;
        try {
          Persist();
          Handler.Commit();
        }
        finally {
          isDisposed = true;
        }
      }
    }

    #endregion
  }
}
