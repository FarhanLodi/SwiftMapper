namespace SwiftMapper.Test.Dto
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Status { get; set; }
        public AddressDto? Address { get; set; }
    }
}


