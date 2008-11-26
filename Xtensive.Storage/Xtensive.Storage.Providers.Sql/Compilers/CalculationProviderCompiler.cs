// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Elena Vakhtina
// Created:    2008.09.30

using Xtensive.Sql.Dom.Dml;
using Xtensive.Storage.Providers.Sql.Expressions;
using Xtensive.Storage.Rse.Providers;
using Xtensive.Storage.Rse.Providers.Compilable;
using SqlFactory = Xtensive.Sql.Dom.Sql;


namespace Xtensive.Storage.Providers.Sql.Compilers
{
  internal class CalculationProviderCompiler : TypeCompiler<CalculationProvider>
  {
    protected override ExecutableProvider Compile(CalculationProvider provider, params ExecutableProvider[] compiledSources)
    {
      var source = compiledSources[0] as SqlProvider;
      if (source==null)
        return null;

      var sqlSelect = (SqlSelect) source.Request.Statement.Clone();
      var request = new SqlFetchRequest(sqlSelect, provider.Header, source.Request.ParameterBindings);
      var visitor = new ExpressionProcessor(request);

      foreach (var column in provider.CalculatedColumns)
        visitor.AppendCalculatedColumnToRequest(column.Expression, column.Name);

      return new SqlProvider(provider, request, Handlers, source);
    }


    // Constructor

    public CalculationProviderCompiler(Rse.Compilation.Compiler compiler)
      : base(compiler)
    {
    }
  }
}