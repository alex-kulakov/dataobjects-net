// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.02.16

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Core.Collections;
using Xtensive.Core.Disposing;
using Xtensive.Core.Internals.DocTemplates;
using Xtensive.Core.Parameters;
using Xtensive.Core.Tuples;
using Xtensive.Core.Tuples.Transform;

namespace Xtensive.Storage.Rse.Providers.Executable
{
  [Serializable]
  internal sealed class ApplyProvider : BinaryExecutableProvider<Compilable.ApplyProvider>
  {
    private CombineTransform combineTransform;
    private Tuple rightBlank;

    protected internal override IEnumerable<Tuple> OnEnumerate(EnumerationContext context)
    {
      var left = Left.Enumerate(context);
      switch (Origin.ApplyType) {
      case JoinType.Inner:
        return InnerApply(context, left);
      case JoinType.LeftOuter:
        return LeftOuterApply(context, left);
      default:
        throw new ArgumentOutOfRangeException();
      }
    }

    #region Private implementation.

    private IEnumerable<Tuple> InnerApply(EnumerationContext context, IEnumerable<Tuple> left)
    {
      var ctx = new ParameterContext();
      ParameterScope scope = null;
      var batched = left
        .SelectMany(tuple => {
          Origin.ApplyParameter.Value = tuple;
          // Do not cache right part
          var right = Right.OnEnumerate(context);
          return right.Select(rTuple => combineTransform.Apply(TupleTransformType.Auto, tuple, rTuple));
        })
        .Batch(0, 1, 1)
        .ApplyBeforeAndAfter(() => scope = ctx.Activate(), () => scope.DisposeSafely());
      foreach (var batch in batched)
        foreach (var tuple in batch)
          yield return tuple;
    }

    private IEnumerable<Tuple> LeftOuterApply(EnumerationContext context, IEnumerable<Tuple> left)
    {
      var ctx = new ParameterContext();
      ParameterScope scope = null;
      var batched = left
        .SelectMany(tuple => {
          Origin.ApplyParameter.Value = tuple;
          // Do not cache right part
          var right = Right.OnEnumerate(context);
          bool isEmpty = true;
          var result = right.Select(rTuple => {
            isEmpty = false;
            return combineTransform.Apply(TupleTransformType.Auto, tuple, rTuple);
          });
          return isEmpty 
            ? EnumerableUtils.One(combineTransform.Apply(TupleTransformType.Auto, tuple, rightBlank)) 
            : result;
        })
        .Batch(0, 1, 1)
        .ApplyBeforeAndAfter(() => scope = ctx.Activate(),() => scope.DisposeSafely());
      foreach (var batch in batched)
        foreach (var tuple in batch)
          yield return tuple;
    }

    /*private IEnumerable<Tuple> ApplyExisting(EnumerationContext context, IEnumerable<Tuple> left)
    {
      using (new ParameterContext().Activate())
      foreach (var tuple in left) {
        Origin.ApplyParameter.Value = tuple;
        // Do not cache right part
        var right = Right.OnEnumerate(context);
        if (right.Any())
          yield return tuple;
      }
    }

    private IEnumerable<Tuple> ApplyNotExisting(EnumerationContext context, IEnumerable<Tuple> left)
    {
      using (new ParameterContext().Activate())
      foreach (var tuple in left) {
        Origin.ApplyParameter.Value = tuple;
        // Do not cache right part
        var right = Right.OnEnumerate(context);
        if (!right.Any())
          yield return tuple;
      }
    }*/

    #endregion

    protected override void Initialize()
    {
      base.Initialize();
      combineTransform = new CombineTransform(true, Left.Header.TupleDescriptor, Right.Header.TupleDescriptor);
      rightBlank = Tuple.Create(Right.Header.TupleDescriptor);
    }


    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    public ApplyProvider(Compilable.ApplyProvider origin, ExecutableProvider left, ExecutableProvider right)
      : base(origin, left, right)
    {
    }
  }
}