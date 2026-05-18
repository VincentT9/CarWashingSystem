using System;
using DataAccessLayer.Enums;
using System.Collections.Generic;

namespace DataAccessLayer.Entity
{
    public class Service
    {
        public Service()
        {
            ServiceID = Guid.NewGuid();
            BookingDetails = new HashSet<BookingDetail>();
        }

        public Guid ServiceID { get; set; }
        public string ServiceName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public TimeSpan? EstimatedDuration { get; set; }
        public ServiceStatusEnum Status { get; set; }

        public virtual ICollection<BookingDetail> BookingDetails { get; set; }
    }
}
