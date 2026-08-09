namespace CloudOrders.Domain;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = String.Empty;
    public string PasswordHash { get; private set; } = String.Empty;

    private User() { }

    public static User Create(string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));
        }

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash
        };
    }
}