using INFOGEST_Web.Data;
using INFOGEST_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace INFOGEST_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PresencasController : ControllerBase
    {
        private readonly InfogestDbContext _context;

        public PresencasController(InfogestDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém o estado atual e o histórico de registos de ponto do dia para o utilizador autenticado.
        /// </summary>
        /// <remarks>
        /// Este endpoint verifica se o funcionário está atualmente "dentro" ou "fora" da empresa
        /// para ajustar a interface (mostrar botão de Entrada ou Saída).
        /// </remarks>
        /// <returns>Um objeto contendo uma flag de estado (estaDentro) e a lista de registos do dia.</returns>
        [HttpGet("hoje")]
        public async Task<IActionResult> GetRegistosHoje()
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var funcionario = await _context.Funcionarios.FirstOrDefaultAsync(f => f.Username == username);
            if (funcionario == null) return NotFound();

            var hoje = DateTime.Today;

            var registos = await _context.Presencas
                .Where(p => p.Id_Funcionario == funcionario.Id_Funcionario && p.Data == hoje)
                .OrderBy(p => p.Hora_Entrada)
                .ToListAsync();

            var ultimo = registos.LastOrDefault();
            bool estaDentro = ultimo != null && ultimo.Hora_Entrada != TimeSpan.Zero && (ultimo.Hora_Saida == null || ultimo.Hora_Saida == TimeSpan.Zero);

            return Ok(new
            {
                estaDentro = estaDentro,
                registos = registos.Select(r => new 
                {
                    horaEntrada = r.Hora_Entrada.ToString(@"hh\:mm"),
                    horaSaida = r.Hora_Saida != TimeSpan.Zero ? r.Hora_Saida.ToString(@"hh\:mm") : null,
                    estado = r.Estado
                })
            });
        }

        /// <summary>
        /// Regista uma nova movimentação de ponto (Entrada ou Saída).
        /// </summary>
        /// <remarks>
        /// Aplica validações de sequência lógica:
        /// 1. Não permite registar "Entrada" se o utilizador já estiver dentro.
        /// 2. Não permite registar "Saída" se não houver uma entrada aberta.
        /// </remarks>
        /// <param name="request">Objeto indicando o tipo de movimento ("Entrada" ou "Saida").</param>
        /// <returns>Mensagem de sucesso ou erro de validação lógica.</returns>
        [HttpPost("registar")]
        public async Task<IActionResult> RegistarPonto([FromBody] PontoRequest request)
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var funcionario = await _context.Funcionarios.FirstOrDefaultAsync(f => f.Username == username);
            if (funcionario == null) return NotFound();

            var hoje = DateTime.Today;
            var horaAtual = DateTime.Now.TimeOfDay;

            var ultimoRegisto = await _context.Presencas
                .Where(p => p.Id_Funcionario == funcionario.Id_Funcionario && p.Data == hoje)
                .OrderByDescending(p => p.Hora_Entrada)
                .FirstOrDefaultAsync();

            if (request.Tipo == "Entrada")
            {
                if (ultimoRegisto != null && (ultimoRegisto.Hora_Saida == null || ultimoRegisto.Hora_Saida == TimeSpan.Zero))
                {
                    return BadRequest(new { message = "Já efetuou a entrada. Deve registar a saída primeiro." });
                }

                var novaPresenca = new Presenca
                {
                    Id_Funcionario = funcionario.Id_Funcionario,
                    Data = hoje,
                    Hora_Entrada = horaAtual,
                    Hora_Saida = TimeSpan.Zero,
                    Estado = "Presente"
                };

                _context.Presencas.Add(novaPresenca);
            }
            else if (request.Tipo == "Saida")
            {
                if (ultimoRegisto == null || (ultimoRegisto.Hora_Saida != null && ultimoRegisto.Hora_Saida != TimeSpan.Zero))
                {
                    return BadRequest(new { message = "Não existe uma entrada aberta para registar saída." });
                }

                ultimoRegisto.Hora_Saida = horaAtual;
                _context.Entry(ultimoRegisto).State = EntityState.Modified;
            }
            else
            {
                return BadRequest("Tipo inválido.");
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Ponto registado com sucesso!" });
        }

        /// <summary>
        /// DTO para o pedido de registo de ponto.
        /// </summary>
        public class PontoRequest
        {
            public string Tipo { get; set; }
        }
    }
}