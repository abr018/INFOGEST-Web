using INFOGEST_Web.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFOGEST_Web.Models
{
    public class Filial
    {
        [Key]
        public int Id_Filial { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        [StringLength(200)]
        public string Morada { get; set; }

        [StringLength(20)]
        public string Telefone { get; set; }

        public bool Ativo { get; set; } = true;

        public int Id_Empresa { get; set; }

        [ForeignKey("Id_Empresa")]
        public Empresa? Empresa { get; set; }

        public ICollection<Departamento> Departamentos { get; set; } = new List<Departamento>();
    }
}