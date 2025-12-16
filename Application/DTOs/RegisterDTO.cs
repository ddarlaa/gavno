namespace IceBreakerApp.Application.DTOs;

public class RegisterDTO
{
    public string Name { get; set; } 
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DateOfBirth { get; set; }
}