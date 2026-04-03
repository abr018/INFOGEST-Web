using INFOGEST_Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace INFOGEST_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DadosEmpresaController : ControllerBase
    {
        private readonly InfogestDbContext _context;

        public DadosEmpresaController(InfogestDbContext context)
        {
            _context = context;
        }

        // Método auxiliar privado (não necessita de documentação XML pública)
        private async Task<int?> GetIdEmpresaUtilizadorLogado()
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

            return funcionario.Departamento.Filial.Id_Empresa;
        }

        /// <summary>
        /// Obtém a estatística de funcionários por filial da empresa do utilizador autenticado.
        /// </summary>
        /// <remarks>
        /// Retorna apenas as filiais ativas associadas à empresa da sessão atual.
        /// </remarks>
        /// <returns>Uma lista contendo o nome da filial (Label) e a quantidade de funcionários (Value).</returns>
        [HttpGet("funcionarios-por-filial")]
        public async Task<IActionResult> GetFuncionariosPorFilial()
        {
            int? idEmpresa = await GetIdEmpresaUtilizadorLogado();

            if (idEmpresa == null)
            {
                return Unauthorized("Sessão expirada ou utilizador sem empresa.");
            }

            var dados = await _context.Filiais
                .Where(f => f.Id_Empresa == idEmpresa && f.Ativo == true)
                .Select(f => new
                {
                    Label = f.Nome,
                    Value = f.Departamentos.SelectMany(d => d.Funcionarios).Count(func => func.Ativo == true)
                })
                .ToListAsync();

            return Ok(dados);
        }

        /// <summary>
        /// Calcula a soma dos salários brutos por filial ativa da empresa do utilizador.
        /// </summary>
        /// <remarks>
        /// Apenas filiais com um custo salarial superior a zero são incluídas nos resultados.
        /// Útil para gráficos de distribuição de custos.
        /// </remarks>
        /// <returns>Uma lista de objetos com o nome da filial e o valor total dos salários.</returns>
        [HttpGet("custos-por-filial")]
        public async Task<IActionResult> GetCustosPorFilial()
        {
            int? idEmpresa = await GetIdEmpresaUtilizadorLogado();

            if (idEmpresa == null)
            {
                return Unauthorized("Sessão expirada ou utilizador sem empresa.");
            }

            var dados = await _context.Filiais
                .Where(f => f.Id_Empresa == idEmpresa && f.Ativo == true) 
                .Select(f => new
                {
                    Label = f.Nome,
                    Value = f.Departamentos
                             .SelectMany(d => d.Funcionarios)
                             .Where(func => func.Ativo == true)
                             .Sum(func => (decimal?)func.Salario_Bruto) ?? 0
                })
                .Where(f => f.Value > 0)
                .ToListAsync();

            return Ok(dados);
        }

        /// <summary>
        /// Obtém o número total de filiais ativas associadas à empresa atual.
        /// </summary>
        /// <returns>O número total de filiais.</returns>
        [HttpGet("total-filiais")]
        public async Task<IActionResult> GetTotalFiliais()
        {
            int? idEmpresa = await GetIdEmpresaUtilizadorLogado();

            if (idEmpresa == null) return Unauthorized();

            int totalFiliais = await _context.Filiais
                .Where(f => f.Id_Empresa == idEmpresa && f.Ativo == true)
                .CountAsync();

            return Ok(totalFiliais);
        }

        /// <summary>
        /// Obtém o número total de departamentos ativos na empresa do utilizador.
        /// </summary>
        /// <returns>O número total de departamentos.</returns>
        [HttpGet("total-departamentos")]
        public async Task<IActionResult> GetTotalDepartamentos()
        {
            int? idEmpresa = await GetIdEmpresaUtilizadorLogado();

            if (idEmpresa == null) return Unauthorized();

            int totalDepartamentos = await _context.Departamentos
                .Include(d => d.Filial)
                .Where(d => d.Filial.Id_Empresa == idEmpresa && d.Ativo == true)
                .CountAsync();

            return Ok(totalDepartamentos);
        }

        /// <summary>
        /// Obtém o número total de funcionários ativos registados na empresa.
        /// </summary>
        /// <returns>A contagem total de funcionários ativos.</returns>
        [HttpGet("total-funcionarios")]
        public async Task<IActionResult> GetTotalFuncionarios()
        {
            int? idEmpresa = await GetIdEmpresaUtilizadorLogado();

            if (idEmpresa == null) return Unauthorized();

            var totalFuncionarios = await _context.Funcionarios
                .CountAsync(f => f.Departamento.Filial.Id_Empresa == idEmpresa && f.Ativo == true);

            return Ok(totalFuncionarios);
        }

        [HttpGet("total-gastos")]
        public async Task<IActionResult> GetTotalGastos()
        {
            int? idEmpresa = await GetIdEmpresaUtilizadorLogado();
            if (idEmpresa == null) return Unauthorized();
            var totalGastos = await _context.Filiais
                .Where(f => f.Id_Empresa == idEmpresa && f.Ativo == true)
                .SelectMany(f => f.Departamentos)
                .SelectMany(d => d.Funcionarios)
                .Where(func => func.Ativo == true)
                .SumAsync(func => (decimal?)func.Salario_Bruto) ?? 0;
            return Ok(totalGastos);
        }

    }
}