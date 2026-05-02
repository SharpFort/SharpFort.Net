namespace SharpFort.CasbinRbac.Domain.Shared.Options;

public class AliyunOptions
{
    public required string AccessKeyId { get; set; }
    public required string AccessKeySecret { get; set; }
    public required AliyunSms Sms { get; set; }
}

public class AliyunSms
{
    public required string SignName { get; set; }
    public required string TemplateCode { get; set; }
}
