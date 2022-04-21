# Entity Framework Core Customized User Value Strategies

This repository is designed to help you explore two strategies
for storing "dynamic" user values in a relational SQL database and 
Entity Framework Core. These approaches have drawbacks, so use them sparingly. 

## Using JSON Serialization/Deserialization

With Entity Framework Core, you can use conversion methods to serialize
data on writes to the database and deserialize data when reading from a table.

Advantages to this approach:

1. Less complexity in database schema design (just another column)
2. Faster reads and writes

The drawbacks of this approach include:

1. Serialization/Deserialization may be expensive
2. You either get all the custom values, or none. You can't pick which values get returned from your queries.

How do you set this up? First, let's take a look at the object definition.

```c#
public class EntityWithJson
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Values { get; set; }
        = new();
}
```

I've chosen `Dictionary<string,string>` for the sake of simplicity and because
I'll be using `System.Text.Json`, which doesn't have a converter for `Dictionary<string,object>`, although you can
certainly write one, as [Josef Ottosson did in his blog post about a custom converter.](https://josef.codes/custom-dictionary-string-object-jsonconverter-for-system-text-json/)

The next step is to set up the conversion as part of the Entity Framework Core configuration. I'm using SQLite, but this should work with any relational database that can store text in a column.

```c#
public class Database : DbContext
{
    public DbSet<EntityWithJson> EntityWithJsons { get; set; }

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
```

Now, we need to use it in our code, and like any dictionary, we only need to provide a key/value pair
to the dictionary.

```c#
var json = new EntityWithJson
{
    Values = new()
    {
        {"Name", "Khalid"},
        {"Status", "Awesome"}
    }
};
```

## Storing Custom User Data In An Inverted Table

**Inverted tables** are when we store key/value pairs in individual rows. In this case, rows become columns allowing for flexibility in how we store our user data.

Advantages to this approach:

- We can store "infinite" amounts of custom user data
- We can filter down to individual rows to retrieve a single value
- Using the parent records, we can constrain uniqueness of keys at the database schema level

Disadvantages to this approach:

- Value columns must have a strict type, most likely of type string.
- These tables can get very large, very fast depending on user input.

How do you set up an inverted table? Well, it's not different from a typical one-to-many relationship. Let's have a look at the entity modeling.

```c#
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
```

I've added an `AddValue` method to the `EntityWithInvertedTable` type, but there is a caveat to the method.
It will only work if you have all the values in the related table in memory. If you don't, you will get duplicate key/value pairs in your collection.

Let's see how to store these custom user values.

```c#
var invertedTable = new EntityWithInvertedTable()
    .AddValue("Name", "Khalid")
    .AddValue("Status", "Awesome... Again!");
```

The difference between the two methods is that the inverted table approach requires you include
the values when reading entities from the database.

```c#
var invertedTable = db
    .EntityWithInvertedTables
    .OrderByDescending(o => o.CreatedAt)
    // include values from other table
    .Include(x => x.Values)
    .First();
```

Remember, the `Include` call will retrieve all related data. As an advantage in EF Core, you can
filter included values to the ones you need.

```c#
invertedTable = db
    .EntityWithInvertedTables
    .OrderByDescending(o => o.CreatedAt)
    // include values from other table
    .Include(x => x.Values.Where(v => v.Name == "Name"))
    .First();
```

## Conclusion

There you have it, two approaches to storing custom user data in a relational database. Which one you
decide to use in your application is up to you to figure out.
I hope you enjoyed reading this article and feel free to ask me questions on Twitter at [@buhakmeh](https://twitter.com/buhakmeh).

