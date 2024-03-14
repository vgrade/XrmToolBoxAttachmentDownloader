using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using McTools.Xrm.Connection;

namespace XrmToolBox.AttachmentsDownloader
{
    public partial class MyPluginControl : PluginControlBase
    {
        private Settings mySettings;

        private int _Zero = 0;

        public MyPluginControl()
        {
            InitializeComponent();
        }

        private void MyPluginControl_Load(object sender, EventArgs e)
        {
           
            // Loads or creates the settings for the plugin
            if (!SettingsManager.Instance.TryLoad(GetType(), out mySettings))
            {
                mySettings = new Settings();

                LogWarning("Settings not found => a new settings file has been created!");
            }
            else
            {
                LogInfo("Settings found and loaded");
            }
        }

        private void tsbClose_Click(object sender, EventArgs e)
        {
            CloseTool();
        }

        /// <summary>
        /// This event occurs when the plugin is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyPluginControl_OnCloseTool(object sender, EventArgs e)
        {
            // Before leaving, save the settings
            SettingsManager.Instance.Save(GetType(), mySettings);
        }

        /// <summary>
        /// This event occurs when the connection has been updated in XrmToolBox
        /// </summary>
        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            if (mySettings != null && detail != null)
            {
                mySettings.LastUsedOrganizationWebappUrl = detail.WebApplicationUrl;
                LogInfo("Connection has changed to: {0}", detail.WebApplicationUrl);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            lb_totalfilescount.Text = _Zero.ToString();
            listView1.Items.Clear();
            if (String.IsNullOrEmpty(outputpath.Text))
            {
                MessageBox.Show("Please provide output path", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                outputpath.Focus();
            }
            else if (String.IsNullOrEmpty(xmlinput.Text))
            {
                MessageBox.Show("Please provide Fetchxml", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                xmlinput.Focus();
            }
            else if (FormatXML())
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("outputpath", outputpath.Text);
                parameters.Add("xmlinput", xmlinput.Text);
                tabControl1.SelectedTab = tabPage2;
                ExecuteMethod(DownLoadAttachment, parameters);
            }
        }

        private void DownLoadAttachment(Dictionary<string, string> parameters)
        {
            string _outputPath = string.Empty;
            string _fetchXml = string.Empty;
            int recordCount = 0;
            int _loopCounter = 0;
            parameters.TryGetValue("outputpath", out _outputPath);
            parameters.TryGetValue("xmlinput", out _fetchXml);
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Downloading your files...",
                Work = (worker, args) =>
                {
                    int queryCount = 5000;
                    int pageNumber = 1;

                    int totalRecords = AttachmentHelper.GetToalRecordsCount(Service, _fetchXml);
                    string message = $"Total notes records found : {totalRecords}. Do you want to proceed to download?";
                    string title = "Total records Notification";
                    MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                    DialogResult result = MessageBox.Show(message, title, buttons);
                    if (result == DialogResult.Yes)
                    {
                        QueryExpression queryExpression = AttachmentHelper.GetQueryExpression(Service, _fetchXml);
                        queryExpression.PageInfo = new PagingInfo();
                        queryExpression.PageInfo.Count = queryCount;
                        queryExpression.PageInfo.PageNumber = pageNumber;
                        queryExpression.PageInfo.PagingCookie = null;
                        queryExpression.PageInfo.ReturnTotalRecordCount = true;


                        DataTable dt = new DataTable();
                        dt.Columns.Add("Row Id");
                        dt.Columns.Add("Record Name");
                        dt.Columns.Add("Record Guid");
                        dt.Columns.Add("Notes Record Guid");
                        dt.Columns.Add("File Name");
                        dt.Columns.Add("Status");
                        dt.Columns.Add("Message");

                        var dict = new Dictionary<string, int>();

                        while (true)
                        {
                            EntityCollection enColl = Service.RetrieveMultiple(queryExpression);
                            recordCount += enColl.Entities.Count;
                            if (recordCount == 0)
                            {
                                MessageBox.Show("No notes record found against fetch xml's records.");
                                break;
                            }
                            if (enColl.Entities != null)
                            {
                                foreach (Entity annotationRec in enColl.Entities)
                                {
                                    ListViewItem lvItem = new ListViewItem((++_loopCounter).ToString());

                                    try
                                    {
                                        if (annotationRec.Contains("filename"))
                                        {
                                            String filename = annotationRec.GetAttributeValue<String>("filename");

                                            if (dict.ContainsKey(filename))
                                            {
                                                int num = dict[filename] + 1;
                                                dict[filename] = num;

                                                string versionText = " (" + num + ")";
                                                if (filename.LastIndexOf('.') != -1)
                                                {
                                                    int position = filename.LastIndexOf('.');
                                                    filename = filename.Insert(position, versionText);
                                                }
                                                else
                                                {
                                                 filename += versionText;
                                                }
                                            }
                                            else
                                            {
                                                dict.Add(filename, 1);
                                            }

                                            String noteBody = string.Empty;
                                            if (annotationRec.Contains("documentbody"))//for annotation
                                            {
                                                noteBody = annotationRec.GetAttributeValue<String>("documentbody");
                                            }
                                            else if (annotationRec.Contains("body"))//for attachments-activitymimeattachment
                                            {
                                                noteBody = annotationRec.GetAttributeValue<String>("body");
                                            }
                                            string outputFileName = @"" + _outputPath + "\\" + filename;

                                            lvItem.SubItems.Add(annotationRec.GetAttributeValue<AliasedValue>("Regarding." + AttachmentHelper._PrimaryNameAttribute).Value.ToString());
                                            lvItem.SubItems.Add(annotationRec.GetAttributeValue<EntityReference>("objectid").Id.ToString());
                                            lvItem.SubItems.Add(annotationRec.Id.ToString());
                                            lvItem.SubItems.Add(filename);

                                            if (!string.IsNullOrEmpty(noteBody))
                                            {
                                                try
                                                {
                                                    byte[] fileContent = Convert.FromBase64String(noteBody);
                                                    System.IO.File.WriteAllBytes(outputFileName, fileContent);
                                                    lvItem.SubItems.Add("Downloaded");
                                                    lvItem.SubItems.Add(string.Empty);
                                                }
                                                catch (Exception ex)
                                                {
                                                    lvItem.SubItems.Add("Failed");
                                                    lvItem.SubItems.Add($"Failed in converting file to base 64 or writing file to output location. Exception: {ex.Message}");
                                                }
                                            }
                                            else
                                            {
                                                lvItem.SubItems.Add("Failed");
                                                lvItem.SubItems.Add("Notes body is empty");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show("Error in downloading attachments. Exception: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        break;
                                    }
                                    worker.ReportProgress(0, lvItem);
                                    worker.ReportProgress(1);
                                }
                            }
                            if (enColl.MoreRecords)
                            {
                                queryExpression.PageInfo.PageNumber = ++pageNumber;
                                queryExpression.PageInfo.PagingCookie = enColl.PagingCookie;
                                queryExpression.PageInfo.ReturnTotalRecordCount = true;
                            }
                            else
                            {
                                break;
                            }

                        }
                        lb_totalfilescount.Text = recordCount.ToString();
                        args.Result = null;
                    }

                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                },
                ProgressChanged = args =>
                {
                    if (args.ProgressPercentage == 1)
                    {
                        SetWorkingMessage(string.Format("Downloading Notes: {0} of {1}", _loopCounter, recordCount));
                    }
                    else
                    {
                        listView1.Items.Add((ListViewItem)args.UserState);
                    }
                }
            });

        }

        private void BrowseBtn_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog(); ;
            folderBrowserDialog1.Description = "Please select a folder";
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
            DialogResult result = folderBrowserDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                outputpath.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(xmlinput.Text))
            {
                if (FormatXML())
                {
                    BrowseBtn.Focus();
                }
            }
        }

        private bool FormatXML()
        {
            try
            {
                xmlinput.Text = XDocument.Parse(xmlinput.Text).ToString();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in formatting fetxh xml, Details: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                xmlinput.Focus();
                return false;
            }
        }

        private void linkLabel7_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkLabel1.LinkVisited = true;
            System.Diagnostics.Process.Start("https://vishalgrade.com/");
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkLabel1.LinkVisited = true;
            System.Diagnostics.Process.Start("https://www.linkedin.com/in/dynamics365blocks/");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkLabel1.LinkVisited = true;
            System.Diagnostics.Process.Start("https://github.com/vgrade/XrmToolBoxAttachmentDownloader");
        }
    }
}