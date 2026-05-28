namespace SanatoriumIS.Models
{
    public class ServiceUsage
    {
        public int Id { get; set; }

        public int ClientId { get; set; }
        public Client? Client { get; set; }

        public int ServiceId { get; set; }
        public Service? Service { get; set; }

        public DateTime Date { get; set; }
    }
}