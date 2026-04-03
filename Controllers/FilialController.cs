using INFOGEST_Web.Data;
using INFOGEST_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace INFOGEST_Web.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class FiliaisController : ControllerBase
    {
        private readonly InfogestDbContext _context;

        public FiliaisController(InfogestDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém a lista de filiais ativas associadas à empresa do utilizador autenticado.
        /// </summary>
        /// <remarks>
        /// Retorna apenas os dados essenciais (ID, Nome, Morada, Telefone) das filiais ativas.
        /// </remarks>
        /// <returns>Uma lista simplificada de filiais.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetFiliais()
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var funcionario = await _context.Funcionarios
                .Include(f => f.Departamento)
                .ThenInclude(d => d.Filial)
                .FirstOrDefaultAsync(f => f.Username == username);

            if (funcionario?.Departamento?.Filial == null)
            {
                return BadRequest("O utilizador não está associado a nenhuma empresa.");
            }

            int idEmpresaSessao = funcionario.Departamento.Filial.Id_Empresa;

            var filiais = await _context.Filiais
                .Where(f => f.Id_Empresa == idEmpresaSessao)
                .Where(f => f.Ativo == true)
                .Select(f => new
                {
                    id_Filial = f.Id_Filial,
                    nome = f.Nome,
                    morada = f.Morada,
                    telefone = f.Telefone
                })
                .ToListAsync();

            return Ok(filiais);
        }

        /// <summary>
        /// Obtém os detalhes completos de uma filial específica pelo seu ID.
        /// </summary>
        /// <param name="id">O identificador único da filial.</param>
        /// <returns>O objeto Filial completo.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Filial>> GetFilial(int id)
        {
            var filial = await _context.Filiais
                .Include(f => f.Empresa) 
                .FirstOrDefaultAsync(f => f.Id_Filial == id);

            if (filial == null)
            {
                return NotFound();
            }

            return filial;
        }

        /// <summary>
        /// Cria uma nova filial associada automaticamente à empresa do utilizador autenticado.
        /// </summary>
        /// <param name="filial">Os dados da nova filial.</param>
        /// <returns>A filial criada e a rota para consulta.</returns>
        [HttpPost]
        public async Task<ActionResult<Filial>> PostFilial(Filial filial)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var funcionario = await _context.Funcionarios
                .Include(f => f.Departamento)
                .ThenInclude(d => d.Filial)
                .FirstOrDefaultAsync(f => f.Username == username);

            if (funcionario?.Departamento?.Filial == null)
            {
                return BadRequest("Utilizador sem empresa associada.");
            }

            filial.Id_Empresa = funcionario.Departamento.Filial.Id_Empresa;
            filial.Ativo = true;

            _context.Filiais.Add(filial);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFilial), new { id = filial.Id_Filial }, filial);
        }

        /// <summary>
        /// Cria uma filial (ex: Sede) especificando manualmente a empresa (Acesso Público).
        /// </summary>
        /// <remarks>
        /// Endpoint utilizado para configurações iniciais onde não existe sessão de utilizador.
        /// Verifica a existência da empresa e duplicidade de nomes.
        /// </remarks>
        /// <param name="filial">Dados da filial incluindo o ID da Empresa.</param>
        /// <returns>A filial criada.</returns>
        [AllowAnonymous]
        [HttpPost("sede")]
        public async Task<ActionResult<Filial>> PostFilialSede(Filial filial)
        {
            if (filial.Id_Empresa <= 0)
            {
                return BadRequest(new { message = "O ID da Empresa é inválido ou não foi fornecido." });
            }

            bool empresaExiste = await _context.Empresas.AnyAsync(e => e.Id_Empresa == filial.Id_Empresa);
            if (!empresaExiste)
            {
                return NotFound(new { message = "A empresa indicada não existe no sistema." });
            }

            bool nomeDuplicado = await _context.Filiais
                .AnyAsync(f => f.Nome == filial.Nome && f.Id_Empresa == filial.Id_Empresa);

            if (nomeDuplicado)
            {
                return Conflict(new { message = "Já existe uma filial com esse nome nesta empresa." });
            }

            filial.Ativo = true; 

            try
            {
                _context.Filiais.Add(filial);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno ao criar filial: " + ex.Message });
            }

            return CreatedAtAction("GetFilial", new { id = filial.Id_Filial }, filial);
        }

        /// <summary>
        /// Atualiza os dados (Nome, Telefone, Morada) de uma filial existente.
        /// </summary>
        /// <param name="id">O ID da filial a atualizar.</param>
        /// <param name="filialAtualizada">Os novos dados.</param>
        /// <returns>NoContent se bem-sucedido.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFilial(int id, Filial filialAtualizada)
        {
            if (id != filialAtualizada.Id_Filial)
            {
                return BadRequest("ID inconsistente.");
            }

            var filialOriginal = await _context.Filiais.FindAsync(id);

            if (filialOriginal == null)
            {
                return NotFound();
            }

            filialOriginal.Nome = filialAtualizada.Nome;
            filialOriginal.Telefone = filialAtualizada.Telefone;
            filialOriginal.Morada = filialAtualizada.Morada;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FilialExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Inativa uma filial (Soft Delete) e desativa todos os departamentos associados.
        /// </summary>
        /// <remarks>
        /// A operação é bloqueada se existirem funcionários ativos associados a esta filial.
        /// </remarks>
        /// <param name="id">O ID da filial a inativar.</param>
        /// <returns>NoContent se bem-sucedido, Conflict (409) se houver funcionários.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFilial(int id)
        {
            var filial = await _context.Filiais
                .Include(f => f.Departamentos)
                .FirstOrDefaultAsync(f => f.Id_Filial == id);

            if (filial == null) return NotFound();

            bool temFuncionarios = await _context.Funcionarios
                .Include(func => func.Departamento)
                .AnyAsync(func =>
                    func.Ativo == true &&
                    func.Departamento != null && 
                    func.Departamento.Id_Filial == id
                );

            if (temFuncionarios)
            {
                return StatusCode(409, "Bloqueado: Existem funcionários nesta filial.");
            }

            filial.Ativo = false;

            if (filial.Departamentos != null)
            {
                foreach (var dep in filial.Departamentos)
                {
                    dep.Ativo = false;
                }
            }

            _context.Entry(filial).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool FilialExists(int id)
        {
            return _context.Filiais.Any(e => e.Id_Filial == id);
        }
    }
}