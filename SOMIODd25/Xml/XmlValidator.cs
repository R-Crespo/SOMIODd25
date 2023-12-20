using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Schema;

namespace SOMIODd25.Xml
{

    public class XmlValidator
    {
        public string ValidationMessage { get; private set; }
        private bool isValid;
        private readonly string xsdFilePath = HttpContext.Current.Server.MapPath("~/Xml/XMLSchema1.xsd");


        public XmlValidator()
        {
            isValid = true;
            ValidationMessage = "";
        }

        public bool ValidateXML(string xmlContent)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
            {
                ValidationMessage = "XML content is null or empty.";
                return false;
            }

            isValid = true;
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.LoadXml(xmlContent);
                doc.Schemas.Add(null, xsdFilePath);
                doc.Validate(ValidationEventHandler);
            }
            catch (XmlException ex)
            {
                isValid = false;
                ValidationMessage = $"ERROR: {ex.Message}";
            }
            catch (Exception ex)
            {
                isValid = false;
                ValidationMessage = $"General Error: {ex.Message}";
            }

            return isValid;
        }

        private void ValidationEventHandler(object sender, ValidationEventArgs args)
        {
            isValid = false;
            ValidationMessage = args.Severity == XmlSeverityType.Error ? $"ERROR: {args.Message}" : $"WARNING: {args.Message}";
        }
    }

}