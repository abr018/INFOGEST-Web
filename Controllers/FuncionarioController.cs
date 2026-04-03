using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INFOGEST_Web.Data;
using INFOGEST_Web.Models;

namespace INFOGEST_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FuncionariosController : ControllerBase
    {
        private readonly InfogestDbContext _context;

        public FuncionariosController(InfogestDbContext context)
        {
            _context = context;
        }

        // Método auxiliar privado (não necessita de documentação XML pública)
        private async Task<int?> GetIdFilialUtilizadorLogado()
        {
            var username = HttpContext.Session.GetString("username");

            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            var funcionario = await _context.Funcionarios
                .Include(f => f.Departamento)
                .ThenInclude(d => d.Filial)
                .FirstOrDefaultAsync(f => f.Username == username);

            if (funcionario == null || funcionario.Departamento == null || funcionario.Departamento.Filial == null)
            {
                return null;
            }

            return funcionario.Departamento.Filial.Id_Filial;
        }

        /// <summary>
        /// Obtém a lista de funcionários ativos associados à empresa do administrador autenticado.
        /// </summary>
        /// <remarks>
        /// Retorna uma lista otimizada (DTO) contendo apenas informações essenciais para listagem 
        /// (Nome, Cargo, Departamento, Filial).
        /// </remarks>
        /// <returns>Uma lista de objetos com dados resumidos dos funcionários.</returns>
        [HttpGet]
        public async Task<IActionResult> GetFuncionarios()
        {
            var usernameAdmin = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(usernameAdmin)) return Unauthorized();

            var admin = await _context.Funcionarios
                .Include(f => f.Departamento).ThenInclude(d => d.Filial)
                .FirstOrDefaultAsync(f => f.Username == usernameAdmin);

            if (admin?.Departamento?.Filial == null) return BadRequest("Erro de permissão.");

            int idEmpresa = admin.Departamento.Filial.Id_Empresa;

            var lista = await _context.Funcionarios
                .Where(f => f.Departamento.Filial.Id_Empresa == idEmpresa)
                .Where(f => f.Ativo == true)
                .Where(f => f.Cargo.Nome != "Administrador")
                .Select(f => new
                {
                    id_Funcionario = f.Id_Funcionario,
                    nome = f.Nome,
                    username = f.Username,
                    nif = f.Nif,
                    nomeCargo = f.Cargo != null ? f.Cargo.Nome : "Não definido",
                    nomeDepartamento = f.Departamento != null ? f.Departamento.Nome : "Sem Dept.",
                    nomeFilial = (f.Departamento != null && f.Departamento.Filial != null)
                                 ? f.Departamento.Filial.Nome
                                 : "-"
                })
                .ToListAsync();

            return Ok(lista);
        }

        [HttpGet("FunDepartamento")]
        public async Task<IActionResult> GetFuncionariosComDepartamento()
        {
            // 1. Utilizamos o seu método auxiliar para obter o ID da Filial
            int? idFilial = await GetIdFilialUtilizadorLogado();

            if (idFilial == null)
            {
                return Unauthorized("Não foi possível identificar a filial do utilizador.");
            }

            // 2. Filtramos a lista de funcionários apenas por essa Filial
            var lista = await _context.Funcionarios
                .Where(f => f.Ativo == true && f.Departamento.Filial.Id_Filial == idFilial)
                .Where(f => f.Cargo.Nome != "Administrador")
                .Select(f => new
                {
                    id_Funcionario = f.Id_Funcionario,
                    nome = f.Nome,
                    username = f.Username,
                    nif = f.Nif,
                    nomeCargo = f.Cargo != null ? f.Cargo.Nome : "Não definido",
                    nomeDepartamento = f.Departamento != null ? f.Departamento.Nome : "Sem Dept.",
                    nomeFilial = f.Departamento.Filial.Nome // Simplificado pois o filtro já garante que existe filial
                })
                .ToListAsync();

            return Ok(lista);
        }

        /// <summary>
        /// Obtém os detalhes completos de um funcionário específico.
        /// </summary>
        /// <param name="id">O identificador único do funcionário.</param>
        /// <returns>O objeto Funcionário completo, incluindo relações de Cargo e Departamento.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFuncionario(int id)
        {
            var func = await _context.Funcionarios
                .Include(f => f.Cargo)
                .Include(f => f.Departamento).ThenInclude(d => d.Filial)
                .FirstOrDefaultAsync(f => f.Id_Funcionario == id);

            if (func == null) return NotFound();
            return Ok(func);
        }

        /// <summary>
        /// Cria um novo funcionário no sistema, gerando uma hash segura para a password.
        /// </summary>
        /// <remarks>
        /// Este método realiza validações manuais de duplicidade para Username e NIF.
        /// A password é obrigatória na criação e é encriptada antes de ser guardada.
        /// </remarks>
        /// <param name="funcionario">Os dados do novo funcionário.</param>
        /// <returns>O funcionário criado ou mensagem de erro.</returns>
        [HttpPost]
        public async Task<ActionResult<Funcionario>> PostFuncionario(Funcionario funcionario)
        {
            ModelState.Remove("Senha_Hash");

            ModelState.Remove("Cargo");
            ModelState.Remove("Departamento");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(funcionario.Password))
            {
                return BadRequest(new { error = "A Password é obrigatória." });
            }

            if (await _context.Funcionarios.AnyAsync(f => f.Username == funcionario.Username))
                return Conflict(new { error = "Username já existe." });

            if (!string.IsNullOrWhiteSpace(funcionario.Nif))
            {
                if (await _context.Funcionarios.AnyAsync(f => f.Nif == funcionario.Nif))
                    return Conflict(new { error = "NIF já existe." });
            }

            string hashGerada = BCrypt.Net.BCrypt.HashPassword(funcionario.Password);

            var novo = new Funcionario
            {
                Nome = funcionario.Nome,
                Username = funcionario.Username,
                Genero = funcionario.Genero,
                Email = funcionario.Email,
                Telefone = funcionario.Telefone,
                Morada = funcionario.Morada,
                Salario_Bruto = funcionario.Salario_Bruto,
                Nif = funcionario.Nif,
                Id_Cargo = funcionario.Id_Cargo,
                Id_Departamento = funcionario.Id_Departamento,
                Data_Nascimento = funcionario.Data_Nascimento,

                Senha_Hash = hashGerada,

                Ativo = true
            };

            _context.Funcionarios.Add(novo);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Problem(detail: ex.InnerException?.Message ?? ex.Message, statusCode: 500);
            }

            return CreatedAtAction("GetFuncionario", new { id = novo.Id_Funcionario }, novo);
        }

        /// <summary>
        /// Altera a password do utilizador atualmente autenticado.
        /// </summary>
        /// <param name="request">Objeto contendo a senha atual e a nova senha.</param>
        /// <returns>Mensagem de sucesso ou erro se a senha atual estiver incorreta.</returns>
        [HttpPost("alterar-senha")]
        public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaRequest request)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return Unauthorized(new { message = "Sessão expirada." });

            var funcionario = await _context.Funcionarios.FirstOrDefaultAsync(f => f.Username == username);
            if (funcionario == null) return NotFound(new { message = "Utilizador não encontrado." });

            bool senhaCorreta = BCrypt.Net.BCrypt.Verify(request.SenhaAtual, funcionario.Senha_Hash);

            if (!senhaCorreta)
            {
                return BadRequest(new { message = "A senha atual está incorreta." });
            }

            string novoHash = BCrypt.Net.BCrypt.HashPassword(request.SenhaNova);
            funcionario.Senha_Hash = novoHash;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Senha alterada com sucesso." });
        }

        /// <summary>
        /// Atualiza os dados de um funcionário existente.
        /// </summary>
        /// <remarks>
        /// Permite atualizar dados pessoais, cargo, departamento e, opcionalmente, a password (se fornecida).
        /// </remarks>
        /// <param name="id">O ID do funcionário a atualizar.</param>
        /// <param name="funcionario">Os novos dados.</param>
        /// <returns>NoContent em caso de sucesso.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFuncionario(int id, Funcionario funcionario)
        {
            if (id != funcionario.Id_Funcionario) return BadRequest();

            var funcOriginal = await _context.Funcionarios.FindAsync(id);
            if (funcOriginal == null) return NotFound();

            funcOriginal.Nome = funcionario.Nome;
            funcOriginal.Username = funcionario.Username;
            funcOriginal.Genero = funcionario.Genero;
            funcOriginal.Email = funcionario.Email;
            funcOriginal.Telefone = funcionario.Telefone;
            funcOriginal.Morada = funcionario.Morada;
            funcOriginal.Salario_Bruto = funcionario.Salario_Bruto;
            funcOriginal.Id_Cargo = funcionario.Id_Cargo;
            funcOriginal.Data_Nascimento = funcionario.Data_Nascimento;
            funcOriginal.Nif = funcionario.Nif;
            funcOriginal.Id_Departamento = funcionario.Id_Departamento;

            if (!string.IsNullOrEmpty(funcionario.Senha_Hash))
            {
                funcOriginal.Senha_Hash = funcionario.Senha_Hash;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Funcionarios.Any(e => e.Id_Funcionario == id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Inativa um funcionário (Soft Delete), revogando o seu acesso ao sistema.
        /// </summary>
        /// <param name="id">O ID do funcionário a inativar.</param>
        /// <returns>NoContent se bem-sucedido.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFuncionario(int id)
        {
            var funcionario = await _context.Funcionarios.FindAsync(id);
            if (funcionario == null) return NotFound();

            funcionario.Ativo = false;

            _context.Entry(funcionario).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Obtém o perfil do utilizador autenticado, incluindo o link de retorno apropriado ao seu cargo.
        /// </summary>
        /// <returns>Dados do perfil e URL de navegação.</returns>
        [HttpGet("meu-perfil")]
        public async Task<IActionResult> GetMeuPerfil()
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var func = await _context.Funcionarios
                .Include(f => f.Cargo)
                .FirstOrDefaultAsync(f => f.Username == username);

            if (func == null) return NotFound();

            string linkVoltar = "/HomeAdmin";

            if (func.Cargo != null && func.Cargo.Nome == "Funcionario")
            {
                linkVoltar = "/HomeTrabalhadores";
            }

            if (func.Cargo != null && func.Cargo.Nome == "Supervisor")
            {
                linkVoltar = "/HomeSupervisores";
            }

            if (func.Cargo != null && func.Cargo.Nome == "Gerente")
            {
                linkVoltar = "/HomeGerentes";
            }

            var dadosPerfil = new
            {
                Nome = func.Nome,
                Cargo = func.Cargo?.Nome ?? "Sem Cargo",
                NIF = func.Nif,
                Sexo = func.Genero,
                Email = func.Email,
                Telemovel = func.Telefone,
                Morada = func.Morada,

                UrlVoltar = linkVoltar
            };

            return Ok(dadosPerfil);
        }

        /// <summary>
        /// Atualiza os dados de contacto do próprio perfil do utilizador.
        /// </summary>
        /// <param name="dados">DTO com Email, Telemóvel e Morada.</param>
        /// <returns>Mensagem de confirmação.</returns>
        [HttpPut("meu-perfil")]
        public async Task<IActionResult> UpdateMeuPerfil([FromBody] PerfilUpdateDTO dados)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var func = await _context.Funcionarios.FirstOrDefaultAsync(f => f.Username == username);
            if (func == null) return NotFound();

            func.Email = dados.Email;
            func.Telefone = dados.Telemovel;
            func.Morada = dados.Morada;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Dados atualizados com sucesso" });
        }


        [HttpGet("equipa")]
        public async Task<IActionResult> GetMinhaEquipa()
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var supervisor = await _context.Funcionarios
                .FirstOrDefaultAsync(f => f.Username == username);

            if (supervisor == null || supervisor.Id_Departamento <= 0)
                return BadRequest("Supervisor sem departamento associado.");

            var equipa = await _context.Funcionarios
                .Include(f => f.Cargo)
                .Include(f => f.Departamento)
                .Where(f => f.Id_Departamento == supervisor.Id_Departamento)
                .Where(f => f.Ativo == true)
                .Where(f => f.Id_Funcionario != supervisor.Id_Funcionario) 
                .Where(f => f.Cargo.Nome == "Funcionario")
                .OrderBy(f => f.Nome)
                .Select(f => new
                {
                    id = f.Id_Funcionario,
                    nome = f.Nome,
                    email = f.Email,
                    telefone = f.Telefone,
                    cargo = f.Cargo != null ? f.Cargo.Nome : "Sem Cargo",
                    departamento = f.Departamento != null ? f.Departamento.Nome : "Geral"
                })
                .ToListAsync();

            return Ok(equipa);
        }

        /// <summary>
        /// DTO para atualização parcial de perfil.
        /// </summary>
        public class PerfilUpdateDTO
        {
            public string Email { get; set; }
            public string Telemovel { get; set; }
            public string Morada { get; set; }
        }

        /// <summary>
        /// Modelo para pedido de alteração de senha.
        /// </summary>
        public class AlterarSenhaRequest
        {
            public string SenhaAtual { get; set; }
            public string SenhaNova { get; set; }
        }
    }
}