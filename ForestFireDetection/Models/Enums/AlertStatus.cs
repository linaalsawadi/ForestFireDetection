namespace ForestFireDetection.Models.Enums
{
    public enum AlertStatus
    {
        NotReviewed,
        InReview,
        Resolved
    }

    public static class AlertStatusExtensions
    {
        public static string ToDisplayString(this AlertStatus status) => status switch
        {
            AlertStatus.NotReviewed => "NotReviewed",
            AlertStatus.InReview => "InReview",
            AlertStatus.Resolved => "Resolved",
            _ => "NotReviewed"
        };

        public static AlertStatus FromString(string status) => status switch
        {
            "NotReviewed" => AlertStatus.NotReviewed,
            "InReview" => AlertStatus.InReview,
            "Resolved" => AlertStatus.Resolved,
            _ => AlertStatus.NotReviewed
        };
    }
}