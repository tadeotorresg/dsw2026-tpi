namespace Dsw2026Tpi.Domain.Entities;

public class Specialty : SoftDeletableEntity
{
    public string Name { get; private set; }
    public string Description { get; private set; }

    #region Constructor for EF
#pragma warning disable CS8618
    private Specialty() { }
#pragma warning restore CS8618
    #endregion

    public Specialty(string name, string description, Guid? id = null) : base(id)
    {
        Name = name;
        Description = description;
    }
    public void UpdateDetails(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
