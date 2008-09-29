// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2008.09.08

using NUnit.Framework;
using Xtensive.Core.Diagnostics;
using Xtensive.Core.Parameters;
using Xtensive.Core.Testing;
using Xtensive.Core.Tuples;
using Xtensive.Storage.Configuration;
using Xtensive.Storage.Rse;
using Xtensive.Storage.Tests.Storage.Performance.CrudModel;

namespace Xtensive.Storage.Tests.Storage.Performance
{
  [TestFixture]
  public class CrudTest : AutoBuildTest
  {
    public const int BaseCount = 10000;
    public const int InsertCount = BaseCount;
    private bool warmup  = false;
    private bool profile = false;
    private int instanceCount;

    protected override DomainConfiguration BuildConfiguration()
    {
//      DomainConfiguration config = DomainConfigurationFactory.Create("mssql2005");
      DomainConfiguration config = DomainConfigurationFactory.Create("memory");
      config.Types.Register(typeof(Simplest).Assembly, typeof(Simplest).Namespace);
      return config;
    }

    [Test]
    public void RegularTest()
    {
      warmup = true;
      CombinedTest(10, 10);
      warmup = false;
      CombinedTest(BaseCount, InsertCount);
    }

    [Test]
    [Explicit]
    [Category("Profile")]
    public void ProfileTest()
    {
      int instanceCount = 100000;
      InsertTest(instanceCount);
      BulkFetchTest(instanceCount);
    }

    private void CombinedTest(int baseCount, int insertCount)
    {
      InsertTest(insertCount);
      BulkFetchTest(baseCount);
      FetchTest(baseCount / 2);
      QueryTest(baseCount / 5);
      CachedQueryTest(baseCount / 5);
      RemoveTest();
    }

    private void InsertTest(int insertCount)
    {
      var d = Domain;
      using (var ss = d.OpenSession()) {
        var s = ss.Session;
        long sum = 0;
        TestHelper.CollectGarbage();
        using (warmup ? null : new Measurement("Insert", insertCount)) {
          using (var ts = s.OpenTransaction()) {
            for (int i = 0; i < insertCount; i++) {
              var o = new Simplest(i, i);
              sum += i;
            }
            ts.Complete();
          }
        }
      }
      instanceCount = insertCount;
    }

    private void FetchTest(int count)
    {
      var d = Domain;
      using (var ss = d.OpenSession()) {
        var s = ss.Session;
        long sum = (long)count*(count-1)/2;
        using (var ts = s.OpenTransaction()) {
          TestHelper.CollectGarbage();
          using (warmup ? null : new Measurement("Fetch & GetField", count)) {
            for (int i = 0; i < count; i++) {
              var key = Key.Get<Simplest>(Tuple.Create((long) i % instanceCount));
              var o = key.Resolve<Simplest>();
              sum -= o.Id;
            }
            ts.Complete();
          }
        }
        if (count<=instanceCount)
          Assert.AreEqual(0, sum);
      }
    }

    private void BulkFetchTest(int count)
    {
      var d = Domain;
      using (var ss = d.OpenSession()) {
        var s = ss.Session;
        long sum = 0;
        int i = 0;
        using (var ts = s.OpenTransaction()) {
          var rs = d.Model.Types[typeof (Simplest)].Indexes.PrimaryIndex.ToRecordSet();
          TestHelper.CollectGarbage();
          using (warmup ? null : new Measurement("Bulk Fetch & GetField", count)) {
            while (i<count) {
              foreach (var o in rs.ToEntities<Simplest>()) {
                sum += o.Id;
                if (++i >= count)
                  break;
              }
            }
            ts.Complete();
          }
        }
        Assert.AreEqual((long)count*(count-1)/2, sum);
      }
    }

    private void QueryTest(int count)
    {
      var d = Domain;
      using (var ss = d.OpenSession()) {
        var s = ss.Session;
        using (var ts = s.OpenTransaction()) {
          TestHelper.CollectGarbage();
          using (warmup ? null : new Measurement("Query", count)) {
            for (int i = 0; i < count; i++) {
              var pKey = new Parameter<Tuple>();
              var rs = d.Model.Types[typeof (Simplest)].Indexes.PrimaryIndex.ToRecordSet();
              rs = rs.Seek(() => pKey.Value);
              using (new ParameterScope()) {
                pKey.Value = Tuple.Create(i % instanceCount);
                var es = rs.ToEntities<Simplest>();
                foreach (var o in es) {
                  // Doing nothing, just enumerate
                }
              }
            }
            ts.Complete();
          }
        }
      }
    }

    private void CachedQueryTest(int count)
    {
      var d = Domain;
      using (var ss = d.OpenSession()) {
        var s = ss.Session;
        using (var ts = s.OpenTransaction()) {
          TestHelper.CollectGarbage();
          var pKey = new Parameter<Tuple>();
          var rs = d.Model.Types[typeof (Simplest)].Indexes.PrimaryIndex.ToRecordSet();
          rs = rs.Seek(() => pKey.Value);
          using (new ParameterScope()) {
            using (warmup ? null : new Measurement("Cached Query", count)) {
              for (int i = 0; i < count; i++) {
                pKey.Value = Tuple.Create(i % instanceCount);
                var es = rs.ToEntities<Simplest>();
                foreach (var o in es) {
                  // Doing nothing, just enumerate
                }
              }
            }
            ts.Complete();
          }
        }
      }
    }

    private void RemoveTest()
    {
      var d = Domain;
      using (var ss = d.OpenSession()) {
        var s = ss.Session;
        TestHelper.CollectGarbage();
        using (warmup ? null : new Measurement("Remove", instanceCount)) {
          using (var ts = s.OpenTransaction()) {
            var rs = d.Model.Types[typeof (Simplest)].Indexes.PrimaryIndex.ToRecordSet();
            var es = rs.ToEntities<Simplest>();
            foreach (var o in es)
              o.Remove();
            ts.Complete();
          }
        }
      }
    }
  }
}
