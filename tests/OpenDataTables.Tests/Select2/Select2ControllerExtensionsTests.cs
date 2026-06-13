using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OpenSelect2.AspNetCore.Extensions;
using OpenSelect2.AspNetCore.Models;
using Xunit;

namespace OpenDataTables.Tests.Select2;

public class Select2ControllerExtensionsTests
{
    private sealed class TestController : ControllerBase { }

    private static JsonElement SerializeValue(object? value) =>
        JsonSerializer.SerializeToElement(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    [Fact]
    public void Select2Result_from_result_emits_items_and_hasMore()
    {
        var controller = new TestController();
        var result = controller.Select2Result(new Select2Result
        {
            Items = { new Select2ListItem { Id = "1", Text = "One" } },
            HasMore = true
        });

        var json = SerializeValue(result.Value);
        Assert.True(json.GetProperty("hasMore").GetBoolean());
        var items = json.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("1", items[0].GetProperty("id").GetString());
        Assert.Equal("One", items[0].GetProperty("text").GetString());
    }

    [Fact]
    public void Select2Result_from_items_overload_defaults_hasMore_false()
    {
        var controller = new TestController();
        var items = new[] { new Select2ListItem { Id = "a", Text = "A" } };

        var result = controller.Select2Result(items);

        var json = SerializeValue(result.Value);
        Assert.False(json.GetProperty("hasMore").GetBoolean());
        Assert.Equal("a", json.GetProperty("items")[0].GetProperty("id").GetString());
    }

    [Fact]
    public void Select2ListItem_Extra_flattens_to_top_level_json()
    {
        var item = new Select2ListItem
        {
            Id = "1",
            Text = "Cash",
            Extra = new Dictionary<string, object?> { ["currency"] = "USD" }
        };

        var json = SerializeValue(item);
        Assert.Equal("USD", json.GetProperty("currency").GetString()); // top-level, not nested under "extra"
        Assert.False(json.TryGetProperty("extra", out _));
    }
}
