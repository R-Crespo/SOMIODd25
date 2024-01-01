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

        private void Form1_Load(object sender, EventArgs e)
        {
            // Connect to the MQTT broker
            mqttClient.Connect(Guid.NewGuid().ToString());
            if (!mqttClient.IsConnected)
            {
                MessageBox.Show("Error connecting to message broker...");
                return;
            }
            MessageBox.Show("Connected to the broker successfully");
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
                mqttClient.Publish("light_bulb", Encoding.UTF8.GetBytes(message));
                MessageBox.Show($"Message '{message}' published to topic 'light_bulb'");
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
                mqttClient.Disconnect();
            }
        }


    }
}