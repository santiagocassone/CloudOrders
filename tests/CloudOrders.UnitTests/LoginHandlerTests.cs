using Moq;
using CloudOrders.Application.Abstractions;
using CloudOrders.Application.Auth;
using CloudOrders.Domain;

namespace CloudOrders.UnitTests
{
    public class LoginHandlerTests
    {
        [Fact]
        public async Task HandleAsync_ValidCommand_ReturnsToken()
        {
            //Arrange
            var repoUserMock = new Mock<IUserRepository>();
            var repoTokenMock = new Mock<ITokenGenerator>();
            var loginHandler = new LoginHandler(repoUserMock.Object, repoTokenMock.Object);
            var loginCommand = new LoginCommand("testuser", "password");
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("password");
            var newUser = User.Create("testuser", passwordHash);
            var expectedToken = "un-token-cualquiera-de-prueba";

            repoUserMock.Setup(r => r.GetByEmailAsync(loginCommand.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(newUser);
            repoTokenMock.Setup(r => r.GenerateToken(newUser)).Returns(expectedToken);

            //Act
            var resultToken = await loginHandler.HandleAsync(loginCommand, CancellationToken.None);

            //Assert
            Assert.Equal(expectedToken, resultToken);
            repoUserMock.Verify(r => r.GetByEmailAsync(loginCommand.Email, It.IsAny<CancellationToken>()), Times.Once);
            repoTokenMock.Verify(r => r.GenerateToken(newUser), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_InvalidCommand_ReturnsNull()
        {
            //Arrange
            var repoUserMock = new Mock<IUserRepository>();
            var repoTokenMock = new Mock<ITokenGenerator>();
            var loginHandler = new LoginHandler(repoUserMock.Object, repoTokenMock.Object);
            var loginCommand = new LoginCommand("fakeuser", "password");

            repoUserMock.Setup(r => r.GetByEmailAsync(loginCommand.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User)null);

            //Act
            var resultToken = await loginHandler.HandleAsync(loginCommand, CancellationToken.None);

            //Assert
            Assert.Null(resultToken);
            repoUserMock.Verify(r => r.GetByEmailAsync(loginCommand.Email, It.IsAny<CancellationToken>()), Times.Once);
            repoTokenMock.Verify(r => r.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_InvalidPassword_ReturnsNull()
        {
            //Arrange
            var repoUserMock = new Mock<IUserRepository>();
            var repoTokenMock = new Mock<ITokenGenerator>();
            var loginHandler = new LoginHandler(repoUserMock.Object, repoTokenMock.Object);
            var loginCommand = new LoginCommand("testuser", "password");
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("password");
            var newUser = User.Create("testuser", passwordHash);
            var wrongPasswordCommand = new LoginCommand("testuser", "wrongPassword");

            repoUserMock.Setup(r => r.GetByEmailAsync(wrongPasswordCommand.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(newUser);

            //Act
            var resultToken = await loginHandler.HandleAsync(wrongPasswordCommand, CancellationToken.None);

            //Assert
            Assert.Null(resultToken);
            repoUserMock.Verify(r => r.GetByEmailAsync(loginCommand.Email, It.IsAny<CancellationToken>()), Times.Once);
            repoTokenMock.Verify(r => r.GenerateToken(It.IsAny<User>()), Times.Never);
        }
    }
}
