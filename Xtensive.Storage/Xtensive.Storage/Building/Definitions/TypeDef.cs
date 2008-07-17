// Copyright (C) 2007 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2007.08.27

using System;
using System.Diagnostics;
using System.Reflection;
using Xtensive.Core;
using Xtensive.Core.Reflection;
using Xtensive.Storage.Attributes;
using Xtensive.Storage.Building.Builders;
using Xtensive.Storage.Model;
using TypeAttributes=Xtensive.Storage.Model.TypeAttributes;

namespace Xtensive.Storage.Building.Definitions
{
  /// <summary>
  /// Represents a persistent type definition.
  /// </summary>
  [DebuggerDisplay("{underlyingType}")]
  [Serializable]
  public sealed class TypeDef : MappingNode
  {
    private readonly Type underlyingType;
    private TypeAttributes attributes;
    private readonly NodeCollection<FieldDef> fields;
    private readonly NodeCollection<IndexDef> indexes;
  
    /// <summary>
    /// Gets a value indicating whether this instance is entity.
    /// </summary>
    public bool IsEntity
    {
      get { return (attributes & TypeAttributes.Entity) > 0; }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is abstract entity.
    /// </summary>
    public bool IsAbstract
    {
      get { return (attributes & TypeAttributes.Abstract) > 0; }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is interface.
    /// </summary>
    public bool IsInterface
    {
      get { return (attributes & TypeAttributes.Interface) > 0; }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is structure.
    /// </summary>
    public bool IsStructure
    {
      get { return (attributes & TypeAttributes.Structure) > 0; }
    }

    /// <summary>
    /// Gets or sets the underlying system type.
    /// </summary>
    public Type UnderlyingType
    {
      get { return underlyingType; }
    }

    /// <summary>
    /// Gets the attributes.
    /// </summary>
    public TypeAttributes Attributes
    {
      get { return attributes; }
      set { attributes = value; }
    }


    /// <summary>
    /// Gets the indexes for this instance.
    /// </summary>
    public NodeCollection<IndexDef> Indexes
    {
      get { return indexes; }
    }

    /// <summary>
    /// Gets the fields contained in this instance.
    /// </summary>
    public NodeCollection<FieldDef> Fields
    {
      get { return fields; }
    }

    /// <summary>
    /// Defines the index and adds it to the <see cref="Indexes"/>.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">Argument "name" is invalid.</exception>
    public IndexDef DefineIndex(string name)
    {
      if (!Validator.IsNameValid(name, ValidationRule.Index))
        throw new DomainBuilderException(
          string.Format(Resources.Strings.IndexNameXIsInvalid, name));

      IndexDef indexDef = new IndexDef {Name = name};
      indexes.Add(indexDef);
      return indexDef;
    }

    /// <summary>
    /// Defines the field and adds it to the <see cref="Fields"/>.
    /// </summary>
    /// <param name="property">The underlying property.</param>
    /// <returns></returns>
    public FieldDef DefineField(PropertyInfo property)
    {
      ArgumentValidator.EnsureArgumentNotNull(property, "property");

      if (property.DeclaringType != UnderlyingType)
        throw new DomainBuilderException(
          string.Format(Resources.Strings.ExPropertyXMustBeDeclaredInTypeY, property.Name, UnderlyingType.FullName));
            
      FieldDef fieldDef = FieldBuilder.DefineField(this, property);
      fields.Add(fieldDef);
      return fieldDef;      
    }

    /// <summary>
    /// Defines the field and adds it to the <see cref="Fields"/>.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="valueType">The type of the field value.</param>
    /// <returns></returns>
    public FieldDef DefineField(string name, Type valueType)
    {
      ArgumentValidator.EnsureArgumentNotNull(valueType, "type");
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(name, "name");

      FieldDef field = FieldBuilder.DefineField(name, valueType, underlyingType);
      fields.Add(field);
      return field;
    }

    /// <summary>
    /// Performs additional custom processes before setting new name to this instance.
    /// </summary>
    /// <param name="nameToValidate">The new name of this instance.</param>
    protected override void Validate(string nameToValidate)
    {
      base.Validate(nameToValidate);
      if (!Validator.IsNameValid(nameToValidate, ValidationRule.Type))
        throw new ArgumentOutOfRangeException(nameToValidate);
    }


    // Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeDef"/> class.
    /// </summary>
    /// <param name="type">The underlying type.</param>
    internal TypeDef(Type type)
    {
      underlyingType = type;
      if (type.IsInterface)
        Attributes = TypeAttributes.Interface;
      else if (type == typeof (Structure) || type.IsSubclassOf(typeof (Structure)))
        Attributes = TypeAttributes.Structure;
      else
        Attributes = type.IsAbstract
          ? TypeAttributes.Entity | TypeAttributes.Abstract
          : TypeAttributes.Entity;
      fields = new NodeCollection<FieldDef>();
      indexes = new NodeCollection<IndexDef>();
    }
  }
}