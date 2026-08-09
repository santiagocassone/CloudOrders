using CloudOrders.Application.Abstractions;
using CloudOrders.Domain;

namespace CloudOrders.Application.Auth;

public sealed class LoginHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenGenerator _tokenGenerator;
    public LoginHandler(IUserRepository userRepository, ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<string?> HandleAsync(LoginCommand loginCommand, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(loginCommand.Email, cancellationToken);

        if (user == null)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(loginCommand.Password, user.PasswordHash))
        {
            return null;
        }

        return _tokenGenerator.GenerateToken(user);
    }
}