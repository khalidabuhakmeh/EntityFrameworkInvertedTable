// See https://aka.ms/new-console-template for more information


using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

var storing = new Database();

storing.Database.Migrate();

var json = new EntityWithJson
{
    Values = new()
    {
        {"Name", "Khalid"},
        {"Status", "Awesome"}
    }
};

var invertedTable = new EntityWithInvertedTable()
    .AddValue("Name", "Khalid")
    .AddValue("Status", "Awesome... Again!");

storing.EntityWithJsons.Add(json);
storing.EntityWithInvertedTables.Add(invertedTable);
storing.SaveChangesAsync();

var reading = new Database();

json = reading.EntityWithJsons.OrderByDescending(o => o.CreatedAt).First();

invertedTable = reading.EntityWithInvertedTables
    .OrderByDescending(o => o.CreatedAt)
    // include values from other table
    .Include(x => x.Values)
    .First();

Console.WriteLine("Results from JSON are...");
foreach (var (key, value) in json.Values) 
{
    Console.WriteLine($"  - {key}: {value}");
}

Console.WriteLine("\nResults from Entity are...");
foreach (var entry in invertedTable.Values) 
{
    Console.WriteLine($"  - {entry.Name}: {entry.Value}");
}

public class Database : DbContext
{
    public DbSet<EntityWithJson> EntityWithJsons { get; set; }
    public DbSet<EntityWithInvertedTable> EntityWithInvertedTables { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite("Data Source=database.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.General);

        modelBuilder
            .Entity<EntityWithJson>()
            .Property(x => x.Values)
            .HasColumnName("Values")
            .HasColumnType("BLOB") // sqlite BLOB type
            .HasConversion(
                v => JsonSerializer.Serialize(v, options),
                s => JsonSerializer.Deserialize<Dictionary<string, string>>(s, options)!,
                ValueComparer.CreateDefault(typeof(Dictionary<string, string>), true)
            );
    }
}

public class EntityWithJson
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Values { get; set; }
        = new();
}

public class EntityWithInvertedTable
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<CustomValues> Values { get; set; }
        = new List<CustomValues>();

    public EntityWithInvertedTable AddValue(string name, string value)
    {
        var existing = Values?.FirstOrDefault(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing is { })
        {
            existing.Value = value;
        }
        else
        {
            Values?.Add(new CustomValues {Name = name, Value = value});
        }

        return this;
    }
}

public class CustomValues
{
    public int Id { get; set; }
    public int EntityWithInvertedTableId { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
}