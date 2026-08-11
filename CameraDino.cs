using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Text.RegularExpressions;
using System.Net;
using System.Linq;

namespace CameraDino
{
    static class Program
    {
        static Mutex mutex = new Mutex(true, "CameraDinoMutex_V2");
        static string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CameraDino");
        static string configFile = Path.Combine(appDataDir, "config.json");
        static string flagFile = Path.Combine(appDataDir, "ShowApp.flag");
        
        static string cameraIP = "onvif://admin:senha@192.168.1.100:8899";
        static string recordDir = "";
        static bool enableRecord = false;
        static bool startMinimized = false;

        static Process go2rtcProcess = null;
        static Process ffmpegProcess = null;
        
        static NotifyIcon trayIcon;
        static SettingsForm mainForm;
        static System.Windows.Forms.Timer backgroundTimer;
        static int cleanupCounter = 0;

        [STAThread]
        static void Main()
        {
            try {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                if (!Directory.Exists(appDataDir))
                    Directory.CreateDirectory(appDataDir);

                try {
                    if (!mutex.WaitOne(TimeSpan.Zero, true))
                    {
                        // App is already running
                        File.WriteAllText(flagFile, "show");
                        return;
                    }
                } catch (AbandonedMutexException) {
                    // Mutex was abandoned by previous crashed process. We own it now.
                }

            LoadConfig();
            StartGo2rtc();
            UpdateGo2rtcYaml();
            UpdateStartupShortcut();
            
            if (enableRecord) StartFfmpeg();

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Camera Dino";
            try {
                trayIcon.Icon = new Icon("dino.ico");
            } catch {
                trayIcon.Icon = SystemIcons.Application;
            }
            
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Settings", null, (s, e) => ShowForm());
            menu.Items.Add("Open Browser", null, (s, e) => Process.Start("http://127.0.0.1:1984/index.html"));
            menu.Items.Add("Exit All", null, (s, e) => ExitApp());
            
            trayIcon.ContextMenuStrip = menu;
            trayIcon.Visible = true;
            
            trayIcon.DoubleClick += (s, e) => ShowForm();

            mainForm = new SettingsForm();

            backgroundTimer = new System.Windows.Forms.Timer();
            backgroundTimer.Interval = 2000;
            backgroundTimer.Tick += BackgroundTimer_Tick;
            backgroundTimer.Start();

            if (string.IsNullOrEmpty(recordDir) || cameraIP.Contains("192.168.1.100"))
            {
                ShowForm();
            }
            else if (!startMinimized)
            {
                Process.Start("http://127.0.0.1:1984/index.html");
            }

            Application.Run();
            } catch (Exception ex) {
                MessageBox.Show(ex.ToString(), "Fatal Error on Startup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void ShowForm()
        {
            if (mainForm.IsDisposed) mainForm = new SettingsForm();
            mainForm.Show();
            if (mainForm.WindowState == FormWindowState.Minimized)
                mainForm.WindowState = FormWindowState.Normal;
            mainForm.Activate();
        }

        static void ExitApp()
        {
            StopFfmpeg();
            StopGo2rtc();
            trayIcon.Visible = false;
            Application.Exit();
            Environment.Exit(0);
        }

        static void LoadConfig()
        {
            if (File.Exists(configFile))
            {
                try {
                    string json = File.ReadAllText(configFile);
                    var matchIP = Regex.Match(json, "\"CameraIP\"\\s*:\\s*\"(.*?)\"");
                    if (matchIP.Success) cameraIP = matchIP.Groups[1].Value;
                    
                    var matchDir = Regex.Match(json, "\"RecordDir\"\\s*:\\s*\"(.*?)\"");
                    if (matchDir.Success) recordDir = matchDir.Groups[1].Value.Replace("\\\\", "\\");
                    
                    var matchRecord = Regex.Match(json, "\"EnableRecord\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
                    if (matchRecord.Success) enableRecord = bool.Parse(matchRecord.Groups[1].Value);
                    
                    var matchMin = Regex.Match(json, "\"StartMinimized\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
                    if (matchMin.Success) startMinimized = bool.Parse(matchMin.Groups[1].Value);
                } catch { }
            }
        }

        public static void SaveConfig(string ip, string dir, bool rec, bool min)
        {
            cameraIP = ip;
            recordDir = dir;
            enableRecord = rec;
            startMinimized = min;
            
            string json = string.Format("{{\n  \"CameraIP\": \"{0}\",\n  \"RecordDir\": \"{1}\",\n  \"EnableRecord\": {2},\n  \"StartMinimized\": {3}\n}}", cameraIP, recordDir.Replace("\\", "\\\\"), enableRecord.ToString().ToLower(), startMinimized.ToString().ToLower());
            File.WriteAllText(configFile, json);
            
            UpdateStartupShortcut();
            UpdateGo2rtcYaml();
            
            if (enableRecord) {
                StartFfmpeg();
                MessageBox.Show("Settings saved and recording started!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } else {
                StopFfmpeg();
                MessageBox.Show("Settings saved and recording stopped.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        static void UpdateStartupShortcut()
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = Path.Combine(startupFolder, "CameraDino.lnk");
            
            if (startMinimized)
            {
                try {
                    Type t = Type.GetTypeFromProgID("WScript.Shell");
                    dynamic shell = Activator.CreateInstance(t);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = Application.ExecutablePath;
                    shortcut.WorkingDirectory = Application.StartupPath;
                    shortcut.Save();
                } catch { }
            }
            else
            {
                if (File.Exists(shortcutPath)) File.Delete(shortcutPath);
            }
        }

        static void StartGo2rtc()
        {
            var existing = Process.GetProcessesByName("go2rtc");
            if (existing.Length == 0)
            {
                if (File.Exists("go2rtc.exe")) {
                    ProcessStartInfo startInfo = new ProcessStartInfo("go2rtc.exe");
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.CreateNoWindow = true;
                    go2rtcProcess = Process.Start(startInfo);
                }
            }
            else
            {
                go2rtcProcess = existing[0];
            }
        }

        static void StopGo2rtc()
        {
            try {
                if (go2rtcProcess != null && !go2rtcProcess.HasExited)
                    go2rtcProcess.Kill();
                foreach (var p in Process.GetProcessesByName("go2rtc"))
                    p.Kill();
            } catch { }
        }

        static void StartFfmpeg()
        {
            StopFfmpeg();
            if (!enableRecord || string.IsNullOrEmpty(cameraIP) || string.IsNullOrEmpty(recordDir)) return;
            if (!File.Exists("ffmpeg.exe"))
            {
                enableRecord = false;
                MessageBox.Show("FFmpeg not found. Recording disabled.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!Directory.Exists(recordDir)) Directory.CreateDirectory(recordDir);

            string fullPath = Path.Combine(recordDir, "gravacao_%d-%m-%Y_%H-%M-%S.mkv");
            string sourceUrl = "http://127.0.0.1:1984/api/stream.mp4?src=live_camera";
            string arguments = string.Format("-i \"{0}\" -c copy -f segment -segment_time 3600 -segment_format mkv -reset_timestamps 1 -strftime 1 \"{1}\"", sourceUrl, fullPath);
            
            ProcessStartInfo startInfo = new ProcessStartInfo("ffmpeg.exe", arguments);
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            ffmpegProcess = Process.Start(startInfo);
        }

        public static void StopFfmpeg()
        {
            try {
                if (ffmpegProcess != null && !ffmpegProcess.HasExited)
                    ffmpegProcess.Kill();
                foreach (var p in Process.GetProcessesByName("ffmpeg"))
                    p.Kill();
            } catch { }
        }

        static void UpdateGo2rtcYaml()
        {
            if (File.Exists("go2rtc.yaml"))
            {
                string yaml = File.ReadAllText("go2rtc.yaml");
                string url = cameraIP;
                if (Regex.IsMatch(url, "^(rtsp|onvif)://") && !url.Contains("rtsp_transport"))
                {
                    if (url.Contains("?")) url += "&rtsp_transport=tcp";
                    else if (url.Contains("#")) url = url.Replace("#", "#rtsp_transport=tcp&");
                    else url += "#rtsp_transport=tcp";
                }
                // We skip writing to go2rtc.yaml to avoid UnauthorizedAccessException in Program Files.
                // The stream is updated dynamically in memory via the Web API below.

                try {
                    using (WebClient client = new WebClient())
                    {
                        try { client.UploadString("http://127.0.0.1:1984/api/streams?src=live_camera", "DELETE", ""); } catch { }
                        try { client.UploadString(string.Format("http://127.0.0.1:1984/api/streams?name=live_camera&src={0}", Uri.EscapeDataString(url)), "PUT", ""); } catch { }
                    }
                } catch { }
            }
        }

        static void BackgroundTimer_Tick(object sender, EventArgs e)
        {
            if (go2rtcProcess != null && go2rtcProcess.HasExited)
            {
                ExitApp();
            }

            if (enableRecord && ffmpegProcess != null && ffmpegProcess.HasExited)
            {
                StartFfmpeg();
            }

            if (File.Exists(flagFile))
            {
                try { File.Delete(flagFile); } catch { }
                ShowForm();
            }

            cleanupCounter++;
            if (cleanupCounter >= 30)
            {
                cleanupCounter = 0;
                if (enableRecord && Directory.Exists(recordDir))
                {
                    try {
                        string drive = Path.GetPathRoot(recordDir);
                        DriveInfo di = new DriveInfo(drive);
                        if (di.IsReady && di.AvailableFreeSpace < 5368709120) // 5GB
                        {
                            var oldestFile = new DirectoryInfo(recordDir).GetFiles("*.mkv").OrderBy(f => f.LastWriteTime).FirstOrDefault();
                            if (oldestFile != null) oldestFile.Delete();
                        }
                    } catch { }
                }
            }
        }

        class SettingsForm : Form
        {
            TextBox txtIP, txtDir;
            CheckBox chkRecord, chkMin;
            
            public SettingsForm()
            {
                Text = "Settings - Camera Dino";
                Size = new Size(420, 280);
                StartPosition = FormStartPosition.CenterScreen;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                try { Icon = new Icon("dino.ico"); } catch { }

                this.FormClosing += (s, e) => {
                    if (e.CloseReason == CloseReason.UserClosing) {
                        e.Cancel = true;
                        Hide();
                    }
                };

                Controls.Add(new Label { Text = "Camera Address (RTSP/ONVIF):", Location = new Point(15, 15), Size = new Size(380, 20) });
                txtIP = new TextBox { Text = cameraIP, Location = new Point(15, 35), Size = new Size(370, 20) };
                Controls.Add(txtIP);

                Controls.Add(new Label { Text = "Recording Folder:", Location = new Point(15, 70), Size = new Size(380, 20) });
                txtDir = new TextBox { Text = recordDir, Location = new Point(15, 90), Size = new Size(280, 20) };
                Controls.Add(txtDir);

                Button btnBrowse = new Button { Text = "Browse...", Location = new Point(305, 88), Size = new Size(80, 24) };
                btnBrowse.Click += (s, e) => {
                    using (FolderBrowserDialog fbd = new FolderBrowserDialog()) {
                        fbd.SelectedPath = txtDir.Text;
                        if (fbd.ShowDialog() == DialogResult.OK) txtDir.Text = fbd.SelectedPath;
                    }
                };
                Controls.Add(btnBrowse);

                chkRecord = new CheckBox { Text = "Enable Continuous Recording (1 hour per .mkv file)", Location = new Point(15, 120), Size = new Size(380, 20), Checked = enableRecord };
                Controls.Add(chkRecord);

                chkMin = new CheckBox { Text = "Start Minimized with Windows (Silent)", Location = new Point(15, 145), Size = new Size(380, 20), Checked = startMinimized };
                Controls.Add(chkMin);

                Button btnSave = new Button { Text = "Save", Location = new Point(15, 180), Size = new Size(100, 30), BackColor = Color.LightGreen };
                btnSave.Click += (s, e) => SaveConfig(txtIP.Text, txtDir.Text, chkRecord.Checked, chkMin.Checked);
                Controls.Add(btnSave);

                Button btnVideo = new Button { Text = "Open Video Page", Location = new Point(125, 180), Size = new Size(130, 30) };
                btnVideo.Click += (s, e) => Process.Start("http://127.0.0.1:1984/index.html");
                Controls.Add(btnVideo);

                Button btnKill = new Button { Text = "Stop Service", Location = new Point(265, 180), Size = new Size(130, 30), BackColor = Color.Salmon };
                btnKill.Click += (s, e) => ExitApp();
                Controls.Add(btnKill);
            }
        }
    }
}
