using System;
using System.IO;
using System.Windows.Forms;

namespace MailClient
{
    public partial class Form1 : Form
    {
        private string[] userData;
        Client client;
        object value;
        int count;
        public Form1()
        {
            InitializeComponent();
            userData = new string[5];
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || textBox2.Text == "") throw new Exception("Input e-mail address and password.");
            userData[0] = "pop3.poczta.onet.pl";
            userData[1] = "110";
            userData[2] = textBox1.Text;
            userData[3] = textBox2.Text;
            userData[4] = "7";
            File.WriteAllLines(Environment.CurrentDirectory + "\\Mail.config", userData);
            timer1.Interval = Convert.ToInt32(userData[4]) * 1000;
            client = new Client(userData[0], Convert.ToInt32(userData[1]), userData[2], userData[3]);
            count = client.MessageCount;
            client.CheckNewMessages();
            label3.Text = "Ilość pisem na e-maile: " + client.MessageCount;
            for (int i = 1; i <= client.MessageCount; i++)
            {
                if(client.GetMailHeaders(i).TryGetValue("Subject",out value))
                {
                    if ((string)value == "")
                    {
                        listBox1.Items.Add("<no header>");
                    }
                    else
                    {
                        listBox1.Items.Add(value);
                    }
                }
            }
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            button1.Enabled = false;
            timer1.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            client.Close();
            timer1.Stop();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            client.Connect();
            if (count != client.MessageCount && client.CheckNewMessages())
            {
                MessageBox.Show("Nowyj list!");
            }
            if (client.MessageCount != count)
            {
                for (int i = 1; i <= client.MessageCount; i++)
                {
                    if (client.GetMailHeaders(i).TryGetValue("Subject", out value))
                    {
                        if ((string)value == "")
                        {
                            listBox1.Items.Add("<no header>");
                        }
                        else
                        {
                            listBox1.Items.Add(value);
                        }
                    }
                }
            }
            label3.Text = "Ilość pisem na e-maile: " + client.MessageCount;
        }
    }
}