using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFOGEST_Web.Models
{
    public class Horario
    {
        [Key]
        public int Id_Horario { get; set; }

        public DateTime Data { get; set; }

        public TimeSpan Hora_Entrada { get; set; }

        public TimeSpan Hora_Saida { get; set; }

        public int Id_Funcionario { get; set; }

        [ForeignKey("Id_Funcionario")]
        public Funcionario Funcionario { get; set; }
    }
}