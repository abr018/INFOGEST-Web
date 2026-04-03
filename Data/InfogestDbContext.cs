using INFOGEST_Web.Models; // Importa as suas classes de Modelo
using Microsoft.EntityFrameworkCore;

namespace INFOGEST_Web.Data
{
    // A classe DbContext é a ponte principal entre o C# e a Base de Dados
    public class InfogestDbContext : DbContext
    {
        // O construtor é necessário para a Injeção de Dependência (configurada no Program.cs)
        public InfogestDbContext(DbContextOptions<InfogestDbContext> options)
            : base(options)
        {
        }

        // ==========================================================
        // Mapeamento das Classes (Models) para as Tabelas da BD
        // ==========================================================
        // Cada DbSet<T> representa uma tabela que você pode consultar.

        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Filial> Filiais { get; set; }
        public DbSet<Departamento> Departamentos { get; set; }
        public DbSet<Cargo> Cargos { get; set; }
        public DbSet<Funcionario> Funcionarios { get; set; }
        public DbSet<Horario> Horarios { get; set; }
        public DbSet<Presenca> Presencas { get; set; }
        public DbSet<Ausencia> Ausencias { get; set; }


        [DbFunction("fn_CalcularDiasUteis", Schema = "dbo")]
        public static int CalcularDiasUteis(DateTime dataInicio, DateTime dataFim)
        {
            throw new NotSupportedException("Esta função só pode ser executada numa query LINQ para a BD.");
        }


        // ==========================================================
        // Configuração Adicional (Opcional, mas recomendado)
        // ==========================================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Esta secção garante que o EF Core usa os nomes exatos das tabelas
            // que você definiu no seu script SQL (ex: "Empresa" em vez de "Empresas").
            modelBuilder.Entity<Empresa>().ToTable("Empresa");
            modelBuilder.Entity<Filial>().ToTable("Filial");
            modelBuilder.Entity<Departamento>().ToTable("Departamento");
            modelBuilder.Entity<Cargo>().ToTable("Cargo");
            modelBuilder.Entity<Funcionario>().ToTable("Funcionario", tb => tb.UseSqlOutputClause(false));
            modelBuilder.Entity<Horario>().ToTable("Horario");
            modelBuilder.Entity<Presenca>().ToTable("Presenca");
            modelBuilder.Entity<Ausencia>()
        .ToTable("Ausencia", tb => tb.HasTrigger("TRG_AtualizarSaldoFerias"));
            modelBuilder.HasDbFunction(typeof(InfogestDbContext).GetMethod(nameof(CalcularDiasUteis), new[] { typeof(DateTime), typeof(DateTime) }))
            .HasName("fn_CalcularDiasUteis");

            base.OnModelCreating(modelBuilder);
        }
    }
}