// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Malisa Ncube
// Created:    2011.04.29

using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Security;
using Xtensive.Sql.Info;
using Xtensive.Sql;

namespace Xtensive.Sql.Drivers.Sqlite.v3
{
  internal class TypeMapper : Sql.TypeMapper
  {
    private const int BooleanPrecision = 1;
    private ValueRange<DateTime> dateTimeRange;

    public override object ReadBoolean(DbDataReader reader, int index)
    {
      var value = reader.GetDecimal(index);
      return SQLiteConvert.ToBoolean(value);
    }

    public override void BindSByte(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Int16;
      parameter.Value = value ?? DBNull.Value;
    }

    public override void BindUShort(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Int32;
      parameter.Value = value ?? DBNull.Value;
    }

    public override void BindUInt(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Int64;
      parameter.Value = value ?? DBNull.Value;
    }

    public override void BindULong(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Decimal;
      parameter.Value = value ?? DBNull.Value;
    }

    public override void BindDateTime(DbParameter parameter, object value)
    {
      if (value!=null)
        value = ValueRangeValidator.Correct((DateTime) value, dateTimeRange);
      base.BindDateTime(parameter, value);
    }

    public override void BindTimeSpan(DbParameter parameter, object value)
    {
      parameter.DbType = DbType.Int64;
      if (value!=null) {
        var timeSpan = (TimeSpan) value;
        parameter.Value = timeSpan.Ticks*100;
      }
      else
        parameter.Value = DBNull.Value;
    }

    public override SqlValueType MapDecimal(int? length, int? precision, int? scale)
    {
      return new SqlValueType(SqlType.Decimal);
    }

    public override SqlValueType MapChar(int? length, int? precision, int? scale)
    {
      return new SqlValueType(SqlType.VarCharMax);
    }

    public override SqlValueType MapString(int? length, int? precision, int? scale)
    {
      return new SqlValueType(SqlType.VarCharMax);
    }

    public override SqlValueType MapByteArray(int? length, int? precision, int? scale)
    {
      return new SqlValueType(SqlType.VarBinaryMax);
    }

    public override SqlValueType MapSByte(int? length, int? precision, int? scale)
    {
      return new SqlValueType(SqlType.Int16);
    }

    public override SqlValueType MapUShort(int? length, int? precision, int? scale)
    {
      return new SqlValueType(SqlType.Int32);
    }

    public override SqlValueType MapUInt(int? length, int? precision, int? scale)
    {
      return new SqlValueType(SqlType.Int64);
    }

    public override SqlValueType MapULong(int? length, int? precision, int? scale)
    {
      return new SqlValueType(SqlType.Int64);
    }

    public override SqlValueType MapTimeSpan(int? length, int? precision, int? scale)
    {
      return new SqlValueType(SqlType.Int64);
    }

    public override object ReadTimeSpan(DbDataReader reader, int index)
    {
      long value;
      try {
        value = reader.GetInt64(index);
      }
      catch (InvalidCastException) {
        value = (long) reader.GetDecimal(index);
      }
      return TimeSpan.FromTicks(value / 100);
    }

    public override void Initialize()
    {
      base.Initialize();
      dateTimeRange = (ValueRange<DateTime>) Driver.ServerInfo.DataTypes.DateTime.ValueRange;
    }


    // Constructors

    public TypeMapper(SqlDriver driver)
      : base(driver)
    {
    }
  }
}