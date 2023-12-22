using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using SOMIODd25.Models;

namespace SOMIODd25.Controllers
{
    public class ContainersController
    {
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SOMIODdb.Properties.Settings.ConnStr"].ConnectionString;

        public string GetAllContainers(string appName)
        {
            List<string> containerNames = new List<string>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    // First, find the application ID corresponding to appName
                    string appIdQuery = "SELECT Id FROM Applications WHERE Name = @appName";
                    int appId = 0;
                    using (SqlCommand command = new SqlCommand(appIdQuery, conn))
                    {
                        command.Parameters.AddWithValue("@appName", appName);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                appId = (int)reader["Id"];
                            }
                            else
                            {
                                throw new KeyNotFoundException($"Application with name '{appName}' not found.");
                            }
                        }
                    }

                    // If appId is found, retrieve all containers with Parent as appId
                    if (appId > 0)
                    {
                        string query = "SELECT Name FROM Containers WHERE Parent = @appId ORDER BY Id";
                        using (SqlCommand command = new SqlCommand(query, conn))
                        {
                            command.Parameters.AddWithValue("@appId", appId);
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    containerNames.Add(reader["Name"].ToString());
                                }
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException("Error occurred while retrieving containers for the application", ex);
                }
            }

            // Create XML string with container names
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement root = xmlDoc.CreateElement("Containers");
            xmlDoc.AppendChild(root);
            foreach (var containerName in containerNames)
            {
                XmlElement nameElement = xmlDoc.CreateElement("Container");
                nameElement.SetAttribute("Name", containerName);
                root.AppendChild(nameElement);
            }

            return xmlDoc.OuterXml;
        }


        public string GetContainer(string appName, string containerName)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement containerElement = xmlDoc.CreateElement("Container");
            xmlDoc.AppendChild(containerElement);
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // First, get the application ID corresponding to appName
                    int appId = 0;
                    using (SqlCommand appCommand = new SqlCommand("SELECT Id FROM Applications WHERE Name = @appName", conn))
                    {
                        appCommand.Parameters.AddWithValue("@appName", appName);
                        using (SqlDataReader appReader = appCommand.ExecuteReader())
                        {
                            if (appReader.Read())
                            {
                                appId = (int)appReader["Id"];
                            }
                            else
                            {
                                throw new KeyNotFoundException($"Application with name '{appName}' not found.");
                            }
                        }
                    }

                    // Now retrieve the container details
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Containers WHERE Name = @containerName AND Parent = @appId", conn))
                    {
                        command.Parameters.AddWithValue("@containerName", containerName);
                        command.Parameters.AddWithValue("@appId", appId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                containerElement.SetAttribute("Id", reader["Id"].ToString());
                                containerElement.SetAttribute("Name", reader["Name"].ToString());
                                containerElement.SetAttribute("Creation_dt", ((DateTime)reader["Creation_dt"]).ToString("s")); //ISO 8601 format
                                containerElement.SetAttribute("Parent", reader["Parent"].ToString());
                            }
                            else
                            {
                                throw new KeyNotFoundException($"Container '{containerName}' within application '{appName}' not found.");
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException("Error occurred while retrieving container", ex);
                }
            }

            return xmlDoc.OuterXml;
        }


        public bool PostContainer(string containerXml)
        {
            Container container = null;
            try
            {
                container = DeserializeContainer(containerXml);
            }
            catch
            {
                throw;
            }

            if (container == null)
            {
                throw new ArgumentException("The deserialized container is null.");
            }

            if (string.IsNullOrWhiteSpace(container.Name) || container.Parent <= 0)
            {
                throw new ArgumentException("Invalid container data: Name and Parent are required.");
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "INSERT INTO Containers(Name, Creation_dt, Parent) VALUES (@name, @creation_dt, @parent)";
                    using (SqlCommand command = new SqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@name", container.Name);
                        command.Parameters.AddWithValue("@creation_dt", System.DateTime.UtcNow);
                        command.Parameters.AddWithValue("@parent", container.Parent);

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException("Error occurred while creating container", ex);
                }
            }
        }

        public bool PutContainer(string name, string containerXml)
        {
            Container container = null;
            try
            {
                container = DeserializeContainer(containerXml);
            }
            catch
            {
                throw;
            }



            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "UPDATE Containers SET Creation_dt = @creation_dt WHERE Name = @name";
                    using (SqlCommand command = new SqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@newName", container.Name);
                        command.Parameters.AddWithValue("@creation_dt", container.Creation_dt);
                        command.Parameters.AddWithValue("@parent", container.Parent);
                        command.Parameters.AddWithValue("@name", name);

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException("Error occurred while updating container", ex);
                }
            }
        }

        public bool DeleteContainer(string name)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "DELETE FROM Containers WHERE Name = @name";
                    using (SqlCommand command = new SqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException("Error occurred while deleting container", ex);
                }
            }
        }


        public Container DeserializeContainer(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return null;

            Container container = null;
            XmlSerializer serializer = new XmlSerializer(typeof(Container));

            using (StringReader reader = new StringReader(xml))
            {
                try
                {
                    container = (Container)serializer.Deserialize(reader);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Error occurred while deserializing container", ex);
                }
            }

            return container;
        }
    }
}
