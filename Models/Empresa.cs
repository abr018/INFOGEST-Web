using INFOGEST_Web.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFOGEST_Web.Models
{
    public class Empresa
    {
        [Key]
        public int Id_Empresa { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(100)]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O NIF é obrigatório")]
        [StringLength(9)]
        public string Nif { get; set; }

        [StringLength(200)]
        public string? Morada { get; set; } 

        [StringLength(20)]
        public string? Telefone { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; } 

        [StringLength(100)]
        public string? Website { get; set; }

        public DateTime Data_Criacao { get; set; } = DateTime.Now;

        public int? Id_Criador { get; set; }

        [ForeignKey("Id_Criador")]
        public Funcionario? Criador { get; set; }

        public ICollection<Filial> Filiais { get; set; } = new List<Filial>();
    }
}