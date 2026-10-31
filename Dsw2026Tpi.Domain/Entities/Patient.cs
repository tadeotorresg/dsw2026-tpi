namespace Dsw2026Tpi.Domain.Entities;

public class Patient : SoftDeletableEntity
{
    private readonly List<Appointment> _appointments =
    new List<Appointment>();
    public Guid UserId { get; private set; }
    public string Dni {  get; init; }
    public string? FullName { get; private set; }
    public IReadOnlyCollection<Appointment> Appointments =>
    _appointments.AsReadOnly();


    #region Constructor for EF
#pragma warning disable CS8618
    private Patient() { }
#pragma warning restore CS8618
    #endregion

    public Patient (Guid userId, string dni, string? fullName = null)
    {
        UserId = userId;
        Dni = dni;
        FullName = fullName;
    }
    public void UpdateProfile (string fullName)
    {
        FullName = fullName;
    }
    public Appointment CreateAppointment(Guid availabilitySlotId, string reason)
    {
        var appointment = new Appointment(availabilitySlotId, Id, reason);

        _appointments.Add(appointment);

        return appointment;
    }
}
