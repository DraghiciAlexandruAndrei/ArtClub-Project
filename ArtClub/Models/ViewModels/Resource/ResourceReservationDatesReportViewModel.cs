using System;
using System.Collections.Generic;
using ReservationEntity = ArtClub.Models.Entities.Reservation;

namespace ArtClub.Models.ViewModels.Resource
{
    public class ResourceReservationDatesReportViewModel
    {
        public string ResourceName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ReservationEntity> Reservations { get; set; } = new();
    }
}
