// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Nikolaev
// Created:    2009.09.17

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core;
using Xtensive.Core.Tuples;
using Xtensive.Storage.Model;

namespace Xtensive.Storage.Internals.Prefetch
{
  [Serializable]
  internal sealed class GraphContainer
  {
    private Dictionary<FieldInfo, ReferencedEntityContainer> referencedEntityContainers;
    private Dictionary<FieldInfo, EntitySetTask> entitySetTasks;
    private readonly bool exactType;
    private int? cachedHashCode;
    private bool isReferenceContainerCreated;

    internal readonly PrefetchProcessor processor;

    public readonly Key Key;
    
    public readonly TypeInfo Type;

    public RootEntityContainer RootEntityContainer { get; private set; }

    public bool ContainsTask {
      get {
        return RootEntityContainer!=null
          || (referencedEntityContainers!=null && referencedEntityContainers.Count > 0)
            || (entitySetTasks!=null && entitySetTasks.Count > 0);
      }
    }

    public IEnumerable<ReferencedEntityContainer> ReferencedEntityContainers
    {
      get { return referencedEntityContainers!=null ? referencedEntityContainers.Values : null; }
    }

    public IEnumerable<EntitySetTask> EntitySetTasks
    {
      get { return entitySetTasks!=null ? entitySetTasks.Values : null; }
    }

    public void AddEntityColumns(IEnumerable<ColumnInfo> columns)
    {
      if (RootEntityContainer == null)
        RootEntityContainer = new RootEntityContainer(Key, Type, exactType, processor);
      RootEntityContainer.AddColumns(columns);
    }

    public void RegisterReferencedEntityContainer(Tuple ownerEntityTuple,
      PrefetchFieldDescriptor referencingFieldDescriptor)
    {
      if (referencedEntityContainers != null
        && referencedEntityContainers.ContainsKey(referencingFieldDescriptor.Field))
        return;
      var notLoadedForeignKeyColumns = GetNotLoadedFieldColumns(ownerEntityTuple,
        referencingFieldDescriptor.Field);
      var areAllForeignKeyColumnsLoaded = notLoadedForeignKeyColumns.Count()==0;
      if (!areAllForeignKeyColumnsLoaded)
        RegisterFetchByUnknownForeignKey(referencingFieldDescriptor, notLoadedForeignKeyColumns);
      else
        RegisterFetchByKnownForeignKey(referencingFieldDescriptor, ownerEntityTuple);
    }

    public void RegisterEntitySetTask(PrefetchFieldDescriptor referencingFieldDescriptor)
    {
      if (entitySetTasks==null)
        entitySetTasks = new Dictionary<FieldInfo, EntitySetTask>();
      if (RootEntityContainer==null)
        AddEntityColumns(Key.TypeRef.Type.Fields
          .Where(field => field.IsPrimaryKey || field.IsSystem).SelectMany(field => field.Columns));
      entitySetTasks[referencingFieldDescriptor.Field] =
        new EntitySetTask(Key, referencingFieldDescriptor, processor);
    }
    
    public void NotifyAboutExtractionOfKeysWithUnknownType()
    {
      if (RootEntityContainer != null)
        RootEntityContainer.NotifyOwnerAboutKeyWithUnknownType();
      if (referencedEntityContainers == null)
        return;
      foreach (var pair in referencedEntityContainers)
        pair.Value.NotifyOwnerAboutKeyWithUnknownType();
    }
    
    public bool Equals(GraphContainer other)
    {
      if (ReferenceEquals(null, other))
        return false;
      if (ReferenceEquals(this, other))
        return true;
      if (!Type.Equals(other.Type))
        return false;
      return Key.Equals(other.Key);
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj))
        return false;
      if (ReferenceEquals(this, obj))
        return true;
      var otherType = obj.GetType();
      if (otherType != (typeof (GraphContainer)))
        return false;
      return Equals((GraphContainer) obj);
    }

    public override int GetHashCode()
    {
      if (cachedHashCode==null)
        unchecked {
          cachedHashCode = (Key.GetHashCode() * 397) ^ Type.GetHashCode();
        }
      return cachedHashCode.Value;
    }

    #region Private \ internal methods

    private IEnumerable<ColumnInfo> GetNotLoadedFieldColumns(Tuple tuple, FieldInfo field)
    {
      return field.Columns.Where(column => !IsColumnLoaded(tuple, column));
    }

    private bool IsColumnLoaded(Tuple tuple, ColumnInfo column)
    {
      var columnIndex = Type.Indexes.PrimaryIndex.Columns.IndexOf(column);
      return tuple!=null
        && tuple.GetFieldState(columnIndex).IsAvailable();
    }

    private void RegisterFetchByKnownForeignKey(PrefetchFieldDescriptor referencingFieldDescriptor,
      Tuple ownerEntityTuple)
    {
      var referencedKeyTuple = referencingFieldDescriptor.Field.Association
        .ExtractForeignKey(ownerEntityTuple);
      var referencedKeyTupleState = referencedKeyTuple.GetFieldStateMap(TupleFieldState.Null);
      for (var i = 0; i < referencedKeyTupleState.Length; i++)
        if (referencedKeyTupleState[i])
          return;
      var referencedKey = Key.Create(processor.Owner.Session.Domain,
        referencingFieldDescriptor.Field.Association.TargetType, TypeReferenceAccuracy.BaseType,
        referencedKeyTuple);
      var targetType = referencingFieldDescriptor.Field.Association.TargetType;
      var needToNotifyOwner = false;
      if (!referencedKey.TypeRef.Type.IsLeaf) {
        needToNotifyOwner = true;
        var cachedKey = referencedKey;
        Tuple entityTuple;
        if (!processor.TryGetTupleOfNonRemovedEntity(ref cachedKey, out entityTuple))
          return;
        if (cachedKey.HasExactType
          && referencingFieldDescriptor.Field.Association.TargetType!=cachedKey.Type) {
          targetType = cachedKey.Type;
          needToNotifyOwner = false;
        }
      }
      var fieldsToBeLoaded = PrefetchHelper.CreateDescriptorsForFieldsLoadedByDefault(targetType);
      var graphContainer = processor.SetUpContainers(referencedKey, targetType, fieldsToBeLoaded,
        true, null);
      if (needToNotifyOwner)
        graphContainer.RootEntityContainer.SetParametersOfReference(referencingFieldDescriptor, referencedKey);
    }

    private void RegisterFetchByUnknownForeignKey(PrefetchFieldDescriptor referencingFieldDescriptor,
      IEnumerable<ColumnInfo> notLoadedForeignKeyColumns)
    {
      if (referencedEntityContainers == null)
        referencedEntityContainers = new Dictionary<FieldInfo, ReferencedEntityContainer>();
      referencedEntityContainers.Add(referencingFieldDescriptor.Field, new ReferencedEntityContainer(Key,
        referencingFieldDescriptor, exactType, processor));
      AddEntityColumns(notLoadedForeignKeyColumns);
    }

    #endregion


    // Constructors

    public GraphContainer(Key key, TypeInfo type, bool exactType, PrefetchProcessor processor)
    {
      ArgumentValidator.EnsureArgumentNotNull(key, "key");
      ArgumentValidator.EnsureArgumentNotNull(type, "type");
      ArgumentValidator.EnsureArgumentNotNull(processor, "processor");
      Key = key;
      Type = type;
      this.processor = processor;
      this.exactType = exactType;
    }
  }
}