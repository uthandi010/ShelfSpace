namespace ShelfSpace.App.Core.Models;

public record AuthResult(string Token, int UserId, string Name, string Email);
