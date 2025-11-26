namespace login.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public string? AuthorName { get; set; }
        public string? Content { get; set; }
        public int Rating { get; set; } // 1-5 yıldız
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
