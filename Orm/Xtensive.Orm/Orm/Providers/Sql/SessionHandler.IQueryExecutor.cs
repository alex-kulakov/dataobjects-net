// Copyright (C) 2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2010.02.09

using System.Collections.Generic;
using Xtensive.Tuples;
using Tuple = Xtensive.Tuples.Tuple;
using Xtensive.Sql;

namespace Xtensive.Orm.Providers.Sql
{
  public partial class SessionHandler
  {
    // Implementation of IQueryExecutor

    
    IEnumerator<Tuple> IQueryExecutor.ExecuteTupleReader(QueryRequest request)
    {
      EnsureConnectionIsOpen();
      var enumerator = commandProcessor.ExecuteTasksWithReader(request);
      using (enumerator) {
        while (enumerator.MoveNext())
          yield return enumerator.Current;
      }
    }

    
    int IQueryExecutor.ExecuteNonQuery(ISqlCompileUnit statement)
    {
      EnsureConnectionIsOpen();
      using (var command = connection.CreateCommand(statement))
        return driver.ExecuteNonQuery(Session, command);
    }

    
    object IQueryExecutor.ExecuteScalar(ISqlCompileUnit statement)
    {
      EnsureConnectionIsOpen();
      using (var command = connection.CreateCommand(statement))
        return driver.ExecuteScalar(Session, command);
    }

    
    int IQueryExecutor.ExecuteNonQuery(string commandText)
    {
      EnsureConnectionIsOpen();
      using (var command = connection.CreateCommand(commandText))
        return driver.ExecuteNonQuery(Session, command);
    }

    
    object IQueryExecutor.ExecuteScalar(string commandText)
    {
      EnsureConnectionIsOpen();
      using (var command = connection.CreateCommand(commandText))
        return driver.ExecuteScalar(Session, command);
    }

    
    void IQueryExecutor.Store(TemporaryTableDescriptor descriptor, IEnumerable<Tuple> tuples)
    {
      EnsureConnectionIsOpen();
      foreach (var tuple in tuples)
        commandProcessor.Tasks.Enqueue(new SqlPersistTask(descriptor.StoreRequest, tuple));
      commandProcessor.ExecuteTasks();
    }

    
    void IQueryExecutor.Clear(TemporaryTableDescriptor descriptor)
    {
      EnsureConnectionIsOpen();
      commandProcessor.Tasks.Enqueue(new SqlPersistTask(descriptor.ClearRequest, null));
      commandProcessor.ExecuteTasks();
    }
  }
}