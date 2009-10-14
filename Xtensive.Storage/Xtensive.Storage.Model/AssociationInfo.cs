// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.07.02

using System;
using System.Linq;
using Xtensive.Core;
using Xtensive.Core.Helpers;
using Xtensive.Core.Internals.DocTemplates;
using Xtensive.Core.Tuples;
using Xtensive.Core.Tuples.Transform;
using Xtensive.Storage.Model.Resources;

namespace Xtensive.Storage.Model
{
  /// <summary>
  /// Describes single association.
  /// </summary>
  [Serializable]
  public class AssociationInfo : Node
  {
    private Multiplicity                multiplicity;
    private AssociationInfo             reversed;
    private TypeInfo                    auxiliaryType;
    private bool                        isMaster = true;
    private OnRemoveAction?             onTargetRemove;
    private OnRemoveAction?             onOwnerRemove;
    private SegmentTransform            foreignKeyExtractorTransform;

    /// <summary>
    /// Gets the owner type.
    /// </summary>
    public TypeInfo OwnerType
    {
      get { return OwnerField.DeclaringType; }
    }

    /// <summary>
    /// Gets the owner field.
    /// </summary>
    public FieldInfo OwnerField { get; private set; }

    /// <summary>
    /// Gets the target type.
    /// </summary>
    public TypeInfo TargetType { get; private set; }

    /// <summary>
    /// Gets the auxiliary persistent type that represents this association.
    /// </summary>
    public TypeInfo AuxiliaryType
    {
      get { return auxiliaryType; }
      set
      {
        this.EnsureNotLocked();
        auxiliaryType = value;
      }
    }

    /// <summary>
    /// Gets the underlying index for this instance.
    /// </summary>
    /// <value>The underlying index.</value>
    public IndexInfo UnderlyingIndex { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this instance is master association.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this instance is master association; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsMaster
    {
      get { return isMaster; }
      set
      {
        this.EnsureNotLocked();
        isMaster = value;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is paired.
    /// </summary>
    public bool IsPaired
    {
      get { return reversed!=null; }
    }

    /// <summary>
    /// Gets a value indicating whether this instance represents a loop.
    /// </summary>
    public bool IsLoop
    {
      get { return IsPaired && Reversed == this; }
    }

    /// <summary>
    /// Gets master association.
    /// </summary>
    /// <remarks>
    /// If association is master, returns it. Otherwise returns paired association.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Unable to find master association.</exception>
    public AssociationInfo Master
    {
      get
      {
        if (isMaster) 
          return this;
        if (reversed==null || !reversed.isMaster) 
          throw new InvalidOperationException(String.Format(Strings.ExUnableToFindMasterAssociation, Name));
        return reversed;
      }
    }

    /// <summary>
    /// Gets the association multiplicity.
    /// </summary>
    public Multiplicity Multiplicity
    {
      get { return multiplicity; }
      set
      {
        this.EnsureNotLocked();
        multiplicity = value;
      }
    }

    /// <summary>
    /// Gets or sets the reversed paired <see cref="AssociationInfo"/> for this instance.
    /// </summary>
    public AssociationInfo Reversed
    {
      get { return reversed; }
      set
      {
        this.EnsureNotLocked();
        reversed = value;
      }
    }

    /// <summary>
    /// Gets the <see cref="OnRemoveAction"/> that will be applied on <see cref="TargetType"/> object removal.
    /// </summary>
    public OnRemoveAction? OnTargetRemove
    {
      get { return onTargetRemove; }
      set
      {
        this.EnsureNotLocked();
        onTargetRemove = value;
      }
    }

    /// <summary>
    /// Gets the <see cref="OnRemoveAction"/> that will be applied on <see cref="OwnerType"/> object removal.
    /// </summary>
    public OnRemoveAction? OnOwnerRemove
    {
      get { return onOwnerRemove; }
      set
      {
        this.EnsureNotLocked();
        onOwnerRemove = value;
      }
    }

    /// <summary>
    /// Extracts the foreign key from the specified <see cref="Tuple"/>.
    /// </summary>
    /// <param name="tuple">The tuple.</param>
    /// <returns><see cref="Tuple"/> instance with the extracted foreign key.</returns>
    public Tuple ExtractForeignKey(Tuple tuple)
    {
      return foreignKeyExtractorTransform.Apply(TupleTransformType.TransformedTuple, tuple);
    }

    /// <inheritdoc/>
    public override void UpdateState(bool recursive)
    {
      base.UpdateState(recursive);
      if (!OwnerType.IsEntity)
        return;
      switch (Multiplicity) {
      case Multiplicity.ZeroToOne:
      case Multiplicity.OneToOne:
      case Multiplicity.ManyToOne:
        UnderlyingIndex = OwnerType.Indexes.PrimaryIndex;
        foreignKeyExtractorTransform = OwnerField.valueExtractorTransform;
        break;
      case Multiplicity.OneToMany:
        UnderlyingIndex = Reversed.OwnerType.Indexes.GetIndex(Reversed.OwnerField.Name);
        break;
      case Multiplicity.ZeroToMany:
      case Multiplicity.ManyToMany:
        if (IsMaster)
          UnderlyingIndex = auxiliaryType.Indexes.Where(indexInfo => indexInfo.IsSecondary).First();
        else
          UnderlyingIndex = Reversed.AuxiliaryType.Indexes.Where(indexInfo => indexInfo.IsSecondary).Skip(1).First();
        if (foreignKeyExtractorTransform == null) {
          var foreignKeySegment = new Segment<int>(OwnerType.Columns.Count(c => c.IsPrimaryKey), TargetType.Columns.Count(c => c.IsPrimaryKey));
          foreignKeyExtractorTransform = new SegmentTransform(true, UnderlyingIndex.TupleDescriptor, foreignKeySegment);
        }
        break;
      }
    }


    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="ownerField">The referencing field.</param>
    /// <param name="targetType">The referenced type.</param>
    /// <param name="multiplicity">The association multiplicity.</param>
    /// <param name="onTargetRemove">The <see cref="OnRemoveAction"/> that will be applied on <see cref="TargetType"/> object removal.</param>
    /// <param name="onOwnerRemove">The <see cref="OnRemoveAction"/> that will be applied on <see cref="OwnerType"/> object removal.</param>
    public AssociationInfo(FieldInfo ownerField, TypeInfo targetType, Multiplicity multiplicity, OnRemoveAction? onOwnerRemove, OnRemoveAction? onTargetRemove)
    {
      OwnerField = ownerField;
      TargetType = targetType;
      Multiplicity = multiplicity;
      OnOwnerRemove = onOwnerRemove;
      OnTargetRemove = onTargetRemove;
      ownerField.Association = this;
    }
  }
}