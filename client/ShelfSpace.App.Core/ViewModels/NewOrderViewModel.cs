using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShelfSpace.App.Core.Models;
using ShelfSpace.App.Core.Services;

namespace ShelfSpace.App.Core.ViewModels;

public partial class OrderLineDraft : ObservableObject
{
    public Product Product { get; }

    [ObservableProperty]
    private int quantity;

    public OrderLineDraft(Product product, int quantity)
    {
        Product = product;
        Quantity = quantity;
    }

    public decimal LineTotal => Product.Price * Quantity;
}

public partial class NewOrderViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly IOrderService _orderService;

    public NewOrderViewModel(IProductService productService, IOrderService orderService)
    {
        _productService = productService;
        _orderService = orderService;
    }

    public ObservableCollection<Product> AvailableProducts { get; } = new();
    public ObservableCollection<OrderLineDraft> Lines { get; } = new();

    [ObservableProperty]
    private string customerName = string.Empty;

    [ObservableProperty]
    private Product? selectedProduct;

    [ObservableProperty]
    private string quantityText = "1";

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private Order? createdOrder;

    public decimal TotalPreview => Lines.Sum(l => l.LineTotal);

    [RelayCommand]
    public async Task LoadProductsAsync()
    {
        ErrorMessage = null;
        try
        {
            var products = await _productService.ListAsync();
            AvailableProducts.Clear();
            foreach (var product in products.Where(p => p.QuantityOnHand > 0))
            {
                AvailableProducts.Add(product);
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public void AddLine()
    {
        ErrorMessage = null;

        if (SelectedProduct is null)
        {
            ErrorMessage = "Choose a product first.";
            return;
        }
        if (!int.TryParse(QuantityText, out var quantity) || quantity <= 0)
        {
            ErrorMessage = "Enter a valid quantity.";
            return;
        }

        var existing = Lines.FirstOrDefault(l => l.Product.Id == SelectedProduct.Id);
        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            Lines.Add(new OrderLineDraft(SelectedProduct, quantity));
        }

        OnPropertyChanged(nameof(TotalPreview));
        QuantityText = "1";
    }

    [RelayCommand]
    public void RemoveLine(OrderLineDraft line)
    {
        Lines.Remove(line);
        OnPropertyChanged(nameof(TotalPreview));
    }

    [RelayCommand]
    public async Task SubmitAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            ErrorMessage = "Customer name is required.";
            return;
        }
        if (Lines.Count == 0)
        {
            ErrorMessage = "Add at least one line item.";
            return;
        }

        IsBusy = true;
        try
        {
            var items = Lines.Select(l => (l.Product.Id, l.Quantity)).ToList();
            CreatedOrder = await _orderService.CreateAsync(CustomerName.Trim(), items);
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
}
