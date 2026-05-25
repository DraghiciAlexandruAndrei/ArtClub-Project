namespace ArtClub.Models.ViewModels.ArtPiece
{
    public class ArtPieceDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Creator { get; set; }
        public string Description { get; set; }
        public List<string> AssociatedEvents { get; set; } = new List<string>();
        public string LastUpdated { get; set; }
    }
}