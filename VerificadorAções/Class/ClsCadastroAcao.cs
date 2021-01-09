using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerificadorAções
{
    public class ClsCadastroAcao
    {
        public string AcaoDescricao { get; set; }
        public decimal PrecoCusto { get; set; }
        public int QtdAcao { get; set; }
        public decimal TotalAcao { get; set; }
        public decimal PrecoAlvo { get; set; }
        public decimal PrecoStop { get; set; }
        public decimal PrecoAtual { get; set; }
        public decimal Porcentagem { get; set; }
        public decimal LucroPrejuizo { get; set; }
        public decimal TotalAjustado { get; set; }
       // public decimal precoUltimaConsulta { get; set; }

    }
}
