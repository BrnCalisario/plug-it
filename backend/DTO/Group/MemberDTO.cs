public class MemberDTO
{
    public string Jwt { get; set; }
    public int UserId { get; set; }
    public int GroupId { get; set; }
}

public class MemberItemDTO
{
    public string Name { get; set; }
    public string Role { get; set; }
}