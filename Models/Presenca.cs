using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFOGEST_Web.Models
{
    public class Presenca
    {
        [Key]
        public int Id_Presenca { get; set; }

        public DateTime Data { get; set; }

        public TimeSpan Hora_Entrada { get; set; } 

        public TimeSpan Hora_Saida { get; set; } 

        [StringLength(20)]
        public string Estado { get; set; } 

        public int Id_Funcionario { get; set; }

        [ForeignKey("Id_Funcionario")]
        public Funcionario Funcionario { get; set; }
    }
}