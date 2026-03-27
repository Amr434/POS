public class CustomerDto
{
    public int Id { get; set; }
    public string NationalId {get;set;}
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
}