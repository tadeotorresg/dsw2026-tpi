using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using NSubstitute;

namespace Dsw2026Tpi.Application.Tests
{
    public class SpecialtyServiceTests
    {
        private readonly IPersistence _mockPersistence = Substitute.For<IPersistence>();

        private const string TestNombre = "Cardiología";
        private const string TestDescripcion = "Atención de enfermedades del corazón";
        private readonly Guid _testSpecialtyId = Guid.NewGuid();


        [Fact]
        public async Task DeleteSpecialty_CuandoLaEspecialidadNoExiste_EntoncesGeneraUnaExcepcion()
        {
            Specialty? especialidadInexistente = null;

            _mockPersistence.GetById<Specialty>(Arg.Any<Guid>(), Arg.Any<string[]>())
                .Returns(especialidadInexistente);

            var service = new SpecialtyService(_mockPersistence);

            await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            {
                await service.DeleteSpecialty(_testSpecialtyId);
            });

            await _mockPersistence.DidNotReceive().Update(Arg.Any<Specialty>());
        }
    }
}
