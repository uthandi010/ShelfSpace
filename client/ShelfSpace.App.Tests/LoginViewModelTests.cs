using ShelfSpace.App.Core.Services;
using ShelfSpace.App.Core.ViewModels;
using Xunit;

namespace ShelfSpace.App.Tests;

public class LoginViewModelTests
{
    [Fact]
    public async Task LoginCommand_WithEmptyFields_ShowsErrorAndDoesNotCallService()
    {
        var authService = new FakeAuthService();
        var vm = new LoginViewModel(authService);

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Email and password are required.", vm.ErrorMessage);
        Assert.Null(authService.LastLogin);
    }

    [Fact]
    public async Task LoginCommand_OnSuccess_RaisesLoggedInEvent()
    {
        var authService = new FakeAuthService();
        var vm = new LoginViewModel(authService) { Email = "ada@example.com", Password = "Password123!" };
        var raised = false;
        vm.LoggedIn += (_, _) => raised = true;

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.True(raised);
        Assert.Null(vm.ErrorMessage);
        Assert.Equal(("ada@example.com", "Password123!"), authService.LastLogin);
    }

    [Fact]
    public async Task LoginCommand_OnFailure_SetsErrorMessageAndDoesNotRaiseLoggedIn()
    {
        var authService = new FakeAuthService { ThrowOnNextCall = new ApiException(401, "Invalid email or password.") };
        var vm = new LoginViewModel(authService) { Email = "ada@example.com", Password = "wrong" };
        var raised = false;
        vm.LoggedIn += (_, _) => raised = true;

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.False(raised);
        Assert.Equal("Invalid email or password.", vm.ErrorMessage);
    }
}
