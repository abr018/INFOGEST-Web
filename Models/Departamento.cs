using INFOGEST_Web.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFOGEST_Web.Models
{
    public class Departamento
    {
        [Key]
        public int Id_Departamento { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        public bool Ativo { get; set; } = true;

        public int Id_Filial { get; set; }

        [ForeignKey("Id_Filial")]
        public Filial? Filial { get; set; }

        public ICollection<Funcionario> Funcionarios { get; set; } = new List<Funcionario>();
    }
}