using Dapper;
using System.Data;

namespace DataBridge.Connectors.SQLite;

/// <summary>
/// Registers Dapper type handlers needed for SQLite, which stores all values as TEXT.
/// Call SQLiteTypeHandlers.Register() once at application startup.
/// </summary>
public static class SQLiteTypeHandlers
{
    public static void Register()
    {
        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new NullableGuidHandler());
        SqlMapper.AddTypeHandler(new DateTimeHandler());
        SqlMapper.AddTypeHandler(new NullableDateTimeHandler());
    }
}

public class GuidHandler : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value)
        => Guid.Parse(value.ToString()!);

    public override void SetValue(IDbDataParameter parameter, Guid value)
        => parameter.Value = value.ToString();
}

public class NullableGuidHandler : SqlMapper.TypeHandler<Guid?>
{
    public override Guid? Parse(object value)
        => value == null || value is DBNull ? null : Guid.Parse(value.ToString()!);

    public override void SetValue(IDbDataParameter parameter, Guid? value)
        => parameter.Value = value.HasValue ? value.Value.ToString() : DBNull.Value;
}

public class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override DateTime Parse(object value)
        => DateTime.Parse(value.ToString()!);

    public override void SetValue(IDbDataParameter parameter, DateTime value)
        => parameter.Value = value.ToString("O");
}

public class NullableDateTimeHandler : SqlMapper.TypeHandler<DateTime?>
{
    public override DateTime? Parse(object value)
        => value == null || value is DBNull ? null : DateTime.Parse(value.ToString()!);

    public override void SetValue(IDbDataParameter parameter, DateTime? value)
        => parameter.Value = value.HasValue ? value.Value.ToString("O") : DBNull.Value;
}
