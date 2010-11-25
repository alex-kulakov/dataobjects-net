// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.12.13

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Xtensive.Core;
using Xtensive.Core.Helpers;
using Xtensive.Storage.Linq;
using Xtensive.Storage.Providers;
using Xtensive.Storage.Tests.ObjectModel;
using Xtensive.Storage.Tests.ObjectModel.NorthwindDO;

namespace Xtensive.Storage.Tests.Linq
{
  public class TargetField
  {
    public TargetField(string name, object value)
    {
      if (string.IsNullOrEmpty(name))
        throw new ArgumentException("Parameter 'name' cannot be null or empty.");

      this.Name = name;
      this.Value = value;
    }

    public string Name { get; private set; }

    public object Value { get; private set; }
  }

  [Category("Linq")]
  [TestFixture]
  public class WhereTest : NorthwindDOModelTest
  {
    private Key supplierLekaKey;
    private Key categoryFirstKey;

    [TestFixtureSetUp]
    public override void TestFixtureSetUp()
    {
      base.TestFixtureSetUp();

      using (Session.Open(Domain)) {
        using (Transaction.Open()) {
          supplierLekaKey = Query.All<Supplier>().Single(s => s.CompanyName=="Leka Trading").Key;
          categoryFirstKey = Query.All<Category>().First().Key;
        }
      }
    }

    [Test]
    public void IndexerTest()
    {
      var query = Query.All<Order>();
      var kvp = new Pair<string, object>("Customer", Query.All<Customer>().First());
      query = query.Where(order => order[kvp.First]==kvp.Second);
      var r = query.ToList();
    }

    [Test]
    public void IndexerNullTest()
    {
      var query = Query.All<Order>();
      var parameters = Query.All<Customer>().Take(1).Select(c => new TargetField("Customer", (Customer)null)).ToList();
      foreach (var item in parameters) {
        var field = item; // This is important to use local variable
        query = query.Where(order => order[field.Name]==field.Value);
      }
      var result = query.ToList<Order>();
      }

    [Test]
    public void ReferenceFieldConditionTest()
    {
      var result = Query.All<Order>()
        .Where(order => order.Customer!=null)
        .ToList();

//      var expected = Query.All<Order>()
//        .AsEnumerable()
//        .First(order => order.Customer!=null);
//      Assert.AreEqual(expected.Customer, result.Customer);
    }

    [Test]
    public void IndexerSimpleFieldTest()
    {
      object freight = Query
        .All<Order>()
        .First(order => order.Freight > 0)
        .Freight;
      var result = Query
        .All<Order>()
        .OrderBy(order => order.Id)
        .Where(order => order["Freight"]==freight)
        .ToList();
      var expected = Query
        .All<Order>()
        .AsEnumerable()
        .OrderBy(order => order.Id)
        .Where(order => order.Freight==(decimal?) freight)
        .ToList();
      Assert.IsTrue(expected.SequenceEqual(result));
    }

    [Test]
    public void IndexerStructureTest()
    {
      object address = Query
        .All<Customer>()
        .First(customer => customer.Address.City.Length > 0)
        .Address;
      var result = Query
        .All<Customer>()
        .OrderBy(customer => customer.Id)
        .Where(customer => customer["Address"]==address)
        .ToList();
#pragma warning disable 252,253
      var expected = Query
        .All<Customer>()
        .AsEnumerable()
        .OrderBy(customer => customer.Id)
        .Where(customer => customer.Address==address)
        .ToList();
#pragma warning restore 252,253
      Assert.IsTrue(expected.SequenceEqual(result));
    }

    [Test]
    public void IndexerEntity1Test()
    {
      object customer = Query
        .All<Order>()
        .First(order => order.Customer!=null)
        .Customer;
      var result = Query
        .All<Order>()
        .OrderBy(order => order.Id)
        .Where(order => order["Customer"]==customer)
        .ToList();
      var expected = Query
        .All<Order>()
        .AsEnumerable()
        .OrderBy(order => order.Id)
        .Where(order => order.Customer==customer)
        .ToList();
      Assert.IsTrue(expected.SequenceEqual(result));
    }

    [Test]
    public void EntitySubqueryTest()
    {
      Require.ProviderIsNot(StorageProvider.SqlServerCe);
      var result = Query.All<Order>()
        .OrderBy(order => order.Id)
        .Where(order => order.Customer==
          Query.All<Order>()
            .First(order2 => order2.Customer!=null)
            .Customer)
        .ToList();
      var expected = Query.All<Order>()
        .AsEnumerable()
        .OrderBy(order => order.Id)
        .Where(order => order.Customer==
          Query.All<Order>()
            .First(order2 => order2.Customer!=null)
            .Customer)
        .ToList();
      Assert.IsTrue(expected.SequenceEqual(result));
    }

    [Test]
    public void EntitySubqueryIndexerTest()
    {
      Require.ProviderIsNot(StorageProvider.SqlServerCe);
      var result = Query.All<Order>()
        .OrderBy(order => order.Id)
        .Where(order => order.Customer==
          Query.All<Order>()
            .First(order2 => order2["Customer"]!=null)
            .Customer)
        .ToList();
      var expected = Query.All<Order>()
        .AsEnumerable()
        .OrderBy(order => order.Id)
        .Where(order => order.Customer==
          Query.All<Order>()
            .First(order2 => order2.Customer!=null)
            .Customer)
        .ToList();
      Assert.IsTrue(expected.SequenceEqual(result));
    }

    [Test]
    public void ParameterTest()
    {
      var category = Query.All<Category>().First();
      var result = Query.All<Product>().Where(p => p.Category==category);
      QueryDumper.Dump(result);
    }

    [Test]
    public void MultipleConditionTest()
    {
      var customers = Query.All<Customer>().Select(c => c.CompanyName).Where(cn => cn.StartsWith("A") || cn.StartsWith("B"));
      var list = customers.ToList();
    }

    [Test]
    public void AnonymousTest()
    {
      Customer first = Query.All<Customer>().First();
      var p = new {first.CompanyName, first.ContactName};
      var result = Query.All<Customer>().Select(c => new {c.CompanyName, c.ContactName}).Take(10).Where(x => x==p);
      var list = result.ToList();
    }


    [Test]
    public void AnonymousTest2()
    {
      Customer first = Query.All<Customer>().First();
      var p = new {first.CompanyName, first.ContactName};
      var result = Query.All<Customer>().Select(c => new {c.CompanyName, c.ContactName}).Take(10).Where(x => p==x);
      var list = result.ToList();
    }

    [Test]
    public void AnonymousWithParameterTest()
    {
      Customer first = Query.All<Customer>().First();
      var p = new {first.CompanyName, first.ContactName};
      var result = Query.All<Customer>().Where(c => new {c.CompanyName, c.ContactName}==p);
      var list = result.ToList();
    }

    [Test]
    public void AnonymousWithParameter2Test()
    {
      Customer first = Query.All<Customer>().First();
      var p = new {first.CompanyName, first.ContactName};
      var k = new {InternalFiled = p};
      var result = Query.All<Customer>().Where(c => new {c.CompanyName, c.ContactName}==k.InternalFiled);
      QueryDumper.Dump(result);
    }


    [Test]
    public void AnonymousWithParameter3Test()
    {
      Customer first = Query.All<Customer>().First();
      var p = new {first.CompanyName, first.ContactName};
      var k = new {InternalFiled = p};
      var result = Query.All<Customer>().Where(c => new {X = new {c.CompanyName, c.ContactName}}.X==k.InternalFiled);
      QueryDumper.Dump(result);
    }

    [Test]
    public void Anonymous2Test2()
    {
      Customer first = Query.All<Customer>().First();
      var p = new {first.CompanyName, first.ContactName};
      var result = Query.All<Customer>().Where(c => new {c.CompanyName}.CompanyName=="CompanyName");
      var list = result.ToList();
    }

    [Test]
    public void Anonymous3Test()
    {
      Customer first = Query.All<Customer>().First();
      var result = Query.All<Customer>().Where(c => new {c.CompanyName, c.ContactName}==new {c.CompanyName, c.ContactName});
      var list = result.ToList();
    }

    [Test]
    public void Anonymous4Test()
    {
      Require.AllFeaturesSupported(ProviderFeatures.RowNumber);
      Customer first = Query.All<Customer>().First();
      Customer second = Query.All<Customer>().Skip(1).First();
      var p = new {first.CompanyName, first.ContactName};
      var l = new {second.CompanyName, second.ContactName};
      var result = Query.All<Customer>().Where(c => l==p);
      var list = result.ToList();
    }


    [Test]
    public void ColumnTest()
    {
      var suppliers = Query.All<Supplier>();
      var supplier = suppliers.Where(s => s.CompanyName=="Tokyo Traders").First();
      Assert.IsNotNull(supplier);
      Assert.AreEqual("Tokyo Traders", supplier.CompanyName);
    }

    [Test]
    public void CalculatedTest()
    {
      var expected = Query.All<Product>().ToList().Where(p => p.UnitPrice * p.UnitsInStock >= 100).ToList();
      var actual = Query.All<Product>().Where(p => p.UnitPrice * p.UnitsInStock >= 100).ToList();
      Assert.AreEqual(expected.Count, actual.Count);
    }

    [Test]
    public void StructureTest()
    {
      var suppliers = Query.All<Supplier>();
      var supplier = suppliers.Where(s => s.Address.Region=="Victoria").First();
      Assert.IsNotNull(supplier);
      Assert.AreEqual("Victoria", supplier.Address.Region);
    }

    [Test]
    public void IdTest()
    {
      var suppliers = Query.All<Supplier>();
      var supplier = suppliers.Where(s => s.Id==supplierLekaKey.Value.GetValue<int>(0)).First();
      Assert.IsNotNull(supplier);
      Assert.AreEqual("Leka Trading", supplier.CompanyName);
    }

    [Test]
    public void KeyTest()
    {
      var suppliers = Query.All<Supplier>();
      var key = Key.Create<Supplier>(supplierLekaKey.Value);
      var supplier = suppliers.Where(s => s.Key==key).First();
      Assert.IsNotNull(supplier);
      Assert.AreEqual("Leka Trading", supplier.CompanyName);
    }

    [Test]
    public void InstanceTest()
    {
      var supplierLeka = Query.SingleOrDefault<Supplier>(supplierLekaKey);
      var suppliers = Query.All<Supplier>();
      var supplier = suppliers.Where(s => s==supplierLeka).First();
      Assert.IsNotNull(supplier);
      Assert.AreEqual("Leka Trading", supplier.CompanyName);
    }

    [Test]
    public void ForeignKeyTest()
    {
      var supplierLeka = Query.SingleOrDefault<Supplier>(supplierLekaKey);
      var products = Query.All<Product>();
      var product = products.Where(p => p.Supplier.Key==supplierLeka.Key).First();
      Assert.IsNotNull(product);
      Assert.AreEqual("Leka Trading", product.Supplier.CompanyName);
    }

    [Test]
    public void ForeignIDTest()
    {
      var supplier20 = Query.SingleOrDefault<Supplier>(supplierLekaKey);
      var products = Query.All<Product>();
      var product = products.Where(p => p.Supplier.Id==supplier20.Id).First();
      Assert.IsNotNull(product);
      Assert.AreEqual("Leka Trading", product.Supplier.CompanyName);
    }

    [Test]
    public void ForeignInstanceTest()
    {
      var supplier20 = Query.SingleOrDefault<Supplier>(supplierLekaKey);
      var products = Query.All<Product>();
      var product = products.Where(p => p.Supplier==supplier20).First();
      Assert.IsNotNull(product);
      Assert.AreEqual("Leka Trading", product.Supplier.CompanyName);
    }

    [Test]
    public void ForeignPropertyTest()
    {
      var products = Query.All<Product>();
      var product = products.Where(p => p.Supplier.CompanyName=="Leka Trading").First();
      Assert.IsNotNull(product);
      Assert.AreEqual("Leka Trading", product.Supplier.CompanyName);
      product =
        products.Where(
          p =>
            p.Supplier.CompanyName=="Leka Trading" && p.Category.Key==categoryFirstKey &&
              p.Supplier.ContactTitle=="Owner").First();
      Assert.IsNotNull(product);
      Assert.AreEqual("Leka Trading", product.Supplier.CompanyName);
    }

    [Test]
    public void CoalesceTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => (c.Address.City ?? "Seattle")=="Seattle").First();
      Assert.IsNotNull(customer);
      customer = customers.Where(c => (c.Address.City ?? c.Address.Country ?? "Seattle")=="Seattle").First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void ConditionalTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => (o.Customer.Id=="ALFKI" ? 1000 : 0)==1000).First();
      Assert.IsNotNull(order);
      order =
        orders.Where(o => (o.Customer.Id=="ALFKI" ? 1000 : o.Customer.Id=="ABCDE" ? 2000 : 0)==1000).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void StringLengthTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.Length==7).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringStartsWithLiteralTest()
    {
      var customers = Query.All<Category>();
      var customer = customers.Where(c => c.CategoryName.StartsWith("M")).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringStartsWithColumnTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.ContactName.StartsWith(c.ContactName)).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringEndsWithLiteralTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.ContactName.EndsWith("s")).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringEndsWithColumnTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.ContactName.EndsWith(c.ContactName)).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringContainsLiteralTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.ContactName.Contains("and")).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringContainsColumnTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.ContactName.Contains(c.ContactName)).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringConcatImplicitArgsTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.ContactName + "X"=="X").FirstOrDefault();
      Assert.IsNull(customer);
    }

    [Test]
    public void StringConcatExplicitNArgTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => string.Concat(c.ContactName, "X")=="X").FirstOrDefault();
      Assert.IsNull(customer);
      customer = customers.Where(c => string.Concat(c.ContactName, "X", c.Address.Country)=="X").FirstOrDefault();
      Assert.IsNull(customer);
    }

    [Test]
    public void StringIsNullOrEmptyTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => string.IsNullOrEmpty(c.Address.Region)).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringToUpperTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.ToUpper()=="SEATTLE").First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringToLowerTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.ToLower()=="seattle").First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringReplaceTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.Replace("ea", "ae")=="Saettle").First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringReplaceCharsTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.Replace("e", "y")=="Syattly").First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringSubstringTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.Substring(0, 4)=="Seat").First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringSubstringNoLengthTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.Substring(4)=="tle").First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringRemoveTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.Remove(1, 2)=="Sttle").First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringRemoveNoCountTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.Remove(3)=="Sea").First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringIndexOfTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.IndexOf("tt")==3).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringIndexOfCharTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.IndexOf('t')==3).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringTrimTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.Trim()=="Seattle").First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringToStringTest()
    {
      // just to prove this is a no op
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.ToString()=="Seattle").First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void DateTimeConstructYMDTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.OrderDate >= new DateTime(o.OrderDate.Value.Year, 1, 1)).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DateTimeConstructYMDHMSTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.OrderDate >= new DateTime(o.OrderDate.Value.Year, 1, 1, 10, 25, 55)).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DateTimeTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => ((DateTime?)o.OrderDate) < ((DateTime?)new DateTime(2010, 12, 31))).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DateTimeDayTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.OrderDate.Value.Day==5).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DateTimeMonthTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.OrderDate.Value.Month==12).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DateTimeYearTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.OrderDate.Value.Year==1997).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DateTimeHourTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.OrderDate.Value.Hour==0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DateTimeMinuteTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.OrderDate.Value.Minute==0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DateTimeSecond()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.OrderDate.Value.Second==0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DateTimeMillisecondTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.OrderDate.Value.Millisecond==0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DateTimeDayOfWeekTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.OrderDate.Value.DayOfWeek==DayOfWeek.Friday).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DateTimeDayOfYearTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.OrderDate.Value.DayOfYear==360).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathAbsTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Abs(o.Id)==10 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathAcosTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Acos(Math.Sin(o.Id))==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathAsinTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Asin(Math.Cos(o.Id))==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathAtanTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Atan(o.Id)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
      order = orders.Where(o => Math.Atan2(o.Id, 3)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathCosTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Cos(o.Id)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathSinTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Sin(o.Id)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathTanTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Tan(o.Id)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathExpTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Exp(o.Id < 1000 ? 1 : 2)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathLogTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Log(o.Id)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathLog10Test()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Log10(o.Id)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathSqrtTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Sqrt(o.Id)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathCeilingTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Ceiling((double) o.Id)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathFloorTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Floor((double) o.Id)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathPowTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Pow(o.Id < 1000 ? 1 : 2, 3)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathRoundDefaultTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Round((decimal) o.Id)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathRoundToPlaceTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Round((decimal) o.Id, 2)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void MathTruncateTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => Math.Truncate((double) o.Id)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void StringLessThanTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.LessThan("Seattle")).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringLessThanOrEqualsTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.LessThanOrEqual("Seattle")).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringGreaterThanTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.GreaterThan("Seattle")).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringGreaterThanOrEqualsTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.GreaterThanOrEqual("Seattle")).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringCompareToLTTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.CompareTo("Seattle") < 0).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringCompareToLETest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.CompareTo("Seattle") <= 0).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringCompareToGTTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.CompareTo("Seattle") > 0).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringCompareToGETest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.CompareTo("Seattle") >= 0).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringCompareToEQTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.CompareTo("Seattle")==0).First();
      Assert.IsNotNull(customer);
    }


    [Test]
    public void StringCompareToNETest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City.CompareTo("Seattle")!=0).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringCompareLTTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => string.Compare(c.Address.City, "Seattle") < 0).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringCompareLETest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => string.Compare(c.Address.City, "Seattle") <= 0).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringCompareGTTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => string.Compare(c.Address.City, "Seattle") > 0).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringCompareGETest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => string.Compare(c.Address.City, "Seattle") >= 0).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringCompareEQTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => string.Compare(c.Address.City, "Seattle")==0).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void StringCompareNETest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => string.Compare(c.Address.City, "Seattle")!=0).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void IntCompareToTest()
    {
      // prove that x.CompareTo(y) works for types other than string
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.Id.CompareTo(1000)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DecimalCompareTest()
    {
      // prove that type.Compare(x,y) works with decimal
      var orders = Query.All<Order>();
      var order = orders.Where(o => decimal.Compare((decimal) o.Id, 0.0m)==0 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DecimalAddTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => decimal.Add(o.Id, 0.0m)==0.0m || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DecimalSubtractTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => decimal.Subtract(o.Id, 0.0m)==0.0m || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DecimalMultiplyTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => decimal.Multiply(o.Id, 1.0m)==1.0m || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DecimalDivideTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => decimal.Divide(o.Id, 1.0m)==1.0m || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DecimalRemainderTest()
    {
      Require.ProviderIsNot(StorageProvider.SqlServerCe);
      var orders = Query.All<Order>();
      var order = orders.Where(o => decimal.Remainder(o.Id, 1.0m)==0.0m || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DecimalNegateTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => decimal.Negate(o.Id)==1.0m || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DecimalCeilingTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => decimal.Ceiling(o.Id)==0.0m || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DecimalFloorTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => decimal.Floor(o.Id)==0.0m || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DecimalRoundDefaultTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => decimal.Round(o.Id)==0m || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DecimalRoundPlacesTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => decimal.Round(o.Id, 2)==0.00m || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DecimalTruncateTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => decimal.Truncate(o.Id)==0m || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void DecimalGTTest()
    {
      // prove that decimals are treated normally with respect to normal comparison operators
      var orders = Query.All<Order>();
      var order = orders.Where(o => ((decimal) o.Id) > 0.0m).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void FkCompareTest()
    {
      var employee = Query.All<Employee>().Where(e => e.ReportsTo.Id > 20).First();
      Assert.IsNotNull(employee);
    }

    [Test]
    public void IntLessThanTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.Id < 0).FirstOrDefault();
      Assert.IsNull(order);
    }

    [Test]
    public void IntLessThanOrEqualTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.Id <= 0).FirstOrDefault();
      Assert.IsNull(order);
    }

    [Test]
    public void IntGreaterThanTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void IntGreaterThanOrEqualTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.Id >= 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void IntEqualTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.Id==0).FirstOrDefault();
      Assert.IsNull(order);
    }

    [Test]
    public void IntNotEqualTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.Id!=0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void IntAddTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.Id + 0==0).FirstOrDefault();
      Assert.IsNull(order);
    }

    [Test]
    public void IntSubtractTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.Id - 0==0).FirstOrDefault();
      Assert.IsNull(order);
    }

    [Test]
    public void IntMultiplyTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.Id * 1==1 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void IntDivideTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.Id / 1==1 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void IntModuloTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.Id % 1==0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void IntBitwiseAndTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => (o.Id & 1)==0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void IntBitwiseOrTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => (o.Id | 1)==1 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }


    [Test]
    public void IntBitwiseExclusiveOrTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => (o.Id ^ 1)==1).FirstOrDefault();
      Assert.IsNull(order);
    }

    [Test]
    public void IntBitwiseNotTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => ~o.Id==0).FirstOrDefault();
      Assert.IsNull(order);
    }

    [Test]
    public void IntNegateTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => -o.Id==-1 || o.Id > 0).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void AndTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.Id > 0 && o.Id < 2000).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void OrTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => o.Id < 5 || o.Id > 10).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void NotTest()
    {
      var orders = Query.All<Order>();
      var order = orders.Where(o => !(o.Id==0)).First();
      Assert.IsNotNull(order);
    }

    [Test]
    public void EqualsNullTest()
    {
      //  TODO: Check IsNull or Equals(null)
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => c.Address.City!=null).First();
      Assert.IsNotNull(customer);
      customer = customers.Where(c => !c.Address.Region.Equals(null)).First();
      Assert.IsNotNull(customer);
      customer = customers.Where(c => c!=null).First();
      Assert.IsNotNull(customer);
      customer = customers.Where(c => !c.Equals(null)).First();
      Assert.IsNotNull(customer);
      customer = customers.Where(c => c.Address!=null).First();
      Assert.IsNotNull(customer);
      customer = customers.Where(c => !c.Address.Equals(null)).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void EqualNullReverseTest()
    {
      var customers = Query.All<Customer>();
      var customer = customers.Where(c => null!=c.Address.City).First();
      Assert.IsNotNull(customer);
    }

    [Test]
    public void TimeSpanTest()
    {
      var maxProcessingTime = new TimeSpan(5, 0, 0, 0);
      Query.All<Order>().Where(o => o.ProcessingTime > maxProcessingTime).ToList();
    }

    [Test]
    [ExpectedException(typeof (QueryTranslationException))]
    public void NonPersistentFieldTest()
    {
      var result = from e in Query.All<Employee>() where e.FullName!=null select e;
      result.ToList();
    }

    [Test]
    public void JoinTest()
    {
      var actual = from customer in Query.All<Customer>()
      join order in Query.All<Order>() on customer equals order.Customer
      where order.Freight > 30
      orderby new {customer, order}
      select new {customer, order};
      var expected = from customer in Query.All<Customer>().ToList()
      join order in Query.All<Order>().ToList() on customer equals order.Customer
      where order.Freight > 30
      orderby customer.Id , order.Id
      select new {customer, order};
      Assert.IsTrue(expected.SequenceEqual(actual));
    }

    [Test]
    public void ApplyTest()
    {
      var actual = Query.All<Customer>()
        .Where(customer => customer.Orders.Any(o => o.Freight > 30));
      var expected = Query.All<Customer>()
        .AsEnumerable() // AG: Just to remeber about it.
        .Where(customer => customer.Orders.Any(o => o.Freight > 30));
      Assert.IsTrue(expected.SequenceEqual(actual));
    }
  }
}