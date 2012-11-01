using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Sage.SData.Client.Core;
using Sage.SData.Client.Atom;
using Sage.SData.Client.Extensions;
using Sage.SData.Client.Framework;
using System.IO;
using System.Diagnostics;
using Sage.SData.Client.Mime;

namespace GroupAttachments
{

  
    public partial class Form1 : Form
    {
        
     
        public Form1()
        {
            InitializeComponent();
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {

        
            //this is taken right from the saleslogix SData whitepaper
         var service = new SDataService("http://localhost:3333/sdata/slx/system/-/", txtUserName.Text, txtPassword.Text);
           var request = new SDataResourceCollectionRequest(service)
                {
                    ResourceKind = "groups",
                    //QueryValues =
                    //    {
                    //        {"where", "not isHidden"},
                    //     }
                };
           lblURI.Text = request.ToString();
           foreach (var entry in request.Read().Entries)
           {
               //need to see if the item is in the family list already....
               var group = entry.GetSDataPayload();
               bool add = true;
               foreach (Item item in cboFamily.Items)
               {
                   add = true;
                   if (item.Value.ToUpper() == group.Values["family"].ToString().ToUpper())
                   {
                       add = false;
                   }
               }
               if (add)
               {
                   cboFamily.Items.Add(new Item(group.Values["family"].ToString(), group.Values["family"].ToString(), group.Values["family"].ToString(), group.Key,""));
               }
               
               
           }
        }

        private void cboFamily_SelectedIndexChanged(object sender, EventArgs e)
        {
            cboGroup.Items.Clear();
                //this is taken right from the saleslogix SData whitepaper
         var service = new SDataService("http://localhost:3333/sdata/slx/system/-/", txtUserName.Text, txtPassword.Text);
           var request = new SDataResourceCollectionRequest(service)
                {
                    ResourceKind = "groups",
                  QueryValues =
                        {
                            {"where", "family eq '" + cboFamily.Items[cboFamily.SelectedIndex].ToString() + "' and not isHidden"}
                        }
                };
           lblURI.Text = request.ToString();
           foreach (var entry in request.Read().Entries)
           {
               var group = entry.GetSDataPayload();
               cboGroup.Items.Add(new Item(group.Values["displayName"].ToString(), group.Values["displayName"].ToString(), group.Values["family"].ToString(), group.Key, group.Values["keyField"].ToString()));
           }

        }

        private void cboGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            cboEntity.Items.Clear();

            //need to the key from the selected group.                        
            Item selecteditem = (Item)cboGroup.Items[cboGroup.SelectedIndex];

            ///slx/system/-/groups/$queries/execute?_groupId=p6UJ9A0004TS
            //_groupId is a custom query argument. The SData specification requires that they be prefixed with an underscore.
            //This one will be a bit different since we need the URI to look like the above..
            var testuri = new SDataUri("http://localhost:3333/sdata/slx/system/-/") { CollectionType = "groups" };
            testuri.AppendPath("$queries");
            testuri.AppendPath("execute");
            testuri.QueryArgs.Add("_groupID", selecteditem.Key);
            
            var request = new SDataRequest(testuri.ToString()) {UserName = txtUserName.Text};
            var response = request.GetResponse();
            var feed = (AtomFeed)response.Content;
            lblURI.Text = testuri.ToString();
            foreach (var entry in feed.Entries)
            {
                var group = entry.GetSDataPayload();
                cboEntity.Items.Add(new Item(group.Values[selecteditem.KeyField].ToString(), group.Values[selecteditem.KeyField].ToString(), group.Values[selecteditem.KeyField].ToString(), group.Values[selecteditem.KeyField].ToString(), selecteditem.KeyField));
            }
        }

        private void cboEntity_SelectedIndexChanged(object sender, EventArgs e)
        {

            lstAttachments.Items.Clear();

            //need to go get all the attachments for the selected account..
            //need to the key from the selected group.                        
            Item selecteditem = (Item)cboEntity.Items[cboEntity.SelectedIndex];
            //http://localhost:3333//sdata/slx/system/-/attachments?where=accountId%20eq%20%27AGHEA0002669%27&format=json
            var service = new SDataService("http://localhost:3333/sdata/slx/system/-/", txtUserName.Text, txtPassword.Text);
            var request = new SDataResourceCollectionRequest(service)
            {
                ResourceKind = "attachments",
                QueryValues =
                        {
                                //once you upload a file to your account on your vm you can use this commented line which only shows files that exist.
                          //  {"where", FormatMeasId(selecteditem.KeyField) + " eq '" + selecteditem.Key + "' and fileExists eq True"}
                            {"where", FormatMeasId(selecteditem.KeyField) + " eq '" + selecteditem.Key + "'"}
                        }
            };
            lblURI.Text = request.ToString();
            try
            {
                foreach (var entry in request.Read().Entries)
                {
                    var group = entry.GetSDataPayload();
                    lstAttachments.Items.Add((new
                        Item(
                            group.Values["fileName"].ToString(),
                            group.Values["description"].ToString(),
                            group.Values["physicalFileName"].ToString(),
                            group.Key,
                            group.Values["fileSize"].ToString()
                            )
                           )
                );

                }
            }
            catch (Exception ex)
            {

                MessageBox.Show("It is possible the attachments are not related to entities with the id " + FormatMeasId(selecteditem.KeyField) + "\n This does not mean there are no attachments, but rather that the system is unable to successfully query for attachments using this key field.", "Unable to retrieve attachments.");
            }
           

        }
        private string FormatMeasId(string e)
        {
            string temp = e.ToLower();
            temp = temp.Substring(0, temp.Length - 2);
            temp = temp + e.Substring(e.Length - 2, 1).ToUpper();
            temp = temp + e.Substring(e.Length - 1, 1).ToLower();

            return temp;
        }



        private void txtUserName_Leave(object sender, EventArgs e)
        {
            cboFamily.Items.Clear();
            Form1_Load(sender, e);
        }

        private void lstAttachments_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstAttachments.SelectedIndex > -1)
            {
                Item selecteditem = (Item)lstAttachments.Items[lstAttachments.SelectedIndex];
                //http://localhost:3333//sdata/slx/system/-/attachments?where=accountId%20eq%20%27AGHEA0002669%27&format=json
                var request = new SDataRequest("http://localhost:3333/sdata/slx/system/-/attachments('" + selecteditem.Key + "')"){ UserName = txtUserName.Text , Password = txtPassword.Text};
            
                lblURI.Text = request.Uri;

                try
                {
                    var response = request.GetResponse();
                        var fileentry = (AtomEntry)response.Content;
                        var filedetails = fileentry.GetSDataPayload();
                        rtbDetails.Text = "";
                        foreach (var val in filedetails.Values)
                        {
                            rtbDetails.Text += val.Key + "-" + val.Value + "\n";
                        }               

                   
                }
                catch (Exception ex)
                {

                    MessageBox.Show("It is possible the attachments are not related to entities with the id " + FormatMeasId(selecteditem.KeyField) + "\n This does not mean there are no attachments, but rather that the system is unable to successfully query for attachments using this key field.", "Unable to retrieve attachments.");
                }
           

            }
            else
            {
                MessageBox.Show("Sorry. There is no file selected.", "Please select a file.");
            }
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {

            if (lstAttachments.SelectedIndex > -1)
            {
                Item selecteditem = (Item)lstAttachments.Items[lstAttachments.SelectedIndex];

                //this is right out of the SData whitepaper.
                var request = new SDataRequest("http://localhost:3333/sdata/slx/system/-/attachments('" + selecteditem.Key + "')/file") { UserName = txtUserName.Text };
                var response = request.GetResponse();
                
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    try
                    {
                        //var filePath = Path.Combine(Path.GetTempPath(), selecteditem.Name);
                        var filePath = Path.Combine(@"C:\users\administrator\desktop\", selecteditem.Name);
                        File.WriteAllBytes(filePath, (byte[])response.Content);

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        throw;
                    }
                    finally
                    {
                        if (File.Exists(Path.Combine(@"C:\users\administrator\desktop\", selecteditem.Name)))
                        {
                            MessageBox.Show("File was saved to: " + Path.Combine(@"C:\users\administrator\desktop\", selecteditem.Name), "File Saved!");
                        }
                    
                    }
                    
                }
                
             
            }
            else
            {
                MessageBox.Show("Sorry. There is no file selected.", "Please select a file.");
            }
            
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            Item selecteditem = (Item)cboEntity.Items[cboEntity.SelectedIndex];

            if (selecteditem.KeyField.ToUpper() != "ACCOUNTID")
            {
                MessageBox.Show("Sorry you must have selected an Account first.");
            }
            else if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtFiletoUpload.Text  = ofd.FileName;
                //again, this is right from the white paper:
                var contentType = MimeHelper.FindMimeType(txtFiletoUpload.Text);
                var fileName = Path.GetFileName(txtFiletoUpload.Text);
                var stream = File.OpenRead(txtFiletoUpload.Text);
                var file = new AttachedFile(contentType, fileName, stream);
                var entry = new AtomEntry();
                entry.SetSDataPayload(
                    new SDataPayload
                    {
                        Namespace = "http://schemas.sage.com/slx/system/2010",
                        ResourceName = "attachment",
                        Values = { { "accountId", selecteditem.Value },{"description", Path.GetFileName(fileName)} }
                    });

                var operation = new RequestOperation(HttpMethod.Post, entry)
                { Files = { file }};
                var request = new SDataRequest("http://localhost:3333/sdata/slx/system/-/attachments", operation) { UserName = txtUserName.Text, Password = txtPassword.Text };
                var response = request.GetResponse();
                if (response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    MessageBox.Show("File was uploaded and attached to account: " + selecteditem.Value, "File Uploaded.");
                    cboEntity_SelectedIndexChanged(sender, e);

                }
            } 
        }

      
    }
    //simple item class from:
    //http://social.msdn.microsoft.com/forums/en-US/winforms/thread/c7a82a6a-763e-424b-84e0-496caa9cfb4d/

    public class Item
    {
        public string Name;
        public string Value;
        public string Family;
        public string Key;
        public string KeyField;

        public Item(string name, string value, string family, string key, string keyfield)
        {
            Name = name; Value = value; Family = family; Key = key; KeyField = keyfield;
        }
        public override string ToString()
        {
            // Generates the text shown in the combo box
            return Name;
        }
    }

}
