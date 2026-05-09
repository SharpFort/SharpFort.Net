namespace SharpFort.CasbinRbac.Domain.Shared.Etos
{
    /// <summary>
    /// 用户创建的id
    /// </summary>
    public class UserCreateEventArgs(Guid userId)
    {
        public Guid UserId { get; set; } = userId;
    }
}
