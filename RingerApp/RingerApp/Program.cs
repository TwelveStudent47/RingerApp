using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;

namespace RingerApp
{
    public partial class MainForm : Form
    {
        private List<ScheduledSound> scheduledSounds;
        private System.Windows.Forms.Timer checkTimer;
        private System.Windows.Forms.Timer playbackTimer;
        private ListBox scheduledListBox;
        private DateTimePicker datePicker;
        private DateTimePicker timePicker;
        private TextBox filePathTextBox;
        private Button browseButton;
        private Button addButton;
        private Button removeButton;
        private Button stopButton;
        private Label statusLabel;
        private TrackBar durationTrackBar;
        private Label durationLabel;

        private IWavePlayer wavePlayer;
        private AudioFileReader audioFileReader;
        private DateTime playbackStartTime;
        private ScheduledSound currentPlayingSound;

        public MainForm()
        {
            InitializeComponent();
            scheduledSounds = new List<ScheduledSound>();

            checkTimer = new System.Windows.Forms.Timer();
            checkTimer.Interval = 1000;
            checkTimer.Tick += CheckTimer_Tick;
            checkTimer.Start();

            playbackTimer = new System.Windows.Forms.Timer();
            playbackTimer.Interval = 100;
            playbackTimer.Tick += PlaybackTimer_Tick;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Széchenyi Csengetési Rendszer";
            this.Size = new Size(600, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Icon = SystemIcons.Application;

            Label dateLabel = new Label();
            dateLabel.Text = "Dátum:";
            dateLabel.Location = new Point(20, 20);
            dateLabel.Size = new Size(50, 23);
            this.Controls.Add(dateLabel);

            datePicker = new DateTimePicker();
            datePicker.Location = new Point(80, 20);
            datePicker.Size = new Size(200, 23);
            datePicker.Format = DateTimePickerFormat.Short;
            datePicker.MinDate = DateTime.Today;
            this.Controls.Add(datePicker);

            Label timeLabel = new Label();
            timeLabel.Text = "Idő:";
            timeLabel.Location = new Point(300, 20);
            timeLabel.Size = new Size(35, 23);
            this.Controls.Add(timeLabel);

            timePicker = new DateTimePicker();
            timePicker.Location = new Point(340, 20);
            timePicker.Size = new Size(100, 23);
            timePicker.Format = DateTimePickerFormat.Time;
            timePicker.ShowUpDown = true;
            this.Controls.Add(timePicker);

            Label fileLabel = new Label();
            fileLabel.Text = "Hangfájl:";
            fileLabel.Location = new Point(20, 60);
            fileLabel.Size = new Size(60, 23);
            this.Controls.Add(fileLabel);

            filePathTextBox = new TextBox();
            filePathTextBox.Location = new Point(85, 60);
            filePathTextBox.Size = new Size(280, 23);
            filePathTextBox.ReadOnly = true;
            this.Controls.Add(filePathTextBox);

            browseButton = new Button();
            browseButton.Text = "Tallózás...";
            browseButton.Location = new Point(375, 58);
            browseButton.Size = new Size(80, 27);
            browseButton.Click += BrowseButton_Click;
            this.Controls.Add(browseButton);

            durationLabel = new Label();
            durationLabel.Text = "Lejátszási időtartam: 5 másodperc";
            durationLabel.Location = new Point(20, 100);
            durationLabel.Size = new Size(300, 23);
            durationLabel.Font = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Bold);
            this.Controls.Add(durationLabel);

            durationTrackBar = new TrackBar();
            durationTrackBar.Location = new Point(20, 125);
            durationTrackBar.Size = new Size(525, 45);
            durationTrackBar.Minimum = 1;
            durationTrackBar.Maximum = 30;
            durationTrackBar.Value = 5;
            durationTrackBar.TickFrequency = 5;
            durationTrackBar.LargeChange = 5;
            durationTrackBar.SmallChange = 1;
            durationTrackBar.ValueChanged += DurationTrackBar_ValueChanged;
            this.Controls.Add(durationTrackBar);

            Label minLabel = new Label();
            minLabel.Text = "1s";
            minLabel.Location = new Point(20, 170);
            minLabel.Size = new Size(30, 15);
            minLabel.Font = new Font("Microsoft Sans Serif", 7f);
            this.Controls.Add(minLabel);

            Label midLabel = new Label();
            midLabel.Text = "15s";
            midLabel.Location = new Point(250, 170);
            midLabel.Size = new Size(30, 15);
            midLabel.Font = new Font("Microsoft Sans Serif", 7f);
            this.Controls.Add(midLabel);

            Label maxLabel = new Label();
            maxLabel.Text = "30s";
            maxLabel.Location = new Point(500, 170);
            maxLabel.Size = new Size(30, 15);
            maxLabel.Font = new Font("Microsoft Sans Serif", 7f);
            this.Controls.Add(maxLabel);

            addButton = new Button();
            addButton.Text = "Hozzáadás";
            addButton.Location = new Point(465, 58);
            addButton.Size = new Size(80, 27);
            addButton.BackColor = Color.LightGreen;
            addButton.Click += AddButton_Click;
            this.Controls.Add(addButton);

            stopButton = new Button();
            stopButton.Text = "Leállítás";
            stopButton.Location = new Point(465, 100);
            stopButton.Size = new Size(80, 27);
            stopButton.BackColor = Color.Orange;
            stopButton.Click += StopButton_Click;
            this.Controls.Add(stopButton);

            Label listLabel = new Label();
            listLabel.Text = "Ütemezett hangok:";
            listLabel.Location = new Point(20, 195);
            listLabel.Size = new Size(120, 23);
            this.Controls.Add(listLabel);

            scheduledListBox = new ListBox();
            scheduledListBox.Location = new Point(20, 220);
            scheduledListBox.Size = new Size(525, 220);
            scheduledListBox.Font = new Font("Consolas", 9);
            this.Controls.Add(scheduledListBox);

            removeButton = new Button();
            removeButton.Text = "Kijelölt eltávolítása";
            removeButton.Location = new Point(20, 450);
            removeButton.Size = new Size(150, 30);
            removeButton.BackColor = Color.LightCoral;
            removeButton.Click += RemoveButton_Click;
            this.Controls.Add(removeButton);

            statusLabel = new Label();
            statusLabel.Text = "Készen áll...";
            statusLabel.Location = new Point(20, 490);
            statusLabel.Size = new Size(525, 23);
            statusLabel.ForeColor = Color.Blue;
            this.Controls.Add(statusLabel);

            this.ResumeLayout();
        }

        private void DurationTrackBar_ValueChanged(object sender, EventArgs e)
        {
            int seconds = durationTrackBar.Value;
            durationLabel.Text = $"Lejátszási időtartam: {seconds} másodperc";
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Minden hangfájl|*.mp3;*.wav;*.wma;*.m4a;*.flac|MP3 fájlok (*.mp3)|*.mp3|WAV fájlok (*.wav)|*.wav|Minden fájl (*.*)|*.*";
                openFileDialog.Title = "Hangfájl kiválasztása";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePathTextBox.Text = openFileDialog.FileName;
                }
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(filePathTextBox.Text))
            {
                MessageBox.Show("Kérjük válasszon ki egy hangfájlt!", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(filePathTextBox.Text))
            {
                MessageBox.Show("A kiválasztott fájl nem található!", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!IsAudioFileSupported(filePathTextBox.Text))
            {
                MessageBox.Show("A kiválasztott fájlformátum nem támogatott!\nTámogatott formátumok: MP3, WAV, WMA, M4A, FLAC", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DateTime scheduledTime = datePicker.Value.Date.Add(timePicker.Value.TimeOfDay);

            if (scheduledTime <= DateTime.Now)
            {
                MessageBox.Show("Az időpontnak a jövőben kell lennie!", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ScheduledSound newSound = new ScheduledSound
            {
                ScheduledTime = scheduledTime,
                FilePath = filePathTextBox.Text,
                FileName = Path.GetFileName(filePathTextBox.Text),
                DurationSeconds = durationTrackBar.Value
            };

            scheduledSounds.Add(newSound);
            scheduledSounds = scheduledSounds.OrderBy(s => s.ScheduledTime).ToList();

            UpdateScheduledList();
            filePathTextBox.Text = "";

            statusLabel.Text = $"Hozzáadva: {newSound.ScheduledTime:yyyy.MM.dd HH:mm} - {newSound.FileName} ({newSound.GetDurationText()})";
        }

        private bool IsAudioFileSupported(string filePath)
        {
            try
            {
                using (var reader = new AudioFileReader(filePath))
                {
                    return reader.TotalTime.TotalMilliseconds > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if (scheduledListBox.SelectedIndex >= 0)
            {
                string removedItem = scheduledSounds[scheduledListBox.SelectedIndex].ToString();
                scheduledSounds.RemoveAt(scheduledListBox.SelectedIndex);
                UpdateScheduledList();
                statusLabel.Text = $"Eltávolítva: {removedItem}";
            }
            else
            {
                MessageBox.Show("Kérjük válasszon ki egy elemet az eltávolításhoz!", "Figyelem",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            StopCurrentPlayback();
            statusLabel.Text = "Lejátszás manuálisan leállítva.";
        }

        private void StopCurrentPlayback()
        {
            playbackTimer.Stop();

            if (wavePlayer != null)
            {
                try
                {
                    wavePlayer.Stop();
                    wavePlayer.Dispose();
                }
                catch { }
                wavePlayer = null;
            }

            if (audioFileReader != null)
            {
                try
                {
                    audioFileReader.Dispose();
                }
                catch { }
                audioFileReader = null;
            }

            currentPlayingSound = null;
        }

        private void UpdateScheduledList()
        {
            scheduledListBox.Items.Clear();
            foreach (var sound in scheduledSounds)
            {
                scheduledListBox.Items.Add(sound.ToString());
            }
        }

        private void CheckTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            var soundsToPlay = scheduledSounds.Where(s =>
                Math.Abs((s.ScheduledTime - now).TotalSeconds) < 1).ToList();

            foreach (var sound in soundsToPlay)
            {
                PlaySound(sound);
                scheduledSounds.Remove(sound);
            }

            if (soundsToPlay.Any())
            {
                UpdateScheduledList();
            }

            if (currentPlayingSound == null && scheduledSounds.Any())
            {
                var nextSound = scheduledSounds.First();
                TimeSpan timeUntilNext = nextSound.ScheduledTime - now;
                statusLabel.Text = $"Következő: {nextSound.FileName} - {timeUntilNext.Days}n {timeUntilNext.Hours:D2}:{timeUntilNext.Minutes:D2}:{timeUntilNext.Seconds:D2}";
            }
            else if (currentPlayingSound == null)
            {
                statusLabel.Text = "Nincs ütemezett hang.";
            }
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            if (currentPlayingSound != null && wavePlayer != null)
            {
                TimeSpan elapsed = DateTime.Now - playbackStartTime;
                TimeSpan totalDuration = TimeSpan.FromSeconds(currentPlayingSound.DurationSeconds);

                if (elapsed >= totalDuration || wavePlayer.PlaybackState == PlaybackState.Stopped)
                {
                    string finishedFileName = currentPlayingSound.FileName;
                    StopCurrentPlayback();
                    statusLabel.Text = $"Lejátszás befejezve: {finishedFileName}";
                }
                else
                {
                    TimeSpan remaining = totalDuration - elapsed;
                    statusLabel.Text = $"Lejátszás: {currentPlayingSound.FileName} - Hátralévő: {remaining.Minutes:D2}:{remaining.Seconds:D2}";
                }
            }
        }

        private async void PlaySound(ScheduledSound sound)
        {
            try
            {
                StopCurrentPlayback();

                currentPlayingSound = sound;
                playbackStartTime = DateTime.Now;

                audioFileReader = new AudioFileReader(sound.FilePath);
                wavePlayer = new WaveOutEvent();
                wavePlayer.Init(audioFileReader);
                wavePlayer.Play();

                playbackTimer.Start();

                statusLabel.Text = $"Lejátszás: {sound.FileName} - {sound.GetDurationText()}";

                await Task.Delay(100);

                ShowSilentNotification($"🎵 Hang lejátszva: {sound.FileName}\n⏰ Időpont: {sound.ScheduledTime:yyyy.MM.dd HH:mm:ss}\n⏱️ Időtartam: {sound.GetDurationText()}\n✅ Automatikus leállítás aktív!");
            }
            catch (Exception ex)
            {
                ShowSilentNotification($"❌ Hiba: {ex.Message}\n\nLehetséges okok:\n• Nem támogatott fájl\n• Sérült hangfájl\n• Fájl használatban", true);
                StopCurrentPlayback();
            }
        }

        private void ShowSilentNotification(string message, bool isError = false)
        {
            Form notificationForm = new Form();
            notificationForm.Text = isError ? "Hiba" : "Ütemezett hang";
            notificationForm.Size = new Size(400, 250);
            notificationForm.StartPosition = FormStartPosition.CenterParent;
            notificationForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            notificationForm.MaximizeBox = false;
            notificationForm.MinimizeBox = false;
            notificationForm.TopMost = true;
            notificationForm.ShowInTaskbar = false;

            Label messageLabel = new Label();
            messageLabel.Text = message;
            messageLabel.Location = new Point(20, 20);
            messageLabel.Size = new Size(350, 150);
            messageLabel.Font = new Font("Microsoft Sans Serif", 9f);
            notificationForm.Controls.Add(messageLabel);

            Button okButton = new Button();
            okButton.Text = "OK";
            okButton.Size = new Size(80, 30);
            okButton.Location = new Point(160, 180);
            okButton.BackColor = isError ? Color.LightCoral : Color.LightGreen;
            okButton.Click += (s, e) => notificationForm.Close();
            notificationForm.Controls.Add(okButton);

            notificationForm.AcceptButton = okButton;
            notificationForm.CancelButton = okButton;

            notificationForm.ShowDialog(this);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            StopCurrentPlayback();
            checkTimer?.Stop();
            base.OnFormClosed(e);
        }
    }

    public class ScheduledSound
    {
        public DateTime ScheduledTime { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public int DurationSeconds { get; set; }

        public string GetDurationText()
        {
            return $"{DurationSeconds}s";
        }

        public override string ToString()
        {
            return $"{ScheduledTime:yyyy.MM.dd HH:mm} - {FileName} ({GetDurationText()})";
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}