using Flowframes.Data;
using Flowframes.OS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.Forms
{
    public partial class UpdaterForm : Form
    {
        Version installed;
        Version latestVer;

        public UpdaterForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            InitializeComponent();
        }

        private async void UpdaterForm_Load(object sender, EventArgs e)
        {
            installed = Updater.GetInstalledVer();
            latestVer = Updater.GetLatestVer();

            installedLabel.Text = installed.ToString();
            await Task.Delay(100);
            latestLabel.Text = $"{latestVer}";

            if (Updater.CompareVersions(installed, latestVer) == Updater.VersionCompareResult.Equal)
            {
                statusLabel.Text = "Latest Version Is Installed.";

                if (Updater.CompareVersions(installed, latestVer) == Updater.VersionCompareResult.Newer)
                    statusLabel.Text += "\nUpdate Available";

                return;
            }

        }

        float lastProg = -1f;
        public void SetProgLabel (float prog, string str)
        {
            if (prog == lastProg) return;
            lastProg = prog;
            downloadingLabel.Text = str;
        }

        private void updateFreeBtn_Click(object sender, EventArgs e)
        {
            string link = "https://github.com/animafps/flowframes/releases/latest";
            if (!string.IsNullOrWhiteSpace(link))
                Process.Start(link);
        }
    }
}
