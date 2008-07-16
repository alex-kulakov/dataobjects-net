// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.07.04

using System;
using Xtensive.Storage.Rse.Providers;
using Xtensive.Storage.Rse.Providers;

namespace Xtensive.Storage.Rse.Compilation
{
  /// <summary>
  /// Strongly typed version of <see cref="TypeCompiler"/>.
  /// </summary>
  /// <typeparam name="TProvider">The type of the provider this compiler can compile.</typeparam>
  public abstract class TypeCompiler<TProvider> : TypeCompiler
    where TProvider : CompilableProvider
  {
    /// <inheritdoc/>
    public sealed override Provider Compile(CompilableProvider provider)
    {
      return Compile((TProvider) provider);
    }

    /// <summary>
    /// Compiles the specified provider.
    /// </summary>
    /// <param name="provider">The provider to compile.</param>
    /// <returns>Compiled provider.</returns>
    protected abstract Provider Compile(TProvider provider);


    // Constructor

    /// <inheritdoc/>
    protected TypeCompiler(Compiler compiler)
      : base(compiler)
    {
    }
  }
}