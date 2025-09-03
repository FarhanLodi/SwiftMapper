using System.Collections.Generic;

namespace SwiftMapper.Test.Models
{
    enum UserStatus { Inactive = 0, Active = 1 }

    class Address
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }

    class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public UserStatus Status { get; set; }
        public Address? Address { get; set; }
    }

    class Order
    {
        public int OrderId { get; set; }
        public List<OrderItem> Items { get; set; } = new();
    }

    class OrderItem
    {
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    class WithProfile
    {
        public string Id { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public Address? Address { get; set; }
    }
}


