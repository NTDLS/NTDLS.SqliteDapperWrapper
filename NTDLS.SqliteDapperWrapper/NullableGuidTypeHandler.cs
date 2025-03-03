﻿using Dapper;
using System.Data;

namespace NTDLS.SqliteDapperWrapper
{
    /// <summary>
    /// Used to handle mappings for Text->Guid?.
    /// </summary>
    internal class NullableGuidTypeHandler : SqlMapper.TypeHandler<Guid?>
    {
        public override Guid? Parse(object? value)
        {
            return Guid.Parse(value?.ToString()?.ToLower()!);
        }

        public override void SetValue(IDbDataParameter parameter, Guid? value)
        {
            parameter.Value = value?.ToString().ToLower();
            parameter.DbType = DbType.String;
        }
    }
}
