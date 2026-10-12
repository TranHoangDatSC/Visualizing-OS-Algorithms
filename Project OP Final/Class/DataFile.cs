using System.IO;
using System.Windows.Forms;

namespace Project_OP_Final.Class
{
    /// <summary>Shared helpers for picking data files inside the application's "Data" folder.</summary>
    public static class DataFile
    {
        private const string TextFilter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

        public static string DataDirectory
        {
            get
            {
                string dir = Path.Combine(Application.StartupPath, "Data");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        /// <summary>Shows an Open dialog starting in the Data folder. Returns null when the user cancels.</summary>
        public static string PickFileToOpen(string defaultFileName)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = TextFilter;
                dialog.InitialDirectory = DataDirectory;
                dialog.FileName = defaultFileName;
                return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
            }
        }

        /// <summary>Shows a Save dialog starting in the Data folder. Returns null when the user cancels.</summary>
        public static string PickFileToSave(string defaultFileName)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = TextFilter;
                dialog.InitialDirectory = DataDirectory;
                dialog.FileName = defaultFileName;
                return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
            }
        }
    }
}
