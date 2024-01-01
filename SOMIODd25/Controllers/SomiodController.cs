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
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;
using System.Xml;
using System.Data.SqlTypes;
using System.Security.Cryptography;


namespace SOMIODd25.Controllers
{
    [RoutePrefix("api/somiod")]
    public class SomiodController : ApiController
    {
        ApplicationsController applicationsController;
        ContainersController containersController;
        DatasController datasController;
        XmlValidator validator;
        SubscriptionsController subscriptionsController;

        public SomiodController()
        {
            applicationsController = new ApplicationsController();
            containersController = new ContainersController();
            datasController = new DatasController();
            subscriptionsController = new SubscriptionsController();

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
                string xmlString;
                switch (discoverType)
                {
                    case "application":
                        xmlString = applicationsController.GetAllApplications();
                        break;
                    default:
                        return BadRequest("Missing or invalid somiod-discover header");
                }
                
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(xmlString, Encoding.UTF8, "application/xml")
                };
                return ResponseMessage(response);
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

                            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                            {
                                Content = new StringContent(xmlDataContainers, Encoding.UTF8, "application/xml")
                            };
                            return ResponseMessage(response);
                        default:
                            // If the discoverType is not recognized, return a Bad Request
                            return BadRequest("Invalid somiod-discover header value");
                    }
                }
                else
                {
                    // Standard request to get application details
                    string xmlDataApplication = applicationsController.GetApplication(appName);
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(xmlDataApplication, Encoding.UTF8, "application/xml")
                    };
                    return ResponseMessage(response);
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
                        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(xmlData, Encoding.UTF8, "application/xml")
                        };
                        return ResponseMessage(response);
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
        public IHttpActionResult PutApplication(string appName, [FromBody] XElement appXml)
        {
            if (validator.ValidateXML(appXml.ToString()))
            {
                try
                {
                    if (applicationsController.PutApplication(appName, appXml.ToString()))
                    {
                        Application app = applicationsController.DeserializeApplication(appXml.ToString());
                        string xmlData = applicationsController.GetApplication(app.Name);
                        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(xmlData, Encoding.UTF8, "application/xml")
                        };
                        return ResponseMessage(response);
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
            else { return BadRequest(); }
        }

        [Route("{appName}")]
        public IHttpActionResult DeleteApplication(string appName)
        {
            try
            {
                string xmlData = applicationsController.GetApplication(appName);
                if (string.IsNullOrEmpty(xmlData))
                {
                    return NotFound(); // If the application doesn't exist
                }

                // Get all containers of the application
                string containersXml = containersController.GetAllContainers(appName);
                var containerNames = new List<string>(); 
                XDocument doc = XDocument.Parse(containersXml);
                foreach (XElement containerElement in doc.Descendants("Container"))
                {
                    string name = containerElement.Attribute("Name")?.Value;
                    Console.WriteLine("Container to be deleted: " + name);
                    if (!string.IsNullOrEmpty(name))
                    {
                        containerNames.Add(name);
                    }
                }

                foreach (var containerName in containerNames)
                {
                    // Delete all data in the container
                    string dataXml = datasController.GetAllData(appName, containerName);
                    doc = XDocument.Parse(dataXml);
                    foreach (XElement nameElement in doc.Descendants("name"))
                    {
                        datasController.DeleteData(nameElement.Value, appName, containerName);
                    }

                    // Delete all subscriptions in the container
                    string subscriptionsXml = subscriptionsController.GetAllSubscriptions(appName, containerName);
                    doc = XDocument.Parse(subscriptionsXml);
                    foreach (XElement nameElement in doc.Descendants("name"))
                    {
                        subscriptionsController.DeleteSubscrition(nameElement.Value, appName, containerName);
                    }

                    // Delete the container itself
                    containersController.DeleteContainer(appName, containerName);
                }

                if (applicationsController.DeleteApplication(appName))
                {
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(xmlData, Encoding.UTF8, "application/xml")
                    };
                    return ResponseMessage(response);
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

                            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                            {
                                Content = new StringContent(xmlContainerData, Encoding.UTF8, "application/xml")
                            };
                            return ResponseMessage(response);
                        default:
                            // If the discoverType is not recognized, return a Bad Request
                            return BadRequest("Invalid somiod-discover header value");
                    }
                }
                else
                {
                    string xmlData = containersController.GetContainer(appName ,containerName);
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(xmlData, Encoding.UTF8, "application/xml")
                    };
                    return ResponseMessage(response);
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

                        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(xmlData, Encoding.UTF8, "application/xml")
                        };
                        return ResponseMessage(response);
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
                    if (containersController.PutContainer(appName, containerName, containerXml.ToString()))
                    {
                        Container container = containersController.DeserializeContainer(containerXml.ToString());
                        string xmlData = containersController.GetContainer(appName, containerName);

                        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(xmlData, Encoding.UTF8, "application/xml")
                        };
                        return ResponseMessage(response);
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
                // Delete all data in the container
                string dataXml = datasController.GetAllData(appName, containerName);
                XDocument doc = XDocument.Parse(dataXml);
                foreach (XElement nameElement in doc.Descendants("name"))
                {
                    datasController.DeleteData(nameElement.Value, appName, containerName);
                }

                // Delete all subscriptions in the container
                string subscriptionsXml = subscriptionsController.GetAllSubscriptions(appName, containerName);
                doc = XDocument.Parse(subscriptionsXml);
                foreach (XElement nameElement in doc.Descendants("name"))
                {
                    subscriptionsController.DeleteSubscrition(nameElement.Value, appName, containerName);
                }
                // Assuming the ContainersController.DeleteContainer method returns true if the deletion is successful
                if (containersController.DeleteContainer(appName, containerName))
                {
                    // If deletion is successful, return HTTP 204 No Content as there's no content to return

                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(xmlData, Encoding.UTF8, "application/xml")
                    };
                    return ResponseMessage(response);
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

        //SUBSCRIPTION
        [HttpGet]
        [Route("{appName}/{containerName}/subscription/{subsName}")]
        public IHttpActionResult GetSubscription(string subsName, string appName, string containerName)
        {
            try
            {
                string xmlData = subscriptionsController.GetSubscription(subsName, appName, containerName);

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(xmlData, Encoding.UTF8, "application/xml")
                };
                return ResponseMessage(response);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("{appName}/{containerName}/subscription")]
        public IHttpActionResult PostSubscription([FromBody] XElement subsXml, string appName, string containerName)
        {
            if (validator.ValidateXML(subsXml.ToString()))
            {
                try
                {
                    
                    if (subscriptionsController.PostSubscrition(subsXml.ToString(), appName, containerName))
                    {
                        Subscription subs = subscriptionsController.DeserializeSubscrition(subsXml.ToString());
                        string xmlSubs = subscriptionsController.GetSubscription(subs.Name, appName, containerName);

                        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(xmlSubs, Encoding.UTF8, "application/xml")
                        };
                        return ResponseMessage(response);
                    }
                    else
                    {
                        return BadRequest("Failed to create Subscrition");
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
        [Route("{appName}/{containerName}/subscription/{subsName}")]
        public IHttpActionResult DeleteSubscription(string subsName, string appName, string containerName)
        {
            try
            {
                string xmlSubs = subscriptionsController.GetSubscription(subsName, appName, containerName);
                if (subscriptionsController.DeleteSubscrition(subsName, appName, containerName))
                {
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(xmlSubs, Encoding.UTF8, "application/xml")
                    };
                    return ResponseMessage(response);
                }
                else
                {
                    return BadRequest("Failed to delete Subscrition");
                }
            }
            catch (Exception ex)
            {
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

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(xmlData, Encoding.UTF8, "application/xml")
                };
                return ResponseMessage(response);
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
                        PublishToMqtt(containerName, data.Content);

                        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(xmlData, Encoding.UTF8, "application/xml")
                        };
                        return ResponseMessage(response);
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

                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(xmlData, Encoding.UTF8, "application/xml")
                    };
                    return ResponseMessage(response);
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

        private void PublishToMqtt(string topic, string message)
        {
            MqttClient client = new MqttClient(IPAddress.Parse("127.0.0.1")); // Replace with your MQTT broker address

            try
            {
                client.Connect(Guid.NewGuid().ToString());
                if (client.IsConnected)
                {
                    client.Publish(topic, Encoding.UTF8.GetBytes(message));
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions here
                Console.WriteLine("Error in MQTT Publish: " + ex.Message);
            }
            finally
            {
                if (client.IsConnected)
                {
                    client.Disconnect();
                }
            }
        }
    }
}
