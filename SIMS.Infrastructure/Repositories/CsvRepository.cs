using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Infrastructure.Repositories;

/// <summary>
/// File-backed <see cref="IGenericRepository{T}"/> that stores one entity per row in a CSV file
/// (header row, RFC 4180 quoting). Only public read/write properties of simple types are persisted
/// (string, numbers, bool, Guid, DateTime, enums and their nullable forms); navigation properties are skipped.
/// The file is loaded lazily on first use and rewritten after every mutating call (there is no separate commit step).
/// An empty field is read back as null for nullable types and as an empty string for string properties.
/// </summary>
public class CsvRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    private static readonly PropertyInfo[] Columns = typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0 && IsSimple(p.PropertyType))
        .ToArray();

    private static readonly char[] SpecialChars = { ',', '"', '\r', '\n' };

    private readonly string _filePath;
    private readonly object _gate = new();
    private List<T>? _items;

    public CsvRepository(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public Task<T?> GetByIdAsync(Guid id)
    {
        lock (_gate)
        {
            return Task.FromResult(Load().FirstOrDefault(e => e.Id == id));
        }
    }

    public Task<IEnumerable<T>> GetAllAsync()
    {
        lock (_gate)
        {
            return Task.FromResult<IEnumerable<T>>(Load().ToList());
        }
    }

    public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression)
    {
        var predicate = expression.Compile();
        lock (_gate)
        {
            return Task.FromResult<IEnumerable<T>>(Load().Where(predicate).ToList());
        }
    }

    public Task AddAsync(T entity) => AddRangeAsync(new[] { entity });

    public Task AddRangeAsync(IEnumerable<T> entities)
    {
        var list = entities.ToList();
        lock (_gate)
        {
            var items = Load();
            var ids = new HashSet<Guid>(items.Select(e => e.Id));
            foreach (var entity in list)
            {
                if (!ids.Add(entity.Id))
                    throw new InvalidOperationException($"An entity with Id {entity.Id} already exists.");
            }
            items.AddRange(list);
            Save(items);
        }
        return Task.CompletedTask;
    }

    public void Update(T entity)
    {
        lock (_gate)
        {
            var items = Load();
            var index = items.FindIndex(e => e.Id == entity.Id);
            if (index < 0)
                throw new InvalidOperationException($"No entity with Id {entity.Id} exists.");
            items[index] = entity;
            Save(items);
        }
    }

    public void Remove(T entity) => RemoveRange(new[] { entity });

    public void RemoveRange(IEnumerable<T> entities)
    {
        var ids = entities.Select(e => e.Id).ToHashSet();
        lock (_gate)
        {
            var items = Load();
            if (items.RemoveAll(e => ids.Contains(e.Id)) > 0)
                Save(items);
        }
    }

    // ---- persistence -----------------------------------------------------------------------

    private List<T> Load()
    {
        if (_items != null) return _items;

        var items = new List<T>();
        if (File.Exists(_filePath))
        {
            var records = ParseCsv(File.ReadAllText(_filePath, Encoding.UTF8));
            if (records.Count > 0)
            {
                var header = records[0];
                var map = Columns.Select(c => (Column: c, Index: Array.IndexOf(header, c.Name))).ToList();
                foreach (var record in records.Skip(1))
                {
                    var entity = Activator.CreateInstance<T>();
                    foreach (var (column, index) in map)
                    {
                        if (index < 0 || index >= record.Length) continue;
                        column.SetValue(entity, ConvertFromText(record[index], column.PropertyType));
                    }
                    items.Add(entity);
                }
            }
        }
        return _items = items;
    }

    private void Save(List<T> items)
    {
        var sb = new StringBuilder();
        sb.Append(string.Join(",", Columns.Select(c => Escape(c.Name)))).Append("\r\n");
        foreach (var item in items)
            sb.Append(string.Join(",", Columns.Select(c => Escape(ConvertToText(c.GetValue(item)))))).Append("\r\n");

        var dir = Path.GetDirectoryName(Path.GetFullPath(_filePath));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        // Write to a temp file first so a failed write cannot truncate the existing data.
        var tmp = _filePath + ".tmp";
        File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(false));
        File.Move(tmp, _filePath, overwrite: true);
    }

    // ---- RFC 4180 -------------------------------------------------------------------------

    internal static string Escape(string field) =>
        field.IndexOfAny(SpecialChars) >= 0
            ? "\"" + field.Replace("\"", "\"\"") + "\""
            : field;

    internal static List<string[]> ParseCsv(string text)
    {
        var records = new List<string[]>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var fieldStarted = false;

        void EndField() { fields.Add(field.ToString()); field.Clear(); fieldStarted = false; }
        void EndRecord()
        {
            EndField();
            // A record consisting of a single empty field is a blank line; skip it.
            if (!(fields.Count == 1 && fields[0].Length == 0)) records.Add(fields.ToArray());
            fields.Clear();
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else field.Append(c);
            }
            else if (c == '"' && !fieldStarted) { inQuotes = true; fieldStarted = true; }
            else if (c == ',') EndField();
            else if (c == '\r') { if (i + 1 < text.Length && text[i + 1] == '\n') i++; EndRecord(); }
            else if (c == '\n') EndRecord();
            else { field.Append(c); fieldStarted = true; }
        }

        if (inQuotes) throw new FormatException("CSV ends inside a quoted field.");
        if (fieldStarted || field.Length > 0 || fields.Count > 0) EndRecord();
        return records;
    }

    // ---- value conversion -------------------------------------------------------------------

    private static bool IsSimple(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(Guid)
            || t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(TimeSpan);
    }

    private static string ConvertToText(object? value) => value switch
    {
        null => "",
        string s => s,
        DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? ""
    };

    private static object? ConvertFromText(string text, Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (type == typeof(string)) return text;
        if (text.Length == 0) return underlying != null ? null : Activator.CreateInstance(type);

        var t = underlying ?? type;
        if (t == typeof(Guid)) return Guid.Parse(text);
        if (t == typeof(DateTime)) return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        if (t == typeof(DateTimeOffset)) return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        if (t == typeof(TimeSpan)) return TimeSpan.Parse(text, CultureInfo.InvariantCulture);
        if (t.IsEnum) return Enum.Parse(t, text);
        return Convert.ChangeType(text, t, CultureInfo.InvariantCulture);
    }
}
