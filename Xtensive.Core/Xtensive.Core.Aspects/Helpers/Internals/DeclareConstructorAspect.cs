// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.11.18

using System;
using PostSharp.Laos;

namespace Xtensive.Core.Aspects.Helpers.Internals
{
  /// <summary>
  /// Internally applied by <see cref="Helpers.ProtectedConstructorAspect"/>.
  /// </summary>
  [Serializable]
  public sealed class DeclareConstructorAspect : LaosTypeLevelAspect,
    ILaosWeavableAspect
  {
    private readonly ProtectedConstructorAspect constructorAspect;

    public ProtectedConstructorAspect Aspect
    {
      get { return constructorAspect; }
    }

    int ILaosWeavableAspect.AspectPriority
    {
      get { return (int) ProtectedConstructorAspectPriority.Declare; }
    }


    // Constructors

    internal DeclareConstructorAspect(ProtectedConstructorAspect protectedConstructorAspect)
    {
      this.constructorAspect = protectedConstructorAspect;
    }
  }
}