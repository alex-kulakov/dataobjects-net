// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Gamzov
// Created:    2008.05.20

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Xtensive.Core.Disposable;
using Xtensive.Core.Tuples;
using Xtensive.Sql.Dom;
using Xtensive.Storage.Providers.Sql.Resources;

namespace Xtensive.Storage.Providers.Sql
{
  public abstract class SessionHandler : Providers.SessionHandler
  {
    private SqlConnection connection;
    private DomainHandler domainHandler;

    /// <summary>
    /// Gets the connection.
    /// </summary>
    public SqlConnection Connection {
      get {
        EnsureConnectionIsOpen();
        return connection;
      }
    }

    /// <summary>
    /// Gets the active transaction.
    /// </summary>    
    public DbTransaction Transaction { get; private set; }

    protected internal DomainHandler DomainHandler
    {
      get { return domainHandler; }
      set { domainHandler = value; }
    }

    /// <inheritdoc/>
    public override void BeginTransaction()
    {
      EnsureConnectionIsOpen();

      if (Transaction!=null)
        throw new InvalidOperationException(Strings.TransactionIsAlreadyOpen);

      Transaction = connection.BeginTransaction();
    }

    /// <inheritdoc/>
    public override void CommitTransaction()
    {
      if (Transaction==null)
        throw new InvalidOperationException(Strings.TransactionIsNotOpen);

      Transaction.Commit();
      Transaction = null;
    }

    /// <inheritdoc/>
    public override void RollbackTransaction()
    {
      if (Transaction==null)
        throw new InvalidOperationException(Strings.TransactionIsNotOpen);

      Transaction.Rollback();
      Transaction = null;
    }

    public IEnumerator<Tuple> Execute(SqlFetchRequest request)
    {
      using (DbDataReader reader = ExecuteReader(request)) {
        Tuple tuple;
        while ((tuple = ReadTuple(reader, request.TupleDescriptor))!=null)
          yield return tuple;
      }
    }

    protected virtual Tuple ReadTuple(DbDataReader reader, TupleDescriptor tupleDescriptor)
    {
      if (!reader.Read())
        return null;
      var tuple = Tuple.Create(tupleDescriptor);
      for (int i = 0; i < reader.FieldCount; i++) {
        var value = reader[i];
        tuple.SetValue(i, DBNull.Value==value ? null : value);
      }
      return tuple;
    }

    public virtual int ExecuteNonQuery(ISqlCompileUnit statement)
    {
      EnsureConnectionIsOpen();
      using (var command = new SqlCommand(connection)) {
        command.Statement = statement;
        command.Prepare();
        command.Transaction = Transaction;
        return command.ExecuteNonQuery();
      }
    }

    public virtual int ExecuteNonQuery(SqlModificationRequest request)
    {
      EnsureConnectionIsOpen();
      using (var command = new SqlCommand(connection)) {
        command.CommandText = request.CompiledStatement;
        command.Parameters.AddRange(request.GetParameters().ToArray());
        command.Prepare();
        command.Transaction = Transaction;
        return command.ExecuteNonQuery();
      }
    }

    public virtual object ExecuteScalar(SqlScalarRequest request)
    {
      EnsureConnectionIsOpen();
      using (var command = new SqlCommand(connection)) {
        command.CommandText = request.CompiledStatement;
        List<SqlParameter> parameters = request.GetParameters();
        if (parameters != null)
          command.Parameters.AddRange(parameters.ToArray());
        command.Prepare();
        command.Transaction = Transaction;
        return command.ExecuteScalar();
      }
    }

    public virtual object ExecuteScalar(ISqlCompileUnit statement)
    {
      EnsureConnectionIsOpen();
      using (var command = new SqlCommand(connection)) {
        command.Statement = statement;
        command.Prepare();
        command.Transaction = Transaction;
        return command.ExecuteScalar();
      }
    }

    public virtual DbDataReader ExecuteReader(SqlFetchRequest request)
    {
      EnsureConnectionIsOpen();
      using (var command = new SqlCommand(connection)) {
        command.CommandText = request.CompiledStatement;
        command.Parameters.AddRange(request.GetParameters().ToArray());
        command.Prepare();
        command.Transaction = Transaction;
        return command.ExecuteReader();
      }
    }

    /// <inheritdoc/>
    protected override void Insert(EntityData data)
    {
      SqlRequestBuilderTask task = new SqlRequestBuilderTask(SqlModificationRequestKind.Insert, data.Type);
      SqlModificationRequest request = domainHandler.SqlRequestCache.GetValue(task, _task => DomainHandler.SqlRequestBuilder.BuildRequest(_task));
      request.BindParametersTo(data);
      int rowsAffected = ExecuteNonQuery(request);
      if (rowsAffected!=request.ExpectedResult)
        throw new InvalidOperationException(String.Format(Strings.ExErrorOnInsert, data.Type.Name, rowsAffected, request.ExpectedResult));
    }

    /// <inheritdoc/>
    protected override void Update(EntityData data)
    {
      SqlRequestBuilderTask task = new SqlRequestBuilderTask(SqlModificationRequestKind.Update, data.Type, data.Values.Difference.GetFieldStateMap(TupleFieldState.IsAvailable));
      SqlModificationRequest request = domainHandler.SqlRequestCache.GetValue(task, _task => DomainHandler.SqlRequestBuilder.BuildRequest(_task));
      request.BindParametersTo(data);
      int rowsAffected = ExecuteNonQuery(request);
      if (rowsAffected!=request.ExpectedResult)
        throw new InvalidOperationException(String.Format(Strings.ExErrorOnUpdate, data.Type.Name, rowsAffected, request.ExpectedResult));
    }

    /// <inheritdoc/>
    protected override void Remove(EntityData data)
    {
      SqlRequestBuilderTask task = new SqlRequestBuilderTask(SqlModificationRequestKind.Remove, data.Type);
      SqlModificationRequest request = domainHandler.SqlRequestCache.GetValue(task, _task => DomainHandler.SqlRequestBuilder.BuildRequest(_task));
      request.BindParametersTo(data);
      int rowsAffected = ExecuteNonQuery(request);
      if (rowsAffected!=request.ExpectedResult)
        if (rowsAffected==0)
          throw new InvalidOperationException(String.Format(Strings.ExInstanceNotFound, data.Key.Type.Name));
        else
          throw new InvalidOperationException(String.Format(Strings.ExInstanceMultipleResults, data.Key.Type.Name));
    }

    public void EnsureConnectionIsOpen()
    {
      if (connection==null || connection.State!=ConnectionState.Open) {
        connection = ((DomainHandler) Handlers.DomainHandler).ConnectionProvider.CreateConnection(Handlers.Domain.Configuration.ConnectionInfo.ToString()) as SqlConnection;
        if (connection==null)
          throw new InvalidOperationException(Strings.ExUnableToCreateConnection);
        connection.Open();
      }
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
      DomainHandler = Handlers.DomainHandler as DomainHandler;
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
      connection.DisposeSafely();
    }
  }
}