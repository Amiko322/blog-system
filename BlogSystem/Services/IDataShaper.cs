using System.Dynamic;
using System.Reflection;

namespace BlogSystem.Services;

public interface IDataShaper<T>
{
    IEnumerable<ExpandoObject> ShapeData(
        IEnumerable<T> entities,
        string fields);

    ExpandoObject ShapeData(
        T entity,
        string fields);
}

public class DataShaper<T> : IDataShaper<T>
{
    public PropertyInfo[] Properties { get; set; }

    public DataShaper()
    {
        Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }

    public IEnumerable<ExpandoObject> ShapeData(
        IEnumerable<T> entities,
        string fields)
    {
        IEnumerable<PropertyInfo> props = GetRequiredProperties(fields);

        return FetchData(entities, props);
    }

    public ExpandoObject ShapeData(
        T entity,
        string fields)
    {
        IEnumerable<PropertyInfo> props = GetRequiredProperties(fields);

        return FetchData(entity, props);
    }

    private static ExpandoObject FetchData(
        T entity,
        IEnumerable<PropertyInfo> props)
    {
        ExpandoObject shapedObject = new();

        // Создание свойства info.Name со значением info.GetValue(entity)
        foreach (PropertyInfo prop in props)
        {
            object? value = prop.GetValue(entity);

            shapedObject.TryAdd(
                prop.Name,
                value);
        }

        return shapedObject;
    }

    private static IEnumerable<ExpandoObject> FetchData(
        IEnumerable<T> entities,
        IEnumerable<PropertyInfo> props)
    {
        List<ExpandoObject> shapedData = [];

        foreach (T entity in entities)
        {
            ExpandoObject shapedObject = FetchData(entity, props);
            shapedData.Add(shapedObject);
        }

        return shapedData;
    }

    private IEnumerable<PropertyInfo> GetRequiredProperties(string fields)
    {
        // Обязательные опциональные поля для ответа
        List<PropertyInfo> props = [];

        if (!string.IsNullOrWhiteSpace(fields))
        {
            // Получение всех свойств из строки запроса
            string[] strings = fields.Split(
                ",",
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            // Check: существует ли свойство prop из строки запроса в сущности T
            foreach (string field in strings)
            {
                // Equals используется для сравнения строк с  режимом сравнения StringComparison
                // OrdinalIgnoreCase - режим сравнения строк, который выполняет сравнение
                // без учёта регистра (A == a) и без влияния культуры
                PropertyInfo? prop = Properties
                    .FirstOrDefault(i => i.Name.Equals(field.Trim(), StringComparison.OrdinalIgnoreCase));

                if (prop == null)
                    continue;

                props.Add(prop);
            }
        }
        else
        {
            props = Properties.ToList();
        }

        return props;
    }
}
