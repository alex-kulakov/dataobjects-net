// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Gamzov
// Created:    2008.08.07

using System.Configuration;
using Xtensive.Configuration;

namespace Xtensive.Orm.Configuration.Elements
{
  /// <summary>
  /// Builder configuration element within a configuration file.
  /// </summary>
  public class BuilderElement : ConfigurationCollectionElementBase
  {
    private const string TypeElementName = "type";


    /// <summary>
    /// Gets identifier for this instance.
    /// </summary>
    public override object Identifier {
      get { return Type; }
    }

    /// <summary>
    /// Gets or sets the type of the builder.
    /// </summary>
    [ConfigurationProperty(TypeElementName, IsRequired = true, IsKey = true)]
    public string Type {
      get { return (string)this[TypeElementName]; }
      set { this[TypeElementName] = value; }
    }
  }
}