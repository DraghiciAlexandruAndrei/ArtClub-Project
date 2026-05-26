namespace ArtClub.Models.Entities
{
    public class EventArtPiece
    {
        public int EventId { get; set; }
        public virtual Event Event { get; set; }

        public int ArtPieceId { get; set; }
        public virtual ArtPiece ArtPiece { get; set; }
    }
}

