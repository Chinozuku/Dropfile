using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Microsoft.VisualBasic;

using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;

namespace Dropfile
{
    public partial class Form1 : Form
    {
        static IAmazonS3 client;
        static TransferUtility utility;
        public static String user = "";
        static String bucket = "chinozuku";

        public Form1()
        {
            InitializeComponent();
            utility = new TransferUtility();
            initAWS();
        }

        private void initAWS()
        {
            NameValueCollection appConfig = ConfigurationManager.AppSettings;
            treeView1.Nodes.Clear();
            listView1.Items.Clear();
            if(Directory.Exists(user)) Directory.Delete(user, true);
            using (client = new AmazonS3Client())
            {
                S3Object[] data = ListingObjects(user);
                createDirectory(data);
                //PopulateTreeView(data);
                
            }
        }

        private void createDirectory(S3Object[] data)
        {
            foreach(S3Object dir in data)
            {
                if(dir.Size == 0)
                {
                    Directory.CreateDirectory(dir.Key);
                }
            }
            foreach (S3Object dir in data)
            {
                if (dir.Size != 0)
                {
                    File.Create(dir.Key);
                }
            }
            PopulateTreeView();
        }

        private void PopulateTreeView()
        {
            TreeNode rootNode;

            DirectoryInfo info = new DirectoryInfo(user);
            if (info.Exists)
            {
                rootNode = new TreeNode(user);
                rootNode.Tag = info;
                GetDirectories(info.GetDirectories(), rootNode);
                treeView1.Nodes.Add(rootNode);
            }
        }

        private void GetDirectories(DirectoryInfo[] subDirs, TreeNode nodeToAddTo)
        {
            TreeNode aNode;
            DirectoryInfo[] subSubDirs;
            foreach (DirectoryInfo subDir in subDirs)
            {
                aNode = new TreeNode(subDir.Name, 0, 0);
                aNode.Tag = subDir;
                aNode.ImageKey = "folder";
                subSubDirs = subDir.GetDirectories();
                if (subSubDirs.Length != 0)
                {
                    GetDirectories(subSubDirs, aNode);
                }
                nodeToAddTo.Nodes.Add(aNode);
            }
        }

        private S3Object[] ListingObjects(String user)
        {
            try
            {
                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = bucket;
                ListObjectsResponse response = client.ListObjects(request);

                //get all data about currently logged in user
                request.Prefix = user;
                response = client.ListObjects(request);
                return response.S3Objects.ToArray();
            }
            catch (AmazonS3Exception amazonS3Exception) { return null; }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode newSelected = e.Node;
            listView1.Items.Clear();
            DirectoryInfo nodeDirInfo = (DirectoryInfo)newSelected.Tag;
            ListViewItem.ListViewSubItem[] subItems;
            ListViewItem item = null;

            foreach (DirectoryInfo dir in nodeDirInfo.GetDirectories())
            {
                item = new ListViewItem(dir.Name, 0);
                subItems = new ListViewItem.ListViewSubItem[]
                  {new ListViewItem.ListViewSubItem(item, "Directory"), 
                   new ListViewItem.ListViewSubItem(item, 
				dir.LastAccessTime.ToShortDateString())};
                item.SubItems.AddRange(subItems);
                listView1.Items.Add(item);
            }

            foreach (FileInfo file in nodeDirInfo.GetFiles())
            {
                item = new ListViewItem(file.Name, 1);
                subItems = new ListViewItem.ListViewSubItem[]
                  { new ListViewItem.ListViewSubItem(item, "File"), 
                   new ListViewItem.ListViewSubItem(item, 
				file.LastAccessTime.ToShortDateString())};

                item.SubItems.AddRange(subItems);
                listView1.Items.Add(item);
            }

            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                String filePath = "/";
                if (listView1.FocusedItem != null) filePath += listView1.FocusedItem.Text;
                DeletingAnObject(treeView1.SelectedNode.FullPath + filePath);
                initAWS();
            }
            else MessageBox.Show("Please select an item to delete");
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            if (listView1.FocusedItem != null)
            {
                String filePath = "/";
                if (listView1.FocusedItem != null) filePath += listView1.FocusedItem.Text;
                utility.Download(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), listView1.FocusedItem.Text), "chinozuku", treeView1.SelectedNode.FullPath + filePath);
                MessageBox.Show("Download Complete");
            }
            else MessageBox.Show("Please choose a file to download", "No File Choosen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if(ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                String[] tmp = ofd.FileName.Split('\\');
                utility.Upload(ofd.FileName, bucket, treeView1.SelectedNode.FullPath + "/" + tmp[tmp.Length - 1]);
                MessageBox.Show("Upload Complete");
                initAWS();
            }
        }

        private void btnFolder_Click(object sender, EventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Input a Folder Name", "Folder Name", "Default", -1, -1);
            if (input != "")
            {
                WritingAnObject(treeView1.SelectedNode.FullPath + "/" + input + "/");
                initAWS();
            }
            else MessageBox.Show("Please enter a valid folder name");
        }

        private void DeletingAnObject(String file)
        {
            try
            {
                DeleteObjectRequest request = new DeleteObjectRequest()
                {
                    BucketName = bucket,
                    Key = file
                };

                client = new AmazonS3Client();
                client.DeleteObject(request);
            }
            catch (AmazonS3Exception amazonS3Exception) { }
        }

        static void WritingAnObject(String folderName)
        {
            try
            {
                client = new AmazonS3Client();
                PutObjectRequest request = new PutObjectRequest()
                {
                    ContentBody = "Uploaded at " + DateAndTime.DateString,
                    BucketName = bucket,
                    Key = folderName
                };

                PutObjectResponse response = client.PutObject(request);
            }
            catch (AmazonS3Exception amazonS3Exception) { }
        }
    }
}
