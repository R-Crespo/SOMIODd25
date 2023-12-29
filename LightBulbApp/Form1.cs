using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace LightBulbApp
{
    public partial class Form1 : Form
    {
        MqttClient mClient = new MqttClient(IPAddress.Parse("127.0.0.1")); //OR use the broker hostname
        string[] mStrTopicsInfo = { "light_bulb" };
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                mClient.Connect(Guid.NewGuid().ToString());
                if (!mClient.IsConnected)
                {
                    MessageBox.Show("Error connecting to message broker...");
                    return;
                }
                // Rest of your code...

                mClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                //Subscribe to topics
                byte[] qosLevels = { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
                MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE}; //QoS – depends on the topics number
                mClient.Subscribe(mStrTopicsInfo, qosLevels);

                MessageBox.Show("CONECTADO COM SUCESSO");
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
                    else
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
                mClient.Unsubscribe(mStrTopicsInfo); //Put this in a button to see notif!
                mClient.Disconnect(); //Free process and process's resources
            }
        }
    }
}
