// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Kofman
// Created:    2009.03.18

using System;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using Xtensive.Storage.Resources;

namespace Xtensive.Storage.Serialization
{
  /// <summary>
  /// Object to be serialized instead of <see cref="Entity"/> when serialization <see cref="SerializationKind.ByReference"/> is used.
  /// </summary>
  [Serializable]
  internal sealed class EntityReference : IObjectReference, 
    ISerializable
  {
    private const string KeyValueName = WellKnown.KeyFieldName;
    private readonly Entity entity;

    #if NET40
    [SecurityCritical]
    #endif
    public object GetRealObject(StreamingContext context)
    {
      return entity;
    }
      
    #if NET40
    [SecurityCritical]
    #else
    [SecurityPermission(SecurityAction.LinkDemand, SerializationFormatter=true)]
    #endif
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue(KeyValueName, entity.Key.Format());
    }

    public EntityReference(Entity entity)
    {
      this.entity = entity;
    }

    private EntityReference(SerializationInfo info, StreamingContext context)
    {
      Key key = Key.Parse(info.GetString(KeyValueName));
      entity = Query.SingleOrDefault(key);

      if (entity==null)
        throw new InvalidOperationException(
          string.Format(Strings.ExCannotResolveEntityWithKeyX, key));
    }
  }
}