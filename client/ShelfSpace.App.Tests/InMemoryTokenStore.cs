using ShelfSpace.App.Core.Services;

namespace ShelfSpace.App.Tests;

public class InMemoryTokenStore : ITokenStore
{
    public string? Token { get; set; }
}
