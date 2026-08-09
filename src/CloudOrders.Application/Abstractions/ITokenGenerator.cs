using CloudOrders.Domain;

namespace CloudOrders.Application.Abstractions;

public interface ITokenGenerator
{
    string GenerateToken(User user);
}