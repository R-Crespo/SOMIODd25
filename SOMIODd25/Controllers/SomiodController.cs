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
        ContainersController containersController;
        DatasController datasController;
        XmlValidator validator;

        public SomiodController()
        {
            applicationsController = new ApplicationsController();
            containersController = new ContainersController();
            datasController = new DatasController();
            validator = new XmlValidator();
        }


        //APPLICATION
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


        [HttpGet]
        [Route("{appName}")]
        public IHttpActionResult GetApplicationOrDiscoverContainers(string appName)
        {
            string discoverType = HttpContext.Current.Request.Headers["somiod-discover"];

            try
            {
                if (!string.IsNullOrEmpty(discoverType))
                {
                    switch (discoverType)
                    {
                        case "container":
                            // Discovery request to get all containers within the specified application
                            string xmlDataContainers = containersController.GetAllContainers(appName);
                            return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.OK, xmlDataContainers, "application/xml"));
                        default:
                            // If the discoverType is not recognized, return a Bad Request
                            return BadRequest("Invalid somiod-discover header value");
                    }
                }
                else
                {
                    // Standard request to get application details
                    string xmlDataApplication = applicationsController.GetApplication(appName);
                    return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.OK, xmlDataApplication, "application/xml"));
                }
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



        //CONTAINER





        [HttpGet]
        [Route("{appName}/{containerName}")]
        public IHttpActionResult GetContainerOrDiscoverData(string appName, string containerName)
        {
            string discoverType = HttpContext.Current.Request.Headers["somiod-discover"];
            try
            {
                if (!string.IsNullOrEmpty(discoverType))
                {
                    switch (discoverType)
                    {
                        case "data":
                            // Discovery request to get all data within the specified container
                            string xmlContainerData = datasController.GetAllData(appName, containerName);
                            return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.OK, xmlContainerData, "application/xml"));
                        default:
                            // If the discoverType is not recognized, return a Bad Request
                            return BadRequest("Invalid somiod-discover header value");
                    }
                }
                else
                {
                    string xmlData = containersController.GetContainer(appName ,containerName);
                    return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.OK, xmlData, "application/xml"));
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [HttpPost]
        [Route("{appName}")]
        public IHttpActionResult PostContainer(string appName, [FromBody] XElement containerXml)
        {
            if (validator.ValidateXML(containerXml.ToString()))
            {
                try
                {
                    if (containersController.PostContainer(containerXml.ToString(), appName))
                    {
                        Container container = containersController.DeserializeContainer(containerXml.ToString());
                        string xmlData = containersController.GetContainer(appName, container.Name);
                        return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.Created, xmlData, "application/xml"));
                    }
                    else
                    {
                        return BadRequest("Error while creating container");
                    }
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
            }
            else
            {
                string validationErrorMessage = "XML validation failed. The following issues were found:\n" + validator.ValidationMessage;
                return BadRequest(validationErrorMessage);
            }
        }


        [HttpPut]
        [Route("{appName}/{containerName}")]
        public IHttpActionResult PutContainer(string appName, string containerName, [FromBody] XElement containerXml)
        {
            if (validator.ValidateXML(containerXml.ToString()))
            {
                try
                {
                    if (containersController.PutContainer(appName ,containerName, containerXml.ToString()))
                    {
                        Container container = containersController.DeserializeContainer(containerXml.ToString());
                        string xmlData = containersController.GetContainer(appName, containerName);
                        return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.OK, xmlData, "application/xml"));
                    }
                    else
                    {
                        return BadRequest("Error while updating Container");
                    }
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpDelete]
        [Route("{appName}/{containerName}")]
        public IHttpActionResult DeleteContainer(string appName, string containerName)
        {

            //Nao esta finalizado
            //Quando uma application e eliminada deve se eliminar todos os dados dependentes desta
            //Ou seja, eliminar a data, e as subscriptions
            string xmlData = containersController.GetContainer(appName, containerName);

            try
            {
                // Assuming the ContainersController.DeleteContainer method returns true if the deletion is successful
                if (containersController.DeleteContainer(appName, containerName))
                {
                    // If deletion is successful, return HTTP 204 No Content as there's no content to return
                    return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.OK, xmlData, "application/xml"));
                }
                else
                {
                    // If the container could not be found or deletion failed, return HTTP 404 Not Found
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                // In case of any exceptions, return HTTP 500 Internal Server Error with exception details
                return InternalServerError(ex);
            }
        }


        //DATA

        [HttpGet]
        [Route("{appName}/{containerName}/data/{dataName}")]
        public IHttpActionResult GetData(string dataName, string appName, string containerName)
        {
            try
            {
                string xmlData = datasController.GetData(dataName, appName, containerName);
                return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.OK, xmlData, "application/xml"));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("{appName}/{containerName}/data")]
        public IHttpActionResult PostData([FromBody] XElement dataXml, string appName, string containerName)
        {
            if (validator.ValidateXML(dataXml.ToString()))
            {
                try
                {
                    
                    if (datasController.PostData(dataXml.ToString(), appName, containerName))
                    {
                        Data data = datasController.DeserializeData(dataXml.ToString());
                        string xmlData = datasController.GetData(data.Name, appName, containerName);
                        return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.Created, xmlData, "application/xml"));
                    }
                    else
                    {
                        return BadRequest("Error while creating Data");
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

        [HttpDelete]
        [Route("{appName}/{containerName}/data/{dataName}")]
        public IHttpActionResult DeleteData(string dataName, string appName, string containerName)
        {
            try
            {
                string xmlData = datasController.GetData(dataName, appName, containerName);
                if (datasController.DeleteData(dataName, appName, containerName))
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