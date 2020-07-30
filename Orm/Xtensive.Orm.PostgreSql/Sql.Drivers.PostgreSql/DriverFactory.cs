// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.06.23

using System;
using System.Data.Common;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Xtensive.Orm;
using Xtensive.Sql.Info;
using Xtensive.Sql.Drivers.PostgreSql.Resources;

namespace Xtensive.Sql.Drivers.PostgreSql
{
  /// <summary>
  /// A <see cref="SqlDriver"/> factory for PostgreSQL.
  /// </summary>
  public class DriverFactory : SqlDriverFactory
  {
    private const string DatabaseAndSchemaQuery = "select current_database(), current_schema()";

    /// <inheritdoc/>
    [SecuritySafeCritical]
    protected override string BuildConnectionString(UrlInfo url)
    {
      SqlHelper.ValidateConnectionUrl(url);

      var builder = new NpgsqlConnectionStringBuilder();

      // host, port, database
      builder.Host = url.Host;
      if (url.Port!=0) {
        builder.Port = url.Port;
      }

      builder.Database = url.Resource ?? string.Empty;

      // user, password
      if (!string.IsNullOrEmpty(url.User)) {
        builder.Username = url.User;
        builder.Password = url.Password;
      }
      else {
        builder.IntegratedSecurity = true;
      }

      // custom options
      foreach (var param in url.Params) {
        builder[param.Key] = param.Value;
      }

      return builder.ToString();
    }

    /// <inheritdoc/>
    [SecuritySafeCritical]
    protected override SqlDriver CreateDriver(string connectionString, SqlDriverConfiguration configuration)
    {
      using var connection = new NpgsqlConnection(connectionString);
      connection.Open();
      SqlHelper.ExecuteInitializationSql(connection, configuration);
      var version = GetVersion(configuration, connection);
      var defaultSchema = GetDefaultSchema(connection);
      return CreateDriverInstance(connectionString, version, defaultSchema);
    }

    /// <inheritdoc/>
    protected override async Task<SqlDriver> CreateDriverAsync(
      string connectionString, SqlDriverConfiguration configuration, CancellationToken token)
    {
      var connection = new NpgsqlConnection(connectionString);
      await using (connection.ConfigureAwait(false)) {
        await connection.OpenAsync(token).ConfigureAwait(false);
        await SqlHelper.ExecuteInitializationSqlAsync(connection, configuration, token).ConfigureAwait(false);
        var version = GetVersion(configuration, connection);
        var defaultSchema = await GetDefaultSchemaAsync(connection, token: token).ConfigureAwait(false);
        return CreateDriverInstance(connectionString, version, defaultSchema);
      }
    }

    private static Version GetVersion(SqlDriverConfiguration configuration, NpgsqlConnection connection)
    {
      var version = string.IsNullOrEmpty(configuration.ForcedServerVersion)
        ? connection.PostgreSqlVersion
        : new Version(configuration.ForcedServerVersion);
      return version;
    }

    private static SqlDriver CreateDriverInstance(
      string connectionString, Version version, DefaultSchemaInfo defaultSchema)
    {
      var coreServerInfo = new CoreServerInfo {
        ServerVersion = version,
        ConnectionString = connectionString,
        MultipleActiveResultSets = false,
        DatabaseName = defaultSchema.Database,
        DefaultSchemaName = defaultSchema.Schema,
      };

      if (version.Major < 8 || (version.Major == 8 && version.Minor < 3)) {
        throw new NotSupportedException(Strings.ExPostgreSqlBelow83IsNotSupported);
      }

      // We support 8.3, 8.4 and any 9.0+

      if (version.Major == 8) {
        return version.Minor == 3 ? new v8_3.Driver(coreServerInfo) : new v8_4.Driver(coreServerInfo);
      }

      if (version.Major == 9) {
        return version.Minor == 0 ? new v9_0.Driver(coreServerInfo) : new v9_1.Driver(coreServerInfo);
      }

      if (version.Major < 12) {
        return new v10_0.Driver(coreServerInfo);
      }

      return new v12_0.Driver(coreServerInfo);
    }

    /// <inheritdoc/>
    protected override DefaultSchemaInfo ReadDefaultSchema(DbConnection connection, DbTransaction transaction) =>
      SqlHelper.ReadDatabaseAndSchema(DatabaseAndSchemaQuery, connection, transaction);

    /// <inheritdoc/>
    protected override Task<DefaultSchemaInfo> ReadDefaultSchemaAsync(
      DbConnection connection, DbTransaction transaction, CancellationToken token) =>
      SqlHelper.ReadDatabaseAndSchemaAsync(DatabaseAndSchemaQuery, connection, transaction, token);
  }
}