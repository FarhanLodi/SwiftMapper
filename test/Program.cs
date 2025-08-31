using SwiftMapper;
using SwiftMapper.Test.Models;
using SwiftMapper.Test.Dto;

class Program
{
    static void Main()
    {
        Run("User -> UserDto (flat + nested)", Test_User_To_UserDto);
        Run("Order -> OrderDto (list mapping)", Test_Order_To_OrderDto_ListMapping);
        Run("Sub-object injection", Test_SubObject_Injection);

        Console.WriteLine();
        Console.WriteLine($"Summary: PASS={passed}, FAIL={failed}");
    }

    static int passed = 0;
    static int failed = 0;

    static void Run(string name, Action test)
    {
        try
        {
            test();
            passed++;
            Console.WriteLine($"[PASS] {name}");
        }
        catch (Exception ex)
        {
            failed++;
            Console.WriteLine($"[FAIL] {name}: {ex.Message}");
        }
    }

    static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"{message} (expected: {expected}, actual: {actual})");
    }

    static void Test_User_To_UserDto()
    {
        var user = new User
        {
            Id = 42,
            Name = "Ada",
            Status = UserStatus.Active,
            Address = new Address { Street = "123 Main", City = "London" }
        };

        var userDto = Mapper.Map<User, UserDto>(user);
        AssertEqual(42, userDto.Id, "Id should map");
        AssertEqual("Ada", userDto.Name, "Name should map");
        AssertEqual(1, userDto.Status, "Enum should map to underlying int");
        AssertTrue(userDto.Address != null, "Nested Address should map");
    }

    static void Test_Order_To_OrderDto_ListMapping()
    {
        var order = new Order
        {
            OrderId = 1001,
            Items = new List<OrderItem>
            {
                new OrderItem { Sku = "ABC", Quantity = 2 },
                new OrderItem { Sku = "XYZ", Quantity = 5 }
            }
        };

        var orderDto = Mapper.Map<Order, OrderDto>(order);
        AssertEqual(1001, orderDto.OrderId, "OrderId should map");
        AssertEqual(2, orderDto.Items.Count, "Order items should map");
    }

    static void Test_SubObject_Injection()
    {
        var withProfile = new WithProfile
        {
            Id = "p-123",
            IsActive = true,
            CreatedOn = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Address = new Address { Street = "10 Downing St", City = "London" }
        };
        var injectedProfile = new ProfileDto { DisplayName = "Ada Lovelace", Email = "ada@example.com" };

        var withProfileDto = Mapper.Map<WithProfile, WithProfileDto>(withProfile, ("Profile", injectedProfile));

        AssertEqual("p-123", withProfileDto.Id, "Id should map from source object");
        AssertTrue(withProfileDto.Profile != null, "Injected sub-object should not be null");
        AssertEqual("Ada Lovelace", withProfileDto.Profile!.DisplayName, "Injected sub-object should set DisplayName");
        AssertEqual("ada@example.com", withProfileDto.Profile!.Email, "Injected sub-object should set Email");
        AssertTrue(object.ReferenceEquals(withProfileDto.Profile, injectedProfile), "Injected instance should be assigned directly");
    }
}
