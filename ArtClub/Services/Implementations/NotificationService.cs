using ArtClub.Services.Interfaces;
using System.Diagnostics;

namespace ArtClub.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // Simulăm trimiterea unui email. 
            // În consolă sau în Debug Output vom vedea mesajul.

            string logMessage = $"[EMAIL SENT] To: {to} | Subject: {subject} | Body: {body}";

            Debug.WriteLine(logMessage);
            Console.WriteLine(logMessage);

            // Simulăm o mică întârziere de rețea
            await Task.Delay(100);
        }
    }
}