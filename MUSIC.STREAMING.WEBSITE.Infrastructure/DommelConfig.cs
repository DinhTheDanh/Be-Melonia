using Dommel;
using System.Reflection;

namespace MUSIC.STREAMING.WEBSITE.Infrastructure;

public class SnakeCaseColumnNameResolver : IColumnNameResolver
{
    public string ResolveColumnName(PropertyInfo propertyInfo)
    {
        return string.Concat(propertyInfo.Name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
    }
}

public class SnakeCaseTableNameResolver : ITableNameResolver
{
    public string ResolveTableName(Type type)
    {
        // PascalCase -> snake_case
        var name = string.Concat(type.Name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        // Pluralize: nếu đã kết thúc bằng 's' thì không thêm nữa
        if (!name.EndsWith("s"))
            name += "s";
        return name;
    }
}