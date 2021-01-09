using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using System.Diagnostics;
using VerificadorAções.Class;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Collections.ObjectModel;

namespace VerificadorAções
{
    public partial class VerificadorAcoes : Form
    {
        // ObservableCollection<ClsCadastroAcao> lista = new ObservableCollection<ClsCadastroAcao>();
        //ObservableCollection<ClsRetornoAcao> listaPesq = new ObservableCollection<ClsRetornoAcao>();

        int tempoConsulta = 2;
        bool gerarManual = false;
        bool pesquisaManual = false;
        bool alertaDesativado = false;
        int alterouInforNum = 0;
        decimal hora_inicio = 10;
        decimal hora_fim = 18;
        bool editar = false;

        Point DragCursor;
        Point DragForm;
        bool Dragging;

        public ClsConfigGeral config = null;
        public List<ClsListarAcoes> listaAcoes_ = null;
        public List<ClsRetornoAcao> listagemAcao = null;
        public VerificadorAcoes()
        {

            // Nome do processo atual
            string nomeProcesso = Process.GetCurrentProcess().ProcessName;

            // Obtém todos os processos com o nome do atual
            Process[] processes = Process.GetProcessesByName(nomeProcesso);

            // Maior do que 1, porque a instância atual também conta
            if (processes.Length > 1)
            {
                MessageBox.Show("Já existe uma instancia do programa em execução", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Environment.Exit(0);
            }

            InitializeComponent();

           // dtgAcao.DataSource = listaPesq;
        }

        private void VerificadorAcoes_Load(object sender, EventArgs e)
        {
            this.iniciarAplicativo();
        }

        private void iniciarAplicativo()
        {
            try
            {
                lblNomeAcao.Text = "";
                txtInforValorAtual.Text = "0,00";
                lblInforPorcentagem.Text = "0,00";

                this.carregarCadastro();

                if (config != null)
                {
                    //Limpar histórico de 2 meses atras
                    this.Remover("", true);
                    this.carregarLista();
                    lblAtualizacao.Text = "";

                    this.atualizarLabelInfor();
                    this.analisarAcoes(false,true);

                    this.CarregarGrid();
                    // dtgGridCadastroAcao.DataSource = lista;
                    // dtgAcao.DataSource = listaPesq;

                    if (config.cadastroAcoes.Count > 0)
                    {
                        this.CarregarGridCadastroArquivo();


                        //400=> Qtd de Consulta    |  8 horas => tempo de Mercado Aberto | 60 min 
                        int qtdAcoes = config.cadastroAcoes.Count;
                        decimal qtdConsultaPorHora = ((400 / 8) / qtdAcoes);

                        tempoConsulta = Convert.ToInt32(60 / qtdConsultaPorHora);
                        //lblTempoConsulta.Text = "Consulta atualizada a cada " + tempoConsulta + " min.";
                        lblTempoConsulta.Text = "Consulta atualizada a cada 1 Min e Meio.";
                    }

                }

            }
            catch (Exception ex)
            {
                this.gerarLogErro(ex.Message + " - Erro ao iniciar Aplicativo");
            }
        }
        private void carregarLista()
        {
            ClsListarAcoes e = null;

            if (config.cadastroAcoes != null)
            {
                foreach (var r in config.cadastroAcoes)
                {
                    e = new ClsListarAcoes();
                    e.AcaoDescricao = r.AcaoDescricao;

                    if (listaAcoes_ == null)
                    {
                        listaAcoes_ = new List<ClsListarAcoes>();
                    }
                    listaAcoes_.Add(e);
                }

            }
        }
        private void monitorandoAcoes(bool utilizarChaveApi)
        {
            gerarManual = false;
            pesquisaManual = false;

            try
            {
                var lista = listaAcoes_;
                if (lista != null)
                {
                    foreach (var r in lista)
                    {
                        bool resultado = this.ConsultaAcoesWebRequest(r.AcaoDescricao.Trim());

                        if (resultado == false && utilizarChaveApi == true && config.Chave_Api != "" && config.Chave_Api != null)
                        {
                            this.consultarAcao(r.AcaoDescricao.Trim());
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                this.gerarLogErro(ex.Message + " - Erro ao monitorar Ações");
            }
        }

        //Consultar Ações 
        public ClsRetornoAcao consultarAcao(string Acao)
        {
            ClsRetornoAcao e = null;
            try
            {
                if (config.Chave_Api != "" && config.Chave_Api != null)
                {
                    using (WebClient wb = new WebClient())
                    {
                        var json = "";
                        try
                        {
                            lblRecado.Visible = false;
                            json = wb.DownloadString("https://api.hgbrasil.com/finance/stock_price?key=" + config.Chave_Api.Trim() + "&symbol=" + Acao.ToUpper().Trim());

                        }
                        catch (Exception)
                        {
                            try
                            {
                                lblRecado.Visible = true;
                                lblRecado.Text = "Chave de consulta 1 venceu!";

                                if (config.Chave_Api_02.Trim() != "")
                                {
                                    json = wb.DownloadString("https://api.hgbrasil.com/finance/stock_price?key=" + config.Chave_Api_02.Trim() + "&symbol=" + Acao.ToUpper().Trim());

                                }

                            }
                            catch (Exception)
                            {
                                if (config.Chave_Api_03.Trim() != "")
                                {
                                    json = wb.DownloadString("https://api.hgbrasil.com/finance/stock_price?key=" + config.Chave_Api_03.Trim() + "&symbol=" + Acao.ToUpper().Trim());
                                }
                            }

                        }

                        JavaScriptSerializer serializer = new JavaScriptSerializer();
                        var consulta = serializer.Deserialize<ClsInformacaoAcao>(json.Replace(Acao.ToUpper().Trim(), "Acao"));

                        if (consulta != null)
                        {

                            if (consulta.results.Acao == null)
                            {
                                MessageBox.Show("Ação não encontrada, verifique se digitou corretamente!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                if (consulta.results.Acao.symbol == null)
                                {
                                    if (gerarManual == true)
                                    {
                                        MessageBox.Show("Ação não encontrada, verifique se digitou corretamente!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                    else
                                    {
                                        this.gerarLogErro("Ação não encontrada, verifique se digitou corretamente! - Erro ao consultar Ações");
                                    }

                                }
                                else
                                {
                                    e = new ClsRetornoAcao();
                                    e.simbolo = Acao.ToUpper().Trim();
                                    e.nomeAcao = consulta.results.Acao.name.ToString().ToUpper();
                                    e.regiao = consulta.results.Acao.region.ToString().ToUpper();
                                    e.moeda = consulta.results.Acao.currency.ToString().ToUpper();
                                    e.cap_mercado = consulta.results.Acao.market_cap;
                                    e.preco = consulta.results.Acao.price;
                                    e.mudanca_porcentagem = consulta.results.Acao.change_percent;
                                    e.data_Atualizacao = consulta.results.Acao.updated_at;

                                    if (listagemAcao == null)
                                    {
                                        listagemAcao = new List<ClsRetornoAcao>();
                                    }

                                    //limpar caso exista registro
                                    this.Remover(Acao, false);
                                    listagemAcao.Add(e);

                                    //Salvar Histórico a atualizar cadastro para monitorar
                                    if (config != null)
                                    {
                                        if (config.historicoAcoesPesquisadas == null)
                                        {
                                            config.historicoAcoesPesquisadas = new List<ClsRetornoAcao>();
                                        }
                                        config.historicoAcoesPesquisadas.Add(e);

                                        if (pesquisaManual == false)
                                        {
                                            this.salvar(e.simbolo, e.mudanca_porcentagem, e.preco, false);
                                        }
                                    }


                                }
                            }
                        }

                    }

                }
                else
                {
                    MessageBox.Show("Não existe Chave API cadastrada, verifique para poder fazer consulta!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }

            catch (Exception ex)
            {
                if (pesquisaManual == true)
                {
                    MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    this.gerarLogErro(ex.Message + " - Erro ao consultar Ações");
                }

                //O servidor remoto retornou um erro: (403) Proibido.

                if (ex.Message.Contains("O servidor remoto retornou um erro: (403) Proibido."))
                {
                    MessageBox.Show("A chave API está bloqueada, será necessário gerar uma nova!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
            }

            return e;
        }

        private void conferirAcoes()
        {
            if (pesquisaManual == false)
            {
                if (config != null)
                {
                    var lista = config;
                    string exibirMensagem = "";
                    string exibirMensagemStopLoss = "";
                    bool msgJaFoiExibida = false;
                    bool precoAbaixoDoStopLoss = false;

                    foreach (var e in lista.cadastroAcoes)
                    {
                        //if (e.PrecoAtual != e.precoUltimaConsulta)
                        //{
                        if (e.PrecoAtual >= e.PrecoAlvo)
                        {
                            exibirMensagem = "Atenção o preço Alvo foi atingido! Ação: " + e.AcaoDescricao;
                        }
                        else if (e.PrecoAtual >= e.PrecoStop)
                        {
                            if (e.PrecoAtual - 5 <= e.PrecoStop)
                            {
                                exibirMensagemStopLoss = "Atenção! O Stop Loss pode ser atingido, fique atento no ativo! Ação: " + e.AcaoDescricao;
                            }
                        }
                        else if (e.PrecoAtual < e.PrecoStop)
                        {
                            precoAbaixoDoStopLoss = true;
                        }
                        //}                 
                    }


                    if (exibirMensagem != "" && exibirMensagemStopLoss == "" && precoAbaixoDoStopLoss == false)
                    {
                        this.WindowState = FormWindowState.Normal;
                        MessageBox.Show(exibirMensagem, "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (exibirMensagem == "" && exibirMensagemStopLoss != "")
                    {
                        this.WindowState = FormWindowState.Normal;
                        MessageBox.Show(exibirMensagemStopLoss, "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (exibirMensagem != "" && exibirMensagemStopLoss != "")
                    {
                        this.WindowState = FormWindowState.Normal;
                        MessageBox.Show(" Existe Ação que irá atingir o Stop Loss! \n\rTambém existe Ação que atingiu o Preço Alvo!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }

                    if (precoAbaixoDoStopLoss == true)
                    {
                        if (alertaDesativado == false && msgJaFoiExibida == false)
                        {
                            if (this.WindowState != FormWindowState.Normal)
                            {
                                this.WindowState = FormWindowState.Normal;
                            }

                            msgJaFoiExibida = true; //exibir só uma vez

                            if (MessageBox.Show("Atenção!!! Preço Atual menor que o Stop Loss! Deseja desativar o alerta?", "Informação", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                            {
                                alertaDesativado = true;
                            }
                        }
                    }

                }


            }
        }

        public void gerarLogErro(string erro)
        {
            try
            {
                string CAMINHO_ARQUIVO;
                CAMINHO_ARQUIVO = System.Environment.CurrentDirectory + @"\LogErro.txt";

                StreamWriter Arquivo = new StreamWriter(CAMINHO_ARQUIVO, true, System.Text.Encoding.UTF8);
                Arquivo.WriteLine(erro + " - " + DateTime.Now, "dd/MM/yyyy HH:mm:ss");
                Arquivo.Close();
            }
            catch
            { }
        }
        private void CarregarGrid()
        {
            dtgAcao.DataSource = new List<ClsRetornoAcao>();
            dtgAcao.DataSource = listagemAcao;
        }
        private void Remover(string acao, bool historico)
        {
            string itemSelecionado = acao;

            if (historico == false)
            {
                if (itemSelecionado != null)
                {
                    listagemAcao.Remove(listagemAcao.Where(x => x.simbolo == itemSelecionado).FirstOrDefault());
                }
            }
            else
            {
                if (itemSelecionado == "")
                {
                    DateTime dataHistorico = DateTime.Today.AddDays(-Convert.ToInt32(-60));

                    //Remove todos os registros a partir da data especificada
                    config.historicoAcoesPesquisadas.RemoveAll(x => x.data_Atualizacao <= dataHistorico);
                    this.salvarCadastro();
                }
            }
        }
        private void txtAcao_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (txtAcao.Text != "")
                {
                    pesquisaManual = true;
                    bool resultado = this.ConsultaAcoesWebRequest(txtAcao.Text.Trim());

                    if (resultado == false && config.Chave_Api != "" && config.Chave_Api != null)
                    {
                        this.consultarAcao(txtAcao.Text.Trim());
                    }
                }
            }

        }

        private void btnMinimizar_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void btnFechar_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnPesquisa_Click(object sender, EventArgs e)
        {
            if (txtAcao.Text != "")
            {
                pesquisaManual = true;
                bool resultado = this.ConsultaAcoesWebRequest(txtAcao.Text.Trim());

                if (resultado == false && config.Chave_Api != "" && config.Chave_Api != null)
                {
                    this.consultarAcao(txtAcao.Text.Trim());
                }

            }
        }

        private bool ConsultaAcoesWebRequest(string acao)
        {
            bool consultaRealizada = false;

            ClsRetornoAcao e = null;
            try
            {
                WebRequest request = WebRequest.Create("https://br.advfn.com/bolsa-de-valores/bovespa/" + acao + "/cotacao");
                WebResponse response = request.GetResponse();

                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.ASCII);
                string Texto = reader.ReadToEnd();
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(Texto);

                decimal preco = 0;
                decimal porcentagem = 0;

                foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//div[@class='price-info']"))
                {
                    ClsRetornoAcao record = new ClsRetornoAcao();
                    foreach (HtmlNode node2 in node.SelectNodes(".//span[@id]"))
                    {
                        string attributeValue = node2.GetAttributeValue("id", "class");
                        if (attributeValue == "quoteElementPiece1")
                        {
                            preco = Convert.ToDecimal(node2.InnerText);
                        }
                        else if (attributeValue == "quoteElementPiece2")
                        {
                            porcentagem = Convert.ToDecimal(node2.InnerText);
                        }
                    }
                }

                //Atualizar grid
                if (preco > 0)
                {
                    consultaRealizada = true;

                    e = new ClsRetornoAcao();
                    e.simbolo = acao.ToUpper().Trim();
                    e.nomeAcao = acao.ToUpper().Trim();
                    e.regiao = "BRAZIL/SAO PAULO";
                    e.moeda = "BRL";
                    e.cap_mercado = 0;
                    e.preco = preco;
                    e.mudanca_porcentagem = porcentagem;
                    e.data_Atualizacao = DateTime.Now;

                    if (listagemAcao == null)
                    {
                        listagemAcao = new List<ClsRetornoAcao>();
                    }

                    //limpar caso exista registro
                    this.Remover(acao, false);
                    listagemAcao.Add(e);

                    //Salvar Histórico a atualizar cadastro para monitorar
                    if (config != null)
                    {
                        if (config.historicoAcoesPesquisadas == null)
                        {
                            config.historicoAcoesPesquisadas = new List<ClsRetornoAcao>();
                        }
                        config.historicoAcoesPesquisadas.Add(e);

                        if (pesquisaManual == false)
                        {
                            this.salvar(e.simbolo, e.mudanca_porcentagem, e.preco, false);
                        }
                    }

                    if (pesquisaManual==true)
                    {
                        this.CarregarGrid();
                    }
                }
            }
            catch (Exception ex)
            {
                this.gerarLogErro(ex.Message + " - Erro ao consultar Ação no site: https://br.advfn.com/bolsa-de-valores/bovespa/");
                // MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return consultaRealizada;
        }

        private void panel2_MouseDown(object sender, MouseEventArgs e)
        {
            Dragging = true;
            DragCursor = Cursor.Position;
            DragForm = this.Location;
        }

        private void panel2_MouseMove(object sender, MouseEventArgs e)
        {
            if (Dragging == true)
            {
                Point dif = Point.Subtract(Cursor.Position, new Size(DragCursor));
                this.Location = Point.Add(DragForm, new Size(dif));
            }
        }

        private void panel2_MouseUp(object sender, MouseEventArgs e)
        {
            Dragging = false;
        }

        private void btnIncluir_Click(object sender, EventArgs e)
        {
            timer2.Enabled = false;
            gerarManual = true;

            if (this.validar(true) == true)
            {
                this.salvar(txtAcaoDescricao.Text, 0, 0, true);
                this.atualizarLista();
            }
            timer2.Enabled = true;
        }

        private void atualizarLista()
        {
            this.salvarCadastro();
            this.carregarCadastro();
            this.carregarLista();

            txtAcaoDescricao.ReadOnly = false;
            txtAcaoDescricao.Text = "";
            txtCusto.Text = "0,00";
            txtQtd.Text = "0";
            txtPrecoAlvo.Text = "0,00";
            txtStopSaida.Text = "0,00";

            if (config.cadastroAcoes.Count > 0)
            {
                //400=> Qtd de Consulta    |  8 horas => tempo de Mercado Aberto | 60 min 
                int qtdAcoes = config.cadastroAcoes.Count;
                decimal qtdConsultaPorHora = ((400 / 8) / qtdAcoes);

                tempoConsulta = Convert.ToInt32(60 / qtdConsultaPorHora);
                //lblTempoConsulta.Text = "Consulta atualizada a cada " + tempoConsulta + " min.";
                lblTempoConsulta.Text = "Consulta atualizada a cada 1 Min e Meio.";
            }
        }
        public bool validar(bool editarCaminho)
        {
            if (txtAcaoDescricao.Text == "")
            {
                MessageBox.Show("Informe a ação!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtAcaoDescricao.Focus();
                return false;
            }

            if (editar == false)
            {
                if (verificarSeAcaoExiste(txtAcaoDescricao.Text) == false)
                {
                    MessageBox.Show("Ação informada inválida!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    txtAcaoDescricao.Focus();
                    return false;
                }
            }

            if (txtCusto.Text == "")
            {
                MessageBox.Show("Informe o custo!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtCusto.Focus();
                return false;
            }
            else if (Convert.ToDecimal(txtCusto.Text) <= 0)
            {
                MessageBox.Show("Informe o custo!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtCusto.Focus();
                return false;
            }

            if (txtQtd.Text == "")
            {
                MessageBox.Show("Informe a Qtd!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtQtd.Focus();
                return false;
            }
            else if (Convert.ToInt32(txtQtd.Text) <= 0)
            {
                MessageBox.Show("Informe a Qtd!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtQtd.Focus();
                return false;
            }

            if (txtPrecoAlvo.Text == "")
            {
                MessageBox.Show("Informe a Preço Alvo!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtPrecoAlvo.Focus();
                return false;
            }
            else if (Convert.ToDecimal(txtPrecoAlvo.Text) <= 0)
            {
                MessageBox.Show("Informe a Preço Alvo!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtPrecoAlvo.Focus();
                return false;
            }

            if (txtStopSaida.Text == "")
            {
                MessageBox.Show("Informe o Preço de Stop!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtStopSaida.Focus();
                return false;
            }
            else if (Convert.ToDecimal(txtStopSaida.Text) <= 0)
            {
                MessageBox.Show("Informe o Preço de Stop!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtStopSaida.Focus();
                return false;
            }

            return true;
        }

        public bool verificarSeAcaoExiste(string Acao)
        {
            bool consultaRealizada = false;

            try
            {
                WebRequest request = WebRequest.Create("https://br.advfn.com/bolsa-de-valores/bovespa/" + Acao + "/cotacao");
                WebResponse response = request.GetResponse();

                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.ASCII);
                string Texto = reader.ReadToEnd();
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(Texto);

                decimal preco = 0;
                decimal porcentagem = 0;

                foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//div[@class='price-info']"))
                {
                    ClsRetornoAcao record = new ClsRetornoAcao();
                    foreach (HtmlNode node2 in node.SelectNodes(".//span[@id]"))
                    {
                        string attributeValue = node2.GetAttributeValue("id", "class");
                        if (attributeValue == "quoteElementPiece1")
                        {
                            preco = Convert.ToDecimal(node2.InnerText);
                        }
                        else if (attributeValue == "quoteElementPiece2")
                        {
                            porcentagem = Convert.ToDecimal(node2.InnerText);
                        }
                    }
                }

                if (preco > 0)
                {
                    consultaRealizada = true;
                }
            }
            catch (Exception ex)
            {
                this.gerarLogErro(ex.Message + " - Erro ao consultar Ação no site: https://br.advfn.com/bolsa-de-valores/bovespa/");
                // MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return consultaRealizada;
        }


        private void salvar(string acaoDesc, decimal porcentagem, decimal precoAtual, bool cadastro)
        {
            ClsCadastroAcao e = null;

            e = new ClsCadastroAcao();
            e.AcaoDescricao = acaoDesc.ToUpper().Replace(" ", "").Trim();
            //e.precoUltimaConsulta = PrecoAtual;
            e.PrecoAtual = precoAtual;
            e.Porcentagem = porcentagem;

            if (cadastro == true)
            {
                e.PrecoAtual = Convert.ToDecimal(txtCusto.Text); //repetir valor caso for cadastro novo
                e.PrecoCusto = Convert.ToDecimal(txtCusto.Text);
                e.QtdAcao = Convert.ToInt32(txtQtd.Text);
                e.PrecoAlvo = Convert.ToDecimal(txtPrecoAlvo.Text);
                e.PrecoStop = Convert.ToDecimal(txtStopSaida.Text);
            }
            else
            {
                foreach (var r in config.cadastroAcoes)
                {
                    if (r.AcaoDescricao == e.AcaoDescricao)
                    {
                        e.PrecoCusto = r.PrecoCusto;
                        e.QtdAcao = r.QtdAcao;
                        e.PrecoAlvo = r.PrecoAlvo;
                        e.PrecoStop = r.PrecoStop;
                    }

                }
            }


            //Totalizadores
            e.TotalAcao = e.PrecoCusto * e.QtdAcao;
            e.TotalAjustado = e.PrecoAtual * e.QtdAcao;
            e.LucroPrejuizo = e.TotalAjustado - e.TotalAcao;

            if (config == null)
            {
                config = new ClsConfigGeral();

                config.Chave_Api = "";
                config.Chave_Api_02 = "";
                config.Chave_Api_03 = "";
                config.dataCadastro = DateTime.Now;
            }

            if (config.cadastroAcoes == null)
            {
                config.cadastroAcoes = new List<ClsCadastroAcao>();
            }
            else
            {
                //limpar caso exista registro
                this.RemoverCadastroAcao(e.AcaoDescricao);
            }


            config.cadastroAcoes.Add(e);

            if (config.dataCadastro == null)
            {
                config.dataCadastro = DateTime.Now;
            }

            config.dataAlteracao = DateTime.Now;

        }

        private void CarregarGridCadastroArquivo()
        {
            try
            {
                dtgGridCadastroAcao.DataSource = new List<ClsCadastroAcao>();
                dtgGridCadastroAcao.DataSource = config.cadastroAcoes;
            }
            catch (Exception ex)
            {
                this.gerarLogErro(ex.Message + " - Erro ao Carregar gride de cadastro");
            }

        }

        private void carregarCadastro()
        {
            //Carregar XML  
            try
            {
                    Models.XML_Cadastro carregar = new Models.XML_Cadastro();
                    config = carregar.CarregarCadastrosMonitoramentoAcoes();

                    if (config != null)
                    {
                        this.CarregarGridCadastroArquivo();
                    }                
            }
            catch (Exception ex)
            {
                this.gerarLogErro(ex.Message + " - Erro ao Carregar Cadastro_MonitoramentoAcoes.XML");
            }
        }

        private void salvarCadastro()
        {
            //Salvar Cadastro na XML
            Models.XML_Cadastro gerarCadastro = new Models.XML_Cadastro();
            gerarCadastro.GerarXMLCadastro_MonitoramentoAcao(config);
        }
        private void RemoverCadastroAcao(string acao)
        {;
            if (config.cadastroAcoes != null)
            {
                string itemSelecionado = acao;
                if (itemSelecionado != null)
                {
                    config.cadastroAcoes.Remove(config.cadastroAcoes.Where(x => x.AcaoDescricao == itemSelecionado).FirstOrDefault());
                }
            }
           
        }

        private void txtCusto_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox txt = txtCusto;
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != Convert.ToChar(Keys.Back))
            {
                if (e.KeyChar == ',')
                {
                    e.Handled = (txt.Text.Contains(','));
                }
                else
                    e.Handled = true;
            }
        }

        private void txtQtd_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox txt = txtQtd;
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != Convert.ToChar(Keys.Back))
            {
                if (e.KeyChar == ',')
                {
                    e.Handled = (txt.Text.Contains(','));
                }
                else
                    e.Handled = true;
            }
        }

        private void txtPrecoAlvo_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox txt = txtPrecoAlvo;
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != Convert.ToChar(Keys.Back))
            {
                if (e.KeyChar == ',')
                {
                    e.Handled = (txt.Text.Contains(','));
                }
                else
                    e.Handled = true;
            }
        }

        private void txtStopSaida_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox txt = txtStopSaida;
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != Convert.ToChar(Keys.Back))
            {
                if (e.KeyChar == ',')
                {
                    e.Handled = (txt.Text.Contains(','));
                }
                else
                    e.Handled = true;
            }
        }

        private void txtAcaoDescricao_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                txtCusto.Focus();
                txtCusto.SelectAll();
            }
        }

        private void txtCusto_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                txtQtd.Focus();
                txtQtd.SelectAll();
            }
        }

        private void txtQtd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                txtPrecoAlvo.Focus();
                txtPrecoAlvo.SelectAll();
            }
        }

        private void txtPrecoAlvo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                txtStopSaida.Focus();
                txtStopSaida.SelectAll();
            }
        }

        private void txtStopSaida_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                gerarManual = true;

                if (this.validar(true) == true)
                {
                    this.salvar(txtAcaoDescricao.Text, 0, 0, true);
                    this.atualizarLista();
                }

            }
        }

        private void dtgGridCadastroAcao_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dtgGridCadastroAcao.Columns[e.ColumnIndex].Name == "btnRemover")
            {
                if (MessageBox.Show("Deseja remover a Ação selecionada?", "Informação", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    timer2.Enabled = false;

                    string itemSelecionado = dtgGridCadastroAcao.CurrentRow.Cells[1].Value.ToString();
                    this.RemoverCadastroAcao(itemSelecionado);

                    this.atualizarLista();
                    timer2.Enabled = true;
                }
            }
        }

        private void btnSalvarConfiguracoes_Click(object sender, EventArgs e)
        {
            timer2.Enabled = false;

            frmConfiguracao configuracao = new frmConfiguracao();
            configuracao.ShowDialog();
            this.carregarCadastro();
            this.CarregarGridCadastroArquivo();

            timer2.Enabled = true;
        }


        bool executando = false;
        private void analisarAcoes(bool utilizarChaveApi, bool iniciando)
        {
            if (DateTime.Now.Hour >= hora_inicio && DateTime.Now.Hour <= hora_fim || iniciando==true)
            {
                if (executando == false)
                {
                    executando = true;
                    this.monitorandoAcoes(utilizarChaveApi);
                    this.conferirAcoes();
                }
            }
        }

        int atual = 0;
        private void atualizarLabelInfor()
        {
            try
            {
                alterouInforNum += 1;
                int totalRegistro = 0;
                if (config.cadastroAcoes != null)
                {
                    totalRegistro = config.cadastroAcoes.Count;

                    if (alterouInforNum > totalRegistro)
                    {
                        alterouInforNum = 1;
                    }

                    if (totalRegistro == atual)
                    {
                        atual = 0;
                    }

                    atual += 1;

                    this.alterarInfor(config.cadastroAcoes[atual - 1].AcaoDescricao.ToUpper(), config.cadastroAcoes[atual - 1].PrecoAtual, config.cadastroAcoes[atual - 1].Porcentagem);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void alterarInfor(string acaoDesc_, decimal precoAtual_, decimal porc_)
        {
            lblNomeAcao.Text = acaoDesc_.ToUpper();
            txtInforValorAtual.Text = precoAtual_.ToString("N2");
            lblInforPorcentagem.Text = porc_.ToString("N2");
            label9.Visible = true;
            txtInforValorAtual.Visible = true;

            if (Convert.ToDecimal(lblInforPorcentagem.Text) < 0)
            {
                lblInforPorcentagem.ForeColor = Color.Red;
            }
            else if (Convert.ToDecimal(lblInforPorcentagem.Text) > 0)
            {
                lblInforPorcentagem.ForeColor = Color.Green;
            }

            lblInforPorcentagem.Text += "%";

        }

        private void lblNomeAcao_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process pStart = new System.Diagnostics.Process();
            pStart.StartInfo = new System.Diagnostics.ProcessStartInfo(@"https://economia.uol.com.br/cotacoes/bolsas/acoes/bvsp-bovespa/" + lblNomeAcao.Text.ToLower().Trim() + "-sa/");
            pStart.Start();
        }

        private void dtgAcao_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (this.dtgAcao.Columns[e.ColumnIndex].Name == "mudanca_porcentagem")
            {
                if (e.Value != null)
                {
                    if (Convert.ToDecimal(e.Value) < 0)
                    {
                        e.CellStyle.ForeColor = Color.Red;
                        e.CellStyle.BackColor = Color.Linen;
                    }
                    else
                    {
                        e.CellStyle.ForeColor = Color.Green;
                    }
                }
            }
        }

        private void dtgGridCadastroAcao_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (this.dtgGridCadastroAcao.Columns[e.ColumnIndex].Name == "Porcentagem")
            {
                if (e.Value != null)
                {
                    if (Convert.ToDecimal(e.Value) < 0)
                    {
                        e.CellStyle.ForeColor = Color.Red;
                        e.CellStyle.BackColor = Color.Linen;
                    }
                    else
                    {
                        e.CellStyle.ForeColor = Color.Green;
                    }
                }
            }

            if (this.dtgGridCadastroAcao.Columns[e.ColumnIndex].Name == "LucroPrejuizo")
            {
                if (e.Value != null)
                {
                    if (Convert.ToDecimal(e.Value) < 0)
                    {
                        e.CellStyle.ForeColor = Color.Red;
                        e.CellStyle.BackColor = Color.Linen;
                    }
                    else
                    {
                        e.CellStyle.ForeColor = Color.Green;
                    }
                }
            }
        }

        private void dtgGridCadastroAcao_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            editar = true;

            txtAcaoDescricao.ReadOnly = true;
            txtAcaoDescricao.Text = dtgGridCadastroAcao.CurrentRow.Cells[1].Value.ToString();
            txtCusto.Text = dtgGridCadastroAcao.CurrentRow.Cells[2].Value.ToString();
            txtQtd.Text = dtgGridCadastroAcao.CurrentRow.Cells[3].Value.ToString();
            txtPrecoAlvo.Text = dtgGridCadastroAcao.CurrentRow.Cells[5].Value.ToString();
            txtStopSaida.Text = dtgGridCadastroAcao.CurrentRow.Cells[6].Value.ToString();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (config != null)
            {
                this.Cursor = Cursors.WaitCursor;
                lblAtualizacao.Text = "Atualizando aguarde...";

                if (DateTime.Now.Hour >= hora_inicio && DateTime.Now.Hour <= hora_fim)
                {
                    lblRecado.Visible = false;
                }
                else
                {
                    lblRecado.Visible = true;
                }

                if (bgwConsultar != null)
                {
                    if (!bgwConsultar.IsBusy) //verificar se a operação está em execução
                    {
                        bgwConsultar.RunWorkerAsync();
                    }
                }
            }
        }

        private void bgwConsultar_DoWork(object sender, DoWorkEventArgs e)
        {            
            this.analisarAcoes(false,false);
        }

        private void bgwConsultar_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.consultaCompletada();
           
        }

        private void consultaCompletada()
        {
            this.CarregarGrid();
            this.salvarCadastro();
            this.carregarCadastro();
            this.carregarLista();

            executando = false;
            lblAtualizacao.Text = "Últ. Consulta: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            this.Cursor = Cursors.Arrow;
        }

        DateTime tempoSinc = DateTime.Now;
        private void timer1_Tick(object sender, EventArgs e)
        {
            //if (bgwConsultarApiChave != null)
            //{
            //    if (!bgwConsultarApiChave.IsBusy) //verificar se a operação está em execução
            //    {
            //        bgwConsultarApiChave.RunWorkerAsync();
            //    }
            //}

            this.atualizarLabelInfor();
        }

        private void bgwConsultarApiChave_DoWork(object sender, DoWorkEventArgs e)
        {
            if (config != null)
            {
                var tempo = DateTime.Now.Subtract(tempoSinc);
                if (tempo.TotalMinutes > tempoConsulta)
                {
                    this.analisarAcoes(true,false);
                    tempoSinc = DateTime.Now;
                }
            }
        }

        private void bgwConsultarApiChave_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.consultaCompletada();
        }
    }
}
