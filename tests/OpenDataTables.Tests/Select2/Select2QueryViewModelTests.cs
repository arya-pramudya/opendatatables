using OpenSelect2.AspNetCore.Models;
using Xunit;

namespace OpenDataTables.Tests.Select2;

public class Select2QueryViewModelTests
{
    [Fact]
    public void SearchTerm_defaults_to_empty_string()
    {
        Assert.Equal("", new Select2QueryViewModel().SearchTerm);
    }

    [Fact]
    public void SearchTerm_coerces_null_to_empty_string()
    {
        // MVC model binding converts an empty query value (searchTerm=) to null by default; the
        // setter must keep it non-null so endpoints can safely call x.Contains(query.SearchTerm).
        var model = new Select2QueryViewModel { SearchTerm = null! };
        Assert.Equal("", model.SearchTerm);
    }
}
