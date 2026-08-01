namespace Dsw2026Tpi.Domain.Entities;

public abstract class SoftDeletableEntity (Guid? id = null) : EntityBase(id)
{
    public bool Deleted { get; private set; } = false;

    public void SetDeleted()
    {
        Deleted = true;
    }
}
