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

namespace VerificadorAções
{
    public partial class VerificadorAcoes : Form
    {
        int tempoConsulta = 2;
        bool gerarManual = false;
        bool pesquisaManual = false;
        bool sincronizando = false;
        bool alertaDesativado = false;
        int alterouInforNum = 0;

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
                    this.analisarAcoes();

                    if (config.cadastroAcoes.Count > 0)
                    {
                        //400=> Qtd de Consulta    |  8 horas => tempo de Mercado Aberto | 60 min 
                        int qtdAcoes = config.cadastroAcoes.Count;
                        decimal qtdConsultaPorHora = ((400 / 8) / qtdAcoes);

                        tempoConsulta = Convert.ToInt32(60 / qtdConsultaPorHora);
                        lblTempoConsulta.Text = "Consulta atualizada a cada " + tempoConsulta + " min.";
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
        private void monitorandoAcoes()
        {
            gerarManual = false;
            pesquisaManual = false;
            sincronizando = true;
            try
            {
                var lista = listaAcoes_;
                if (lista != null)
                {
                    foreach (var r in lista)
                    {
                        this.consultarAcao(r.AcaoDescricao);
                    }
                }

                sincronizando = false;

                this.salvarCadastro();
                this.carregarCadastro();


                this.carregarLista();

                lblAtualizacao.Text = "Ult. Consulta: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
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
                                    this.CarregarGrid();


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

                        if (e.PrecoAtual >= e.PrecoAlvo)
                        {
                            exibirMensagem = "Atenção o preço Alvo foi atingido! Ação: " + e.AcaoDescricao;
                        }
                        else if (e.PrecoAtual >= e.PrecoStop)
                        {
                            if (e.PrecoAtual - 5 <= e.PrecoStop)
                            {
                                exibirMensagemStopLoss = "Atenção o preço Stop Loss poder atingido, fique atento para vender o ativo! Ação: " + e.AcaoDescricao;
                            }
                        }
                        else if (e.PrecoAtual < e.PrecoStop)
                        {
                            precoAbaixoDoStopLoss = true;
                        }
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
                        MessageBox.Show(" Existe Ação que irá atingir o Preço Stop Loss \n\rTambém existe Ação que atingiu o Preço Alvo", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }

                    if (precoAbaixoDoStopLoss == true)
                    {
                        if (alertaDesativado == false && msgJaFoiExibida == false)
                        {
                            this.WindowState = FormWindowState.Normal;
                            msgJaFoiExibida = true; //exibir só uma vez

                            if (MessageBox.Show("Atenção!!! Preço Atual menor que Preço de Stop Loss! Deseja desativar o alerta?", "Informação", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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
                    this.carregarCadastro();
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
                    this.consultarAcao(txtAcao.Text.Trim());
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
            pesquisaManual = true;
            if (txtAcao.Text != "")
            {
                this.consultarAcao(txtAcao.Text.Trim());
                this.ConsultaAcoesApi(txtAcao.Text.Trim());
            }
        }

        private void ConsultaAcoesApi(string acao)
        {

            WebClient web = new WebClient();
            web.DownloadStringAsync(new Uri("https://br.advfn.com/bolsa-de-valores/bovespa/"+ acao + "/cotacao", UriKind.RelativeOrAbsolute));
            web.DownloadStringCompleted += (sender, e) =>
            {
                string htmlDaPagina = "";
                if (e.Error == null)
                {
                    htmlDaPagina = e.Result;
                }

                string textoTeste = RecuperaColunaValor(htmlDaPagina, Coluna.NomeAcao);

                //Regex regexStart = new Regex(@"<table id=\produtos\>"); // Linha de partida
                //Regex regexValor = new Regex(@"<td class=\valor\>(.+?)</td>"); // Valor
                //Regex regexStop = new Regex(@"</table>"); // Para de buscar o valor
                //bool buscar = false;
                ////
                //// Faz o loop linha a linha
                ////
                //foreach (string linha in htmlDaPagina.Split('\n'))
                //{
                //    buscar = (buscar == false && regexStart.IsMatch(linha)) ? true : false;
                //    buscar = (buscar && regexStop.IsMatch(linha)) ? false : buscar;

                //    if (regexValor.IsMatch(linha) && buscar)
                //    {
                //        string textoTeste = regexValor.Match(linha).Groups[1].Value;
                //    }
                //}

            };

            // using (WebClient wb = new WebClient())
            //{
            //https://br.tradingview.com/symbols/BMFBOVESPA-VVAR3/  
            //var json = wb.DownloadString("https://www.infomoney.com.br/cotacoes/viavarejo-vvar3/");

            //var conteudo = wb.DownloadString("https://br.advfn.com/bolsa-de-valores/bovespa/via-varejo-on-"+ acao +"/cotacao");
            //Console.WriteLine(conteudo);
            //Console.ReadLine();
            //}
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
            gerarManual = true;

            if (this.validar(true) == true)
            {
                this.salvar(txtAcaoDescricao.Text, 0, 0, true);
                this.atualizarLista();
                this.CarregarGridCadastroArquivo();
            }

        }

        private void atualizarLista()
        {
            this.salvarCadastro();
            this.carregarCadastro();

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
                lblTempoConsulta.Text = "Consulta atualizada a cada " + tempoConsulta + " min.";
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
            bool existe = false;
            try
            {
                if (config.Chave_Api != "" && config.Chave_Api != null)
                {

                    using (WebClient wb = new WebClient())
                    {
                        var json = wb.DownloadString("https://api.hgbrasil.com/finance/stock_price?key=" + config.Chave_Api.Trim() + "&symbol=" + Acao.ToUpper().Trim());
                        JavaScriptSerializer serializer = new JavaScriptSerializer();
                        var consulta = serializer.Deserialize<ClsInformacaoAcao>(json.Replace(Acao.ToUpper().Trim(), "Acao"));

                        if (consulta != null)
                        {
                            if (consulta.results.Acao != null)
                            {
                                if (consulta.results.Acao.symbol != null)
                                {
                                    existe = true;
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
                if (gerarManual == true)
                {
                    MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    this.gerarLogErro(ex.Message + " - Erro ao consultar Ações");
                }

            }

            return existe;
        }
        private void salvar(string acaoDesc, decimal porcentagem, decimal precoAtual, bool cadastro)
        {
            ClsCadastroAcao e = null;

            e = new ClsCadastroAcao();
            e.AcaoDescricao = acaoDesc.ToUpper().Replace(" ", "").Trim();
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
                dtgGridCadastroAcao.DataSource = new List<ClsCadastroAcao>(); ;
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
                if (sincronizando == false)
                {
                    Models.XML_Cadastro carregar = new Models.XML_Cadastro();
                    config = carregar.CarregarCadastrosMonitoramentoAcoes();

                    if (config != null)
                    {
                        this.CarregarGridCadastroArquivo();
                    }
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
        {
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
                    string itemSelecionado = dtgGridCadastroAcao.CurrentRow.Cells[1].Value.ToString();
                    this.RemoverCadastroAcao(itemSelecionado);

                    this.salvarCadastro();
                    this.carregarCadastro();

                    this.carregarLista();
                }
            }
        }

        private void btnSalvarConfiguracoes_Click(object sender, EventArgs e)
        {
            frmConfiguracao configuracao = new frmConfiguracao();
            configuracao.ShowDialog();
            this.carregarCadastro();
        }


        DateTime tempoSinc = DateTime.Now;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (config != null)
            {

                var tempo = DateTime.Now.Subtract(tempoSinc);
                if (tempo.TotalMinutes > tempoConsulta)
                {
                    this.analisarAcoes();
                }

                this.atualizarLabelInfor();

                if (tempo.TotalMinutes > 1 && tempo.TotalMinutes < 2)
                {
                    executando = false;
                }
            }
        }


        bool executando = false;
        private void analisarAcoes()
        {
            if (DateTime.Now.Hour >= 10 && DateTime.Now.Hour <= 18)
            {
                lblRecado.Visible = false;

                if (executando == false)
                {
                    executando = true;
                    this.monitorandoAcoes();
                    this.conferirAcoes();
                }

                tempoSinc = DateTime.Now;
            }
            else
            {
                lblRecado.Visible = true;
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

        private void dtgGridCadastroAcao_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (this.dtgGridCadastroAcao.Columns[e.ColumnIndex].Name == "Porcentagem")
            {
                if (e.Value != null && Convert.ToInt32(e.Value) < 0)
                {
                    e.CellStyle.ForeColor = Color.Red;
                    e.CellStyle.BackColor = Color.Linen;
                }
                else
                {
                    e.CellStyle.ForeColor = Color.Green;
                }
            }

            if (this.dtgGridCadastroAcao.Columns[e.ColumnIndex].Name == "LucroPrejuizo")
            {
                if (e.Value != null && Convert.ToInt32(e.Value) < 0)
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

        private enum Coluna
        {
            Simbolo = 0,
            NomeAcao,
            Preco,
            Porcentagem,
            Data_Atualizacao,
            //Regiao,
            //Moeda,
            //Cap_Mercado       


        }

        private string RecuperaColunaValor(string pattern, Coluna col)
        {
            string S = pattern.Replace("\n", "").Replace("\t", "").Replace("\r", "");
            switch (col)
            {
                case Coluna.Simbolo:
                    {
                        S = StringEntreString(S, "<!-- Início Linha NOME EMPRESARIAL -->", "<!-- Fim Linha NOME EMPRESARIAL -->");
                        S = StringEntreString(S, "<tr>", "</tr>");
                        S = StringEntreString(S, "<b>", "</b>");
                        return S.Trim();
                    }

                case Coluna.NomeAcao:
                    {
                        S = StringEntreString(S, "<!-- Início Linha ESTABELECIMENTO -->", "<!-- Fim Linha ESTABELECIMENTO -->");
                        S = StringEntreString(S, "<tr>", "</tr>");
                        S = StringEntreString(S, "<b>", "</b>");
                        return S.Trim();
                    }

                case Coluna.Preco:
                    {
                        S = StringEntreString(S, "<!-- Início Linha NATUREZA JURÍDICA -->", "<!-- Fim Linha NATUREZA JURÍDICA -->");
                        S = StringEntreString(S, "<tr>", "</tr>");
                        S = StringEntreString(S, "<b>", "</b>");
                        return S.Trim();
                    }
                case Coluna.Porcentagem:
                    {
                        S = StringEntreString(S, "<!-- Início Linha ESTABELECIMENTO -->", "<!-- Fim Linha ESTABELECIMENTO -->");
                        S = StringEntreString(S, "<tr>", "</tr>");
                        S = StringEntreString(S, "<b>", "</b>");
                        return S.Trim();
                    }

                case Coluna.Data_Atualizacao:
                    {
                        S = StringEntreString(S, "<!-- Início Linha NATUREZA JURÍDICA -->", "<!-- Fim Linha NATUREZA JURÍDICA -->");
                        S = StringEntreString(S, "<tr>", "</tr>");
                        S = StringEntreString(S, "<b>", "</b>");
                        return S.Trim();
                    }


                default:
                    {
                        return S;
                    }
            }
        }

        private string StringEntreString(string Str, string StrInicio, string StrFinal)
        {
            int Ini;
            int Fim;
            int Diff;
            Ini = Str.IndexOf(StrInicio);
            Fim = Str.IndexOf(StrFinal);
            if (Ini > 0)
                Ini = Ini + StrInicio.Length;
            if (Fim > 0)
                Fim = Fim + StrFinal.Length;
            Diff = ((Fim - Ini) - StrFinal.Length);
            if ((Fim > Ini) && (Diff > 0))
                return Str.Substring(Ini, Diff);
            else
                return "";
        }

    }
}
