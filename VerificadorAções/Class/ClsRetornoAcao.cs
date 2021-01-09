using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerificadorAções
{
    public class ClsRetornoAcao
    {
        public string simbolo { get; set; }
        public string nomeAcao { get; set; }
        public string regiao { get; set; }
        public string moeda { get; set; }
        public decimal cap_mercado { get; set; }
        public decimal preco { get; set; }
        public decimal mudanca_porcentagem { get; set; }
        public DateTime? data_Atualizacao { get; set; }

    }
}
