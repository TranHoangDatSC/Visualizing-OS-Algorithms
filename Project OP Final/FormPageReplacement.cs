using Project_OP_Final.Class;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Project_OP_Final
{
    public partial class FormPageReplacement : Form
    {
        private static readonly char[] Separators = { ' ', ',', '\t', '\r', '\n' };

        private readonly Dictionary<string, string> algorithmNames = new Dictionary<string, string>
        {
            { "OPT", "Optimal (OPT)" },
            { "LRU", "Least Recently Used (LRU)" },
            { "FIFO", "First-In First-Out (FIFO)" },
            { "Clock", "Clock Replacement" }
        };

        public FormPageReplacement()
        {
            InitializeComponent();
        }

        private string SelectedAlgorithm => cb_Algorithm.SelectedItem?.ToString() ?? "";

        private void FormPageReplacement_Load(object sender, EventArgs e)
        {
            cb_Algorithm.Items.AddRange(algorithmNames.Keys.ToArray());
            cb_Algorithm.SelectedIndex = 0;

            dgv_Result.ReadOnly = true;
            dgv_Result.AllowUserToAddRows = false;
            dgv_Result.AllowUserToDeleteRows = false;
            dgv_Result.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            dgv_Result.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv_Result.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            lb_Pagefaults.Text = "Page faults: ";
        }

        private void cb_Algorithm_SelectedIndexChanged(object sender, EventArgs e)
        {
            labelAlgorithm.Text = algorithmNames.TryGetValue(SelectedAlgorithm, out string name)
                ? $"Algorithm: {name}"
                : "Algorithm: ";
        }

        private void btn_Simulation_Click(object sender, EventArgs e)
        {
            if (!TryReadReferenceString(out List<int> reference))
                return;

            var pr = new PageReplacement(reference, (int)nud_Frame.Value);
            switch (SelectedAlgorithm)
            {
                case "OPT": pr.RunOPT(); break;
                case "LRU": pr.RunLRU(); break;
                case "FIFO": pr.RunFIFO(); break;
                case "Clock": pr.RunClock(); break;
                default:
                    MessageBox.Show("Invalid algorithm", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
            }

            ShowFrameStates(pr);
            lb_Pagefaults.Text = $"Page faults: {pr.Faults}";
        }

        private void btn_Reset_Click(object sender, EventArgs e)
        {
            txt_Page_reference_string.Text = "";
            cb_Algorithm.SelectedIndex = 0;
            nud_Frame.Value = 3;
            dgv_Result.Rows.Clear();
            dgv_Result.Columns.Clear();
            lb_Pagefaults.Text = "Page faults: ";
        }

        private void btn_LoadFromFile_Click(object sender, EventArgs e)
        {
            string filePath = DataFile.PickFileToOpen("PageReferenceString.txt");
            if (filePath == null) return;

            try
            {
                txt_Page_reference_string.Text = File.ReadAllText(filePath).Trim();
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Failed to read the file:\n" + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btn_SaveFile_Click(object sender, EventArgs e)
        {
            string content = txt_Page_reference_string.Text.Trim();
            if (content.Length == 0)
            {
                MessageBox.Show("⚠️ No content to save. Please enter or load a page reference string first.",
                                "Empty Content", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string filePath = DataFile.PickFileToSave("PageReferenceString.txt");
            if (filePath == null) return;

            try
            {
                File.WriteAllText(filePath, content);
                MessageBox.Show("✅ Page reference string saved successfully.",
                                "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Failed to save the file:\n" + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ---------------- Helpers ----------------

        private bool TryReadReferenceString(out List<int> reference)
        {
            reference = new List<int>();

            foreach (string token in txt_Page_reference_string.Text.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(token, out int page) || page < 0)
                {
                    MessageBox.Show("The page reference string must contain non-negative integers, separated by spaces or commas.",
                                    "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                reference.Add(page);
            }

            if (reference.Count == 0)
            {
                MessageBox.Show("Please enter page reference string.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// One column per reference, one row per frame and a last row marking page faults.
        /// For the Clock algorithm, pages whose reference bit is set are shown with a "*".
        /// </summary>
        private void ShowFrameStates(PageReplacement pr)
        {
            dgv_Result.Rows.Clear();
            dgv_Result.Columns.Clear();

            int columnCount = pr.FrameStates.Count;
            for (int col = 0; col < columnCount; col++)
            {
                int index = dgv_Result.Columns.Add($"Ref{col}", pr.ReferenceString[col].ToString());
                dgv_Result.Columns[index].SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            for (int row = 0; row < pr.FrameCount; row++)
            {
                object[] rowData = new object[columnCount];
                for (int col = 0; col < columnCount; col++)
                {
                    List<int> frames = pr.FrameStates[col];
                    if (row >= frames.Count)
                    {
                        rowData[col] = "";
                        continue;
                    }

                    bool bitSet = pr.BitStates != null && pr.BitStates[col][row];
                    rowData[col] = bitSet ? $"{frames[row]}*" : frames[row].ToString();
                }

                int rowIndex = dgv_Result.Rows.Add(rowData);
                dgv_Result.Rows[rowIndex].HeaderCell.Value = $"Frame {row + 1}";
            }

            object[] faultRow = pr.PageFaults.Select(f => (object)(f ? "F" : "")).ToArray();
            int faultIndex = dgv_Result.Rows.Add(faultRow);
            DataGridViewRow faultGridRow = dgv_Result.Rows[faultIndex];
            faultGridRow.HeaderCell.Value = "Fault";
            faultGridRow.DefaultCellStyle.ForeColor = Color.Red;
            faultGridRow.DefaultCellStyle.Font = new Font(dgv_Result.Font, FontStyle.Bold);

            // Highlight the page that was loaded at each fault
            for (int col = 0; col < columnCount; col++)
            {
                if (!pr.PageFaults[col]) continue;

                int loadedSlot = pr.FrameStates[col].IndexOf(pr.ReferenceString[col]);
                dgv_Result.Rows[loadedSlot].Cells[col].Style.BackColor = Color.LightCoral;
            }
        }
    }
}
