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