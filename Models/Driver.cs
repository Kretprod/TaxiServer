using System.Text.Json.Serialization;

namespace server.Models
{
    public class Driver : User
    {
        [JsonIgnore]
        public ICollection<Ride> Rides { get; set; } = new List<Ride>();
        public DriverDetails? DriverDetails { get; set; }
    }
}
