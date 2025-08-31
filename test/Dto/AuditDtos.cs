namespace SwiftMapper.Test.Dto
{
    public class AuditDto { public string CreatedBy { get; set; } = string.Empty; }
    public class WithAuditDto { public AuditDto? Audit { get; set; } }
}


