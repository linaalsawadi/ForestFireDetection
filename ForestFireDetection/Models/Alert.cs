using System;

namespace ForestFireDetection.Models
{
    public class Alert
    {
        public Guid Id { get; set; }
        public Guid SensorId { get; set; }

        public float Temperature { get; set; }
        public float Smoke { get; set; }
        public float Humidity { get; set; }
        public double FireScore { get; set; }

        public DateTime Timestamp { get; set; }

        public string Status { get; set; } // NotReviewed | InReview | Resolved
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ResolutionNote { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

}
