using System.Linq;
using OpenDataTables.AspNetCore.Models;
using Xunit;

namespace OpenDataTables.Tests.DataTable;

/// <summary>Tests for the model-level helpers added for issues #13 (Validate) and #14 (NormalizeSort).</summary>
public class DataTableModelTests
{
    // ----- UnsavedEditBehavior (page change with unsaved inline edits) -----

    [Fact]
    public void UnsavedEditBehavior_defaults_to_Warn()
    {
        // Edits must never be lost silently by default — the page-change guard warns unless opted out.
        Assert.Equal(UnsavedEditBehavior.Warn, new DataTableViewModel { AjaxUrl = "/data" }.UnsavedEditBehavior);
    }

    // ----- FilterCard: a hidden column can still be filtered -----

    [Fact]
    public void GetFilterableColumns_includes_hidden_columns_that_have_a_filter()
    {
        var model = new FilterCardViewModel
        {
            Columns =
            {
                new() { Data = "id",   Title = "Id",   IsVisible = false, Filter = DataTableFilterType.Text }, // hidden but filterable
                new() { Data = "name", Title = "Name", IsVisible = true,  Filter = DataTableFilterType.Text },
                new() { Data = "note", Title = "Note", IsVisible = true,  Filter = DataTableFilterType.None }, // no filter
            }
        };

        var cols = model.GetFilterableColumns();

        // Grid visibility is irrelevant — the hidden "id" gets a filter; the filter-less "note" does not.
        Assert.Equal(new[] { "id", "name" }, cols.Select(c => c.Data));
    }

    // ----- NormalizeSort (issue #14: one canonical sort representation) -----

    [Fact]
    public void NormalizeSort_when_sortorders_present_derives_the_back_compat_scalars()
    {
        var query = new DataTableQueryViewModel
        {
            SortColumnName = "stale",
            SortDirection = "asc",
            SortOrders = { new SortDescriptor { Column = "Name", Direction = "desc" } }
        };

        query.NormalizeSort();

        Assert.Equal("Name", query.SortColumnName);
        Assert.Equal("desc", query.SortDirection);
    }

    [Fact]
    public void NormalizeSort_when_only_scalars_set_synthesizes_a_matching_sortorders_entry()
    {
        var query = new DataTableQueryViewModel { SortColumnName = "Name", SortDirection = "desc" };

        query.NormalizeSort();

        var entry = Assert.Single(query.SortOrders);
        Assert.Equal("Name", entry.Column);
        Assert.Equal("desc", entry.Direction);
    }

    [Fact]
    public void NormalizeSort_tolerates_a_null_sortorders_list()
    {
        var query = new DataTableQueryViewModel { SortOrders = null! };

        query.NormalizeSort();

        Assert.NotNull(query.SortOrders);
        Assert.Empty(query.SortOrders);
    }

    [Fact]
    public void NormalizeSort_with_no_sort_at_all_leaves_sortorders_empty()
    {
        var query = new DataTableQueryViewModel(); // SortColumnName defaults to ""

        query.NormalizeSort();

        Assert.Empty(query.SortOrders);
    }

    [Fact]
    public void NormalizeSort_is_idempotent()
    {
        var query = new DataTableQueryViewModel
        {
            SortOrders = { new SortDescriptor { Column = "Name", Direction = "desc" } }
        };

        query.NormalizeSort();
        query.NormalizeSort();

        Assert.Single(query.SortOrders);
        Assert.Equal("Name", query.SortColumnName);
    }

    // ----- Validate (issue #13: typed, property-named configuration errors) -----

    [Theory]
    [InlineData(DataTableFilterUiMode.Inline)]
    [InlineData(DataTableFilterUiMode.Top)]
    [InlineData(DataTableFilterUiMode.Mixed)]
    public void Validate_throws_for_unimplemented_filter_ui_modes(DataTableFilterUiMode mode)
    {
        var model = new DataTableViewModel { AjaxUrl = "/data", FilterUiMode = mode };

        var ex = Assert.Throws<DataTableConfigurationException>(() => model.Validate());
        Assert.Equal(nameof(DataTableViewModel.FilterUiMode), ex.PropertyName);
    }

    [Theory]
    [InlineData(DataTableFilterUiMode.None)]
    [InlineData(DataTableFilterUiMode.FilterCard)]
    public void Validate_allows_supported_filter_ui_modes(DataTableFilterUiMode mode)
    {
        var model = new DataTableViewModel { AjaxUrl = "/data", FilterUiMode = mode };

        model.Validate(); // does not throw
    }

    [Fact]
    public void Validate_throws_for_a_range_filter_when_filters_render()
    {
        var model = new DataTableViewModel
        {
            AjaxUrl = "/data",
            FilterUiMode = DataTableFilterUiMode.FilterCard,
            Columns = { new DataTableColumnViewModel { Data = "age", Title = "Age", Filter = DataTableFilterType.Range } }
        };

        var ex = Assert.Throws<DataTableConfigurationException>(() => model.Validate());
        Assert.Equal(nameof(DataTableViewModel.Columns), ex.PropertyName);
    }

    [Fact]
    public void Validate_allows_a_range_filter_when_no_filter_ui_is_rendered()
    {
        var model = new DataTableViewModel
        {
            AjaxUrl = "/data",
            FilterUiMode = DataTableFilterUiMode.None,
            Columns = { new DataTableColumnViewModel { Data = "age", Title = "Age", Filter = DataTableFilterType.Range } }
        };

        model.Validate(); // does not throw — the Range filter never renders
    }
}
