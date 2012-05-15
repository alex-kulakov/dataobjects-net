﻿// Copyright (C) 2012 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2012.03.08

using Xtensive.Tuples;

namespace Xtensive.Orm
{
  /// <summary>
  /// Temporary key generator contract for use with <see cref="DisconnectedState"/>.
  /// </summary>
  public interface ITemporaryKeyGenerator : IKeyGenerator
  {
    /// <summary>
    /// Checks if the specified key is local.
    /// </summary>
    /// <param name="keyTuple">Key tuple to check</param>
    /// <returns>true, if the specified <paramref name="keyTuple"/> represents local key;
    /// otherwise, false.</returns>
    bool IsTemporaryKey(Tuple keyTuple);
  }
}