namespace Yi.Framework.Ai.Application.Contracts.Dtos;

public class SendMessageInput
{
    public List<Message> Messages { get; set; }
    public string Model { get; set; }
    
    public Guid? SessionId{ get; set; }
}

public class Message
{
    public string Role { get; set; }
    public string Content { get; set; }
}
