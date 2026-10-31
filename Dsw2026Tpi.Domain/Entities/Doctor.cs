namespace Dsw2026Tpi.Domain.Entities;

public class Doctor : SoftDeletableEntity
{
    public string Name { get; private set; }
    public string? LicenseNumber { get; private set; }
    public Guid? SpecialtyId { get; private set; }
    public Specialty? Specialty { get; private set; }
    public bool IsActive { get; private set; } = true;
    public ICollection<AvailabilityRule> Availabilities { get; private set; }= new List<AvailabilityRule>();
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
        SpecialtyId = specialty.Id;
    }
    public void UpdateProfile(string name, string? licenseNumber, Specialty specialty)
    {
        Name = name;
        LicenseNumber = licenseNumber;
        SpecialtyId = specialty.Id;
        Specialty = specialty;
    }

    public void ChangeActiveStatus(bool isActive)
    {
        IsActive = isActive;
    }
    public bool CheckAvailability( byte month,short year, byte dayOfWeek, TimeSpan startTime, TimeSpan endTime)
    {
        return !Availabilities.Any(rule =>
            !rule.Deleted &&
            rule.Month == month &&
            rule.Year == year &&
            rule.DayOfWeek == dayOfWeek &&
            startTime < rule.EndTime &&
            endTime > rule.StartTime);
    }
    public AvailabilityRule CreateAvailability(byte month, short year, byte dayOfWeek, TimeSpan startTime, TimeSpan endTime)
    {
        var availability = new AvailabilityRule(Id, month, year, dayOfWeek, startTime, endTime);

        Availabilities.Add(availability);
        return availability;
    }
}
