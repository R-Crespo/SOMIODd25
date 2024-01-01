using SOMIODd25.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Web.Http;

namespace SOMIODd25.Controllers
{
    public class SubscriptionsController
    {
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SOMIODdb.Properties.Settings.ConnStr"].ConnectionString;

        public string GetAllSubscriptions(string appName, string containerName)
        {
            List<string> subsNames = new List<string>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    var (appExists, containerId) = VerifyAppAndContainer(appName, containerName);
                    if (!appExists)
                    {
                        throw new KeyNotFoundException("Application not found.");
                    }
                    if (containerId == 0)
                    {
                        throw new KeyNotFoundException("Container not found or does not belong to the application.");
                    }
                    string query = "SELECT name FROM Subscriptions WHERE parent = @container ORDER BY id";
                    using (SqlCommand command = new SqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@container", containerId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                subsNames.Add(reader["name"].ToString());
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException("Error occurred while retriving all subscriptions", ex);
                }
            }
            // Create an XML string with application names
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement root = xmlDoc.CreateElement("Subscription");
            xmlDoc.AppendChild(root);

            XmlElement nameList = xmlDoc.CreateElement("NameList");
            root.AppendChild(nameList);
            foreach (var subsName in subsNames)
            {
                XmlElement nameElement = xmlDoc.CreateElement("name");
                nameElement.InnerText = subsName;
                nameList.AppendChild(nameElement);
            }

            // To return the XML document as a string, use OuterXml property
            return xmlDoc.OuterXml;
        }

        public string GetSubscription(string subscription, string appName, string containerName)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement element = doc.CreateElement("Subscription");
            doc.AppendChild(element);
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    var (appExists, containerId) = VerifyAppAndContainer(appName, containerName);
                    if (!appExists)
                    {
                        throw new KeyNotFoundException("Application not found.");
                    }
                    if (containerId == 0)
                    {
                        throw new KeyNotFoundException("Container not found or does not belong to the application.");
                    }
                    string query = "SELECT * FROM Subscriptions WHERE name = @subscription AND parent = @container";
                    using (SqlCommand command = new SqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@subscription", subscription);
                        command.Parameters.AddWithValue("@container", containerId);
                        command.CommandType = System.Data.CommandType.Text;
                        command.Connection = conn;
                        int id = 0;
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                id = (int)reader["Id"];
                                element.SetAttribute("Id", reader["Id"].ToString());
                                element.SetAttribute("Name", reader["Name"].ToString());
                                element.SetAttribute("Creation_dt", ((DateTime)reader["Creation_dt"]).ToString("s"));
                                element.SetAttribute("Parent", reader["Parent"].ToString());
                                element.SetAttribute("Event", reader["Event"].ToString());
                                element.SetAttribute("Endpoint", reader["Endpoint"].ToString());

                            }
                            else
                            {
                                throw new KeyNotFoundException($"Subscription with name '{subscription}' not found.");
                            }
                        }
                    }

                    return doc.OuterXml;
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException("Error occurred while retriving Subscription ", ex);
                }
            }
        }

        public bool PostSubscrition(string subsXml, string appName, string containerName)
        {
            Subscription subscription = null;
            try
            {
                subscription = DeserializeSubscrition(subsXml);
            }
            catch
            {
                throw;
            }

            if (subscription == null)
            {
                throw new ArgumentException("The deserialized subscription is null.");
            }

            if (string.IsNullOrWhiteSpace(subscription.Name) || string.IsNullOrWhiteSpace(subscription.Endpoint) || string.IsNullOrWhiteSpace(subscription.Event))
            {
                throw new ArgumentException("Invalid subscription data: Name, Endpoint and Event are required.");
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    var (appExists, containerId) = VerifyAppAndContainer(appName, containerName);
                    if (!appExists)
                    {
                        throw new KeyNotFoundException("Application not found.");
                    }
                    if (containerId == 0)
                    {
                        throw new KeyNotFoundException("Container not found or does not belong to the application.");
                    }

                    string query = "INSERT INTO subscriptions(Name, Creation_dt, Parent, Event, Endpoint) VALUES (@name, @creation_dt, @parent, @event, @endpoint)";
                    using (SqlCommand command = new SqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@name", subscription.Name);
                        command.Parameters.AddWithValue("@creation_dt", System.DateTime.UtcNow);
                        command.Parameters.AddWithValue("@parent", containerId);
                        command.Parameters.AddWithValue("@event", subscription.Event);
                        command.Parameters.AddWithValue("@endpoint", subscription.Endpoint);

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException("Error occurred while creating subscriptions", ex);
                }
            }
        }
        public bool DeleteSubscrition(string name, string appName, string containerName)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    var (appExists, containerId) = VerifyAppAndContainer(appName, containerName);
                    if (!appExists)
                    {
                        throw new KeyNotFoundException("Application not found.");
                    }
                    if (containerId == 0)
                    {
                        throw new KeyNotFoundException("Container not found or does not belong to the application.");
                    }
                    string query = "DELETE FROM subscriptions WHERE Name = @name AND parent = @parent";
                    using (SqlCommand command = new SqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        command.Parameters.AddWithValue("@parent", containerId);
                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException("Error occurred while deleting subscrition", ex);
                }
            }
        }
        public Subscription DeserializeSubscrition(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return null;

            Subscription subscrition = null;
            XmlSerializer serializer = new XmlSerializer(typeof(Subscription));

            using (StringReader reader = new StringReader(xml))
            {
                try
                {
                    subscrition = (Subscription)serializer.Deserialize(reader);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Error occurred while deserializing subscrition", ex);
                }
            }

            return subscrition;
        }

        private (bool appExists, int containerId) VerifyAppAndContainer(string appName, string containerName)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Query to check if the application exists and get its ID
                string appQuery = "SELECT Id FROM Applications WHERE Name = @appName";
                using (SqlCommand appCommand = new SqlCommand(appQuery, conn))
                {
                    appCommand.Parameters.AddWithValue("@appName", appName);
                    object appResult = appCommand.ExecuteScalar();

                    if (appResult == null)
                    {
                        throw new KeyNotFoundException("Application does not exist.");
                    }

                    int appId = Convert.ToInt32(appResult);

                    string containerQuery = "SELECT Id FROM Containers WHERE Name = @containerName AND Parent = @appId";
                    using (SqlCommand containerCommand = new SqlCommand(containerQuery, conn))
                    {
                        containerCommand.Parameters.AddWithValue("@containerName", containerName);
                        containerCommand.Parameters.AddWithValue("@appId", appId);
                        object containerResult = containerCommand.ExecuteScalar();

                        bool containerExists = containerResult != null;
                        int containerId = containerExists ? Convert.ToInt32(containerResult) : 0;

                        return (true, containerId);
                    }
                }
            }
        }
    }
}