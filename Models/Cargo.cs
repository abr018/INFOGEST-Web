using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace INFOGEST_Web.Models
{
    public class Cargo
    {
        [Key]
        public int Id_Cargo { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        [StringLength(30)]
        public string Nivel_Acesso { get; set; }

        public ICollection<Funcionario> Funcionarios { get; set; } = new List<Funcionario>();
    }
}