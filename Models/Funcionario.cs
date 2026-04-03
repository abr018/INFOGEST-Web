using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace INFOGEST_Web.Models
{
    public class Funcionario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id_Funcionario { get; set; }

        [Required(ErrorMessage = "O Nome é obrigatório")]
        [StringLength(100)]
        public string Nome { get; set; }

        [StringLength(10)]
        public string? Genero { get; set; } 

        public DateTime? Data_Nascimento { get; set; } 

        [StringLength(9)]
        public string? Nif { get; set; } 

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; } 

        [StringLength(20)]
        public string? Telefone { get; set; } 

        [StringLength(200)]
        public string? Morada { get; set; } 

        public DateTime Data_Admissao { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Salario_Bruto { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal? Salario_Liquido { get; set; } 

        [Required(ErrorMessage = "O Username é obrigatório")]
        [StringLength(50)]
        public string Username { get; set; }

        [StringLength(255)]
        public string? Senha_Hash { get; set; }

        [NotMapped] 
        public string? Password { get; set; }

        public int Dias_Ferias_Totais { get; set; }
        public int Dias_Ferias_Usados { get; set; }

        public bool Ativo { get; set; } = true;

        public int? Id_Criado_Por { get; set; } 

        public int Id_Cargo { get; set; }
        public int Id_Departamento { get; set; }

        [ForeignKey("Id_Cargo")]
        public Cargo? Cargo { get; set; }

        [ForeignKey("Id_Departamento")]
        public Departamento? Departamento { get; set; }

        [ForeignKey("Id_Criado_Por")]
        public Funcionario? Criador { get; set; }

        public ICollection<Horario> Horarios { get; set; } = new List<Horario>();
        public ICollection<Presenca> Presencas { get; set; } = new List<Presenca>();
        public ICollection<Ausencia> Ferias { get; set; } = new List<Ausencia>();
    }
}