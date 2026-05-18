using System;
using DataAccessLayer.Enums;
using System.Collections.Generic;

namespace DataAccessLayer.Entity
{
    public class Branch
    {
        public Branch()
        {
            BranchID = Guid.NewGuid();
            Bookings = new HashSet<Booking>();
        }

        public Guid BranchID { get; set; }
        public string BranchName { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }
        public BranchStatusEnum Status { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; }
    }
}
