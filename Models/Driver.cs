using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace server.Models
{
    public class Driver : User
    {
        public ICollection<Ride> Rides { get; set; } = new List<Ride>();
    }
}
