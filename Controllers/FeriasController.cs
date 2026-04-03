using INFOGEST_Web.Data;
using INFOGEST_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace INFOGEST_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeriasController : ControllerBase
    {
        private readonly InfogestDbContext _context;

        public FeriasController(InfogestDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém o resumo do saldo de férias e o histórico de pedidos do utilizador autenticado.
        /// </summary>
        /// <remarks>
        /// Este método recalcula automaticamente os dias de direito com base na data de admissão e no ano corrente.
        /// O saldo disponível é calculado subtraindo os dias gozados e pendentes (contabilizando apenas dias úteis) ao total de direito.
        /// </remarks>
        /// <returns>Um objeto contendo o total de dias, dias usados, saldo disponível e a lista de histórico.</returns>
        [HttpGet("resumo")]
        public async Task<IActionResult> GetResumoFerias()
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var funcionario = await _context.Funcionarios
                .FirstOrDefaultAsync(f => f.Username == username);

            if (funcionario == null) return NotFound();

            // 1. OBTER TOTAIS E USADOS DIRETAMENTE DA BD
            // O Trigger 'TRG_AtualizarSaldoFerias' mantém o campo 'Dias_Ferias_Usados' atualizado.
            // O Stored Procedure 'sp_RecalcularFeriasMensais' (ou lógica similar) deve manter 'Dias_Ferias_Totais' atualizado.
            int diasDireito = funcionario.Dias_Ferias_Totais;
            int diasGozados = funcionario.Dias_Ferias_Usados;

            // 2. CALCULAR PENDENTES USANDO A FUNÇÃO SQL
            // Usamos a função fn_CalcularDiasUteis diretamente na query
            int diasPendentes = await _context.Ausencias
                .Where(a => a.Id_Funcionario == funcionario.Id_Funcionario)
                .Where(a => a.Tipo == "Ferias")
                .Where(a => a.Estado == "Pendente")
                .Select(a => InfogestDbContext.CalcularDiasUteis(a.Data_Inicio, a.Data_Fim)) // <--- A MAGIA ACONTECE AQUI
                .SumAsync();

            // 3. CÁLCULO FINAL
            int diasDisponiveis = diasDireito - diasGozados - diasPendentes;

            // 4. OBTER HISTÓRICO
            var historico = await _context.Ausencias
                .Where(a => a.Id_Funcionario == funcionario.Id_Funcionario && a.Tipo == "Ferias")
                .OrderByDescending(a => a.Data_Inicio)
                .Select(a => new
                {
                    id = a.Id_Ausencia,
                    dataInicio = a.Data_Inicio.ToString("yyyy-MM-dd"),
                    dataFim = a.Data_Fim.ToString("yyyy-MM-dd"),
                    estado = a.Estado,
                    dias = InfogestDbContext.CalcularDiasUteis(a.Data_Inicio, a.Data_Fim) // Mostra também os dias calculados
                })
                .ToListAsync();

            return Ok(new
            {
                diasTotais = diasDireito,
                diasUsados = diasGozados,
                diasPendentes = diasPendentes, // Útil mostrar também os pendentes no front-end
                diasDisponiveis = diasDisponiveis,
                historico = historico
            });
        }

        /// <summary>
        /// Submete um novo pedido de férias para aprovação.
        /// </summary>
        /// <remarks>
        /// Aplica validações rigorosas:
        /// 1. Impede marcação em datas passadas.
        /// 2. Verifica se o funcionário tem saldo suficiente (contando apenas dias úteis do pedido).
        /// 3. Deteta sobreposição com férias já marcadas ou pendentes.
        /// </remarks>
        /// <param name="pedido">Objeto com a data de início e fim pretendidas.</param>
        /// <returns>Mensagem de sucesso ou erro de validação.</returns>
        [HttpPost("pedir")]
        public async Task<IActionResult> PedirFerias([FromBody] PedidoFeriasDTO pedido)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return Unauthorized(new { message = "Sessão expirada." });

            var funcionario = await _context.Funcionarios.FirstOrDefaultAsync(f => f.Username == username);
            if (funcionario == null) return NotFound(new { message = "Funcionário não encontrado." });

            if (pedido.DataInicio.Date < DateTime.Today)
                return BadRequest(new { message = "Não é permitido marcar férias em datas passadas." });

            if (pedido.DataInicio.Date > pedido.DataFim.Date)
                return BadRequest(new { message = "A data de início deve ser anterior à data de fim." });

            int diasDisponiveis = funcionario.Dias_Ferias_Totais - funcionario.Dias_Ferias_Usados;

            int diasSolicitados = 0;
            for (var date = pedido.DataInicio.Date; date <= pedido.DataFim.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    diasSolicitados++;
                }
            }

            if (diasSolicitados > diasDisponiveis)
            {
                return BadRequest(new
                {
                    message = $"Saldo insuficiente. Tem {diasDisponiveis} dias disponíveis, mas pediu {diasSolicitados} dias úteis."
                });
            }

            bool temSobreposicao = await _context.Ausencias
                .AnyAsync(a => a.Id_Funcionario == funcionario.Id_Funcionario
                            && a.Tipo == "Ferias"
                            && a.Estado != "Recusado" 
                            && a.Data_Inicio <= pedido.DataFim
                            && a.Data_Fim >= pedido.DataInicio);

            if (temSobreposicao)
            {
                return BadRequest(new { message = "Já tem férias ou um pedido pendente neste período." });
            }

            var novaAusencia = new Ausencia
            {
                Id_Funcionario = funcionario.Id_Funcionario,
                Tipo = "Ferias",
                Data_Inicio = pedido.DataInicio,
                Data_Fim = pedido.DataFim,
                Motivo = "Pedido via Portal",
                Estado = "Pendente"
            };

            try
            {
                _context.Ausencias.Add(novaAusencia);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro de BD: " + (ex.InnerException?.Message ?? ex.Message) });
            }

            return Ok(new { message = "Pedido submetido com sucesso!" });
        }

        /// <summary>
        /// Objeto de transferência de dados (DTO) para pedidos de férias.
        /// </summary>
        public class PedidoFeriasDTO
        {
            public DateTime DataInicio { get; set; }
            public DateTime DataFim { get; set; }
        }
    }
}