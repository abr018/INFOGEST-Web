using INFOGEST_Web.Data;
using INFOGEST_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace INFOGEST_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DadosFiliaisController : ControllerBase
    {
        private readonly InfogestDbContext _context;

        public DadosFiliaisController(InfogestDbContext context)
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

        [HttpGet("funcionarios-por-departamento")]
        public async Task<IActionResult> GetFuncionariosPorFilial()
        {
            int? idEmpresa = await GetIdEmpresaUtilizadorLogado();
            int? idFilial = await GetIdFilialUtilizadorLogado();

            if (idEmpresa == null) return Unauthorized();
            if (idFilial == null) return Unauthorized();

            var dados = await _context.Departamentos
                .Where(f => f.Id_Filial == idFilial
                         && f.Ativo == true)
                .Select(f => new
                {
                    Label = f.Nome,
                    Value = f.Funcionarios
                             .Count(func => func.Ativo == true)
                })
                .Where(item => item.Value > 0)
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
        [HttpGet("custos-por-departamento")]
        public async Task<IActionResult> GetCustosPorFilial()
        {
            int? idEmpresa = await GetIdEmpresaUtilizadorLogado();
            int? idFilial = await GetIdFilialUtilizadorLogado();

            if (idEmpresa == null) return Unauthorized();
            if (idFilial == null) return Unauthorized();

            var dados = await _context.Departamentos
                .Where(f => f.Id_Filial == idFilial
                         && f.Ativo == true)
                .Select(f => new
                {
                    Label = f.Nome,
                    Value = f.Funcionarios
                             .Where(func => func.Ativo == true)
                             .Sum(func => func.Salario_Bruto)
                })
                .Where(f => f.Value > 0)
                .ToListAsync();

            return Ok(dados);
        }

        /// <summary>
        /// Obtém o número total de departamentos ativos na empresa do utilizador.
        /// </summary>
        /// <returns>O número total de departamentos.</returns>
        [HttpGet("total-departamentos")]
        public async Task<IActionResult> GetTotalDepartamentos()
        {
            int? idEmpresa = await GetIdEmpresaUtilizadorLogado();
            int? idFilial = await GetIdFilialUtilizadorLogado();

            if (idEmpresa == null) return Unauthorized();
            if (idFilial == null) return Unauthorized();

            int totalDepartamentos = await _context.Departamentos
                .Where(d => d.Id_Filial == idFilial && d.Ativo == true)
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
            int? idFilial = await GetIdFilialUtilizadorLogado();

            if (idEmpresa == null) return Unauthorized();
            if (idFilial == null) return Unauthorized();

            var totalFuncionarios = await _context.Funcionarios
                .CountAsync(f => f.Departamento.Id_Filial == idFilial && f.Ativo == true);

            return Ok(totalFuncionarios);
        }

        [HttpGet("total-gastos")]
        public async Task<IActionResult> GetTotalGastos()
        {
            int? idEmpresa = await GetIdEmpresaUtilizadorLogado();
            int? idFilial = await GetIdFilialUtilizadorLogado();

            if (idEmpresa == null) return Unauthorized();
            if (idFilial == null) return Unauthorized();

            var totalGastos = await _context.Departamentos
                .Where(f => f.Id_Filial == idFilial && f.Ativo == true)
                .SelectMany(d => d.Funcionarios)
                .Where(func => func.Ativo == true)
                .SumAsync(func => func.Salario_Bruto);
            return Ok(totalGastos);
        }
    }
}
