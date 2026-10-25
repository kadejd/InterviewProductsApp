using System.Diagnostics;
using Infrastructure;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Interview.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Interview.Web.Controllers;

public class HomeController : Controller
{
    private readonly InMemDbContext _context;

    public HomeController(InMemDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Products(
        string? search,
        string? category,
        Guid? categoryId = null,
        bool includeInactive = false,
        string? sortBy = "name-asc",
        int page = 1,
        int pageSize = 10)
    {
        page = Math.Max(page, 1);
        if (pageSize is not (5 or 10 or 15))
        {
            pageSize = 10;
        }

        var trimmedSearch = search?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedSearch) && trimmedSearch.Length > 100)
        {
            ModelState.AddModelError(nameof(search), "Search must be 100 characters or fewer.");
        }

        var selectedSortBy = sortBy?.ToLowerInvariant();
        if (selectedSortBy is not ("name-asc" or "name-desc" or "sku-asc" or "sku-desc" or "category-asc" or "category-desc"))
        {
            ModelState.AddModelError(nameof(sortBy), "Select a valid sort order.");
            selectedSortBy = "name-asc";
        }

        var categories = _context.Categories
            .OrderBy(c => c.Name)
            .ToList();

        var hasCategoryFilter = categoryId.HasValue || !string.IsNullOrWhiteSpace(category);
        var selectedCategory = categoryId.HasValue
            ? categories.FirstOrDefault(c => c.CategoryId == categoryId.Value)
            : categories.FirstOrDefault(c => string.Equals(c.Name, category, StringComparison.OrdinalIgnoreCase));
        var selectedCategoryId = selectedCategory?.CategoryId;
        var categoryLookup = categories.ToDictionary(c => c.CategoryId, c => c.Name);

        string GetCategoryName(Product product) =>
            product.CategoryId.HasValue && categoryLookup.TryGetValue(product.CategoryId.Value, out var categoryName)
                ? categoryName
                : "Uncategorized";

        var productsQuery = _context.Products.AsQueryable();

        if (!includeInactive)
        {
            productsQuery = productsQuery.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(trimmedSearch))
        {
            var normalizedSearch = trimmedSearch.ToLower();
            productsQuery = productsQuery.Where(p =>
                p.Name.ToLower().Contains(normalizedSearch) ||
                p.Sku.ToLower().Contains(normalizedSearch));
        }

        if (selectedCategoryId.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.CategoryId == selectedCategoryId.Value);
        }
        else if (hasCategoryFilter)
        {
            productsQuery = productsQuery.Where(_ => false);
        }

        var filteredProducts = productsQuery.ToList();
        filteredProducts = selectedSortBy switch
        {
            "name-desc" => filteredProducts
                .OrderByDescending(product => product.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(product => product.Sku, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            "sku-asc" => filteredProducts
                .OrderBy(product => product.Sku, StringComparer.OrdinalIgnoreCase)
                .ThenBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            "sku-desc" => filteredProducts
                .OrderByDescending(product => product.Sku, StringComparer.OrdinalIgnoreCase)
                .ThenBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            "category-asc" => filteredProducts
                .OrderBy(GetCategoryName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            "category-desc" => filteredProducts
                .OrderByDescending(GetCategoryName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            _ => filteredProducts
                .OrderBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(product => product.Sku, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
        var totalCount = filteredProducts.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Min(page, totalPages);
        var pageProducts = filteredProducts
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var model = new ProductCatalogViewModel
        {
            SearchTerm = trimmedSearch,
            SelectedCategoryId = selectedCategoryId,
            SelectedCategoryName = selectedCategory?.Name,
            SortBy = selectedSortBy,
            IncludeInactive = includeInactive,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Products = pageProducts
                .Select(product => new ProductSummaryViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Sku = product.Sku,
                    IsActive = product.IsActive,
                    CategoryName = GetCategoryName(product)
                })
                .ToList(),
            Categories = categories
                .Select(category => new CategorySummaryViewModel
                {
                    CategoryId = category.CategoryId,
                    Name = category.Name,
                    ProductCount = _context.Products.Count(p => p.CategoryId == category.CategoryId && (includeInactive || p.IsActive))
                })
                .ToList()
        };

        if (hasCategoryFilter && selectedCategory is null)
        {
            ModelState.AddModelError(string.Empty, "The selected category is not available.");
        }

        return View(model);
    }

    public IActionResult Product(Guid id, string? returnUrl)
    {
        var product = _context.Products.FirstOrDefault(product => product.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        var categoryName = product.CategoryId.HasValue
            ? _context.Categories
                .Where(category => category.CategoryId == product.CategoryId.Value)
                .Select(category => category.Name)
                .FirstOrDefault() ?? "Uncategorized"
            : "Uncategorized";

        return View(new ProductDetailsViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            IsActive = product.IsActive,
            CategoryName = categoryName,
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(Products)) ?? "/Home/Products"
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}