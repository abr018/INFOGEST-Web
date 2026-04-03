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
    public class CargosController : ControllerBase
    {
        private readonly InfogestDbContext _context;

        public CargosController(InfogestDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém a lista de todos os cargos registados na base de dados.
        /// </summary>
        /// <returns>Uma lista de objetos do tipo Cargo.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cargo>>> GetCargos()
        {
            return await _context.Cargos.ToListAsync();
        }

        /// <summary>
        /// Obtém os detalhes de um cargo específico através do seu ID.
        /// </summary>
        /// <param name="id">O identificador único do cargo.</param>
        /// <returns>O objeto Cargo se for encontrado, ou NotFound caso contrário.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Cargo>> GetCargo(int id)
        {
            var cargo = await _context.Cargos.FindAsync(id);

            if (cargo == null)
            {
                return NotFound();
            }

            return cargo;
        }


        /// <summary>
        /// Cria um novo registo de cargo na base de dados.
        /// </summary>
        /// <param name="cargo">O objeto com os dados do novo cargo.</param>
        /// <returns>O cargo criado e a rota para o consultar.</returns>
        [HttpPost]
        public async Task<ActionResult<Cargo>> PostCargo(Cargo cargo)
        {
            _context.Cargos.Add(cargo);
            await _context.SaveChangesAsync(); 

            return CreatedAtAction(nameof(GetCargo), new { id = cargo.Id_Cargo }, cargo);
        }

        /// <summary>
        /// Atualiza os dados de um cargo existente.
        /// </summary>
        /// <param name="id">O ID do cargo a ser atualizado.</param>
        /// <param name="cargo">O objeto com os dados atualizados.</param>
        /// <returns>NoContent se a atualização for bem-sucedida.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCargo(int id, Cargo cargo)
        {
            if (id != cargo.Id_Cargo)
            {
                return BadRequest();
            }

            _context.Entry(cargo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync(); 
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Cargos.Any(e => e.Id_Cargo == id))
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
    }
}