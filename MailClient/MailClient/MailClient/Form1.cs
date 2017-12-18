using System;
using System.IO;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
/*
********************************************************************************************************************************************
************************************_____________TCPCLIENT OR SOCKET?____________***********************************************************
********************************************************************************************************************************************
*/
//pop3.poczta.onet.pl
//995
//baranovpaul24@onet.pl
//Tamerlan1963
namespace MailClient
{
    public partial class Form1 : Form
    {
        private string[] userData;
        Client client;
        public Form1()
        {
            InitializeComponent();
            userData = new string[5];
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || textBox2.Text == "") throw new Exception("Input e-mail address and password.");
            /*if (File.Exists(Environment.CurrentDirectory + "\\Mail.config") == false)
            {*/
            userData[0] = "pop3.poczta.onet.pl";
            userData[1] = "110";
            userData[2] = textBox1.Text;
            userData[3] = textBox2.Text;
            userData[4] = "7";
            File.WriteAllLines(Environment.CurrentDirectory + "\\Mail.config", userData);
            /*}
            {
                userData = File.ReadAllLines(Environment.CurrentDirectory + "Mail.config");
            }*/
            timer1.Interval = Convert.ToInt32(userData[4]) * 1000;
            client = new Client(userData[0], Convert.ToInt32(userData[1]), userData[2], userData[3]);
            /*TcpClient cl = new TcpClient();
            WriteToLog("Соединяюсь с сервером {0}:{1}", userData[0], userData[1]);
            cl.Connect(userData[0], Convert.ToInt32(userData[1]));*/
            
            label3.Text = "Ilość pisem na e-maile: " + client.MessageCount;
            Height = 212;
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            button1.Enabled = false;
            timer1.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Height = 134;
            client.Close();
            timer1.Stop();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //client.Connect();
            label3.Text = "Ilość pisem na e-maile: " + client.MessageCount;
        }

        
    }
}