namespace SharpFort.Ai.Application.Contracts.Dtos;

public class SendMessageInput
{
    public List<Message> Messages { get; set; } = [];
    public string Model { get; set; } = null!;

    public Guid? SessionId { get; set; }
}

public class Message
{
    public string Role { get; set; } = null!;
    public string Content { get; set; } = null!;
}
