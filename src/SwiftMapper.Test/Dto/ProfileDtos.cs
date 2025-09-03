namespace SwiftMapper.Test.Dto
{
    public class ProfileDto
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class WithProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public ProfileDto? Profile { get; set; }
    }
}



