using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

using MySql.Data.MySqlClient;

namespace Dropfile
{
    public partial class Login : Form
    {
        MySQLClient bridge;
        public Login()
        {
            InitializeComponent();
            bridge = new MySQLClient("cc.cv3g5aczna8b.us-east-1.rds.amazonaws.com", "cc", "admin", "admin123", 3306, 100);
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            Dictionary<String, String> tmp = bridge.Select("user", "username", "`username` = '" + tbUsername.Text.ToLower() + "' and `password` = '" + tbPassword.Text + "'");
            if (tmp.ContainsKey("username"))
            {
                //if (Directory.Exists(Form1.user)) Directory.Delete(Form1.user, true);
                Form1.user = tmp["username"];
                Form1 f = new Form1();
                f.Show(this);
                this.Hide();
                f.FormClosed += f_FormClosed;
            }
            else Console.WriteLine("Username not found");
        }

        void f_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Show();
            tbPassword.Text = "";
            tbUsername.Text = "";
            if (Directory.Exists(Form1.user)) Directory.Delete(Form1.user, true);
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            Dictionary<String, String> tmp = bridge.Select("user", "username", "`username` = '" + tbUsername.Text.ToLower() + "'");
            if (tmp.ContainsKey("username"))
            {
                MessageBox.Show("username already exists");
            }
            else
            {
                try
                {
                    AmazonS3Client client = new AmazonS3Client();
                    PutObjectRequest request = new PutObjectRequest()
                    {
                        BucketName = "chinozuku",
                        Key = tbUsername.Text.ToLower() + "/"
                    };

                    PutObjectResponse response = client.PutObject(request);
                    bridge.Insert("user", "username, password", "'" + tbUsername.Text.ToLower() + "', '" + tbPassword.Text + "'");
                    MessageBox.Show("Registration Complete");
                }
                catch (AmazonS3Exception amazonS3Exception) { }
            }
        }
    }
}
