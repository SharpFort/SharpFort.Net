namespace Yi.Framework.Ai.Application.Contracts.Dtos.Token;

public class TokenSelectListOutputDto
{
    public Guid TokenId { get; set; }
    public string Name { get; set; }
    
    public bool IsDisabled { get; set; }
}
