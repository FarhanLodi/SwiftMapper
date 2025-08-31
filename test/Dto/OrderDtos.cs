using System.Collections.Generic;

namespace SwiftMapper.Test.Dto
{
    public class OrderItemDto
    {
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class OrderDto
    {
        public int OrderId { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }
}


