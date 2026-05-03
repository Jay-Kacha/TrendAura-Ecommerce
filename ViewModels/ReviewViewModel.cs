namespace trendaura.ViewModels
{
    public class ReviewViewModel
    {
        public int Id { get; set; }
        public string? ReviewerName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsVerifiedPurchase { get; set; }
        public string UserId { get; set; } = null!;
    }
}
