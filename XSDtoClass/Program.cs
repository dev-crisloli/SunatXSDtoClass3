using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace XSDtoClass
{
    class Program
    {
        static void Main(string[] args)
        {

            XmlDocument xmlDocument = new XmlDocument();
            XmlElement firma = xmlDocument.CreateElement("ext:firma", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");

            InvoiceType invoiceType = new InvoiceType()
            {
                UBLExtensions = new UBLExtensionType[]{
                        new UBLExtensionType{ ExtensionContent = firma }
                },
                UBLVersionID = new UBLVersionIDType { Value = "2.1" },
                CustomizationID = new CustomizationIDType { Value = "2.0" },
                ProfileID = new ProfileIDType
                {
                    schemeName = "SUNAT:Identificador de Tipo de Operación",
                    schemeAgencyName = "PE:SUNAT",
                    schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo17",
                    Value = "0101"
                },
                InvoiceLine = new InvoiceLineType[3]
            };

            for (int i = 0; i <= 2; i++)
            {
                invoiceType.InvoiceLine[i] = new InvoiceLineType
                {
                    ID = new IDType { Value = (i + 1).ToString() }
                };
            };

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(InvoiceType));

            XmlSerializerNamespaces xmlSerializerNamespaces = new XmlSerializerNamespaces();
            xmlSerializerNamespaces.Add("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
            xmlSerializerNamespaces.Add("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
            xmlSerializerNamespaces.Add("ccts", "urn:un:unece:uncefact:documentation:2");
            xmlSerializerNamespaces.Add("ds", "http://www.w3.org/2000/09/xmldsig#");
            xmlSerializerNamespaces.Add("ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");
            xmlSerializerNamespaces.Add("qdt", "urn:oasis:names:specification:ubl:schema:xsd:QualifiedDatatypes-2");
            xmlSerializerNamespaces.Add("udt", "urn:un:unece:uncefact:data:specification:UnqualifiedDataTypesSchemaModule:2");
            xmlSerializerNamespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");


            var oStringWriter = new StringWriter();
            xmlSerializer.Serialize(XmlWriter.Create(oStringWriter), invoiceType, xmlSerializerNamespaces);
            string stringXML = oStringWriter.ToString();
            XmlDocument xmlDocument_sinFirmar = new XmlDocument();
            xmlDocument_sinFirmar.LoadXml(stringXML);
            xmlDocument_sinFirmar.Save("XML_Sunat_SinFirmar.xml");
            FirmarDocumentoXML(xmlDocument_sinFirmar, "privave_llamepe.pfx", "123456").Save("XML_Sunat_Firmado.xml");
        }

        public static XmlDocument FirmarDocumentoXML(XmlDocument xmlDocument,string rutaArchivoCertificado, string passwordCertificado)
        {
            xmlDocument.PreserveWhitespace = true;
            XmlNode ExtensionContent = xmlDocument.GetElementsByTagName("ExtensionContent", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2").Item(0);
            ExtensionContent.RemoveAll();

            X509Certificate2 x509Certificate2 = new X509Certificate2(File.ReadAllBytes(rutaArchivoCertificado), passwordCertificado, X509KeyStorageFlags.Exportable);
            RSACryptoServiceProvider key = new RSACryptoServiceProvider(new CspParameters(24));
            SignedXml signedXML = new SignedXml(xmlDocument);
            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            KeyInfo keyInfo = new KeyInfo();
            KeyInfoX509Data keyInfoX509Data = new KeyInfoX509Data(x509Certificate2);
            Reference reference = new Reference("");

            string exportarLlave = x509Certificate2.PrivateKey.ToXmlString(true);
            key.PersistKeyInCsp = false;
            key.FromXmlString(exportarLlave);
            reference.AddTransform(env);
            signedXML.SigningKey = key;

            Signature XMLSignature = signedXML.Signature;
            XMLSignature.SignedInfo.AddReference(reference);
            keyInfoX509Data.AddSubjectName(x509Certificate2.Subject);

            keyInfo.AddClause(keyInfoX509Data);

            XMLSignature.KeyInfo = keyInfo;
            XMLSignature.Id = "SignatureKG";
            signedXML.ComputeSignature();

            ExtensionContent.AppendChild(signedXML.GetXml());

            return xmlDocument;
        }
    }
}
