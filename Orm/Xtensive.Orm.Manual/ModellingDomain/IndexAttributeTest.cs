// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Kofman
// Created:    2009.06.17

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Xtensive.Orm.Tests;

namespace Xtensive.Orm.Manual.ModellingDomain.IndexAttribute_
{
  #region Model

  [Serializable]
  [Index("FirstName", "LastName", Unique = true)]
  [Index("Age")]
  [HierarchyRoot]
  public class Person : Entity
  {
    [Field, Key]
    public int Id { get; private set; }

    [Field]
    public string FirstName { get; set; }

    [Field]
    public string LastName { get; set; }
     
    [Field]
    public int Age { get; set; }

    public Person(Session session)
      : base(session)
    {}
  }

  #endregion

  [TestFixture]
  public class IndexAttributeTest
  {
    [Test]
    public void MainTest()
    {
      var config = DomainConfigurationFactory.CreateWithoutSessionConfigurations();
      config.UpgradeMode = DomainUpgradeMode.Recreate;
      config.Types.Register(typeof(Person));
      var domain = Domain.Build(config);

      using (var session = domain.OpenSession()) {
        using (var transactionScope = session.OpenTransaction()) {
          var alex = new Person (session) { FirstName = "Alex", LastName = "Kofman", Age = 26};
          var ivan = new Person (session) { FirstName = "Ivan", LastName = "Galkin", Age = 28};

          Assert.AreEqual(alex, GetPersonByName(session, "Alex", "Kofman"));
          
          var adults = GetPersonOverAge(session, 27);
          Assert.AreEqual(1, adults.Count());
          Assert.AreEqual(ivan, adults.First());

          transactionScope.Complete();
        }

        AssertEx.Throws<StorageException>(() => {
          using (var transactionScope = session.OpenTransaction()) {
            new Person (session) {FirstName = "Alex", LastName = "Kofman", Age = 0};
            transactionScope.Complete();
          }
        });
      }
    }

    public IEnumerable<Person> GetPersonOverAge(Session session, int age)
    {
      // Filtering and ordering uses secondary index
      return
        from person in session.Query.All<Person>()
        where person.Age > age
        orderby person.Age
        select person;
    }

    public Person GetPersonByName(Session session, string firstName, string lastName)
    {
      // Filtering uses unique secondary index
      return (
        from person in session.Query.All<Person>()
        where person.FirstName==firstName && person.LastName==lastName
        select person
        ).FirstOrDefault();
    }
  }
}