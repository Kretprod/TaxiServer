using System.Text.Json.Serialization;

namespace server.Models
{
    public class Passenger : User
    {
        [JsonIgnore]
        public ICollection<Ride> Rides { get; set; } = new List<Ride>();
    }
}
