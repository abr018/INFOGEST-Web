using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INFOGEST_Web.Data;
using INFOGEST_Web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace INFOGEST_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmpresasController : ControllerBase
    {
        private readonly InfogestDbContext _context;

        public EmpresasController(InfogestDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém a lista de todas as empresas registadas no sistema.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite acesso anónimo (público) para listagem geral.
        /// </remarks>
        /// <returns>Uma lista de objetos do tipo Empresa.</returns>
        [HttpGet]
        [AllowAnonymous] 
        public async Task<ActionResult<IEnumerable<Empresa>>> GetEmpresas()
        {
            return await _context.Empresas.ToListAsync();
        }

        /// <summary>
        /// Obtém os detalhes de uma empresa específica através do seu ID.
        /// </summary>
        /// <param name="id">O identificador único da empresa.</param>
        /// <returns>O objeto Empresa se encontrado, ou NotFound caso contrário.</returns>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Empresa>> GetEmpresa(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);
            if (empresa == null) { return NotFound(); }
            return empresa;
        }

        /// <summary>
        /// Regista uma nova empresa na base de dados.
        /// </summary>
        /// <remarks>
        /// Endpoint acessível anonimamente para permitir registos iniciais.
        /// </remarks>
        /// <param name="empresa">O objeto com os dados da nova empresa.</param>
        /// <returns>A empresa criada com o código 201 Created.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<Empresa>> PostEmpresa(Empresa empresa)
        {
            _context.Empresas.Add(empresa);
            await _context.SaveChangesAsync();

            return StatusCode(201, empresa);
        }

        /// <summary>
        /// Atualiza os dados de uma empresa existente.
        /// </summary>
        /// <param name="id">O ID da empresa a atualizar.</param>
        /// <param name="empresa">Os novos dados da empresa.</param>
        /// <returns>NoContent se a atualização for bem-sucedida.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmpresa(int id, Empresa empresa)
        {
            if (id != empresa.Id_Empresa) { return BadRequest(); }

            _context.Entry(empresa).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Empresas.Any(e => e.Id_Empresa == id)) { return NotFound(); }
                else { throw; }
            }

            return NoContent(); 
        }

        /// <summary>
        /// Elimina permanentemente uma empresa da base de dados.
        /// </summary>
        /// <param name="id">O ID da empresa a eliminar.</param>
        /// <returns>NoContent se a eliminação for bem-sucedida.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmpresa(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);
            if (empresa == null) { return NotFound(); }

            _context.Empresas.Remove(empresa);
            await _context.SaveChangesAsync();

            return NoContent(); 
        }
    }
}