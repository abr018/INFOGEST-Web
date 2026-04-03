using BCrypt.Net;
using INFOGEST_Web.Data;
using INFOGEST_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace INFOGEST_Web.Controllers
{
    [Route("api/login")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly InfogestDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(InfogestDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Autentica um utilizador no sistema e gera um token JWT de acesso.
        /// </summary>
        /// <remarks>
        /// O processo de login realiza as seguintes operações:
        /// 1. Verifica se o utilizador existe e está ativo.
        /// 2. Valida a password comparando-a com a hash segura (BCrypt).
        /// 3. Inicia a sessão no servidor e gera um Token JWT para autenticação na API.
        /// 4. Determina o URL de redirecionamento com base no cargo (Admin vs Gerente vs Supervisor vs Funcionário).
        /// </remarks>
        /// <param name="loginRequest">Objeto contendo o username e a password.</param>
        /// <returns>Retorna o token JWT, dados do utilizador e url de destino.</returns>
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] INFOGEST_Web.Models.LoginRequest loginRequest)
        {
            var funcionario = await _context.Funcionarios
                .Include(f => f.Cargo)
                .FirstOrDefaultAsync(f => f.Username == loginRequest.Username);

            if (funcionario == null || funcionario.Ativo == false)
            {
                return Unauthorized(new { message = "Utilizador ou password inválida." });
            }

            bool passwordValida = false;
            try
            {
                passwordValida = BCrypt.Net.BCrypt.Verify(loginRequest.Password, funcionario.Senha_Hash);
            }
            catch { return Unauthorized(new { message = "Erro ao validar credenciais." }); }

            if (!passwordValida) return Unauthorized(new { message = "Utilizador ou password inválida." });

            HttpContext.Session.SetString("username", funcionario.Username);
            
            if (funcionario.Cargo != null)
                HttpContext.Session.SetString("cargo", funcionario.Cargo.Nome);

            string token = GenerateJwtToken(funcionario);

            string urlDestino = "/HomeAdmin"; 

            if (funcionario.Cargo != null && funcionario.Cargo.Nome == "Funcionario")
            {
                urlDestino = "/HomeTrabalhadores";
            }

            if (funcionario.Cargo != null && funcionario.Cargo.Nome == "Supervisor")
            {
                urlDestino = "/HomeSupervisores";
            }

            if (funcionario.Cargo != null && funcionario.Cargo.Nome == "Gerente")
            {
                urlDestino = "/HomeGerentes";
            }

            return Ok(new
            {
                token = token,
                message = "Login efetuado com sucesso",
                redirectUrl = urlDestino,
                utilizador = new
                {
                    id = funcionario.Id_Funcionario,
                    nome = funcionario.Nome,
                    username = funcionario.Username,
                    cargo = funcionario.Cargo?.Nome
                }
            });
        }

        // Método auxiliar para geração de tokens (privado, não exposto no Swagger)
        private string GenerateJwtToken(Funcionario funcionario)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, funcionario.Username),
                new Claim(JwtRegisteredClaimNames.Email, funcionario.Email ?? ""),
                new Claim("id", funcionario.Id_Funcionario.ToString()),
                new Claim(ClaimTypes.Role, funcionario.Cargo?.Nome ?? "Funcionario")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}