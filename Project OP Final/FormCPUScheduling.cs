using Project_OP_Final.Class;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Project_OP_Final
{
    public partial class FormCPUScheduling : Form
    {
        private const string AlgoPriority = "Priority Scheduling";
        private const string AlgoFCFS = "(FCFS) First Come First Serve";
        private const string AlgoSJF = "(SJF) Shortest Job First";
        private const string AlgoSRTF = "(SRTF) Shortest Remaining Time First";
        private const string AlgoRR = "(RR) Round Robin";

        private const string ColProcessID = "ProcessID";
        private const string ColArrivalTime = "ArrivalTime";
        private const string ColBurstTime = "BurstTime";
        private const string ColPriority = "Priority";

        // Results of the last run, used when saving the report
        private List<Process> lastProcesses;
        private List<ProcessSnapshot> lastTimeline;

        public FormCPUScheduling()
        {
            InitializeComponent();
        }

        private void FormCPUScheduling_Load(object sender, EventArgs e)
        {
            comboAlgorithm.Items.AddRange(new object[] { AlgoPriority, AlgoFCFS, AlgoSJF, AlgoSRTF, AlgoRR });

            SetUpGridColumns();
            gridData.RowsRemoved += (s, args) => RenumberProcesses();
            radWithPriority.CheckedChanged += (s, args) => UpdatePriorityColumn();

            DefaultUI();
        }

        private string SelectedAlgorithm => comboAlgorithm.SelectedItem?.ToString() ?? "";

        private bool UsesPriority => radWithPriority.Checked || SelectedAlgorithm == AlgoPriority;

        // ---------------- Event handlers ----------------

        private void comboAlgorithm_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Quantum time is only needed by Round Robin
            bool isRoundRobin = SelectedAlgorithm == AlgoRR;
            lblQuantumTime.Visible = isRoundRobin;
            txtQuantumTime.Visible = isRoundRobin;
            lblSecond.Visible = isRoundRobin;

            // Priority Scheduling always uses priority, so the with/without option is hidden
            bool isPriorityScheduling = SelectedAlgorithm == AlgoPriority;
            lblPriority.Visible = !isPriorityScheduling;
            radWithoutPriority.Visible = !isPriorityScheduling;
            radWithPriority.Visible = !isPriorityScheduling;
            radWithPriority.Checked = isPriorityScheduling;
            radWithoutPriority.Checked = !isPriorityScheduling;

            UpdatePriorityColumn();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            gridData.Rows.Clear();
            ClearResults();
            DefaultUI();
        }

        private void gridData_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            RenumberProcesses();
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            if (!TryReadProcesses(out List<Process> processes))
                return;

            int quantumTime = 0;
            if (SelectedAlgorithm == AlgoRR &&
                (!int.TryParse(txtQuantumTime.Text.Trim(), out quantumTime) || quantumTime <= 0))
            {
                ShowError("Please enter a positive Quantum Time.", "⚠️ Invalid Quantum Time");
                return;
            }

            List<ProcessSnapshot> timeline = RunAlgorithm(processes, quantumTime);
            if (timeline == null)
            {
                InformError("⚠️ Build Failed - Please select a valid algorithm.");
                return;
            }

            ShowResults(processes, timeline);
        }

        private void btnLoadFromFile_Click(object sender, EventArgs e)
        {
            string filePath = DataFile.PickFileToOpen("SRTF_CPU.txt");
            if (filePath == null) return;

            try
            {
                var rows = new List<int[]>();
                foreach (string line in File.ReadAllLines(filePath))
                {
                    string[] parts = line.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0) continue;
                    if (parts.Length < 2)
                        throw new FormatException($"Line \"{line}\" needs at least Arrival Time and Burst Time.");

                    int arrival = int.Parse(parts[0]);
                    int burst = int.Parse(parts[1]);
                    int priority = parts.Length >= 3 ? int.Parse(parts[2]) : 0;
                    rows.Add(new[] { arrival, burst, priority });
                }

                gridData.Rows.Clear();
                for (int i = 0; i < rows.Count; i++)
                    gridData.Rows.Add($"P{i + 1}", rows[i][0], rows[i][1], rows[i][2]);

                ClearResults();
                txtProcessNumber.Text = rows.Count.ToString();
                Inform($"Loaded {rows.Count} process(es) from {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                ShowError($"Error loading file: {ex.Message}", "⚠️ Failed to load file");
            }
        }

        private void btnSaveData_Click(object sender, EventArgs e)
        {
            if (lastProcesses == null)
            {
                MessageBox.Show("Please run an algorithm before saving the result.",
                                "Empty Content", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                InformError("⚠️ No content to save");
                return;
            }

            string filePath = DataFile.PickFileToSave("CPU_FileSave.txt");
            if (filePath == null) return;

            try
            {
                File.WriteAllText(filePath, BuildReport(), Encoding.UTF8);
                Inform($"File saved successfully: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message, "⚠️ Failed to save file");
            }
        }

        // ---------------- Algorithm ----------------

        private List<ProcessSnapshot> RunAlgorithm(List<Process> processes, int quantumTime)
        {
            if (UsesPriority)
            {
                switch (SelectedAlgorithm)
                {
                    case AlgoPriority:
                    case AlgoFCFS: // FCFS with priority is non-preemptive Priority Scheduling
                        return CPUScheduling.Priority(processes);
                    case AlgoSJF:
                        return CPUScheduling.PrioritySJF(processes);
                    case AlgoSRTF:
                        return CPUScheduling.PrioritySRTF(processes);
                    case AlgoRR:
                        return CPUScheduling.PriorityRoundRobin(processes, quantumTime);
                }
            }
            else
            {
                switch (SelectedAlgorithm)
                {
                    case AlgoFCFS:
                        return CPUScheduling.FCFS(processes);
                    case AlgoSJF:
                        return CPUScheduling.SJF(processes);
                    case AlgoSRTF:
                        return CPUScheduling.SRTF(processes);
                    case AlgoRR:
                        return CPUScheduling.RoundRobin(processes, quantumTime);
                }
            }
            return null;
        }

        /// <summary>Reads and validates the processes entered in gridData.</summary>
        private bool TryReadProcesses(out List<Process> processes)
        {
            processes = new List<Process>();

            foreach (DataGridViewRow row in gridData.Rows)
            {
                if (row.IsNewRow) continue;

                string id = $"P{row.Index + 1}";

                if (!int.TryParse(row.Cells[ColArrivalTime].Value?.ToString(), out int arrivalTime) || arrivalTime < 0)
                {
                    ShowError($"Invalid Arrival Time for {id} (must be an integer ≥ 0).", "⚠️ Invalid Arrival Time");
                    return false;
                }

                if (!int.TryParse(row.Cells[ColBurstTime].Value?.ToString(), out int burstTime) || burstTime <= 0)
                {
                    ShowError($"Invalid Burst Time for {id} (must be an integer > 0).", "⚠️ Invalid Burst Time");
                    return false;
                }

                int priority = 0;
                if (UsesPriority && !int.TryParse(row.Cells[ColPriority].Value?.ToString(), out priority))
                {
                    ShowError($"Invalid Priority for {id} (must be an integer).", "⚠️ Invalid Priority");
                    return false;
                }

                processes.Add(new Process(id, arrivalTime, burstTime, priority));
            }

            if (processes.Count == 0)
            {
                ShowError("⚠️ No data in the grid.", "⚠️ Build Failed - Please ensure all data is entered correctly.");
                return false;
            }

            return true;
        }

        // ---------------- Display ----------------

        private void ShowResults(List<Process> processes, List<ProcessSnapshot> timeline)
        {
            gridResult.Rows.Clear();
            foreach (var p in processes)
                gridResult.Rows.Add(p.ID, p.CompletionTime, p.TurnaroundTime, p.WaitingTime);

            gridAverage.Rows.Clear();
            gridAverage.Rows.Add(processes.Average(p => p.TurnaroundTime).ToString("0.##"),
                                 processes.Average(p => p.WaitingTime).ToString("0.##"));

            txtProcessNumber.Text = processes.Count.ToString();

            lblChartSequence.ForeColor = Color.Black;
            lblChartSequence.Text = FormatTimeline(timeline);

            lastProcesses = processes;
            lastTimeline = timeline;
        }

        private static string FormatTimeline(List<ProcessSnapshot> timeline)
        {
            // Example: 0 [P1] 5 [Idle] 6 [P2] 10
            var sb = new StringBuilder();
            sb.Append(timeline[0].StartTime);
            foreach (var block in timeline)
                sb.Append($" [{block.Id}] {block.EndTime}");
            return sb.ToString();
        }

        private string BuildReport()
        {
            var sb = new StringBuilder();
            string mode = UsesPriority ? "With Priority" : "Without Priority";
            sb.AppendLine($"{SelectedAlgorithm} ({mode}) Implementation File");
            sb.AppendLine();

            sb.AppendLine("=== Dữ liệu ===");
            foreach (var p in lastProcesses)
            {
                sb.AppendLine($"Process ID: {p.ID}, Arrival Time: {p.ArrivalTime}, " +
                              $"Burst Time: {p.BurstTime}, Priority: {p.Priority}");
            }

            sb.AppendLine();
            sb.AppendLine("=== Kết Quả ===");
            foreach (var p in lastProcesses)
            {
                sb.AppendLine($"Process ID: {p.ID}, Completion Time: {p.CompletionTime}, " +
                              $"Turn-around Time: {p.TurnaroundTime}, Waiting Time: {p.WaitingTime}");
            }

            sb.AppendLine();
            sb.AppendLine("=== Trung Bình ===");
            sb.AppendLine($"Average Turn-around Time: {lastProcesses.Average(p => p.TurnaroundTime):0.##}");
            sb.AppendLine($"Average Waiting Time: {lastProcesses.Average(p => p.WaitingTime):0.##}");

            sb.AppendLine();
            sb.AppendLine("=== Gantt Chart ===");
            sb.AppendLine(FormatTimeline(lastTimeline));

            return sb.ToString();
        }

        // ---------------- UI helpers ----------------

        private void SetUpGridColumns()
        {
            gridData.Columns.Add(ColProcessID, "Process ID");
            gridData.Columns.Add(ColArrivalTime, "Arrival Time");
            gridData.Columns.Add(ColBurstTime, "Burst Time");
            gridData.Columns.Add(ColPriority, "Priority");
            gridData.Columns[ColProcessID].ReadOnly = true; // Auto-generated
            gridData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridData.AllowUserToAddRows = true;

            gridResult.Columns.Add("ProcessID", "Process ID");
            gridResult.Columns.Add("CompletionTime", "Completion Time");
            gridResult.Columns.Add("TurnaroundTime", "Turn-around Time");
            gridResult.Columns.Add("WaitingTime", "Waiting Time");
            gridResult.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            gridAverage.Columns.Add("AvgTAT", "Avg TAT");
            gridAverage.Columns.Add("AvgWT", "Avg WT");
            gridAverage.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void DefaultUI()
        {
            comboAlgorithm.SelectedItem = AlgoFCFS;
            radWithoutPriority.Checked = true;
            txtQuantumTime.Text = "";
            txtProcessNumber.Text = "0";

            lblChartSequence.MaximumSize = new Size(panelGanttChart.Width - 40, 0);
            Inform("Step 1: Select Algorithm of choice\r\nStep 2: Load data\r\nStep 3: Press Run");
        }

        private void ClearResults()
        {
            gridResult.Rows.Clear();
            gridAverage.Rows.Clear();
            lastProcesses = null;
            lastTimeline = null;
        }

        private void UpdatePriorityColumn()
        {
            var column = gridData.Columns[ColPriority];
            if (column != null)
                column.Visible = UsesPriority;
        }

        /// <summary>Process IDs follow the row order: P1, P2, ...</summary>
        private void RenumberProcesses()
        {
            foreach (DataGridViewRow row in gridData.Rows)
            {
                if (row.IsNewRow) continue;

                string id = $"P{row.Index + 1}";
                if (!Equals(row.Cells[ColProcessID].Value, id))
                    row.Cells[ColProcessID].Value = id;
            }
        }

        private void ShowError(string message, string status)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            InformError(status);
        }

        private void InformError(string text)
        {
            lblChartSequence.ForeColor = Color.Red;
            lblChartSequence.Text = text;
        }

        private void Inform(string text)
        {
            lblChartSequence.ForeColor = Color.Green;
            lblChartSequence.Text = text;
        }
    }
}
