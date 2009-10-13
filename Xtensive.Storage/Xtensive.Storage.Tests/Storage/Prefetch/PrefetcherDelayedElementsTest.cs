// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Nikolaev
// Created:    2009.10.07

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Xtensive.Storage.Internals;
using Xtensive.Storage.Model;
using Xtensive.Storage.Tests.Storage.Prefetch.Model;

namespace Xtensive.Storage.Tests.Storage.Prefetch
{
  [TestFixture]
  public sealed class PrefetcherDelayedElementsTest : AutoBuildTest
  {
    private TypeInfo orderType;
    private TypeInfo customerType;

    protected override Xtensive.Storage.Configuration.DomainConfiguration BuildConfiguration()
    {
      var config = base.BuildConfiguration();
      config.UpgradeMode = DomainUpgradeMode.Recreate;
      config.Types.Register(typeof(Supplier).Assembly, typeof(Supplier).Namespace);
      return config;
    }

    [TestFixtureSetUp]
    public override void TestFixtureSetUp()
    {
      base.TestFixtureSetUp();
      using (var session = Session.Open(Domain))
      using (var transactionScope = Transaction.Open()) {
        for (int i = 0; i < 111; i++)
          PrefetchProcessorTest.FillDataBase(session);
        orderType = typeof (Order).GetTypeInfo();
        customerType = typeof (Customer).GetTypeInfo();
        transactionScope.Complete();
      }
    }

    [Test]
    public void SimpleTest()
    {
      List<Key> keys;
      using (Session.Open(Domain))
      using (var tx = Transaction.Open()) {
        keys = Query<Person>.All.AsEnumerable().Select(p => Key.Create<Person>(p.Key.Value)).ToList();
        Assert.IsTrue(keys.All(key => !key.HasExactType));
        Assert.Greater(keys.Count, 0);
      }

      using (var session = Session.Open(Domain))
      using (var tx = Transaction.Open()) {
        var prefetcher = keys.Prefetch<Person, Key>(key => key);
        foreach (var key in prefetcher) {
          Key cachedKey;
          Assert.IsFalse(key.HasExactType);
          Assert.IsTrue(Domain.KeyCache.TryGetItem(key, true, out cachedKey));
          Assert.IsTrue(cachedKey.HasExactType);
          PrefetchTestHelper.AssertOnlySpecifiedColumnsAreLoaded(key, cachedKey.Type, session,
            PrefetchTestHelper.IsFieldToBeLoadedByDefault);
        }
      }
    }

    [Test]
    public void PrefetchManyTest()
    {
      List<Key> keys;
      using (Session.Open(Domain))
      using (var tx = Transaction.Open()) {
        keys = Query<Customer>.All.AsEnumerable().Select(p => Key.Create<Person>(p.Key.Value)).ToList();
        Assert.IsTrue(keys.All(key => !key.HasExactType));
        Assert.Greater(keys.Count, 0);
      }

      using (var session = Session.Open(Domain))
      using (var tx = Transaction.Open()) {
        var ordersField = customerType.Fields["Orders"];
        var employeeField = orderType.Fields["Employee"];
        var employeeType = typeof (Employee).GetTypeInfo();
        var prefetcher = keys.Prefetch<Customer, Key>(key => key)
          .PrefetchMany(c => c.Orders, orders => orders, orders => {
            var ordersSet = (EntitySet<Order>) orders;
            PrefetchTestHelper.AssertOnlySpecifiedColumnsAreLoaded(ordersSet.Owner.Key, customerType,
              session, PrefetchTestHelper.IsFieldToBeLoadedByDefault);
            EntitySetState state;
            session.Handler.TryGetEntitySetState(ordersSet.Owner.Key, ordersSet.Field, out state);
            Assert.IsTrue(state.IsFullyLoaded);
            return orders.Prefetch(o => o.Employee);
          });
        foreach (var key in prefetcher) {
          var cachedKey = GetCachedKey(key, Domain);
          PrefetchTestHelper.AssertOnlySpecifiedColumnsAreLoaded(key, cachedKey.Type, session,
            PrefetchTestHelper.IsFieldToBeLoadedByDefault);
          EntitySetState state;
          session.Handler.TryGetEntitySetState(key, ordersField, out state);
          Assert.IsTrue(state.IsFullyLoaded);
          Assert.Greater(state.Count, 0);
          foreach (var orderKey in state) {
            var orderState = session.EntityStateCache[orderKey, true];
            Assert.IsNotNull(orderState);
            PrefetchTestHelper.AssertOnlySpecifiedColumnsAreLoaded(orderKey, orderType, session,
              PrefetchTestHelper.IsFieldToBeLoadedByDefault);
            var employeeKey = Key.Create<Person>(employeeField.Association.ExtractForeignKey(orderState.Tuple));
            PrefetchTestHelper.AssertOnlySpecifiedColumnsAreLoaded(employeeKey, employeeType, session,
              PrefetchTestHelper.IsFieldToBeLoadedByDefault);
          }
        }
      }
    }

    [Test]
    public void PrefetchViaEntityTest()
    {
      List<Key> keys;
      using (Session.Open(Domain))
      using (var tx = Transaction.Open()) {
        keys = Query<Customer>.All.AsEnumerable().Select(p => Key.Create<Person>(p.Key.Value)).ToList();
        Assert.IsTrue(keys.All(key => !key.HasExactType));
        Assert.Greater(keys.Count, 0);
      }

      using (var session = Session.Open(Domain))
      using (var tx = Transaction.Open()) {
        var prefetcher = keys.Prefetch<Entity, Key>(key => key);
        foreach (var key in prefetcher) {
          var cachedKey = GetCachedKey(key, Domain);
          PrefetchTestHelper.AssertOnlySpecifiedColumnsAreLoaded(cachedKey, customerType, session,
            PrefetchTestHelper.IsFieldToBeLoadedByDefault);
        }
      }
    }

    [Test]
    public void ThreeStepsFetchTest()
    {
      List<Key> keys;
      using (Session.Open(Domain))
      using (var tx = Transaction.Open()) {
        keys = Query<Person>.All.Take(20).AsEnumerable()
          .Select(p => Key.Create<Person>(p.Key.Value)).ToList();
        Assert.IsTrue(keys.All(key => !key.HasExactType));
        Assert.Greater(keys.Count, 0);
      }

      using (var session = Session.Open(Domain))
      using (var tx = Transaction.Open()) {
        var prefetchCount = session.Handler.PrefetchTaskExecutionCount;
        var prefetcher = keys.Prefetch<AdvancedPerson, Key>(key => key);
        foreach (var key in prefetcher) {
          var cachedKey = GetCachedKey(key, Domain);
          PrefetchTestHelper.AssertOnlySpecifiedColumnsAreLoaded(cachedKey, cachedKey.Type, session,
            PrefetchTestHelper.IsFieldToBeLoadedByDefault);
        }
        Assert.AreEqual(prefetchCount + 2, session.Handler.PrefetchTaskExecutionCount);
      }
    }

    public static Key GetCachedKey(Key key, Domain domain)
    {
      Key result;
      Assert.IsFalse(key.HasExactType);
      Assert.IsTrue(domain.KeyCache.TryGetItem(key, true, out result));
      Assert.IsTrue(result.HasExactType);
      return result;
    }
  }
}