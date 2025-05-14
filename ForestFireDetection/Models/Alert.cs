using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ForestFireDetection.Models
{
    public class Alert
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        // Foreign Key for Sensor
        [Required]
        [ForeignKey(nameof(Sensor))]
        [JsonPropertyName("sensorId")]
        public string SensorId { get; set; }

        [Required]
        [Range(-50, 100)] // درجة حرارة منطقية
        public float Temperature { get; set; }

        [Required]
        [Range(0, 1000)]
        public float Smoke { get; set; }

        [Required]
        [Range(0, 100)]
        public float Humidity { get; set; }

        [Required]
        [Range(0, 100)]
        public double FireScore { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } // NotReviewed | InReview | Resolved

        [MaxLength(100)]
        public string? ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [MaxLength(1000)]
        public string? ResolutionNote { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        // Navigation Property
        public Sensor Sensor { get; set; }
    }
}
