using CGI.LeadTracker.Domain.SeedWork;

namespace CGI.LeadTracker.Domain.AggregatesModel.User;

public class User : Entity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string Role { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public static User Create(string name, string email, string passwordHash, string role = "Admin") =>
        new()
        {
            Name         = name,
            Email        = email,
            PasswordHash = passwordHash,
            Role         = role,
            CreatedAt    = DateTime.UtcNow
        };

    // Define um novo hash de senha. A validação (senha atual correta, política da nova
    // senha) é responsabilidade do handler da aplicação — aqui só persiste o novo hash.
    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Hash de senha inválido.", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
    }
}
