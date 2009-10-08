// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.05.28

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Core;
using Xtensive.Core.Collections;
using Xtensive.Core.Disposing;
using Xtensive.Core.Parameters;
using Xtensive.Core.Tuples;
using Xtensive.Core.Tuples.Transform;
using Xtensive.Storage.Resources;
using Xtensive.Storage.Rse;
using Xtensive.Core.Linq;
using System.Linq;

namespace Xtensive.Storage.Linq.Materialization
{
  internal static class MaterializationHelper
  {
    public static readonly MethodInfo MaterializeMethodInfo;
    public static readonly MethodInfo GetDefaultMethodInfo;
    public static readonly MethodInfo CompileItemMaterializerMethodInfo;
    public static readonly MethodInfo IsNullMethodInfo;
    public static readonly MethodInfo ThrowSequenceExceptionMethodInfo;

    public static int[] CreateSingleSourceMap(int targetLength, Pair<int>[] remappedColumns)
    {
      var map = new int[targetLength];
      for (int i = 0; i < map.Length; i++)
        map[i] = MapTransform.NoMapping;

      for (int i = 0; i < remappedColumns.Length; i++) {
        var targetIndex = remappedColumns[i].First;
        var sourceIndex = remappedColumns[i].Second;
        map[targetIndex] = sourceIndex;
      }
      return map;
    }

// ReSharper disable UnusedMember.Global

    public static T GetDefault<T>()
    {
      return default(T);
    }

    public static bool IsNull(Tuple tuple, int[] columns)
    {
      var result = true;
      for (int i = 0; i < columns.Length; i++) {
        var column = columns[i];
        result &= tuple.GetFieldState(column).IsNull();
      }
      return result;
    }

    public static object ThrowSequenceException()
    {
      throw new InvalidOperationException(Strings.ExSequenceContainsNoElements);
    }

    public static IEnumerable<TResult> Materialize<TResult>(IEnumerable<Tuple> dataSource, MaterializationContext context, Func<Tuple, ItemMaterializationContext, TResult> itemMaterializer, Dictionary<Parameter<Tuple>, Tuple> tupleParameterBindings)
    {
      ParameterContext ctx;
      var session = Session.Demand();
      using (session.OpenTransaction()) {
        using (new ParameterContext().Activate()) {
          ctx = ParameterContext.Current;
          foreach (var tupleParameterBinding in tupleParameterBindings)
            tupleParameterBinding.Key.Value = tupleParameterBinding.Value;
        }
        ParameterScope scope = null;
        var batched = dataSource.Select(tuple => itemMaterializer.Invoke(tuple, new ItemMaterializationContext(context, session))).Batch(2)
          .ApplyBeforeAndAfter(() => scope = ctx.Activate(), () => scope.DisposeSafely());
        foreach (var batch in batched)
          foreach (var result in batch)
            yield return result;
      }
    }

    public static Func<Tuple, ItemMaterializationContext, TResult> CompileItemMaterializer<TResult>(Expression<Func<Tuple, ItemMaterializationContext, TResult>> itemMaterializerLambda)
    {
      return itemMaterializerLambda.CachingCompile();
    }

// ReSharper restore UnusedMember.Global


    // Type initializer

    static MaterializationHelper()
    {
      MaterializeMethodInfo = typeof (MaterializationHelper)
        .GetMethod("Materialize", BindingFlags.Public | BindingFlags.Static);
      CompileItemMaterializerMethodInfo = typeof (MaterializationHelper)
        .GetMethod("CompileItemMaterializer", BindingFlags.Public | BindingFlags.Static);
      GetDefaultMethodInfo = typeof(MaterializationHelper)
        .GetMethod("GetDefault", BindingFlags.Public | BindingFlags.Static);
      IsNullMethodInfo = typeof(MaterializationHelper)
        .GetMethod("IsNull", BindingFlags.Public | BindingFlags.Static);
      ThrowSequenceExceptionMethodInfo = typeof(MaterializationHelper)
        .GetMethod("ThrowSequenceException", BindingFlags.Public | BindingFlags.Static);
    }
  }
}