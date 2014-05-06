// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.

using System;

namespace Xtensive.Sql.Dml
{
  [Serializable]
  public enum SqlFunctionType
  {
    Concat,
    CurrentDate,
    CurrentDateTimeOffset,
    CurrentTime,
    CurrentTimeStamp,
    Lower,
    CharLength,
    BinaryLength,
    Position,
    Replace,
    Substring,
    Upper,
    UserDefined,
    CurrentUser,
    SessionUser,
    SystemUser,
    User,
    NullIf,
    Coalesce,
    LastAutoGeneratedId,
    PadLeft,
    PadRight,

    // mathematical functions
    Abs,
    Acos,
    Asin,
    Atan,
    Atan2,
    Ceiling,
    Cos,
    Cot,
    Degrees,
    Exp,
    Floor,
    Log,
    Log10,
    Pi,
    Power,
    Radians,
    Rand,
    Round,
    Truncate,
    Sign,
    Sin,
    Sqrt,
    Square,
    Tan,

    // date time / interval functions
    // not ansi sql but our cross-server solution

    DateTimeConstruct,
    DateTimeAddYears,
    DateTimeAddMonths,
    DateTimeTruncate,
    DateTimeToStringIso,
    IntervalConstruct,
    IntervalToMilliseconds,
    IntervalToNanoseconds,
    IntervalAbs,
    IntervalNegate,

    // DateTimeOffset / interval functions
    DateTimeOffsetConstruct,
    DateTimeOffsetAddYears,
    DateTimeOffsetAddMonths,
    DateTimeOffsetTimeOfDay,
    DateTimeOffsetToLocalTime, 
    DateTimeOffsetToUtcTime,
    DateTimeToDateTimeOffset,

    // .NET like rounding functions

    RoundDecimalToEven,
    RoundDecimalToZero,
    RoundDoubleToEven,
    RoundDoubleToZero,

    // Functions of the spatial types

    // Npgsql
    NpgsqlTypeExtractPoint,

    // NpgsqlPoint
    NpgsqlPointExtractX,
    NpgsqlPointExtractY,

    // NpgsqlBox
    NpgsqlBoxExtractHeight,
    NpgsqlBoxExtractWidth,
  }
}
