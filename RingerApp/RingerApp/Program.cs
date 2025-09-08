using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using NAudio.Wave;

namespace RingerApp
{
    public partial class MainForm : Form
    {
        private List<ScheduledSound> scheduledSounds;
        private List<Template> templates;
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
        private CheckBox repeatCheckBox;
        private CheckBox[] weekdayCheckBoxes;
        private ComboBox templateComboBox;
        private Button saveTemplateButton;
        private Button deleteTemplateButton;
        private TextBox templateNameTextBox;
        private Button clearAllButton;
        private Button normalScheduleButton;
        private Button shortScheduleButton;

        private IWavePlayer wavePlayer;
        private AudioFileReader audioFileReader;
        private DateTime playbackStartTime;
        private ScheduledSound currentPlayingSound;
        private Form currentNotificationForm;

        public MainForm()
        {
            try
            {
                InitializeComponent();
                scheduledSounds = new List<ScheduledSound>();
                templates = new List<Template>();
                LoadTemplates();

                checkTimer = new System.Windows.Forms.Timer();
                checkTimer.Interval = 1000;
                checkTimer.Tick += CheckTimer_Tick;
                checkTimer.Start();

                playbackTimer = new System.Windows.Forms.Timer();
                playbackTimer.Interval = 100;
                playbackTimer.Tick += PlaybackTimer_Tick;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az alkalmazás indításakor: {ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Kritikus hiba", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void InitializeComponent()
        {
            try
            {
                this.SuspendLayout();

                this.Text = "Széchenyi Csengetési Rendszer";
                this.Size = new Size(700, 780);
                this.StartPosition = FormStartPosition.CenterScreen;
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
                this.MaximizeBox = false;
                this.Icon = SystemIcons.Application;

                // Gyors sablon választó gombok
                GroupBox quickTemplateGroup = new GroupBox();
                quickTemplateGroup.Text = "Gyors sablon választás";
                quickTemplateGroup.Location = new Point(20, 10);
                quickTemplateGroup.Size = new Size(640, 70);
                this.Controls.Add(quickTemplateGroup);

                normalScheduleButton = new Button();
                normalScheduleButton.Text = "Rendes csengetési rend";
                normalScheduleButton.Location = new Point(50, 25);
                normalScheduleButton.Size = new Size(200, 35);
                normalScheduleButton.BackColor = Color.LightGreen;
                normalScheduleButton.Font = new Font("Microsoft Sans Serif", 10f, FontStyle.Bold);
                normalScheduleButton.Click += NormalScheduleButton_Click;
                quickTemplateGroup.Controls.Add(normalScheduleButton);

                shortScheduleButton = new Button();
                shortScheduleButton.Text = "Rövidített órák";
                shortScheduleButton.Location = new Point(390, 25);
                shortScheduleButton.Size = new Size(200, 35);
                shortScheduleButton.BackColor = Color.LightBlue;
                shortScheduleButton.Font = new Font("Microsoft Sans Serif", 10f, FontStyle.Bold);
                shortScheduleButton.Click += ShortScheduleButton_Click;
                quickTemplateGroup.Controls.Add(shortScheduleButton);

                GroupBox templateGroup = new GroupBox();
                templateGroup.Text = "Egyéb sablonok";
                templateGroup.Location = new Point(20, 90);
                templateGroup.Size = new Size(640, 100);
                this.Controls.Add(templateGroup);

                Label templateLabel = new Label();
                templateLabel.Text = "Sablon:";
                templateLabel.Location = new Point(10, 25);
                templateLabel.Size = new Size(50, 23);
                templateGroup.Controls.Add(templateLabel);

                templateComboBox = new ComboBox();
                templateComboBox.Location = new Point(70, 22);
                templateComboBox.Size = new Size(200, 23);
                templateComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                templateComboBox.SelectedIndexChanged += TemplateComboBox_SelectedIndexChanged;
                templateGroup.Controls.Add(templateComboBox);

                Button loadTemplateButton = new Button();
                loadTemplateButton.Text = "Sablon betöltése";
                loadTemplateButton.Location = new Point(280, 20);
                loadTemplateButton.Size = new Size(110, 27);
                loadTemplateButton.BackColor = Color.LightBlue;
                loadTemplateButton.Click += LoadTemplateButton_Click;
                templateGroup.Controls.Add(loadTemplateButton);

                deleteTemplateButton = new Button();
                deleteTemplateButton.Text = "Sablon törlése";
                deleteTemplateButton.Location = new Point(400, 20);
                deleteTemplateButton.Size = new Size(100, 27);
                deleteTemplateButton.BackColor = Color.LightCoral;
                deleteTemplateButton.Click += DeleteTemplateButton_Click;
                templateGroup.Controls.Add(deleteTemplateButton);

                clearAllButton = new Button();
                clearAllButton.Text = "Mind törlése";
                clearAllButton.Location = new Point(510, 20);
                clearAllButton.Size = new Size(90, 27);
                clearAllButton.BackColor = Color.Orange;
                clearAllButton.Click += ClearAllButton_Click;
                templateGroup.Controls.Add(clearAllButton);

                Label templateNameLabel = new Label();
                templateNameLabel.Text = "Új sablon név:";
                templateNameLabel.Location = new Point(10, 55);
                templateNameLabel.Size = new Size(90, 23);
                templateGroup.Controls.Add(templateNameLabel);

                templateNameTextBox = new TextBox();
                templateNameTextBox.Location = new Point(105, 52);
                templateNameTextBox.Size = new Size(165, 23);
                templateGroup.Controls.Add(templateNameTextBox);

                saveTemplateButton = new Button();
                saveTemplateButton.Text = "Sablon mentése";
                saveTemplateButton.Location = new Point(280, 50);
                saveTemplateButton.Size = new Size(110, 27);
                saveTemplateButton.BackColor = Color.LightGreen;
                saveTemplateButton.Click += SaveTemplateButton_Click;
                templateGroup.Controls.Add(saveTemplateButton);

                GroupBox dateTimeGroup = new GroupBox();
                dateTimeGroup.Text = "Időpont beállítása";
                dateTimeGroup.Location = new Point(20, 200);
                dateTimeGroup.Size = new Size(640, 85);
                this.Controls.Add(dateTimeGroup);

                Label dateLabel = new Label();
                dateLabel.Text = "Dátum:";
                dateLabel.Location = new Point(10, 25);
                dateLabel.Size = new Size(50, 23);
                dateTimeGroup.Controls.Add(dateLabel);

                datePicker = new DateTimePicker();
                datePicker.Location = new Point(70, 22);
                datePicker.Size = new Size(150, 23);
                datePicker.Format = DateTimePickerFormat.Short;
                datePicker.MinDate = DateTime.Today;
                dateTimeGroup.Controls.Add(datePicker);

                Label timeLabel = new Label();
                timeLabel.Text = "Idő:";
                timeLabel.Location = new Point(240, 25);
                timeLabel.Size = new Size(35, 23);
                dateTimeGroup.Controls.Add(timeLabel);

                timePicker = new DateTimePicker();
                timePicker.Location = new Point(280, 22);
                timePicker.Size = new Size(100, 23);
                timePicker.Format = DateTimePickerFormat.Time;
                timePicker.ShowUpDown = true;
                dateTimeGroup.Controls.Add(timePicker);

                repeatCheckBox = new CheckBox();
                repeatCheckBox.Text = "Ismétlés heti napokon:";
                repeatCheckBox.Location = new Point(10, 55);
                repeatCheckBox.Size = new Size(140, 20);
                repeatCheckBox.CheckedChanged += RepeatCheckBox_CheckedChanged;
                dateTimeGroup.Controls.Add(repeatCheckBox);

                string[] dayNames = { "H", "K", "Sz", "Cs", "P", "Szo", "V" };
                weekdayCheckBoxes = new CheckBox[7];
                for (int i = 0; i < 7; i++)
                {
                    weekdayCheckBoxes[i] = new CheckBox();
                    weekdayCheckBoxes[i].Text = dayNames[i];
                    weekdayCheckBoxes[i].Location = new Point(160 + i * 45, 55);
                    weekdayCheckBoxes[i].Size = new Size(40, 20);
                    weekdayCheckBoxes[i].Enabled = false;
                    dateTimeGroup.Controls.Add(weekdayCheckBoxes[i]);
                }

                Button selectWeekdaysButton = new Button();
                selectWeekdaysButton.Text = "Hétköznapok";
                selectWeekdaysButton.Location = new Point(480, 52);
                selectWeekdaysButton.Size = new Size(80, 25);
                selectWeekdaysButton.Click += SelectWeekdaysButton_Click;
                dateTimeGroup.Controls.Add(selectWeekdaysButton);

                GroupBox audioGroup = new GroupBox();
                audioGroup.Text = "Hangfájl beállítása";
                audioGroup.Location = new Point(20, 295);
                audioGroup.Size = new Size(640, 140);
                this.Controls.Add(audioGroup);

                Label fileLabel = new Label();
                fileLabel.Text = "Hangfájl:";
                fileLabel.Location = new Point(10, 25);
                fileLabel.Size = new Size(60, 23);
                audioGroup.Controls.Add(fileLabel);

                filePathTextBox = new TextBox();
                filePathTextBox.Location = new Point(75, 22);
                filePathTextBox.Size = new Size(350, 23);
                filePathTextBox.ReadOnly = true;
                audioGroup.Controls.Add(filePathTextBox);

                browseButton = new Button();
                browseButton.Text = "Tallózás...";
                browseButton.Location = new Point(435, 20);
                browseButton.Size = new Size(80, 27);
                browseButton.Click += BrowseButton_Click;
                audioGroup.Controls.Add(browseButton);

                addButton = new Button();
                addButton.Text = "Hozzáadás";
                addButton.Location = new Point(525, 20);
                addButton.Size = new Size(80, 27);
                addButton.BackColor = Color.LightGreen;
                addButton.Click += AddButton_Click;
                audioGroup.Controls.Add(addButton);

                durationLabel = new Label();
                durationLabel.Text = "Lejátszási időtartam: 5 másodperc";
                durationLabel.Location = new Point(10, 60);
                durationLabel.Size = new Size(300, 23);
                durationLabel.Font = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Bold);
                audioGroup.Controls.Add(durationLabel);

                durationTrackBar = new TrackBar();
                durationTrackBar.Location = new Point(10, 85);
                durationTrackBar.Size = new Size(525, 45);
                durationTrackBar.Minimum = 1;
                durationTrackBar.Maximum = 30;
                durationTrackBar.Value = 5;
                durationTrackBar.TickFrequency = 5;
                durationTrackBar.LargeChange = 5;
                durationTrackBar.SmallChange = 1;
                durationTrackBar.ValueChanged += DurationTrackBar_ValueChanged;
                audioGroup.Controls.Add(durationTrackBar);

                stopButton = new Button();
                stopButton.Text = "Leállítás";
                stopButton.Location = new Point(545, 85);
                stopButton.Size = new Size(80, 27);
                stopButton.BackColor = Color.Orange;
                stopButton.Click += StopButton_Click;
                audioGroup.Controls.Add(stopButton);

                GroupBox listGroup = new GroupBox();
                listGroup.Text = "Ütemezett hangok";
                listGroup.Location = new Point(20, 445);
                listGroup.Size = new Size(640, 280);
                this.Controls.Add(listGroup);

                scheduledListBox = new ListBox();
                scheduledListBox.Location = new Point(10, 20);
                scheduledListBox.Size = new Size(615, 200);
                scheduledListBox.Font = new Font("Consolas", 9);
                listGroup.Controls.Add(scheduledListBox);

                removeButton = new Button();
                removeButton.Text = "Kijelölt eltávolítása";
                removeButton.Location = new Point(10, 230);
                removeButton.Size = new Size(150, 30);
                removeButton.BackColor = Color.LightCoral;
                removeButton.Click += RemoveButton_Click;
                listGroup.Controls.Add(removeButton);

                statusLabel = new Label();
                statusLabel.Text = "Készen áll...";
                statusLabel.Location = new Point(20, 735);
                statusLabel.Size = new Size(640, 23);
                statusLabel.ForeColor = Color.Blue;
                this.Controls.Add(statusLabel);

                this.ResumeLayout();

                UpdateTemplateComboBox();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a felhasználói felület létrehozásakor: {ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Inicializálási hiba", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void NormalScheduleButton_Click(object sender, EventArgs e)
        {
            try
            {
                LoadTemplateByName("Rendes csengetési rend");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a rendes csengetési rend betöltésekor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShortScheduleButton_Click(object sender, EventArgs e)
        {
            try
            {
                LoadTemplateByName("Rövidített órák");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a rövidített órák betöltésekor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadTemplateByName(string templateName)
        {
            try
            {
                var template = templates.FirstOrDefault(t => t.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));
                if (template == null)
                {
                    MessageBox.Show($"A '{templateName}' sablon nem található a templates.json fájlban!\n\nEllenőrizze, hogy a sablon létezik és a neve pontosan egyezik.",
                        "Sablon nem található", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                LoadTemplate(template);

                // Frissítjük a combobox kiválasztást is
                for (int i = 0; i < templateComboBox.Items.Count; i++)
                {
                    if (templateComboBox.Items[i].ToString().Equals(templateName, StringComparison.OrdinalIgnoreCase))
                    {
                        templateComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a sablon betöltésekor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RepeatCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                bool enabled = repeatCheckBox.Checked;
                foreach (var checkbox in weekdayCheckBoxes)
                {
                    checkbox.Enabled = enabled;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az ismétlés beállításakor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SelectWeekdaysButton_Click(object sender, EventArgs e)
        {
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    weekdayCheckBoxes[i].Checked = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a hétköznapok kijelölésekor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateTemplateComboBox()
        {
            try
            {
                templateComboBox.Items.Clear();
                templateComboBox.Items.Add("-- Válasszon sablont --");
                foreach (var template in templates)
                {
                    // Kihagyjuk a gyors gombok sablonjai, hogy ne duplikálódjanak
                    if (!template.Name.Equals("Rendes csengetési rend", StringComparison.OrdinalIgnoreCase) &&
                        !template.Name.Equals("Rövidített órák", StringComparison.OrdinalIgnoreCase))
                    {
                        templateComboBox.Items.Add(template.Name);
                    }
                }
                templateComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a sablon lista frissítésekor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveTemplateButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(templateNameTextBox.Text))
                {
                    MessageBox.Show("Kérjük adja meg a sablon nevét!", "Hiba",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (scheduledSounds.Count == 0)
                {
                    MessageBox.Show("Nincsenek ütemezett hangok a mentéshez!", "Hiba",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string templateName = templateNameTextBox.Text.Trim();

                var existingTemplate = templates.FirstOrDefault(t => t.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));
                if (existingTemplate != null)
                {
                    var result = MessageBox.Show($"A '{templateName}' nevű sablon már létezik. Felülírja?",
                        "Megerősítés", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No) return;

                    templates.Remove(existingTemplate);
                }

                var template = new Template
                {
                    Name = templateName,
                    Sounds = new List<TemplateSound>()
                };

                foreach (var sound in scheduledSounds)
                {
                    template.Sounds.Add(new TemplateSound
                    {
                        TimeOfDay = sound.ScheduledTime.TimeOfDay,
                        FilePath = sound.FilePath,
                        FileName = sound.FileName,
                        DurationSeconds = sound.DurationSeconds,
                        IsRepeating = sound.IsRepeating,
                        RepeatDays = sound.RepeatDays?.ToList() ?? new List<DayOfWeek>()
                    });
                }

                templates.Add(template);
                SaveTemplates();
                UpdateTemplateComboBox();
                templateComboBox.SelectedItem = templateName;
                templateNameTextBox.Text = "";

                statusLabel.Text = $"Sablon mentve: {templateName} ({template.Sounds.Count} hang)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a sablon mentésekor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadTemplateButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (templateComboBox.SelectedIndex <= 0) return;

                string templateName = templateComboBox.SelectedItem.ToString();
                var template = templates.FirstOrDefault(t => t.Name == templateName);
                if (template == null) return;

                LoadTemplate(template);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a sablon betöltésekor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadTemplate(Template template)
        {
            try
            {
                scheduledSounds.Clear();
                DateTime baseDate = DateTime.Today;

                foreach (var templateSound in template.Sounds)
                {
                    if (templateSound.IsRepeating)
                    {
                        foreach (var day in templateSound.RepeatDays)
                        {
                            DateTime nextOccurrence = GetNextWeekday(baseDate, day).Add(templateSound.TimeOfDay);

                            var scheduledSound = new ScheduledSound
                            {
                                ScheduledTime = nextOccurrence,
                                FilePath = templateSound.FilePath,
                                FileName = templateSound.FileName,
                                DurationSeconds = templateSound.DurationSeconds,
                                IsRepeating = true,
                                RepeatDays = templateSound.RepeatDays.ToArray()
                            };

                            scheduledSounds.Add(scheduledSound);
                        }
                    }
                    else
                    {
                        DateTime nextTime = baseDate.Add(templateSound.TimeOfDay);
                        if (nextTime <= DateTime.Now)
                            nextTime = nextTime.AddDays(1);

                        var scheduledSound = new ScheduledSound
                        {
                            ScheduledTime = nextTime,
                            FilePath = templateSound.FilePath,
                            FileName = templateSound.FileName,
                            DurationSeconds = templateSound.DurationSeconds,
                            IsRepeating = false
                        };

                        scheduledSounds.Add(scheduledSound);
                    }
                }

                scheduledSounds = scheduledSounds.OrderBy(s => s.ScheduledTime).ToList();
                UpdateScheduledList();
                statusLabel.Text = $"Sablon betöltve: {template.Name} ({scheduledSounds.Count} hang ütemezve)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a sablon betöltésekor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteTemplateButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (templateComboBox.SelectedIndex <= 0) return;

                string templateName = templateComboBox.SelectedItem.ToString();
                var result = MessageBox.Show($"Biztosan törli a '{templateName}' sablont?",
                    "Megerősítés", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    templates.RemoveAll(t => t.Name == templateName);
                    SaveTemplates();
                    UpdateTemplateComboBox();
                    statusLabel.Text = $"Sablon törölve: {templateName}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a sablon törlésekor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearAllButton_Click(object sender, EventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Biztosan törli az összes ütemezett hangot?",
                    "Megerősítés", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    scheduledSounds.Clear();
                    UpdateScheduledList();
                    statusLabel.Text = "Minden ütemezett hang törölve.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az ütemezett hangok törlésekor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TemplateComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                deleteTemplateButton.Enabled = templateComboBox.SelectedIndex > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a sablon kiválasztásakor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            int daysUntilTarget = ((int)day - (int)start.DayOfWeek + 7) % 7;
            if (daysUntilTarget == 0) daysUntilTarget = 7;
            return start.AddDays(daysUntilTarget);
        }

        private void DurationTrackBar_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                int seconds = durationTrackBar.Value;
                durationLabel.Text = $"Lejátszási időtartam: {seconds} másodperc";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az időtartam beállításakor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a fájl kiválasztásakor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            try
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

                if (repeatCheckBox.Checked)
                {
                    var selectedDays = new List<DayOfWeek>();
                    for (int i = 0; i < 7; i++)
                    {
                        if (weekdayCheckBoxes[i].Checked)
                        {
                            selectedDays.Add((DayOfWeek)((i + 1) % 7));
                        }
                    }

                    if (selectedDays.Count == 0)
                    {
                        MessageBox.Show("Ismétlés esetén legalább egy napot ki kell választani!", "Hiba",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    foreach (var day in selectedDays)
                    {
                        DateTime nextOccurrence = GetNextWeekday(DateTime.Today, day).Add(timePicker.Value.TimeOfDay);

                        var newSound = new ScheduledSound
                        {
                            ScheduledTime = nextOccurrence,
                            FilePath = filePathTextBox.Text,
                            FileName = Path.GetFileName(filePathTextBox.Text),
                            DurationSeconds = durationTrackBar.Value,
                            IsRepeating = true,
                            RepeatDays = selectedDays.ToArray()
                        };

                        scheduledSounds.Add(newSound);
                    }

                    statusLabel.Text = $"Ismétlődő hang hozzáadva: {selectedDays.Count} napon";
                }
                else
                {
                    DateTime scheduledTime = datePicker.Value.Date.Add(timePicker.Value.TimeOfDay);

                    if (scheduledTime <= DateTime.Now)
                    {
                        MessageBox.Show("Az időpontnak a jövőben kell lennie!", "Hiba",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var newSound = new ScheduledSound
                    {
                        ScheduledTime = scheduledTime,
                        FilePath = filePathTextBox.Text,
                        FileName = Path.GetFileName(filePathTextBox.Text),
                        DurationSeconds = durationTrackBar.Value,
                        IsRepeating = false
                    };

                    scheduledSounds.Add(newSound);
                    statusLabel.Text = $"Hozzáadva: {newSound.ScheduledTime:yyyy.MM.dd HH:mm} - {newSound.FileName}";
                }

                scheduledSounds = scheduledSounds.OrderBy(s => s.ScheduledTime).ToList();
                UpdateScheduledList();
                filePathTextBox.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a hang hozzáadásakor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            try
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
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az elem eltávolításakor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            try
            {
                StopCurrentPlayback();
                statusLabel.Text = "Lejátszás manuálisan leállítva.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a lejátszás leállításakor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopCurrentPlayback()
        {
            try
            {
                playbackTimer?.Stop();

                if (currentNotificationForm != null && !currentNotificationForm.IsDisposed)
                {
                    try
                    {
                        currentNotificationForm.Close();
                        currentNotificationForm = null;
                    }
                    catch { }
                }

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hiba a playback leállításakor: {ex.Message}");
            }
        }

        private void UpdateScheduledList()
        {
            try
            {
                scheduledListBox.Items.Clear();
                foreach (var sound in scheduledSounds)
                {
                    scheduledListBox.Items.Add(sound.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a lista frissítésekor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                DateTime now = DateTime.Now;
                var soundsToPlay = scheduledSounds.Where(s =>
                    Math.Abs((s.ScheduledTime - now).TotalSeconds) < 1).ToList();

                foreach (var sound in soundsToPlay)
                {
                    PlaySound(sound);

                    if (sound.IsRepeating)
                    {
                        sound.ScheduledTime = sound.ScheduledTime.AddDays(7);
                    }
                    else
                    {
                        scheduledSounds.Remove(sound);
                    }
                }

                if (soundsToPlay.Any())
                {
                    scheduledSounds = scheduledSounds.OrderBy(s => s.ScheduledTime).ToList();
                    UpdateScheduledList();
                }

                if (currentPlayingSound == null && scheduledSounds.Any())
                {
                    var nextSound = scheduledSounds.First();
                    TimeSpan timeUntilNext = nextSound.ScheduledTime - now;
                    string repeatInfo = nextSound.IsRepeating ? " (ismétlődő)" : "";
                    statusLabel.Text = $"Következő: {nextSound.FileName}{repeatInfo} - {timeUntilNext.Days}n {timeUntilNext.Hours:D2}:{timeUntilNext.Minutes:D2}:{timeUntilNext.Seconds:D2}";
                }
                else if (currentPlayingSound == null)
                {
                    statusLabel.Text = "Nincs ütemezett hang.";
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Hiba a timer ellenőrzéskor: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"CheckTimer hiba: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (currentPlayingSound != null && wavePlayer != null)
                {
                    TimeSpan elapsed = DateTime.Now - playbackStartTime;
                    TimeSpan totalDuration = TimeSpan.FromSeconds(currentPlayingSound.DurationSeconds);

                    if (elapsed >= totalDuration || wavePlayer.PlaybackState == PlaybackState.Stopped)
                    {
                        string finishedFileName = currentPlayingSound.FileName;

                        if (currentNotificationForm != null && !currentNotificationForm.IsDisposed)
                        {
                            try
                            {
                                currentNotificationForm.Close();
                                currentNotificationForm = null;
                            }
                            catch { }
                        }

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
            catch (Exception ex)
            {
                statusLabel.Text = $"Hiba a lejátszás követésekor: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"PlaybackTimer hiba: {ex.Message}");
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

                string repeatInfo = sound.IsRepeating ? " (ismétlődő)" : "";
                statusLabel.Text = $"Lejátszás: {sound.FileName}{repeatInfo} - {sound.GetDurationText()}";

                await Task.Delay(100);

                ShowSilentNotification($"🎵 Hang lejátszva: {sound.FileName}\n⏰ Időpont: {sound.ScheduledTime:yyyy.MM.dd HH:mm:ss}\n⏱️ Időtartam: {sound.GetDurationText()}\n{(sound.IsRepeating ? "🔄 Ismétlődő hang" : "📅 Egyszeri hang")}\n✅ Automatikus leállítás aktív!");
            }
            catch (Exception ex)
            {
                ShowSilentNotification($"❌ Hiba: {ex.Message}\n\nLehetséges okok:\n• Nem támogatott fájl\n• Sérült hangfájl\n• Fájl használatban", true);
                StopCurrentPlayback();
            }
        }

        private void LoadTemplates()
        {
            try
            {
                string templatesFile = Path.Combine(Application.StartupPath, "templates.json");
                if (File.Exists(templatesFile))
                {
                    string json = File.ReadAllText(templatesFile);
                    templates = JsonSerializer.Deserialize<List<Template>>(json) ?? new List<Template>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a sablonok betöltésekor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                templates = new List<Template>();
            }
        }

        private void SaveTemplates()
        {
            try
            {
                string templatesFile = Path.Combine(Application.StartupPath, "templates.json");
                string json = JsonSerializer.Serialize(templates, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(templatesFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a sablonok mentésekor: {ex.Message}", "Hiba",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowSilentNotification(string message, bool isError = false)
        {
            try
            {
                if (currentNotificationForm != null && !currentNotificationForm.IsDisposed)
                {
                    try
                    {
                        currentNotificationForm.Close();
                    }
                    catch { }
                }

                currentNotificationForm = new Form();
                currentNotificationForm.Text = isError ? "Hiba" : "Ütemezett hang";
                currentNotificationForm.Size = new Size(400, 250);
                currentNotificationForm.StartPosition = FormStartPosition.CenterParent;
                currentNotificationForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                currentNotificationForm.MaximizeBox = false;
                currentNotificationForm.MinimizeBox = false;
                currentNotificationForm.TopMost = true;
                currentNotificationForm.ShowInTaskbar = false;

                Label messageLabel = new Label();
                messageLabel.Text = message;
                messageLabel.Location = new Point(20, 20);
                messageLabel.Size = new Size(350, 150);
                messageLabel.Font = new Font("Microsoft Sans Serif", 9f);
                currentNotificationForm.Controls.Add(messageLabel);

                Button okButton = new Button();
                okButton.Text = "OK";
                okButton.Size = new Size(80, 30);
                okButton.Location = new Point(160, 180);
                okButton.BackColor = isError ? Color.LightCoral : Color.LightGreen;
                okButton.Click += (s, e) =>
                {
                    currentNotificationForm.Close();
                    currentNotificationForm = null;
                };
                currentNotificationForm.Controls.Add(okButton);

                currentNotificationForm.AcceptButton = okButton;
                currentNotificationForm.CancelButton = okButton;

                currentNotificationForm.FormClosed += (s, e) =>
                {
                    currentNotificationForm = null;
                };

                currentNotificationForm.Show(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Notification hiba: {ex.Message}");
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                StopCurrentPlayback();
                checkTimer?.Stop();
                SaveTemplates();
                base.OnFormClosed(e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Form bezárás hiba: {ex.Message}");
                base.OnFormClosed(e);
            }
        }
    }

    public class ScheduledSound
    {
        public DateTime ScheduledTime { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public int DurationSeconds { get; set; }
        public bool IsRepeating { get; set; }
        public DayOfWeek[] RepeatDays { get; set; }

        public string GetDurationText()
        {
            return $"{DurationSeconds}s";
        }

        public string GetRepeatDaysText()
        {
            if (!IsRepeating || RepeatDays == null || RepeatDays.Length == 0)
                return "";

            var dayNames = new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Monday, "H" },
                { DayOfWeek.Tuesday, "K" },
                { DayOfWeek.Wednesday, "Sz" },
                { DayOfWeek.Thursday, "Cs" },
                { DayOfWeek.Friday, "P" },
                { DayOfWeek.Saturday, "Szo" },
                { DayOfWeek.Sunday, "V" }
            };

            return string.Join(",", RepeatDays.Select(d => dayNames[d]));
        }

        public override string ToString()
        {
            string repeatInfo = IsRepeating ? $" [ismétlődő: {GetRepeatDaysText()}]" : "";
            return $"{ScheduledTime:yyyy.MM.dd HH:mm} - {FileName} ({GetDurationText()}){repeatInfo}";
        }
    }

    public class Template
    {
        public string Name { get; set; }
        public List<TemplateSound> Sounds { get; set; } = new List<TemplateSound>();
    }

    public class TemplateSound
    {
        public TimeSpan TimeOfDay { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public int DurationSeconds { get; set; }
        public bool IsRepeating { get; set; }
        public List<DayOfWeek> RepeatDays { get; set; } = new List<DayOfWeek>();
    }

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kritikus hiba az alkalmazás indításakor:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Alkalmazás hiba", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}