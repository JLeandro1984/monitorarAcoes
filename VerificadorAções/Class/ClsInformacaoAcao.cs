using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerificadorAções
{
    public class ClsInformacaoAcao
    {

            public string by { get; set; }
            public bool valid_key { get; set; }
            public Results results { get; set; }
            public float execution_time { get; set; }
            public bool from_cache { get; set; }
        

        public class Results
        {
            public detalheDaAcao Acao { get; set; }
        }

        public class detalheDaAcao
        {
            public string symbol { get; set; }
            public string name { get; set; }
            public string region { get; set; }
            public string currency { get; set; }
            public Market_Time market_time { get; set; }
            public decimal market_cap { get; set; }
            public decimal price { get; set; }
            public decimal change_percent { get; set; }
            public DateTime? updated_at { get; set; }
        }

        public class Market_Time
        {
            public string open { get; set; }
            public string close { get; set; }
            public int timezone { get; set; }
        }


    }
}
