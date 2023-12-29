using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace SubscriberApp
{
    public partial class Form1 : Form
    {
        MqttClient mClient = new MqttClient(IPAddress.Parse("127.0.0.1")); //OR use the broker hostname
        string[] mStrTopicsInfo = { "light_bulb" };
        public Form1()
        {
            InitializeComponent();
        }
        private void UpdateConnectionStatus(bool isConnected)
        {
            // This ensures the update is thread-safe and happens on the UI thread
            connectionStatusPanel.Invoke((MethodInvoker)delegate
            {
                connectionStatusPanel.BackColor = isConnected ? Color.Green : Color.Red;
            });
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                if (!await ApplicationExistsAsync())
                {
                    await CreateResourcesAsync();
                }

                mClient.Connect(Guid.NewGuid().ToString());
                if (!mClient.IsConnected)
                {
                    UpdateConnectionStatus(false);
                    MessageBox.Show("Error connecting to message broker...");
                    return;
                }

                mClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                //Subscribe to topics
                byte[] qosLevels = { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE}; //QoS – depends on the topics number
                mClient.Subscribe(mStrTopicsInfo, qosLevels);

                pictureBox1.ImageLocation = "./light-bulb.png";
                UpdateConnectionStatus(true);
            }
            catch (MqttClientException ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"General Exception: {ex.Message}");
            }
        }


        private async Task<bool> ApplicationExistsAsync()
        {
            string applicationName = "Lighting";
            using (var client = new HttpClient())
            {
                // Set the base address to the SOMIOD API
                client.BaseAddress = new Uri("http://localhost:55921/api/somiod/");

                // Set the request header for discover
                client.DefaultRequestHeaders.Add("somiod-discover", "application");

                // Send a GET request to the discover endpoint
                HttpResponseMessage response = await client.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(content);

                    // Assuming the XML has a structure with <name> elements for each application
                    var appNodes = xmlDoc.GetElementsByTagName("name");
                    foreach (XmlNode node in appNodes)
                    {
                        if (node.InnerText.Equals(applicationName, StringComparison.OrdinalIgnoreCase))
                        {
                            return true; // The application already exists
                        }
                    }
                }
                else
                {
                    // Handle error or unsuccessful status code
                    throw new HttpRequestException($"Discover request failed with status code: {response.StatusCode}");
                }
            }
            return false; // The application does not exist
        }


        private async Task CreateResourcesAsync()
        {
            string applicationXml = "<Application><Name>Lighting</Name></Application>";
            string containerXml = "<Container><Name>light_bulb</Name></Container>";
            using (var client = new HttpClient())
            {
                // Set the base address to the SOMIOD API
                client.BaseAddress = new Uri("http://localhost:55921/api/somiod/");

                // Set headers if necessary, e.g. client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Create the application resource
                HttpResponseMessage appResponse = await client.PostAsync("", new StringContent(applicationXml, Encoding.UTF8, "application/xml"));
                if (!appResponse.IsSuccessStatusCode)
                {
                    // Handle error
                    MessageBox.Show("Failed to create application resource.");
                    return;
                }

                string appName = "Lighting";
                // Create the container resource
                HttpResponseMessage containerResponse = await client.PostAsync($"{appName}", new StringContent(containerXml, Encoding.UTF8, "application/xml"));
                if (!containerResponse.IsSuccessStatusCode)
                {
                    // Handle error
                    MessageBox.Show("Failed to create container resource.");
                    return;
                }
            }
        }


        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                string msg = Encoding.UTF8.GetString(e.Message);
                this.Invoke((MethodInvoker)delegate
                {
                    // Update UI safely within this block
                    string texto = "Received = " + msg + " on topic " + e.Topic;
                    if (msg == "off")
                    {
                        pictureBox1.ImageLocation = "./light-bulb.png";
                    }
                    else if (msg == "on")
                    {
                        pictureBox1.ImageLocation = "./light-bulb (1).png";
                    }
                    pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                    MessageBox.Show(texto);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in publish event: {ex.Message}");
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mClient.IsConnected)
            {
                UpdateConnectionStatus(false);
                mClient.Unsubscribe(mStrTopicsInfo); //Put this in a button to see notif!
                mClient.Disconnect(); //Free process and process's resources
            }
        }
    }
}
