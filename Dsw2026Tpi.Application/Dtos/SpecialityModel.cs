using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos
{
    public record SpecialityModel
    {
        public record Request(String Name, String Description);

        public record Response(Guid Id, String Name, String Description);
    }
}
