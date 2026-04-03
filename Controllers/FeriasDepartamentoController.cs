using INFOGEST_Web.Data;
using INFOGEST_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace INFOGEST_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeriasDepartamentoController : ControllerBase
    {
        private readonly InfogestDbContext _context;

        public FeriasDepartamentoController(InfogestDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetFeriasEquipa()
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var supervisor = await _context.Funcionarios
                .Include(f => f.Departamento)
                .FirstOrDefaultAsync(f => f.Username == username);

            if (supervisor == null) return NotFound(new { message = "User not found." });

            if (supervisor.Id_Departamento <= 0)
                return BadRequest(new { message = "User has no associated department." });

            int idDepartamento = supervisor.Id_Departamento;

            var listaFerias = await _context.Ausencias
                .Include(a => a.Funcionario)
                .Where(a => a.Funcionario.Id_Departamento == idDepartamento) 
                .Where(a => a.Tipo == "Ferias") 
                .Where(a => a.Id_Funcionario != supervisor.Id_Funcionario) 
                .OrderByDescending(a => a.Data_Inicio)
                .Select(a => new
                {
                    id = a.Id_Ausencia,
                    funcionario = a.Funcionario.Nome,
                    inicio = a.Data_Inicio.ToString("yyyy-MM-dd"),
                    fim = a.Data_Fim.ToString("yyyy-MM-dd"),
                    dias = EF.Functions.DateDiffDay(a.Data_Inicio, a.Data_Fim) + 1,
                    estado = a.Estado
                })
                .ToListAsync();

            return Ok(listaFerias);
        }

        [HttpPut("{id}/estado")]
        public async Task<IActionResult> DecidirFerias(int id, [FromBody] EstadoFeriasRequest request)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var supervisor = await _context.Funcionarios.FirstOrDefaultAsync(f => f.Username == username);
            if (supervisor == null || supervisor.Id_Departamento <= 0) return Unauthorized();

            var ausencia = await _context.Ausencias
                .Include(a => a.Funcionario)
                .FirstOrDefaultAsync(a => a.Id_Ausencia == id);

            if (ausencia == null) return NotFound(new { message = "Request not found." });

            if (ausencia.Funcionario.Id_Departamento != supervisor.Id_Departamento)
            {
                return StatusCode(403, new { message = "Permission denied." });
            }

            if (request.NovoEstado != "Aprovado" && request.NovoEstado != "Recusado" && request.NovoEstado != "Recusado")
            {
                return BadRequest(new { message = "Invalid status. Use: Aprovado or Rejeitado." });
            }

            ausencia.Estado = request.NovoEstado;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Request updated to {request.NovoEstado}." });
        }

        // DTO Class for the request body
        public class EstadoFeriasRequest
        {
            public string NovoEstado { get; set; }
        }
    }
}