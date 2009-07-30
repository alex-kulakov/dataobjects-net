// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2009.07.21

using System.Linq;
using NUnit.Framework;
using Xtensive.Storage.Configuration;
using Xtensive.Storage.Tests.Issues.Issue0296_Model;

namespace Xtensive.Storage.Tests.Issues.Issue0296_Model
{
  [HierarchyRoot]
  public class Node : Entity
  {
    [Field, Key]
    public int Id { get; private set; }

    [Field]
    public Node Right { get; set; }

    [Field]
    public Node Left { get; set; }

    [Field]
    public Node Top { get; set; }

    [Field]
    public Node Bottom { get; set; }
  }
}

namespace Xtensive.Storage.Tests.Issues
{
  [Ignore("Requires manual profiling")]
  public class Issue0296_DeferredConstraints : AutoBuildTest
  {
    protected override DomainConfiguration BuildConfiguration()
    {
      var config = base.BuildConfiguration();
      config.Types.Register(typeof (Node).Assembly, typeof (Node).Namespace);
      return config;
    }

    [Test]
    public void MainTest()
    {
      const int count = 50000;
      using (Session.Open(Domain)) {
        using (var t = Transaction.Open()) {

          var root = new Node();
          for (int i = 0; i < count-1; i++) {
            var next = new Node();
            root.Right = next;
            root.Left = new Node();
            root = next;
          }

          Assert.Less(count, Session.Current.EntityChangeRegistry.GetItems(PersistenceState.New).Count());

          t.Complete();
        }
      }
    }
  }
}