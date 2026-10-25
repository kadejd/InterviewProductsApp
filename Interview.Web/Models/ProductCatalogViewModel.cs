namespace Interview.Web.Models;

public class ProductCatalogViewModel
{
    public string? SearchTerm { get; set; }
    public Guid? SelectedCategoryId { get; set; }
    public string? SelectedCategoryName { get; set; }
    public string SortBy { get; set; } = "name-asc";
    public bool IncludeInactive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; } = 1;
    public List<ProductSummaryViewModel> Products { get; set; } = [];
    public List<CategorySummaryViewModel> Categories { get; set; } = [];
}

public class ProductSummaryViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string CategoryName { get; set; } = "Uncategorized";
}

public class ProductDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string CategoryName { get; set; } = "Uncategorized";
    public string ReturnUrl { get; set; } = "/Home/Products";
}

public class CategorySummaryViewModel
{
    public Guid? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ProductCount { get; set; }
}
