using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;


namespace AppB
{
    public partial class Form1 : Form
    {
        private MqttClient mqttClient;

        public Form1()
        {
            InitializeComponent();
            // Initialize the MQTT client with the broker's IP address.
            mqttClient = new MqttClient("127.0.0.1"); // Use the correct IP address for your MQTT broker
        }

        private void UpdateConnectionStatus(bool isConnected)
        {
            // This ensures the update is thread-safe and happens on the UI thread
            connectionStatusPanel.Invoke((MethodInvoker)delegate
            {
                connectionStatusPanel.BackColor = isConnected ? Color.Green : Color.Red;
            });
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Connect to the MQTT broker
            mqttClient.Connect(Guid.NewGuid().ToString());
            if (!mqttClient.IsConnected)
            {
                MessageBox.Show("Error connecting to message broker...");
                return;
            }
            UpdateConnectionStatus(true);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Publish "on" message to the "light_bulb" topic
            PublishMessage("on");
        }

        private void buttonOFF_Click(object sender, EventArgs e)
        {
            // Publish "off" message to the "light_bulb" topic
            PublishMessage("off");
        }


        private void PublishMessage(string message)
        {
            if (mqttClient.IsConnected)
            {
                // The topic "light_bulb" should match the topic the subscriber is listening to.
                mqttClient.Publish("irrigation_sistem", Encoding.UTF8.GetBytes(message));
            }
            else
            {
                MessageBox.Show("Not connected to the MQTT broker.");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Disconnect from the MQTT broker when the form is closing
            if (mqttClient.IsConnected)
            {
                UpdateConnectionStatus(false);
                mqttClient.Disconnect();
            }
        }
    }
}