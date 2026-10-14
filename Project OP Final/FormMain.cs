using System;
using System.Windows.Forms;

namespace Project_OP_Final
{
    public partial class FormMain : Form
    {
        private const string HomeTitle = "OPERATING SYSTEM ALGORITHMS";

        private Form currentChild;

        public FormMain()
        {
            InitializeComponent();
        }

        /// <summary>Shows <paramref name="childForm"/> inside the main panel, closing the previous one.</summary>
        private void OpenChildForm(Form childForm, string title)
        {
            CloseChildForm();

            currentChild = childForm;
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;
            MainPanel.Controls.Add(childForm);
            childForm.BringToFront();
            childForm.Show();

            labelAlgorithms.Text = title;
        }

        private void CloseChildForm()
        {
            if (currentChild == null) return;

            currentChild.Close();
            currentChild = null;
            labelAlgorithms.Text = HomeTitle;
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            CloseChildForm();
        }

        private void btnBankerAlgorithm_Click(object sender, EventArgs e)
        {
            OpenChildForm(new FormBankerAlgorithm(), "Banker Algorithm");
        }

        private void btnCPUScheduling_Click(object sender, EventArgs e)
        {
            OpenChildForm(new FormCPUScheduling(), "CPU Scheduling Algorithm");
        }

        private void btnPageReplacement_Click(object sender, EventArgs e)
        {
            OpenChildForm(new FormPageReplacement(), "Page Replacement Algorithm");
        }

        private void btnDiskScheduling_Click(object sender, EventArgs e)
        {
            OpenChildForm(new FormDiskScheduling(), "Disk Scheduling Algorithm");
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
