using System.Dynamic;
using Xunit;

namespace logfiler;

public class DatabaseTest
{
    [Fact]
    public async Task SaveDynamicObject_Works()
    {
        // Arrange
        dynamic dynamicObject = new ExpandoObject();
        dynamicObject.hello = "world";
        dynamicObject.number = 42;
        await using var db = new Database(Path.GetTempFileName());

        // Act
        await db.StoreInDatabase([dynamicObject]);

        // Assert
        var table = await db.Get();
        Assert.Single(table);
        var firstRow = table.First();
        Assert.Equal("world", firstRow["hello"]);
        Assert.Equal(Convert.ToInt64(42), firstRow["number"]);
    }
    [Fact]
    public async Task SaveDictionaryObject_Works()
    {
        // Arrange
        var dictionary = new Dictionary<string, object>
        {
            ["hello"] = "world",
            ["number"] = 42
        };
        await using var db = new Database(Path.GetTempFileName());

        // Act
        await db.StoreInDatabase([dictionary]);

        // Assert
        var table = await db.Get();
        Assert.Single(table);
        var firstRow = table.First();
        Assert.Equal("world", firstRow["hello"]);
        Assert.Equal(Convert.ToInt64(42), firstRow["number"]);
    }
}