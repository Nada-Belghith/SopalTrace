 using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.Services;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Application.DTOs.Auth;
using SopalTrace.Domain.Exceptions;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IErpService> _mockErpService;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IJournalConnexionRepository> _mockJournalRepository;
        private readonly Mock<ISecurityService> _mockSecurityService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _mockErpService = new Mock<IErpService>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockJournalRepository = new Mock<IJournalConnexionRepository>();
            _mockSecurityService = new Mock<ISecurityService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<AuthService>>();

            _authService = new AuthService(
                _mockErpService.Object,
                _mockUserRepository.Object,
                _mockJournalRepository.Object,
                _mockSecurityService.Object,
                _mockEmailService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldThrowTooManyAttemptsException_WhenNombreTentativesIs5OrMore()
        {
            // Arrange
            var email = "test@sopal.com";
            var request = new ResetPasswordDto(email, "123456", "NewPass123!");

            var user = new UtilisateursApp
            {
                Id = Guid.NewGuid(),
                Matricule = "1234",
                Email = email,
                CodeRecuperationHash = "hash123",
                DateExpirationCode = DateTime.UtcNow.AddMinutes(10),
                NombreTentativesCode = 5 // 5 tentatives = bloque
            };

            _mockUserRepository.Setup(x => x.GetUserByEmailAsync(email)).ReturnsAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<TooManyAttemptsException>(() => _authService.ResetPasswordAsync(request));

            // Verify VerifyPassword was NOT called because it's blocked before
            _mockSecurityService.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldIncrementNombreTentatives_WhenCodeIsIncorrect()
        {
            // Arrange
            var email = "test@sopal.com";
            var request = new ResetPasswordDto(email, "WrongCode", "NewPass123!");

            var user = new UtilisateursApp
            {
                Id = Guid.NewGuid(),
                Matricule = "1234",
                Email = email,
                CodeRecuperationHash = "hash123",
                DateExpirationCode = DateTime.UtcNow.AddMinutes(10),
                NombreTentativesCode = 2 // Deja 2 tentatives
            };

            _mockUserRepository.Setup(x => x.GetUserByEmailAsync(email)).ReturnsAsync(user);
            
            // Simule code incorrect
            _mockSecurityService.Setup(x => x.VerifyPassword(request.Code, user.CodeRecuperationHash)).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidTokenException>(() => _authService.ResetPasswordAsync(request));

            // Verify NombreTentatives was incremented
            Assert.Equal(3, user.NombreTentativesCode);
            
            // Verify UpdateUserAsync was called to save the increment
            _mockUserRepository.Verify(x => x.UpdateUserAsync(user), Times.Once);
        }
    }
}
