namespace Dsw2026Tpi.Domain.Entities;

public class Doctor : SoftDeletableEntity
{
    public string Name { get; private set; }
    public string? LicenseNumber { get; private set; }
    public Guid? SpecialtyId { get; private set; }
    public Specialty? Specialty { get; private set; }
    public bool IsActive { get; private set; } = true;
    #region Constructor for EF
#pragma warning disable CS8618
    private Doctor()
    {
    }
#pragma warning restore CS8618
    #endregion

    public Doctor(string name, string? licenseNumber, Specialty specialty, Guid? id = null) : base(id)
    {
        Name = name;
        LicenseNumber = licenseNumber;
        Specialty = specialty;
    }
    public void UpdateProfile(string name, string? licenseNumber, Specialty specialty)
    {
        Name = name;
        LicenseNumber = licenseNumber;
        SpecialtyId = specialty.Id;
        Specialty = specialty;
    }

    /* public void ChangeActiveStatus(bool isActive)
    {
        IsActive = isActive;
    } */
}
