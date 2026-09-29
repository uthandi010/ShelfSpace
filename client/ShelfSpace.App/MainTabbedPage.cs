using ShelfSpace.App.Pages;

namespace ShelfSpace.App;

/// <summary>The logged-in shell: three tabs, each page/view-model resolved
/// through DI so they get a real ApiClient wired to the running API.</summary>
public class MainTabbedPage : TabbedPage
{
    public MainTabbedPage(DashboardPage dashboardPage, ProductsPage productsPage, OrdersPage ordersPage)
    {
        dashboardPage.Title = "Dashboard";
        productsPage.Title = "Products";
        ordersPage.Title = "Orders";

        Children.Add(dashboardPage);
        Children.Add(productsPage);
        Children.Add(ordersPage);
    }
}
