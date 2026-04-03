using INFOGEST_Web.Data;
using INFOGEST_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace INFOGEST_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaltasDepartamentoController : ControllerBase
    {
        private readonly InfogestDbContext _context;

        public FaltasDepartamentoController(InfogestDbContext context)
        {
            _context = context;
        }

        
        [HttpGet]
        public async Task<IActionResult> GetFaltasEquipa()
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var supervisor = await _context.Funcionarios
                .Include(f => f.Departamento)
                .FirstOrDefaultAsync(f => f.Username == username);

            if (supervisor == null) return NotFound(new { message = "Utilizador não encontrado." });

            if (supervisor.Id_Departamento <= 0)
                return BadRequest(new { message = "Utilizador sem departamento associado." });

            int idDepartamento = supervisor.Id_Departamento;

            var listaFaltas = await _context.Ausencias
                .Include(a => a.Funcionario)
                .Where(a => a.Funcionario.Id_Departamento == idDepartamento)
                .Where(a => a.Id_Funcionario != supervisor.Id_Funcionario)
                .OrderByDescending(a => a.Data_Inicio)
                .Select(a => new
                {
                    id = a.Id_Ausencia,
                    funcionario = a.Funcionario.Nome,
                    data = a.Data_Inicio.ToString("yyyy-MM-dd"),
                    motivo = a.Motivo,
                    estado = a.Estado
                })
                .ToListAsync();

            return Ok(listaFaltas);
        }

        
        [HttpPut("{id}/estado")]
        public async Task<IActionResult> AtualizarEstado(int id, [FromBody] EstadoFaltaRequest request)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var supervisor = await _context.Funcionarios.FirstOrDefaultAsync(f => f.Username == username);
            if (supervisor == null || supervisor.Id_Departamento <= 0) return Unauthorized();

            var ausencia = await _context.Ausencias
                .Include(a => a.Funcionario)
                .FirstOrDefaultAsync(a => a.Id_Ausencia == id);

            if (ausencia == null) return NotFound(new { message = "Ausência não encontrada." });

            if (ausencia.Funcionario.Id_Departamento != supervisor.Id_Departamento)
            {
                return StatusCode(403, new { message = "Não tem permissão." });
            }

            ausencia.Estado = request.NovoEstado;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Estado atualizado para {request.NovoEstado}." });
        }

        /// <summary>
        /// DTO para receber a alteração de estado.
        /// </summary>
        public class EstadoFaltaRequest
        {
            public string NovoEstado { get; set; }
        }
    }
}