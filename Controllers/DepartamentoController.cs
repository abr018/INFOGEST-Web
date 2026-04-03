using INFOGEST_Web.Data;
using INFOGEST_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace INFOGEST_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartamentosController : ControllerBase
    {
        private readonly InfogestDbContext _context;

        public DepartamentosController(InfogestDbContext context)
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
        /// Obtém a lista de departamentos ativos associados à empresa do utilizador autenticado.
        /// </summary>
        /// <remarks>
        /// O sistema filtra automaticamente os departamentos pela empresa do utilizador que fez o pedido.
        /// Retorna apenas registos marcados como ativos.
        /// </remarks>
        /// <returns>Uma lista de departamentos ou uma mensagem de erro se a lista estiver vazia.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Departamento>>> GetDepartamentos()
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("Sessão inválida ou expirada.");
            }

            var funcionario = await _context.Funcionarios
                .Include(f => f.Departamento)
                .ThenInclude(d => d.Filial)
                .FirstOrDefaultAsync(f => f.Username == username);

            if (funcionario?.Departamento?.Filial == null)
            {
                return BadRequest("O utilizador não está associado a nenhuma empresa.");
            }

            int idEmpresaSessao = funcionario.Departamento.Filial.Id_Empresa;

            var departamentos = await _context.Departamentos
                .Include(d => d.Filial) 
                .Where(d => d.Filial.Id_Empresa == idEmpresaSessao)
                .Where(d => d.Ativo == true) 
                .ToListAsync();

            if (departamentos == null || !departamentos.Any())
            {
                return NotFound("Nenhum departamento encontrado.");
            }

            return departamentos;
        }

        [HttpGet("GerirDepartamentosFil")]
        public async Task<ActionResult<IEnumerable<Departamento>>> GetDepartamentosFilial()
        {
            // Obtém o ID da filial através da sessão
            int? idFilial = await GetIdFilialUtilizadorLogado();

            if (idFilial == null)
            {
                return Unauthorized("Sessão inválida ou o utilizador não possui filial associada.");
            }

            // Busca apenas os departamentos desta filial específica
            var departamentos = await _context.Departamentos
                .Include(d => d.Filial) // Importante para mostrar o nome da filial na tabela
                .Where(d => d.Id_Filial == idFilial)
                .Where(d => d.Ativo == true)
                .ToListAsync();

            if (departamentos == null || !departamentos.Any())
            {
                return NotFound("Nenhum departamento encontrado para esta filial.");
            }

            return Ok(departamentos);
        }

        /// <summary>
        /// Obtém os detalhes de um departamento específico pelo seu ID.
        /// </summary>
        /// <param name="id">O identificador único do departamento.</param>
        /// <returns>O objeto Departamento se encontrado.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Departamento>> GetDepartamento(int id)
        {
            var departamento = await _context.Departamentos
                .Include(d => d.Filial)
                .FirstOrDefaultAsync(d => d.Id_Departamento == id);

            if (departamento == null)
            {
                return NotFound();
            }

            return departamento;
        }

        /// <summary>
        /// Cria um novo departamento na base de dados.
        /// </summary>
        /// <param name="departamento">O objeto com os dados do novo departamento.</param>
        /// <returns>O departamento criado e a rota para o consultar.</returns>
        [HttpPost]
        public async Task<ActionResult<Departamento>> PostDepartamento(Departamento departamento)
        {
            int? idFilialSessao = await GetIdFilialUtilizadorLogado();

            if (idFilialSessao == null)
            {
                return Unauthorized("Não é possível criar departamentos sem estar associado a uma filial.");
            }

            departamento.Id_Filial = idFilialSessao.Value;
            departamento.Ativo = true;

            _context.Departamentos.Add(departamento);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDepartamento), new { id = departamento.Id_Departamento }, departamento);
        }

        [HttpPost("Publico")] // Rota: /api/Departamentos/Publico
        public async Task<ActionResult<Departamento>> PostDepartamentoPublico([FromBody] Departamento departamento)
        {
            // Em vez de buscar na sessão, validamos se o ID foi enviado no JSON
            if (departamento.Id_Filial <= 0)
            {
                return BadRequest("O ID da filial é obrigatório.");
            }

            try
            {
                // Forçamos o estado como Ativo
                departamento.Ativo = true;

                _context.Departamentos.Add(departamento);
                await _context.SaveChangesAsync();

                // Retorna o objeto criado com o novo ID
                return CreatedAtAction(nameof(GetDepartamento), new { id = departamento.Id_Departamento }, departamento);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Atualiza os dados de um departamento existente.
        /// </summary>
        /// <param name="id">O ID do departamento a ser atualizado.</param>
        /// <param name="departamento">Os novos dados do departamento.</param>
        /// <returns>NoContent em caso de sucesso.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDepartamento(int id, Departamento departamento)
        {
            if (id != departamento.Id_Departamento)
            {
                return BadRequest();
            }

            _context.Entry(departamento).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync(); 
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Departamentos.Any(e => e.Id_Departamento == id))
                {
                    return NotFound();
                }
                else
                {
                    throw; 
                }
            }

            return NoContent();
        }

        /// <summary>
        /// Inativa um departamento (Soft Delete), impedindo a sua utilização futura.
        /// </summary>
        /// <remarks>
        /// A operação é bloqueada se existirem funcionários ativos associados a este departamento.
        /// </remarks>
        /// <param name="id">O ID do departamento a inativar.</param>
        /// <returns>NoContent se for bem-sucedido, ou Conflict (409) se tiver funcionários.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDepartamento(int id)
        {
            var departamento = await _context.Departamentos
                .FirstOrDefaultAsync(d => d.Id_Departamento == id);

            if (departamento == null) return NotFound();

            bool temFuncionarios = await _context.Funcionarios
                .AnyAsync(func =>
                    func.Ativo == true &&
                    func.Id_Departamento == id 
                );

            if (temFuncionarios)
            {
                return StatusCode(409, "Bloqueado: Existem funcionários associados a este departamento.");
            }

            departamento.Ativo = false;

            _context.Entry(departamento).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}