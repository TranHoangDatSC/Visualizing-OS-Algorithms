using Project_OP_Final.Class;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Project_OP_Final
{
    public partial class FormDiskScheduling : Form
    {
        private static readonly char[] Separators = { ',', ' ', '\t', '\r', '\n' };

        private readonly Dictionary<string, string> algorithmDescriptions = new Dictionary<string, string>
        {
            { "FCFS", "First-Come, First-Served (FCFS)" },
            { "SSTF", "Shortest Seek Time First (SSTF)" },
            { "SCAN", "Elevator Algorithm (SCAN)" },
            { "C-SCAN", "Circular SCAN (C-SCAN)" },
            { "LOOK", "Optimized SCAN (LOOK)" },
            { "C-LOOK", "Circular LOOK (C-LOOK)" }
        };

        public FormDiskScheduling()
        {
            InitializeComponent();
            SetupDataGridView();

            cbSelectAlgorithm.Items.AddRange(algorithmDescriptions.Keys.ToArray());
            cbSelectAlgorithm.SelectedIndexChanged += cbSelectAlgorithm_SelectedIndexChanged;
            cbSelectAlgorithm.SelectedIndex = 0;
        }

        private string SelectedAlgorithm => cbSelectAlgorithm.SelectedItem?.ToString() ?? "";

        private bool NeedsDiskSize => SelectedAlgorithm == "SCAN" || SelectedAlgorithm == "C-SCAN";

        private bool NeedsDirection => NeedsDiskSize || SelectedAlgorithm == "LOOK" || SelectedAlgorithm == "C-LOOK";

        // ---------------- Event handlers ----------------

        private void cbSelectAlgorithm_SelectedIndexChanged(object sender, EventArgs e)
        {
            labelAlgorithms.Text = algorithmDescriptions[SelectedAlgorithm];

            txtDiskSize.Enabled = NeedsDiskSize;
            if (!NeedsDiskSize)
                txtDiskSize.Text = "";

            radLeft.Visible = NeedsDirection;
            radRight.Visible = NeedsDirection;
            if (NeedsDirection && !radLeft.Checked)
                radRight.Checked = true;
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            dgvAlgorithm.Rows.Clear();
            txtTotalHeadMovement.Clear();
            labelSteps.Visible = false;

            if (!TryReadInput(out List<int> requests, out int head, out int diskSize))
                return;

            Direction direction = radLeft.Checked ? Direction.Left : Direction.Right;
            List<SeekStep> steps;

            switch (SelectedAlgorithm)
            {
                case "FCFS": steps = DiskScheduling.FCFS(requests, head); break;
                case "SSTF": steps = DiskScheduling.SSTF(requests, head); break;
                case "SCAN": steps = DiskScheduling.SCAN(requests, head, diskSize, direction); break;
                case "C-SCAN": steps = DiskScheduling.CSCAN(requests, head, diskSize, direction); break;
                case "LOOK": steps = DiskScheduling.LOOK(requests, head, direction); break;
                case "C-LOOK": steps = DiskScheduling.CLOOK(requests, head, direction); break;
                default: return;
            }

            var sb = new StringBuilder();
            foreach (var step in steps)
            {
                dgvAlgorithm.Rows.Add(step.From, step.To, step.Distance);
                sb.AppendLine($"From {step.From} to {step.To}, Distance: {step.Distance}");
            }

            txtTotalHeadMovement.Text = DiskScheduling.TotalMovement(steps).ToString();
            labelSteps.Text = sb.ToString();
            labelSteps.Visible = steps.Count > 0;
        }

        private void btnLoadFromFile_Click(object sender, EventArgs e)
        {
            string filePath = DataFile.PickFileToOpen("DiskScheduling.txt");
            if (filePath == null) return;

            try
            {
                txtRequestQueue.Text = File.ReadAllText(filePath).Trim();
            }
            catch (Exception ex)
            {
                ShowError("Failed to read file:\n" + ex.Message);
            }
        }

        private void btnSaveRequest_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRequestQueue.Text))
            {
                MessageBox.Show("Request queue is empty!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string filePath = DataFile.PickFileToSave("DiskScheduling.txt");
            if (filePath == null) return;

            try
            {
                File.WriteAllText(filePath, txtRequestQueue.Text.Trim());
                MessageBox.Show($"Saved to {filePath}", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError("Failed to save file:\n" + ex.Message);
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            txtRequestQueue.Clear();
            txtHeadPosition.Clear();
            txtDiskSize.Clear();
            txtTotalHeadMovement.Clear();
            dgvAlgorithm.Rows.Clear();
            labelSteps.Visible = false;
            cbSelectAlgorithm.SelectedIndex = 0;
        }

        // ---------------- Helpers ----------------

        private void SetupDataGridView()
        {
            dgvAlgorithm.Rows.Clear();
            dgvAlgorithm.Columns.Clear();
            dgvAlgorithm.Columns.Add("From", "From");
            dgvAlgorithm.Columns.Add("To", "To");
            dgvAlgorithm.Columns.Add("Distance", "Distance");
            dgvAlgorithm.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvAlgorithm.AllowUserToAddRows = false;
            dgvAlgorithm.ReadOnly = true;
        }

        private bool TryReadInput(out List<int> requests, out int head, out int diskSize)
        {
            requests = new List<int>();
            head = 0;
            diskSize = 0;

            foreach (string token in txtRequestQueue.Text.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(token, out int request) || request < 0)
                {
                    ShowError($"Invalid request \"{token}\". Please enter non-negative integers separated by commas.");
                    return false;
                }
                requests.Add(request);
            }

            if (requests.Count == 0)
            {
                ShowError("Please enter at least one request in the queue!");
                return false;
            }

            if (!int.TryParse(txtHeadPosition.Text.Trim(), out head) || head < 0)
            {
                ShowError("Please enter a valid initial head position (integer ≥ 0)!");
                return false;
            }

            if (NeedsDiskSize)
            {
                int maxCylinder = Math.Max(head, requests.Max());
                if (!int.TryParse(txtDiskSize.Text.Trim(), out diskSize) || diskSize <= maxCylinder)
                {
                    ShowError($"Please enter a valid disk size (greater than {maxCylinder}).\n" +
                              "Cylinders are numbered from 0 to Disk Size - 1.");
                    return false;
                }
            }

            return true;
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
