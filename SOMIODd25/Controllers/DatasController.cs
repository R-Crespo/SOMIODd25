using SOMIODd25.Models;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Web.DynamicData;
using System.Collections;

namespace SOMIODd25.Controllers
{
    public class DatasController : ApiController
    {
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SOMIODdb.Properties.Settings.ConnStr"].ConnectionString;

        public string GetData(string data, string appName, string containerName)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement dataElement = doc.CreateElement("Data");
            doc.AppendChild(dataElement);
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
                    string query = "SELECT * FROM Datas d WHERE d.name = @data AND d.parent = @container";
                    using (SqlCommand command = new SqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@data", data);
                        command.Parameters.AddWithValue("@container", containerId);
                        command.CommandType = System.Data.CommandType.Text;
                        command.Connection = conn;
                        int id = 0;
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                id = (int)reader["Id"];
                                dataElement.SetAttribute("Id", reader["Id"].ToString());
                                dataElement.SetAttribute("Name", reader["Name"].ToString());
                                dataElement.SetAttribute("Content", reader["Content"].ToString());
                                dataElement.SetAttribute("Creation_dt", ((DateTime)reader["Creation_dt"]).ToString("s"));
                                dataElement.SetAttribute("Parent", reader["Parent"].ToString());

                            }
                            else
                            {
                                throw new KeyNotFoundException($"Data with name '{data}' not found.");
                            }
                        }
                    }

                    return doc.OuterXml;
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException("Error occurred while retriving Data ", ex);
                }
            }
        }

        public bool PutData(string dataName, string dataXml, string appName, string containerName)
        {
            Data data = null;
            try
            {
                data = DeserializeData(dataXml);
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
                    var (appExists, containerId) = VerifyAppAndContainer(appName, containerName);
                    if (!appExists)
                    {
                        throw new KeyNotFoundException("Application not found.");
                    }
                    if (containerId == 0)
                    {
                        throw new KeyNotFoundException("Container not found or does not belong to the application.");
                    }
                    string str = "UPDATE Datas SET content = @content WHERE name = @name AND parent = @parent";
                    using (SqlCommand command = new SqlCommand(str, conn))
                    {
                        command.Parameters.AddWithValue("@content", data.Content);
                        command.Parameters.AddWithValue("@name", dataName);
                        command.Parameters.AddWithValue("@parent", containerId);
                        command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException("Error occurred while updating application", ex);
                }
            }
            return true;
        }
        public bool PostData([FromBody] string dataXml, string appName ,string containerName)
        {
            Data data = null;
            try
            {
                data = DeserializeData(dataXml);
            }
            catch
            {
                throw;
            }

            if (data == null)
            {
                throw new ArgumentException("The deserialized data is null.");
            }

            if (string.IsNullOrWhiteSpace(data.Name) || string.IsNullOrWhiteSpace(data.Content))
            {
                throw new ArgumentException("Invalid data: Name and Content are required.");
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

                    string str = "INSERT INTO Datas(content, name, creation_dt, parent) values(@content , @name, @creation_dt, @parent)";
                    using (SqlCommand command = new SqlCommand(str, conn))
                    {
                        command.Parameters.AddWithValue("@content", data.Content);
                        command.Parameters.AddWithValue("@name", data.Name);
                        command.Parameters.AddWithValue("@creation_dt", System.DateTime.UtcNow);
                        command.Parameters.AddWithValue("@parent", containerId);
                        command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException("Error occurred while creating data", ex);
                }
            }
            return true;
        }

        public bool DeleteData(string name, string appName, string containerName)
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
                    string query = "DELETE FROM Datas WHERE Name = @name AND parent = @parent";
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
                    throw new InvalidOperationException("Error occurred while deleting container", ex);
                }
            }
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

        public Data DeserializeData(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return null;

            Data data = null;
            XmlSerializer serializer = new XmlSerializer(typeof(Data));

            using (StringReader reader = new StringReader(xml))
            {
                try
                {
                    data = (Data)serializer.Deserialize(reader);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Error occurred while Deserializing data", ex);
                }
            }

            return data;
        }


    }
}