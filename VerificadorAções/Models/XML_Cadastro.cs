using System;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace VerificadorAções.Models
{
    public class XML_Cadastro
    {
        public void GerarXMLCadastro_MonitoramentoAcao(ClsConfigGeral config)
        {
            try
            {
                String caminho = System.Environment.CurrentDirectory + "\\Cadastro_MonitoramentoAcoes.xml";
                var configuracaoEnvioXML = config;

                if (configuracaoEnvioXML != null)
                {

                    XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
                    {
                        Indent = true,
                        OmitXmlDeclaration = false,
                        Encoding = Encoding.UTF8
                    };

                    using (MemoryStream memoryStream = new MemoryStream())
                    using (XmlWriter xmlWriter = XmlWriter.Create(memoryStream, xmlWriterSettings))
                    {
                        var x = new System.Xml.Serialization.XmlSerializer(typeof(ClsConfigGeral));
                        XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                        x.Serialize(xmlWriter, configuracaoEnvioXML);

                        memoryStream.Position = 0;
                        using (FileStream file = new FileStream(caminho, FileMode.Create, FileAccess.Write))
                        {
                            memoryStream.WriteTo(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

       public ClsConfigGeral  CarregarCadastrosMonitoramentoAcoes()
        {
            ClsConfigGeral config = null;

            try
            {               
                String caminho = System.Environment.CurrentDirectory + "\\Cadastro_MonitoramentoAcoes.xml";

                if (System.IO.File.Exists(caminho))
                {
                    XmlDocument oXML = new XmlDocument();
                    //carrega o arquivo XML
                    oXML.Load(caminho);

                    try
                    {
                        Console.WriteLine("Reading with Stream");
                        // Create an instance of the XmlSerializer.
                        XmlSerializer serializer = new XmlSerializer(typeof(ClsConfigGeral));

                        // Declare an object variable of the type to be deserialized.
                        ClsConfigGeral i;

                        using (Stream reader = new FileStream(caminho, FileMode.Open))
                        {
                            // Call the Deserialize method to restore the object's state.
                            i = (ClsConfigGeral)serializer.Deserialize(reader);
                        }

                        if (i != null)
                        {
                            config = i;
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        this.gerarLogErro(ex.Message + " - Erro ao Carregar Cadastro_MonitoramentoAcoes.XML");
                    }
                }
            }
            catch (Exception ex)
            {
                this.gerarLogErro(ex.Message + " - Erro ao Carregar Cadastro_MonitoramentoAcoes.XML");
            }

            return config;
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
    }
}
