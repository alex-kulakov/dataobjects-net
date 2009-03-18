// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2009.03.18

using System;
using System.Diagnostics;
using Xtensive.Modelling.Attributes;

namespace Xtensive.Modelling.Tests.DatabaseModel
{
  [Serializable]
  public class Server : Model
  {
    [NodeProperty]
    public DatabaseCollection Databases { get; private set; }

    protected override void Initialize()
    {
      base.Initialize();
      if (Databases==null)
        Databases = new DatabaseCollection(this);
    }

    public Server(string name)
      : base(name)
    {
    }
  }
}