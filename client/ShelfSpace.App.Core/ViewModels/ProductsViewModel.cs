using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShelfSpace.App.Core.Models;
using ShelfSpace.App.Core.Services;

namespace ShelfSpace.App.Core.ViewModels;

public partial class ProductsViewModel : ObservableObject
{
    private readonly IProductService _productService;

    public ProductsViewModel(IProductService productService)
    {
        _productService = productService;
    }

    public ObservableCollection<Product> Products { get; } = new();

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string newSku = string.Empty;

    [ObservableProperty]
    private string newName = string.Empty;

    [ObservableProperty]
    private string newPrice = string.Empty;

    [ObservableProperty]
    private string newQuantity = string.Empty;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var products = await _productService.ListAsync();
            Products.Clear();
            foreach (var product in products)
            {
                Products.Add(product);
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task AddProductAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(NewSku) || string.IsNullOrWhiteSpace(NewName))
        {
            ErrorMessage = "SKU and name are required.";
            return;
        }
        if (!decimal.TryParse(NewPrice, out var price) || price < 0)
        {
            ErrorMessage = "Enter a valid, non-negative price.";
            return;
        }
        if (!int.TryParse(NewQuantity, out var quantity) || quantity < 0)
        {
            ErrorMessage = "Enter a valid, non-negative starting quantity.";
            return;
        }

        try
        {
            var product = await _productService.CreateAsync(
                NewSku.Trim(), NewName.Trim(), null, (int)Math.Round(price * 100), quantity, null);
            Products.Add(product);
            NewSku = string.Empty;
            NewName = string.Empty;
            NewPrice = string.Empty;
            NewQuantity = string.Empty;
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public async Task RestockAsync(int productId, int quantity)
    {
        ErrorMessage = null;
        try
        {
            var updated = await _productService.RestockAsync(productId, quantity);
            ReplaceById(updated);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public async Task DeleteAsync(Product product)
    {
        ErrorMessage = null;
        try
        {
            await _productService.DeleteAsync(product.Id);
            Products.Remove(product);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void ReplaceById(Product updated)
    {
        var existing = Products.FirstOrDefault(p => p.Id == updated.Id);
        if (existing is not null)
        {
            var index = Products.IndexOf(existing);
            Products[index] = updated;
        }
    }
}
