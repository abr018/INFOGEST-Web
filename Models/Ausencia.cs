using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFOGEST_Web.Models
{
    public class Ausencia
    {
        [Key]
        public int Id_Ausencia { get; set; }

        public string Tipo { get; set; }
        public DateTime Data_Inicio { get; set; }
        public DateTime Data_Fim { get; set; }
        public string Motivo { get; set; }
        public string? Caminho_Documento { get; set; }

        public string Estado { get; set; } = "Pendente"; 

        public int Id_Funcionario { get; set; }
        [ForeignKey("Id_Funcionario")]
        public Funcionario Funcionario { get; set; }
    }
}