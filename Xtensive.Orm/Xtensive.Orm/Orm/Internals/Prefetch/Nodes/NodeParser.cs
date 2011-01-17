// Copyright (C) 2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2010.11.19

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Xtensive.Linq;
using Xtensive.Orm.Linq;
using Xtensive.Orm.Model;
using Xtensive.Core;
using Xtensive.Orm.Resources;
using Xtensive.Parameters;
using ExpressionVisitor = System.Linq.Expressions.ExpressionVisitor;
using Xtensive.Reflection;

namespace Xtensive.Orm.Internals.Prefetch
{
  [Serializable]
  internal class NodeParser : ExpressionVisitor
  {
    private readonly DomainModel model;
    private Expression extractorExpression;
    private int? top;
    private readonly List<FieldNode> nodes;

    public static KeyExtractorNode<T> Parse<T,TValue>(DomainModel model, Expression<Func<T, TValue>> expression)
    {
      var parameter = expression.Parameters.Single();
      if (expression.Body == parameter)
        return null;
      var parser = new NodeParser(model);
      parser.Visit(expression.Body);
      if (parser.extractorExpression == null)
        return null;
      var pathStack = new Stack<string>();
      var e = parser.extractorExpression;
      while(e.NodeType == ExpressionType.MemberAccess) {
        var memberExpression = (MemberExpression) e;
        pathStack.Push(memberExpression.Member.Name);
        e = memberExpression.Expression;
      }
      var path = pathStack.ToDelimitedString(".");
      if (path.IsNullOrEmpty())
        path = "*";
      var keyExtractor = BuildKeyExtractor<T>(parser.extractorExpression, parameter);
      return new KeyExtractorNode<T>(path, keyExtractor, new ReadOnlyCollection<FieldNode>(parser.nodes));
    }

    private static Expression<Func<T, IEnumerable<Key>>> BuildKeyExtractor<T>(Expression keyExtractorExpression, ParameterExpression parameter)
    {
      if (typeof(IEntity).IsAssignableFrom(keyExtractorExpression.Type)) {
        Expression<Func<IEntity, IEnumerable<Key>>> extractor = e => Enumerable.Repeat(e.Key, 1);
        var body = extractor.BindParameters(keyExtractorExpression);
        return Expression.Lambda<Func<T, IEnumerable<Key>>>(body, parameter);
      }
      if (typeof(IEnumerable).IsAssignableFrom(keyExtractorExpression.Type)) {
        Expression<Func<IEnumerable, IEnumerable<Key>>> extractor = enumerable => enumerable.OfType<IEntity>().Select(e => e.Key);
        var body = extractor.BindParameters(keyExtractorExpression);
        return Expression.Lambda<Func<T, IEnumerable<Key>>>(body, parameter);
      }
      return _ => Enumerable.Empty<Key>();
    }

    private static FieldNode Parse(DomainModel model, LambdaExpression expression)
    {
      var parameter = expression.Parameters.Single();
      if (expression.Body == parameter)
        return null;
      var parser = new NodeParser(model);
      parser.Visit(expression.Body);
      return parser.nodes.SingleOrDefault();
    }

    protected override Expression VisitMember(MemberExpression e)
    {
      if (e.Expression == null)
        return e;

      if (typeof(IEntity).IsAssignableFrom(e.Expression.Type)) {
        var entityType = model.Types[e.Expression.Type];
        var path = e.Member.Name;
        var field = entityType.Fields[path];
        var nestedNodes = new ReadOnlyCollection<FieldNode>(nodes.ToList());
        nodes.Clear();
        if (typeof (IEntity).IsAssignableFrom(e.Type))
          nodes.Add(new ReferenceNode(path, model.Types[field.ValueType], field, nestedNodes));
        else if (typeof (EntitySetBase).IsAssignableFrom(e.Type))
          nodes.Add(new SetNode(path, model.Types[field.ItemType], field, top, nestedNodes));
        else
          nodes.Add(new FieldNode(path, field));
        var visited = (MemberExpression)base.VisitMember(e);
        return visited;
      }
      if (extractorExpression == null)
        extractorExpression = e;
      return e;
    }

    protected override Expression VisitParameter(ParameterExpression e)
    {
      if (extractorExpression == null)
        extractorExpression = e;
      return e;
    }

    protected override Expression VisitMethodCall(MethodCallExpression e)
    {
      if (e.Method.DeclaringType != typeof (PrefetchExtensions) 
        || e.Method.Name != "Prefetch" 
        || e.Method.GetParameters().Length != 2)
      {
        throw new NotSupportedException(
          string.Format(Strings.ExOnlyPrefetchMethodSupportedButFoundX, e.ToString(true)));
      }
      var source = e.Arguments[0];
      var lambda = e.Arguments[1].StripQuotes();
      var fieldNode = Parse(model, lambda);
      if (fieldNode != null)
        nodes.Add(fieldNode);
      Visit(source);
      return e;
    }

    private NodeParser(DomainModel model)
    {
      this.model = model;
      nodes = new List<FieldNode>();
    }
  }
}