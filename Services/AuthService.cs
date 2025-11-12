using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using taskcontrolv1.Data;
using taskcontrolv1.DTOs.Auth;
using taskcontrolv1.Helpers;
using taskcontrolv1.Models;
using taskcontrolv1.Models.Enums;
using taskcontrolv1.Services.Interfaces;
//modelo agregado para uso de TokensSwagger
using System.IdentityModel.Tokens.Jwt;

namespace taskcontrolv1.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenSvc;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, ITokenService tokenSvc, IConfiguration config)
    {
        _db = db;
        _tokenSvc = tokenSvc;
        _config = config;
    }

    public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO dto)
    {
        var user = await _db.Usuarios.Include(u => u.Empresa).FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null || !PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt))
            throw new UnauthorizedAccessException("Credenciales inválidas");

        // Validación de scope empresa:
        if (user.Rol == RolUsuario.AdminEmpresa || user.Rol == RolUsuario.Usuario)
        {
            if (dto.EmpresaId is null || user.EmpresaId != dto.EmpresaId)
                throw new UnauthorizedAccessException("Empresa inválida para el usuario");

            // AdminEmpresa solo puede loguear si Empresa Approved
            if (user.Rol == RolUsuario.AdminEmpresa && user.Empresa!.Estado != EstadoEmpresa.Approved)
                throw new UnauthorizedAccessException("Empresa no aprobada aún");
        }

        // Claims
        var accessMinutes = int.Parse(_config["JwtSettings:AccessTokenExpirationMinutes"]!);
        var expiresAt = DateTime.UtcNow.AddMinutes(accessMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Role, user.Rol.ToString()),
        };
        if (user.EmpresaId.HasValue)
            claims.Add(new("empresaId", user.EmpresaId.Value.ToString()));

        var accessToken = _tokenSvc.CreateAccessToken(claims, expiresAt);

        // Refresh
        var refreshDays = int.Parse(_config["JwtSettings:RefreshTokenExpirationDays"]!);
        var (plainRt, hashRt) = _tokenSvc.CreateRefreshToken();
        var rt = new RefreshToken
        {
            UsuarioId = user.Id,
            TokenHash = hashRt,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays)
        };
        _db.RefreshTokens.Add(rt);
        await _db.SaveChangesAsync();

        return new LoginResponseDTO
        {
            Tokens = new TokenResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = plainRt,
                ExpiresIn = accessMinutes * 60
            },
            Usuario = new {
                id = user.Id,
                email = user.Email,
                nombreCompleto = user.NombreCompleto,
                rol = user.Rol.ToString(),
                empresaId = user.EmpresaId
            }
        };
    }

    public async Task<TokenResponseDTO> RefreshAsync(RefreshTokenRequestDTO dto)
    {
        var hash = _tokenSvc.HashRefreshToken(dto.RefreshToken);
        var stored = await _db.RefreshTokens.Include(r => r.Usuario)
            .FirstOrDefaultAsync(r => r.TokenHash == hash);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token inválido");

        var user = stored.Usuario;

        var accessMinutes = int.Parse(_config["JwtSettings:AccessTokenExpirationMinutes"]!);
        var expiresAt = DateTime.UtcNow.AddMinutes(accessMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Role, user.Rol.ToString()),
        };
        if (user.EmpresaId.HasValue)
            claims.Add(new("empresaId", user.EmpresaId.Value.ToString()));

        var accessToken = _tokenSvc.CreateAccessToken(claims, expiresAt);

        // Rotación de refresh (opcional): emitir uno nuevo y revocar el antiguo
        stored.IsRevoked = true;
        stored.RevokedAt = DateTime.UtcNow;
        stored.RevokeReason = "rotated";

        var refreshDays = int.Parse(_config["JwtSettings:RefreshTokenExpirationDays"]!);
        var (plainRt, hashRt) = _tokenSvc.CreateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            UsuarioId = user.Id,
            TokenHash = hashRt,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays)
        });
        await _db.SaveChangesAsync();

        return new TokenResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = plainRt,
            ExpiresIn = accessMinutes * 60
        };
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var hash = _tokenSvc.HashRefreshToken(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash);
        if (stored is null) return;

        stored.IsRevoked = true;
        stored.RevokedAt = DateTime.UtcNow;
        stored.RevokeReason = "logout";
        await _db.SaveChangesAsync();
    }

    public async Task<int> RegisterAdminEmpresaAsync(RegisterAdminEmpresaDTO dto)
    {
        // 1) Crear empresa en Pending
        var empresa = new Empresa
        {
            Nombre = dto.NombreEmpresa,
            Direccion = dto.DireccionEmpresa,
            Telefono = dto.TelefonoEmpresa
        };
        _db.Empresas.Add(empresa);
        await _db.SaveChangesAsync();

        // 2) Crear usuario AdminEmpresa asociado
        PasswordHasher.CreatePasswordHash(dto.Password, out var hash, out var salt);

        var user = new Usuario
        {
            Email = dto.Email,
            NombreCompleto = dto.NombreCompleto,
            Telefono = dto.Telefono,
            PasswordHash = hash,
            PasswordSalt = salt,
            Rol = RolUsuario.AdminEmpresa,
            EmpresaId = empresa.Id
        };
        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        return empresa.Id;
    }
}
