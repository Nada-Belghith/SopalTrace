using System;
using System.Threading.Tasks;
using Moq;
using SopalTrace.Application.Interfaces;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>Unité de travail simulée : exécute directement l'opération passée à ExecuteInTransactionAsync.</summary>
    internal static class UnitOfWorkMock
    {
        public static Mock<IUnitOfWork> Transactionnelle()
        {
            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(operation => operation());
            unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<Guid>>>()))
                .Returns<Func<Task<Guid>>>(operation => operation());
            return unitOfWork;
        }
    }
}
