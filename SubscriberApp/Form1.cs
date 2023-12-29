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

        private void Form1_Load(object sender, EventArgs e)
        {
            mClient.Connect(Guid.NewGuid().ToString());
            if (!mClient.IsConnected)
            {
                MessageBox.Show("Error connecting to message broker...");
                return;
            }

            mClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            //Subscribe to topics
            byte[] qosLevels = { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE,
            MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE}; //QoS – depends on the topics number
            mClient.Subscribe(mStrTopicsInfo, qosLevels);

            MessageBox.Show("CONECTADO COM SUCESSO");

        }

        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string msg = Encoding.UTF8.GetString(e.Message);
            string texto = "Received = " + msg + " on topic " + e.Topic;
            if(msg == "off")
            {
                pictureBox1.ImageLocation = "./light-bulb.png";
                pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            }
            else
            {
                pictureBox1.ImageLocation = "./light-bulb (1).png";
                pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            }
            MessageBox.Show(texto);
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
