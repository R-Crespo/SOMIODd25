using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using SOMIODd25.Models;
using SOMIODd25.Xml;
using System.Xml.Linq;

namespace SOMIODd25.Controllers
{
    [RoutePrefix("api/somiod")]
    public class SomiodController : ApiController
    {
        ApplicationsController applicationsController;
        XmlValidator validator;

        public SomiodController()
        {
            applicationsController = new ApplicationsController();
            validator = new XmlValidator();
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult DiscoverApplications()
        {
            string discoverType = HttpContext.Current.Request.Headers["somiod-discover"];
            try
            {
                string xmlData;
                switch (discoverType)
                {
                    case "application":
                        xmlData = applicationsController.GetAllApplications();
                        break;
                    default:
                        return BadRequest("Missing or invalid somiod-discover header");
                }
                return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.OK, xmlData, "application/xml"));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [Route("{appName}")]
        public IHttpActionResult GetApplication(string appName)
        {
            try
            {
                string xmlData = applicationsController.GetApplication(appName);
                return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.OK, xmlData, "application/xml"));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("")]
        public IHttpActionResult PostApplication([FromBody] XElement appXml)
        {
            if (validator.ValidateXML(appXml.ToString()))
            {
                try
                {
                    if (applicationsController.PostApplication(appXml.ToString()))
                    {
                        Application app = applicationsController.DeserializeApplication(appXml.ToString());
                        string xmlData = applicationsController.GetApplication(app.Name);
                        return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.Created, xmlData, "application/xml"));
                    }
                    else
                    {
                        return BadRequest("Error while creating application");
                    }
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
            }
            else 
            {
                // If XML validation fails, provide a more descriptive error message
                string validationErrorMessage = "XML validation failed. The following issues were found:\n";
                validationErrorMessage += validator.ValidationMessage; // Use the validation message from your XmlValidator class
                return BadRequest(validationErrorMessage);
            }
        }

        public class ApplicationXmlPayload
        {
            public string Name { get; set; }
            // Add other properties that the XML might contain
        }

        [Route("{appName}")]
        public IHttpActionResult PutApplication(string appName,[FromBody] XElement appXml)
        {
            if (validator.ValidateXML(appXml.ToString()))
            {
                try
                {
                    if (applicationsController.PutApplication(appName, appXml.ToString()))
                    {
                        Application app = applicationsController.DeserializeApplication(appXml.ToString());
                        string xmlData = applicationsController.GetApplication(app.Name);
                        return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.OK, xmlData, "application/xml"));
                    }
                    else
                    {
                        return BadRequest();
                    }
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
            } else { return BadRequest(); }
        }

        [Route("{appName}")]
        public IHttpActionResult DeleteApplication(string appName)
        {

            //Nao esta finalizado
            //Quando uma application e eliminada deve se eliminar todos os dados dependentes desta
            //Ou seja, eliminar os containers, a data, e as subscriptions
            try
            {
                string xmlData = applicationsController.GetApplication(appName);
                if (applicationsController.DeleteApplication(appName))
                {
                    return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.OK, xmlData, "application/xml"));
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}