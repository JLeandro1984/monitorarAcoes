using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using VerificadorAções;

namespace VerificadorAções
{
    public partial class frmConfiguracao : Form
    {
        public ClsConfigGeral config = null;

        Point DragCursor;
        Point DragForm;
        bool Dragging;
        public frmConfiguracao()
        {
            InitializeComponent();
        }

        private void btnFechar_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnConfirmar_Click(object sender, EventArgs e)
        {
            if (txtAPI_01.Text == "" && txtAPI_02.Text == "" && txtAPI_03.Text == "")
            {
                MessageBox.Show("Nenhuma chave API foi cadastrada, verifique!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Question);
                txtAPI_01.Focus();
            }
            else
            {
                this.salvar();
            }
              
        }

        private void salvar()
        {
            if (txtAPI_01.Text !="")
            {
                if (config == null)
                {
                    config = new ClsConfigGeral();
                }
                config.Chave_Api = txtAPI_01.Text.ToString().Trim();
                config.Chave_Api_02 = txtAPI_02.Text.ToString().Trim();
                config.Chave_Api_03 = txtAPI_03.Text.ToString().Trim();

                if (config.dataCadastro == null)
                {
                    config.dataCadastro = DateTime.Now;
                }

                config.dataAlteracao = DateTime.Now;

                Models.XML_Cadastro gerarCadastro = new Models.XML_Cadastro();
                gerarCadastro.GerarXMLCadastro_MonitoramentoAcao(config);

                MessageBox.Show("Configuração salva com sucesso!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Question);

                this.Close();
            }
            else
            {
                if (txtAPI_01.Text != "")
                {
                    MessageBox.Show("O campo para Chave API 01 não pode ficar vazio, verifique!", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Question);
                    txtAPI_01.Focus();
                }              
            }
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

        private void frmConfiguracao_Load(object sender, EventArgs e)
        {
            this.carregarConfig();
        }

        private void carregarConfig()
        {
            Models.XML_Cadastro carregar = new Models.XML_Cadastro();
            config = carregar.CarregarCadastrosMonitoramentoAcoes();

            txtAPI_01.Text= config.Chave_Api;
            txtAPI_02.Text = config.Chave_Api_02;
            txtAPI_03.Text = config.Chave_Api_03;
        }

        private void label9_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process pStart = new System.Diagnostics.Process();
            pStart.StartInfo = new System.Diagnostics.ProcessStartInfo(@"https://hgbrasil.com/apis/cotacao-acao/b3-brasil-bolsa-balcao-b3sa3");
            pStart.Start();
        }
    }
}
