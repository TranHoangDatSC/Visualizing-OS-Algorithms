using Project_OP_Final.Class;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Project_OP_Final
{
    public partial class FormBankerAlgorithm : Form
    {
        private const string MaxDemandFile = "MaxDemand.txt";
        private const string AllocationFile = "Allocation.txt";
        private const string AvailableFile = "Available.txt";

        private static readonly char[] Separators = { ' ', '\t', ',' };

        private BankerAlgorithm banker;
        private readonly List<NumericUpDown> nudRequests = new List<NumericUpDown>();

        public FormBankerAlgorithm()
        {
            InitializeComponent();
        }

        // ---------------- Event handlers ----------------

        private void btnLoad_Click(object sender, EventArgs e)
        {
            int processCount = (int)nudProcesses.Value;
            int resourceCount = (int)nudResources.Value;

            if (processCount <= 0 || resourceCount <= 0)
            {
                MessageBox.Show("Please enter a valid number of processes and resources (> 0).",
                                "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            InitBanker(processCount, resourceCount);
        }

        private void btnLoadFromTxt_Click(object sender, EventArgs e)
        {
            try
            {
                string dataDir = DataFile.DataDirectory;
                List<int[]> maxRows = ReadMatrix(Path.Combine(dataDir, MaxDemandFile));
                List<int[]> allocRows = ReadMatrix(Path.Combine(dataDir, AllocationFile));
                List<int[]> availRows = ReadMatrix(Path.Combine(dataDir, AvailableFile));

                if (maxRows.Count == 0 || availRows.Count == 0)
                    throw new InvalidDataException("Data files are empty.");

                int processCount = maxRows.Count;
                int resourceCount = maxRows[0].Length;

                if (allocRows.Count != processCount)
                    throw new InvalidDataException("Allocation line count does not match MaxDemand.");
                if (maxRows.Concat(allocRows).Any(r => r.Length != resourceCount) || availRows[0].Length != resourceCount)
                    throw new InvalidDataException("Resource count does not match between the files.");
                if (processCount > nudProcesses.Maximum || resourceCount > nudResources.Maximum)
                    throw new InvalidDataException("Too many processes or resources in the files.");

                nudProcesses.Value = processCount;
                nudResources.Value = resourceCount;
                InitBanker(processCount, resourceCount);

                for (int i = 0; i < processCount; i++)
                {
                    for (int j = 0; j < resourceCount; j++)
                    {
                        dgvMaxDemand.Rows[i].Cells[j].Value = maxRows[i][j];
                        dgvAllocation.Rows[i].Cells[j].Value = allocRows[i][j];
                    }
                }

                for (int j = 0; j < resourceCount; j++)
                    dgvAvailable.Rows[0].Cells[j].Value = availRows[0][j];

                if (TryLoadData())
                    ShowNeed();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading file:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSaveFile_Click(object sender, EventArgs e)
        {
            if (!EnsureLoaded("saving") || !TryLoadData()) return;

            try
            {
                string dataDir = DataFile.DataDirectory;

                File.WriteAllLines(Path.Combine(dataDir, MaxDemandFile),
                    Enumerable.Range(0, banker.ProcessCount).Select(i => RowToString(banker.MaxDemand, i)));
                File.WriteAllLines(Path.Combine(dataDir, AllocationFile),
                    Enumerable.Range(0, banker.ProcessCount).Select(i => RowToString(banker.Allocation, i)));
                File.WriteAllText(Path.Combine(dataDir, AvailableFile), string.Join(" ", banker.Available));

                MessageBox.Show("✔ Data successfully saved to files.", "Save Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Error saving files:\n" + ex.Message, "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCheckSafe_Click(object sender, EventArgs e)
        {
            if (!EnsureLoaded("checking system safety") || !TryLoadData()) return;

            ClearResult();
            ShowNeed();

            if (banker.IsSafe(out List<int> safeSequence, out bool isDeadlock))
            {
                SetStatus("✔ The system is in a safe state.", Color.Green);
                labelSequence.Text = "Safe sequence: " + FormatSequence(safeSequence);
                return;
            }

            if (isDeadlock)
            {
                SetStatus("⛔ DEADLOCK detected! All processes are blocked.", Color.DarkRed);
                labelSuggest.Text = banker.SuggestDeadlockSolution();
            }
            else
            {
                SetStatus("❌ The system is in an unsafe state (but no deadlock has occurred).", Color.OrangeRed);
            }

            labelSequence.Text = safeSequence.Count > 0
                ? "Some processes can still proceed: " + FormatSequence(safeSequence)
                : "No process can proceed.";
        }

        private void btnRequestResource_Click(object sender, EventArgs e)
        {
            if (!EnsureLoaded("making a resource request") || !TryLoadData()) return;

            ClearResult();

            int processIndex = cbProcessRequest.SelectedIndex;
            int[] request = nudRequests.Select(n => (int)n.Value).ToArray();
            string requestText = $"P{processIndex} requests ({string.Join(", ", request)}).";

            switch (banker.RequestResource(processIndex, request))
            {
                case RequestResult.Granted:
                    SetStatus("✔ Request granted. System is still in a safe state.\n" + requestText, Color.Green);
                    WriteDataToGrids(); // Show the new Allocation / Available
                    break;

                case RequestResult.ExceedsNeed:
                    SetStatus("❌ Request denied. The process has exceeded its maximum claim.\n" + requestText, Color.Red);
                    labelSuggest.Text = "- A process cannot request more than its Need (Max - Allocation).";
                    break;

                case RequestResult.MustWait:
                    SetStatus("⏳ Request denied. Not enough available resources.\n" +
                              $"{requestText} Available: ({string.Join(", ", banker.Available)}).", Color.OrangeRed);
                    labelSuggest.Text = $"- P{processIndex} must wait until other processes release their resources.";
                    break;

                case RequestResult.UnsafeState:
                    SetStatus("❌ Request denied. System would be unsafe.\n" + requestText, Color.Red);
                    labelSuggest.Text = "Possible solutions:" + Environment.NewLine +
                                        "- Release resources from other process(es)." + Environment.NewLine +
                                        "- Wait until resources become available." + Environment.NewLine +
                                        $"- Roll back or terminate P{processIndex} to avoid deadlock.";
                    break;
            }

            ShowNeed();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            banker = null;

            foreach (var grid in new[] { dgvMaxDemand, dgvAllocation, dgvNeed, dgvAvailable })
            {
                grid.Rows.Clear();
                grid.Columns.Clear();
            }

            flowPanelRequest.Controls.Clear();
            nudRequests.Clear();
            cbProcessRequest.Items.Clear();
            ClearResult();
        }

        // ---------------- Setup ----------------

        private void InitBanker(int processCount, int resourceCount)
        {
            banker = new BankerAlgorithm(processCount, resourceCount);

            InitGrid(dgvMaxDemand, processCount, resourceCount, "P");
            InitGrid(dgvAllocation, processCount, resourceCount, "P");
            InitGrid(dgvNeed, processCount, resourceCount, "P");
            InitGrid(dgvAvailable, 1, resourceCount, null);
            dgvNeed.ReadOnly = true; // Need is always computed
            dgvAvailable.Rows[0].HeaderCell.Value = "Available";

            InitRequestControls(resourceCount);

            cbProcessRequest.Items.Clear();
            for (int i = 0; i < processCount; i++)
                cbProcessRequest.Items.Add($"P{i}");
            cbProcessRequest.SelectedIndex = 0;

            ClearResult();
        }

        private static void InitGrid(DataGridView grid, int rows, int cols, string rowPrefix)
        {
            grid.Rows.Clear();
            grid.Columns.Clear();
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;

            for (int j = 0; j < cols; j++)
            {
                int index = grid.Columns.Add($"R{j}", $"R{j}");
                grid.Columns[index].SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            grid.Rows.Add(rows);
            if (rowPrefix != null)
            {
                for (int i = 0; i < rows; i++)
                    grid.Rows[i].HeaderCell.Value = $"{rowPrefix}{i}";
            }
        }

        private void InitRequestControls(int resourceCount)
        {
            flowPanelRequest.Controls.Clear();
            nudRequests.Clear();

            for (int j = 0; j < resourceCount; j++)
            {
                var panel = new FlowLayoutPanel
                {
                    AutoSize = true,
                    FlowDirection = FlowDirection.LeftToRight,
                    Margin = new Padding(0, 0, 10, 5),
                    Padding = new Padding(0)
                };
                var label = new Label
                {
                    AutoSize = true,
                    Text = $"R{j}:",
                    Anchor = AnchorStyles.Left,
                    Margin = new Padding(0, 6, 4, 0)
                };
                var nud = new NumericUpDown
                {
                    Minimum = 0,
                    Maximum = 1000,
                    Width = 80
                };

                nudRequests.Add(nud);
                panel.Controls.Add(label);
                panel.Controls.Add(nud);
                flowPanelRequest.Controls.Add(panel);
            }
        }

        // ---------------- Data ----------------

        private bool EnsureLoaded(string action)
        {
            if (banker != null) return true;

            MessageBox.Show($"⚠️ Please load data before {action}.",
                            "Data Not Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        /// <summary>Copies the grids into the banker model, validating every cell.</summary>
        private bool TryLoadData()
        {
            for (int i = 0; i < banker.ProcessCount; i++)
            {
                for (int j = 0; j < banker.ResourceCount; j++)
                {
                    if (!TryReadCell(dgvMaxDemand, i, j, "Max Demand", out int max) ||
                        !TryReadCell(dgvAllocation, i, j, "Allocation", out int alloc))
                        return false;

                    if (alloc > max)
                    {
                        ShowMissingData($"Allocation [P{i}, R{j}] = {alloc} is greater than Max Demand = {max}.");
                        return false;
                    }

                    banker.MaxDemand[i, j] = max;
                    banker.Allocation[i, j] = alloc;
                }
            }

            for (int j = 0; j < banker.ResourceCount; j++)
            {
                if (!TryReadCell(dgvAvailable, 0, j, "Available", out int available))
                    return false;
                banker.Available[j] = available;
            }

            return true;
        }

        private bool TryReadCell(DataGridView grid, int row, int col, string tableName, out int value)
        {
            object cell = grid.Rows[row].Cells[col].Value;
            if (int.TryParse(cell?.ToString(), out value) && value >= 0)
                return true;

            string position = grid == dgvAvailable ? $"R{col}" : $"P{row}, R{col}";
            ShowMissingData($"Missing or invalid data at {tableName} [{position}] (must be an integer ≥ 0).");
            return false;
        }

        private void WriteDataToGrids()
        {
            for (int i = 0; i < banker.ProcessCount; i++)
                for (int j = 0; j < banker.ResourceCount; j++)
                    dgvAllocation.Rows[i].Cells[j].Value = banker.Allocation[i, j];

            for (int j = 0; j < banker.ResourceCount; j++)
                dgvAvailable.Rows[0].Cells[j].Value = banker.Available[j];
        }

        private void ShowNeed()
        {
            int[,] need = banker.Need;
            for (int i = 0; i < banker.ProcessCount; i++)
                for (int j = 0; j < banker.ResourceCount; j++)
                    dgvNeed.Rows[i].Cells[j].Value = need[i, j];
        }

        private static List<int[]> ReadMatrix(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            return File.ReadAllLines(filePath)
                       .Where(line => !string.IsNullOrWhiteSpace(line))
                       .Select(line => line.Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(int.Parse)
                                           .ToArray())
                       .ToList();
        }

        private string RowToString(int[,] matrix, int row)
        {
            return string.Join(" ", Enumerable.Range(0, banker.ResourceCount).Select(j => matrix[row, j]));
        }

        // ---------------- UI helpers ----------------

        private static string FormatSequence(IEnumerable<int> sequence)
        {
            return string.Join(" → ", sequence.Select(p => $"P{p}"));
        }

        private static void ShowMissingData(string message)
        {
            MessageBox.Show("⚠️ " + message, "Invalid Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void SetStatus(string text, Color color)
        {
            labelStatus.Text = text;
            labelStatus.ForeColor = color;
        }

        private void ClearResult()
        {
            labelStatus.Text = "";
            labelSequence.Text = "";
            labelSuggest.Text = "";
        }
    }
}
