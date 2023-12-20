using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using SOMIODd25.Models;
using System.Data.SqlClient;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.Web.Http.Results;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Xml.Serialization;

namespace SOMIODd25.Controllers
{
    public class ApplicationsController
    {
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SOMIODdb.Properties.Settings.ConnStr"].ConnectionString;

        public string GetAllApplications()
        {
            List<string> applicationNames = new List<string>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string str = "SELECT name FROM applications ORDER BY id";
                    using (SqlCommand command = new SqlCommand(str, conn))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                applicationNames.Add(reader["name"].ToString());
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException("Error occurred while retriving all applications", ex);
                }
            }

            // Create an XML string with application names
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement root = xmlDoc.CreateElement("Data");
            xmlDoc.AppendChild(root);

            XmlElement nameList = xmlDoc.CreateElement("NameList");
            root.AppendChild(nameList);
            foreach (var appName in applicationNames)
            {
                XmlElement nameElement = xmlDoc.CreateElement("name");
                nameElement.InnerText = appName;
                nameList.AppendChild(nameElement);
            }

            // To return the XML document as a string, use OuterXml property
            return xmlDoc.OuterXml;
        }

        public string GetApplication(string name)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement appElement = xmlDoc.CreateElement("Application");
            xmlDoc.AppendChild(appElement);
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlCommand command = new SqlCommand())
                    {
                        command.CommandText = "SELECT * FROM Applications WHERE name = @name";
                        command.Parameters.AddWithValue("@name", name);
                        command.CommandType = System.Data.CommandType.Text;
                        command.Connection = conn;
                        int id = 0;
                        using (SqlDataReader readerApp = command.ExecuteReader())
                        {
                            if (readerApp.Read())
                            {
                                id = (int)readerApp["Id"];
                                appElement.SetAttribute("Id", readerApp["Id"].ToString());
                                appElement.SetAttribute("Name", readerApp["Name"].ToString());
                                appElement.SetAttribute("Creation_dt", ((DateTime)readerApp["Creation_dt"]).ToString("s")); //ISO 8601 format
                            }
                            else
                            {
                                throw new KeyNotFoundException($"Application with name '{name}' not found.");
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException("Error occurred while retriving application", ex);
                }
            }
            // To return the XML document as a string, use OuterXml property
            return xmlDoc.OuterXml;
        }

        public bool PostApplication([FromBody] string appXml)
        {
            Application app = null;
            try
            {
                app = DeserializeApplication(appXml);
            }
            catch
            {
                throw;
            }

            if (app == null)
            {
                throw new ArgumentException("The deserialized application is null.");
            }

            if (string.IsNullOrWhiteSpace(app.Name))
            {
                throw new ArgumentException("Invalid application data: Name is required.");
            }
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string str = "INSERT INTO Applications(name, creation_dt) values(@name, @creation_dt)";
                    using (SqlCommand command = new SqlCommand(str, conn))
                    {
                        command.Parameters.AddWithValue("@name", app.Name);
                        command.Parameters.AddWithValue("@creation_dt", System.DateTime.UtcNow);
                        command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException("Error occurred while creating application", ex);
                }
            }
            return true;
        }

        public bool PutApplication(string name, string appXml)
        {
            Application app = null;
            try
            {
                app = DeserializeApplication(appXml);
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
                    string str = "UPDATE Applications SET creation_dt = @creation_dt WHERE name = @name";
                    using (SqlCommand command = new SqlCommand(str, conn))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        command.Parameters.AddWithValue("@creation_dt", app.Creation_dt);
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

        public bool DeleteApplication(string name)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string str = "DELETE FROM Applications WHERE name = @name";
                    using (SqlCommand command = new SqlCommand(str, conn))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        command.CommandType = System.Data.CommandType.Text;
                        command.Connection = conn;
                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException("Error occurred while deleting application", ex);
                }
            }
        }

        public Application DeserializeApplication(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return null;

            Application app = null;
            XmlSerializer serializer = new XmlSerializer(typeof(Application));

            using (StringReader reader = new StringReader(xml))
            {
                try
                {
                    app = (Application)serializer.Deserialize(reader);
                }
                catch(Exception ex)
                {
                    throw new InvalidOperationException("Error occurred while Deserializing application", ex);
                }
            }

            return app;
        }
    }
}