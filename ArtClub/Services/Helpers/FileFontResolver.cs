using PdfSharp.Fonts;
using System.IO;

namespace ArtClub.Services.Helpers
{
    public class FileFontResolver : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // Returnăm numele intern al fontului pentru PDFSharp
            return new FontResolverInfo(familyName);
        }

        public byte[] GetFont(string faceName)
        {
            // Căutăm fișierul .ttf în folderul wwwroot/fonts
            // Asigură-te că ai pus fișierul arial.ttf acolo!
            var fontPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "arial.ttf");

            if (File.Exists(fontPath))
            {
                return File.ReadAllBytes(fontPath);
            }

            return null;
        }
    }
}