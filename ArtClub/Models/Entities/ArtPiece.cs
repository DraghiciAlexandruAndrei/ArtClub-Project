namespace ArtClub.Models.Entities
{
    public class ArtPiece
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Creator { get; set; }
        public string ImageUrl { get; set; }
        public bool IsPopular { get; set; }

        public virtual ICollection<EventArtPiece> EventArtPieces { get; set; } = new List<EventArtPiece>();
    }
}
