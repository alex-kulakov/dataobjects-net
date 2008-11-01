// Copyright (C) 2007 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2007.08.01

using System;
using System.Diagnostics;
using Xtensive.Core;
using Xtensive.Core.Aspects;
using Xtensive.Core.Diagnostics;
using Xtensive.Core.Internals.DocTemplates;
using Xtensive.Core.Tuples;
using Xtensive.Integrity.Validation;
using Xtensive.Storage.Attributes;
using Xtensive.Storage.Internals;
using Xtensive.Storage.Model;
using Xtensive.Storage.ReferentialIntegrity;
using Xtensive.Storage.Resources;

namespace Xtensive.Storage
{
  /// <summary>
  /// Principal data objects about which information has to be managed. 
  /// It has a unique identity, independent existence, and forms the operational unit of consistency.
  /// Instance of <see cref="Entity"/> type can be referenced via <see cref="Key"/>.
  /// </summary>
  public abstract class Entity : Persistent,
    IEntity
  {
    private EntityState entityState;

    #region Internal properties

    [Infrastructure]
    internal EntityState EntityState
    {
      [DebuggerStepThrough]
      get { return entityState; }
      set { entityState = value; }
    }

    /// <exception cref="Exception">Property is already initialized.</exception>
    [Field]
    internal int TypeId
    {
      [DebuggerStepThrough]
      get { return GetField<int>(Session.Domain.NameBuilder.TypeIdFieldName); }
    }

    #endregion

    #region Properties: Key, Type, Data, PersistenceState

    /// <exception cref="Exception">Property is already initialized.</exception>
    [Infrastructure]
    public Key Key
    {
      [DebuggerStepThrough]
      get { return EntityState.Key; }
    }

    /// <summary>
    /// Gets a value indicating whether this entity is removed.
    /// </summary>
    [Infrastructure]
    public bool IsRemoved
    {
      get
      {
        EntityState.EnsureIsActual();
        return EntityState.IsRemoved;
      }
    }

    /// <inheritdoc/>
    public override sealed TypeInfo Type
    {
      [DebuggerStepThrough]
      get { return EntityState.Type; }
    }

    /// <inheritdoc/>
    protected internal override sealed Tuple Data
    {
      [DebuggerStepThrough]
      get { return EntityState; }
    }

    /// <summary>
    /// Gets persistence state of the entity.
    /// </summary>
    [Infrastructure]
    public PersistenceState PersistenceState
    {
      [DebuggerStepThrough]
      get { return EntityState.PersistenceState; }
    }

    #endregion

    #region IIdentifier members

    /// <inheritdoc/>
    [Infrastructure]
    Key IIdentified<Key>.Identifier
    {
      [DebuggerStepThrough]
      get { return Key; }
    }

    /// <inheritdoc/>
    [Infrastructure]
    object IIdentified.Identifier
    {
      [DebuggerStepThrough]
      get { return Key; }
    }

    #endregion

    #region Remove method

    /// <inheritdoc/>
    [Infrastructure]
    public void Remove()
    {
      var session = Session;
      if (session.IsDebugEventLoggingEnabled)
        LogTemplate<Log>.Debug("Session '{0}'. Removing: Key = '{1}'", session, Key);

      entityState.EnsureIsActual();
      entityState.EnsureIsNotRemoved();
      OnRemoving();

      session.Persist();
      ReferenceManager.ClearReferencesTo(this);
      session.removedEntities.Add(EntityState);
      session.Cache.Remove(EntityState);
      EntityState.PersistenceState = PersistenceState.Removed;

      OnRemoved();
    }

    #endregion

    #region Protected event-like methods

    /// <inheritdoc/>
    protected internal override bool SkipValidation
    {
      get { return IsRemoved; }
    }

    /// <summary>
    /// Called when entity is to be removed.
    /// </summary>
    [Infrastructure]
    protected virtual void OnRemoving()
    {
    }

    /// <summary>
    /// Called when become removed.
    /// </summary>
    [Infrastructure]
    protected virtual void OnRemoved()
    {
    }

    #endregion

    #region Private \ internal methods

    internal override sealed void EnsureIsFetched(FieldInfo field)
    {
      if (!EntityState.IsFetched(field.MappingInfo.Offset))
        Fetcher.Fetch(Key, field);
    }

    #endregion

    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    protected Entity()
    {
      Session.GetAccessor(this).Initialize(this);
    }

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="tuple">The <see cref="Data"/> that will be used for key building.</param>
    /// <remarks>Use this kind of constructor when you need to explicitly set key for this instance.</remarks>
    protected Entity(Tuple tuple)
    {
      Session.GetAccessor(this).Initialize(this, tuple);
    }

    /// <summary>
    /// <see cref="ClassDocTemplate()" copy="true"/>
    /// </summary>
    /// <param name="state">The initial data of this instance fetched from storage.</param>
    protected Entity(EntityState state)
    {
      entityState = state;

      if (Session.IsDebugEventLoggingEnabled)
        LogTemplate<Log>.Debug("Session '{0}'. Initializing entity: Key = '{1}'", Session, Key);
    }
  }
}