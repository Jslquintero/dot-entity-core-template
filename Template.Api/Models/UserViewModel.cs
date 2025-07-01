namespace Template.Api.Models
{
    public class UserViewModel
    {
        public string? Id { get; set; }
        public string? UserName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public  string? Password { get; set; }
        public  string? Token { get; set; }
        public DateTime? TokenExpirationDate { get; set; }

        public string? RoleId { get; set; }
        public IList<string> Roles { get; set; } = [];
    }
}
