using System.Dynamic;
using logfiler;
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
        await db.PrepareFor(dynamicObject);
        await db.StoreInDatabase([dynamicObject]);

        // Assert
        var table = await db.Get();
        Assert.Single(table);
        dynamic firstRow = table.First();
        Assert.Equal("world", firstRow.hello);
        Assert.Equal(42, firstRow.number);
    }
}