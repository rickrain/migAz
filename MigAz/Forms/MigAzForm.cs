﻿using System;
using System.Windows.Forms;
using MigAz.Providers;
using MigAz.Core.Interface;
using System.Reflection;
using MigAz.UserControls.Migrators;
using MigAz.Core.Generator;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace MigAz.Forms
{
    public partial class MigAzForm : Form
    {
        #region Variables

        private FileLogProvider _logProvider;
        private IStatusProvider _statusProvider;
        private AppSettingsProvider _appSettingsProvider;

        #endregion

        #region Constructors
        public MigAzForm()
        {
            InitializeComponent();
            _logProvider = new FileLogProvider();
            _logProvider.OnMessage += _logProvider_OnMessage;
            _statusProvider = new UIStatusProvider(this.toolStripStatusLabel1);
            _appSettingsProvider = new AppSettingsProvider();

            txtDestinationFolder.Text = AppDomain.CurrentDomain.BaseDirectory;
            propertyPanel1.Clear();
            splitContainer2.SplitterDistance = this.Height / 2;
            lblLastOutputRefresh.Text = String.Empty;
        }

        private void _logProvider_OnMessage(string message)
        {
            txtLog.AppendText(message);
            txtLog.SelectionStart = txtLog.TextLength;
        }

        private void AzureRetriever_OnRestResult(Guid requestGuid, string url, string response)
        {
            txtRest.AppendText(requestGuid.ToString() + " " + url + Environment.NewLine);
            txtRest.AppendText(response + Environment.NewLine + Environment.NewLine);
            txtRest.SelectionStart = txtRest.TextLength;
        }

        #endregion

        #region Properties

        public ILogProvider LogProvider
        {
            get { return _logProvider; }
        }

        public IStatusProvider StatusProvider
        {
            get { return _statusProvider; }
        }

        internal AppSettingsProvider AppSettingsProvider
        {
            get { return _appSettingsProvider; }
        }

        #endregion

        #region Form Events

        private void MigAzForm_Load(object sender, EventArgs e)
        {
            _logProvider.WriteLog("MigAzForm_Load", "Program start");
            this.Text = "MigAz";
        }

        #endregion

        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://aka.ms/MigAz");
        }

        private void splitContainer2_Panel2_Resize(object sender, EventArgs e)
        {
            this.tabMigAzMonitoring.Width = splitContainer2.Panel2.Width - 5;
            this.tabMigAzMonitoring.Height = splitContainer2.Panel2.Height - 5;
            this.tabOutputResults.Width = splitContainer2.Panel2.Width - 5;
            this.tabOutputResults.Height = splitContainer2.Panel2.Height - 55;
        }

        private async void TemplateGenerator_AfterTemplateChanged(object sender, EventArgs e)
        {
            TemplateGenerator a = (TemplateGenerator)sender;
            dgvMigAzMessages.DataSource = a.Alerts.Select(x => new { AlertType = x.AlertType, Message = x.Message, SourceObject = x.SourceObject }).ToList();
            dgvMigAzMessages.Columns["Message"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvMigAzMessages.Columns["SourceObject"].Visible = false;
            btnRefreshOutput.Enabled = true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void tabControl1_Resize(object sender, EventArgs e)
        {
            dgvMigAzMessages.Width = tabMigAzMonitoring.Width - 10;
            dgvMigAzMessages.Height = tabMigAzMonitoring.Height - 30;
            txtLog.Width = tabMigAzMonitoring.Width - 10;
            txtLog.Height = tabMigAzMonitoring.Height - 30;
            txtRest.Width = tabMigAzMonitoring.Width - 10;
            txtRest.Height = tabMigAzMonitoring.Height - 30;
        }

        #region Menu Items

        private void aSMToARMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SplitterPanel parent = (SplitterPanel)splitContainer2.Panel1;

            AsmToArm asmToArm = new AsmToArm(StatusProvider, LogProvider, propertyPanel1);
            asmToArm.AzureContextSourceASM.AzureRetriever.OnRestResult += AzureRetriever_OnRestResult;
            asmToArm.AzureContextSourceASM.AfterAzureSubscriptionChange += AzureContextSourceASM_AfterAzureSubscriptionChange;
            asmToArm.TemplateGenerator.AfterTemplateChanged += TemplateGenerator_AfterTemplateChanged;
            parent.Controls.Add(asmToArm);

            newMigrationToolStripMenuItem.Enabled = false;
            closeMigrationToolStripMenuItem.Enabled = true;

            this.Refresh();
            Application.DoEvents();
            asmToArm.ChangeAzureContext();
        }

        private async Task AzureContextSourceASM_AfterAzureSubscriptionChange(Azure.AzureContext sender)
        {
            dgvMigAzMessages.DataSource = null;
            tabOutputResults.TabPages.Clear();
            btnRefreshOutput.Enabled = false;
            lblLastOutputRefresh.Text = String.Empty;
        }

        private void aRMToARMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SplitterPanel parent = (SplitterPanel)splitContainer2.Panel1;

            AsmToArm asmToArm = new AsmToArm(StatusProvider, LogProvider, propertyPanel1);
            asmToArm.AzureContextSourceASM.AzureRetriever.OnRestResult += AzureRetriever_OnRestResult;
            asmToArm.AzureContextSourceASM.AfterAzureSubscriptionChange += AzureContextSourceASM_AfterAzureSubscriptionChange;
            asmToArm.TemplateGenerator.AfterTemplateChanged += TemplateGenerator_AfterTemplateChanged;
            parent.Controls.Add(asmToArm);

            newMigrationToolStripMenuItem.Enabled = false;
            closeMigrationToolStripMenuItem.Enabled = true;

            asmToArm.ActivateSourceARMTab();

            this.Refresh();
            Application.DoEvents();
            asmToArm.ChangeAzureContext();
        }

        private void aWSToARMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("AWS To ARM has not been finalized in this newer MigAz tool.  Continue to use the stand alone AWS To ARM MigAz tool for AWS.");
            return;

            SplitterPanel parent = (SplitterPanel)splitContainer2.Panel1;

            AwsToArm awsToArm = new AwsToArm(StatusProvider, LogProvider, propertyPanel1);
            awsToArm.Bind();
            parent.Controls.Add(awsToArm);

            newMigrationToolStripMenuItem.Enabled = false ;
            closeMigrationToolStripMenuItem.Enabled = true;
        }

        private void closeMigrationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogProvider.WriteLog("closeMigrationToolStripMenuItem_Click", "Start close current migration session control");

            if (splitContainer2.Panel1.Controls.Count > 0)
            {
                foreach (Control control in splitContainer2.Panel1.Controls)
                {
                    control.Dispose();
                }
            }

            propertyPanel1.Clear();
            dgvMigAzMessages.DataSource = null;
            tabOutputResults.TabPages.Clear();
            btnRefreshOutput.Enabled = false;
            lblLastOutputRefresh.Text = String.Empty;
            newMigrationToolStripMenuItem.Enabled = true;
            closeMigrationToolStripMenuItem.Enabled = false;
            this.Text = "MigAz";

            LogProvider.WriteLog("closeMigrationToolStripMenuItem_Click", "End close current migration session control");
        }

        #endregion

        private void splitContainer1_Panel2_Resize(object sender, EventArgs e)
        {
            propertyPanel1.Width = splitContainer1.Panel2.Width - 10;
            propertyPanel1.Height = splitContainer1.Panel2.Height - 100;
            panel1.Top = splitContainer1.Panel2.Height - panel1.Height - 15;
            panel1.Width = splitContainer1.Panel2.Width;
        }

        private void panel1_Resize(object sender, EventArgs e)
        {
            btnExport.Width = panel1.Width - 15;
            btnChoosePath.Left = panel1.Width - btnChoosePath.Width - 10;
            txtDestinationFolder.Width = panel1.Width - btnChoosePath.Width - 30;
        }

        private void reportAnIssueOnGithubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Azure/migAz/issues/new");
        }

        private void visitMigAzOnGithubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://aka.ms/migaz");
        }

        private void btnExport_Click_1(object sender, EventArgs e)
        {
            if (splitContainer2.Panel1.Controls.Count == 1)
            {
                IMigratorUserControl migrator = (IMigratorUserControl)splitContainer2.Panel1.Controls[0];

                if (migrator.TemplateGenerator.HasErrors)
                {
                    tabMigAzMonitoring.SelectTab("tabMessages");
                    MessageBox.Show("There are still one or more error(s) with the template generation.  Please resolve all errors before exporting.");
                    return;
                }

                // We are refreshing both the MemoryStreams and the Output Tabs via this call, prior to writing to files
                btnRefreshOutput_Click(this, null);

                migrator.TemplateGenerator.OutputDirectory = txtDestinationFolder.Text;
                migrator.TemplateGenerator.Write();

                StatusProvider.UpdateStatus("Ready");

                var exportResults = new ExportResultsDialog(migrator.TemplateGenerator);
                exportResults.ShowDialog(this);
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            SplitterPanel parent = (SplitterPanel)splitContainer2.Panel1;

            if (parent.Controls.Count == 1)
            {
                IMigratorUserControl migrator = (IMigratorUserControl)parent.Controls[0];
                object alert = dgvMigAzMessages.Rows[e.RowIndex].Cells["SourceObject"].Value;
                migrator.SeekAlertSource(alert);
            }

        }

        private void tabOutputResults_Resize(object sender, EventArgs e)
        {
            foreach (TabPage tabPage in tabOutputResults.TabPages)
            {
                foreach (Control control in tabPage.Controls)
                {
                    control.Width = tabOutputResults.Width - 15;
                    control.Height = tabOutputResults.Height - 30;
                }
            }
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OptionsDialog optionsDialog = new OptionsDialog();
            optionsDialog.ShowDialog();
        }

        private void btnChoosePath_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
                txtDestinationFolder.Text = folderBrowserDialog1.SelectedPath;
        }

        private void btnRefreshOutput_Click(object sender, EventArgs e)
        {
            SplitterPanel parent = (SplitterPanel)splitContainer2.Panel1;

            if (parent.Controls.Count == 1)
            {
                IMigratorUserControl migrator = (IMigratorUserControl)parent.Controls[0];

                if (migrator.TemplateGenerator.HasErrors)
                {
                    tabMigAzMonitoring.SelectTab("tabMessages");
                    MessageBox.Show("There are still one or more error(s) with the template generation.  Please resolve all errors before exporting.");
                    return;
                }

                migrator.TemplateGenerator.SerializeStreams();

                foreach (TabPage tabPage in tabOutputResults.TabPages)
                {
                    if (!migrator.TemplateGenerator.TemplateStreams.ContainsKey(tabPage.Name))
                        tabOutputResults.TabPages.Remove(tabPage);
                }

                foreach (var templateStream in migrator.TemplateGenerator.TemplateStreams)
                {
                    TabPage tabPage = null;
                    if (!tabOutputResults.TabPages.ContainsKey(templateStream.Key))
                    {
                        tabPage = new TabPage(templateStream.Key);
                        tabPage.Name = templateStream.Key;
                        tabOutputResults.TabPages.Add(tabPage);

                        if (templateStream.Key.EndsWith(".html"))
                        {
                            WebBrowser webBrowser = new WebBrowser();
                            webBrowser.Width = tabOutputResults.Width - 15;
                            webBrowser.Height = tabOutputResults.Height - 30;
                            webBrowser.AllowNavigation = false;
                            webBrowser.ScrollBarsEnabled = true;
                            tabPage.Controls.Add(webBrowser);
                        }
                        else if (templateStream.Key.EndsWith(".json"))
                        {
                            TextBox textBox = new TextBox();
                            textBox.Width = tabOutputResults.Width - 15;
                            textBox.Height = tabOutputResults.Height - 30;
                            textBox.ReadOnly = true;
                            textBox.Multiline = true;
                            textBox.WordWrap = false;
                            textBox.ScrollBars = ScrollBars.Both;
                            tabPage.Controls.Add(textBox);
                        }
                    }
                    else
                    {
                        tabPage = tabOutputResults.TabPages[templateStream.Key];
                    }

                    if (tabPage.Controls[0].GetType() == typeof(TextBox))
                    {
                        TextBox textBox = (TextBox)tabPage.Controls[0];
                        templateStream.Value.Position = 0;
                        textBox.Text = new StreamReader(templateStream.Value).ReadToEnd();
                    }
                    else if (tabPage.Controls[0].GetType() == typeof(WebBrowser))
                    {
                        WebBrowser webBrowser = (WebBrowser)tabPage.Controls[0];
                        templateStream.Value.Position = 0;
                        webBrowser.DocumentText = new StreamReader(templateStream.Value).ReadToEnd();
                    }
                }

                lblLastOutputRefresh.Text = "Last Refresh Completed: " + DateTime.Now.ToString();
                btnRefreshOutput.Enabled = false;

                // post Telemetry Record to ASMtoARMToolAPI
                if (AppSettingsProvider.AllowTelemetry)
                {
                    StatusProvider.UpdateStatus("BUSY: saving telemetry information");
                    migrator.PostTelemetryRecord();
                }
            }
        }
    }
}
