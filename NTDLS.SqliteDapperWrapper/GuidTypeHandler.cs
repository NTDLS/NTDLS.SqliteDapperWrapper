using Dapper;
using System.Data;

namespace NTDLS.SqliteDapperWrapper
{
    internal class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value)
        {
            return Guid.Parse(value?.ToString()?.ToLower()!);
        }

        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value.ToString().ToLower();
            parameter.DbType = DbType.String;
        }
    }
}
