using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using NSubstitute;


namespace Dsw2026Tpi.Application.Tests
{
    public class DoctorServiceTests
    {
        private readonly IPersistence _mockPersistence =
            Substitute.For<IPersistence>();

        private readonly Guid _testSpecialtyId = Guid.NewGuid();
        private const string TestNombre = "Florencia Espinosa";
        private const string TestMatricula = "60521";

        [Fact]
        public async Task CreateDoctor_CuandoLosDatosSonValidos_EntoncesSeGuardaElMedico()
        {
            var especialidad = new Specialty("Pediatría", "Atención de niños y adolescentes", _testSpecialtyId);

            _mockPersistence.GetById<Specialty>(Arg.Any<Guid>(), Arg.Any<string[]>())
                .Returns(especialidad);

            var service = new DoctorService(_mockPersistence);
            var request = new DoctorModel.Request(TestNombre, TestMatricula, _testSpecialtyId);

            var response = await service.CreateDoctor(request);

            await _mockPersistence.Received().Add(Arg.Any<Doctor>());

            Assert.Equal(TestNombre, response.Name);
            Assert.Equal(TestMatricula, response.LicenseNumber);
            Assert.Equal(_testSpecialtyId, response.Specialty?.Id);
        }

        [Fact]
        public async Task CreateDoctor_CuandoLaEspecialidadNoExiste_EntoncesGeneraUnaExcepcion()
        {
            Specialty? especialidadInexistente = null;

            _mockPersistence.GetById<Specialty>(Arg.Any<Guid>(), Arg.Any<string[]>())
                .Returns(especialidadInexistente);

            var service = new DoctorService(_mockPersistence);
            var request = new DoctorModel.Request(TestNombre, TestMatricula, _testSpecialtyId);

            await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            {
                await service.CreateDoctor(request);
            });

            await _mockPersistence.DidNotReceive().Add(Arg.Any<Doctor>());
        }
    }
}
