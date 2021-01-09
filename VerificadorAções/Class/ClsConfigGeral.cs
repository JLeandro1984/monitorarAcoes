using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerificadorAções
{
    public class ClsConfigGeral
    {
        public DateTime? dataCadastro{ get; set; }
        public DateTime? dataAlteracao { get; set; }
        public int qtdConsulatdo { get; set; } = 0;
        public string Chave_Api { get; set; }
        public string Chave_Api_02 { get; set; }
        public string Chave_Api_03 { get; set; }
        public List<ClsCadastroAcao> cadastroAcoes { get; set; }
        public List<ClsRetornoAcao> historicoAcoesPesquisadas { get; set; }
    }
}
