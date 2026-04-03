using INFOGEST_Web.Data;
using INFOGEST_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace INFOGEST_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaltasController : ControllerBase
    {
        private readonly InfogestDbContext _context;

        public FaltasController(InfogestDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém o histórico pessoal de faltas e justificações do utilizador autenticado.
        /// </summary>
        /// <remarks>
        /// Retorna uma lista com o estado das justificações (Pendente, Aprovado, Rejeitado) e se existe documento anexado.
        /// </remarks>
        /// <returns>Uma lista de objetos contendo os detalhes das ausências.</returns>
        [HttpGet("Minhas")]
        public async Task<ActionResult<IEnumerable<object>>> GetMinhasFaltas()
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var funcionario = await _context.Funcionarios.FirstOrDefaultAsync(f => f.Username == username);
            if (funcionario == null) return NotFound();

            var faltas = await _context.Ausencias
                .Where(a => a.Id_Funcionario == funcionario.Id_Funcionario)
                .OrderByDescending(a => a.Data_Inicio)
                .Select(a => new
                {
                    tipo = a.Tipo,
                    dataInicio = a.Data_Inicio.ToString("yyyy-MM-dd"),
                    dataFim = a.Data_Fim.ToString("yyyy-MM-dd"),
                    motivo = a.Motivo,
                    temDocumento = !string.IsNullOrEmpty(a.Caminho_Documento),
                    estado = a.Estado
                })
                .ToListAsync();

            return Ok(faltas);
        }

        /// <summary>
        /// Submete uma justificação para uma falta ocorrida (registo de ausência).
        /// </summary>
        /// <remarks>
        /// Regras de validação aplicadas:
        /// 1. A data de início não pode ser posterior à data de fim.
        /// 2. Não é permitido registar faltas futuras (utilizar o menu de Férias para esse efeito).
        /// 3. Existe um prazo limite de 5 dias para justificar faltas passadas.
        /// </remarks>
        /// <param name="pedido">Objeto contendo o tipo de falta e o intervalo de datas.</param>
        /// <returns>O registo da ausência criado com sucesso.</returns>
        [HttpPost]
        public async Task<ActionResult<Ausencia>> PostFalta([FromBody] PedidoFaltaDTO pedido)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return Unauthorized(new { message = "Sessão expirada." });

            var funcionario = await _context.Funcionarios.FirstOrDefaultAsync(f => f.Username == username);
            if (funcionario == null) return NotFound(new { message = "Funcionário não encontrado." });

            if (pedido.DataInicio > pedido.DataFim)
            {
                return BadRequest(new { message = "A data de início não pode ser superior à data de fim." });
            }

            if (pedido.DataInicio > DateTime.Today)
            {
                return BadRequest(new { message = "Este formulário serve apenas para justificar faltas passadas. Para agendar férias, use o menu 'Férias'." });
            }

            int diasLimiteParaJustificar = 5;
            DateTime dataLimiteAceitavel = DateTime.Today.AddDays(-diasLimiteParaJustificar);

            if (pedido.DataInicio < dataLimiteAceitavel)
            {
                return BadRequest(new { message = $"O prazo de {diasLimiteParaJustificar} dias para justificar esta falta já expirou." });
            }

            var novaAusencia = new Ausencia
            {
                Id_Funcionario = funcionario.Id_Funcionario,
                Tipo = pedido.Tipo,
                Data_Inicio = pedido.DataInicio,
                Data_Fim = pedido.DataFim,
                Motivo = "Justificação submetida pelo portal",
                Estado = "Pendente"
            };

            try
            {
                _context.Ausencias.Add(novaAusencia);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao registar: " + (ex.InnerException?.Message ?? ex.Message) });
            }

            return CreatedAtAction(nameof(GetMinhasFaltas), new { id = novaAusencia.Id_Ausencia }, novaAusencia);
        }

        /// <summary>
        /// DTO para transferência de dados no pedido de falta.
        /// </summary>
        public class PedidoFaltaDTO
        {
            public string Tipo { get; set; }
            public DateTime DataInicio { get; set; }
            public DateTime DataFim { get; set; }
        }
    }
}