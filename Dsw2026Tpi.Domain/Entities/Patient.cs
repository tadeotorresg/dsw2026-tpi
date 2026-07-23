using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Patient: EntityBase
    {
        public Guid UserId { get; init; }
        public string Dni {  get; init; }
        public string? FullName { get; init; }


        #region Constructor for EF
#pragma warning disable CS8618
        private Patient() { }
#pragma warning restore CS8618
        #endregion

        public Patient (Guid userId, string dni, string fullName)
        {
            UserId = userId;
            Dni = dni;
            FullName = fullName;
        }
    }
}
