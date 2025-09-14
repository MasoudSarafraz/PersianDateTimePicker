using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

public static class PersianDateConverter
{
    private static PersianCalendar pc = new PersianCalendar();

    public static string ToPersianDateString(DateTime date)
    {
        return string.Format("{0}/{1}/{2}",
            pc.GetYear(date),
            pc.GetMonth(date).ToString("00"),
            pc.GetDayOfMonth(date).ToString("00"));
    }

    public static string ToGregorianDateString(DateTime date)
    {
        return date.ToString("yyyy/MM/dd");
    }

    public static DateTime? ToGregorianDateTime(string persianDate)
    {
        if (string.IsNullOrWhiteSpace(persianDate)) return null;

        string[] parts = persianDate.Split('/');
        if (parts.Length != 3) return null;

        if (!int.TryParse(parts[0], out int year) ||
            !int.TryParse(parts[1], out int month) ||
            !int.TryParse(parts[2], out int day))
        {
            return null;
        }

        try
        {
            return pc.ToDateTime(year, month, day, 0, 0, 0, 0);
        }
        catch
        {
            return null;
        }
    }

    public static DateTime? ParseGregorianDateTime(string gregorianDate)
    {
        if (string.IsNullOrWhiteSpace(gregorianDate)) return null;

        // سعی با فرمت‌های مختلف میلادی
        string[] formats = { "yyyy/MM/dd", "yyyy-MM-dd", "MM/dd/yyyy", "MM-dd-yyyy" };

        if (DateTime.TryParseExact(gregorianDate, formats,
                                 CultureInfo.InvariantCulture,
                                 DateTimeStyles.None, out DateTime result))
        {
            return result;
        }

        return null;
    }
}

public class CalendarForm : Form
{
    private Panel pnlHeader;
    private Label lblMonthYear;
    private Button btnPrev;
    private Button btnNext;
    private Panel pnlDays;
    private Button btnToday;
    private PersianCalendar pc = new PersianCalendar();
    public DateTime SelectedDate { get; private set; }
    private int currentYear;
    private int currentMonth;
    private DateTime lastClickTime;
    private Button lastClickedButton;
    private Button[,] dayButtons = new Button[6, 7];

    public CalendarForm(DateTime initialDate)
    {
        SelectedDate = initialDate;
        currentYear = pc.GetYear(initialDate);
        currentMonth = pc.GetMonth(initialDate);
        InitializeComponents();
        SetupCalendar();
    }

    private void InitializeComponents()
    {
        this.pnlHeader = new Panel();
        this.lblMonthYear = new Label();
        this.btnPrev = new Button();
        this.btnNext = new Button();
        this.pnlDays = new Panel();
        this.btnToday = new Button();

        // Form settings
        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.Manual;
        this.Size = new Size(240, 250);
        this.BackColor = Color.White;
        this.Padding = new Padding(1);
        this.DoubleBuffered = true;
        this.KeyPreview = true;
        this.KeyPress += new KeyPressEventHandler(CalendarForm_KeyPress);

        // Header panel
        this.pnlHeader.BackColor = Color.FromArgb(41, 128, 185);
        this.pnlHeader.Dock = DockStyle.Top;
        this.pnlHeader.Height = 35;
        this.pnlHeader.Controls.Add(this.lblMonthYear);
        this.pnlHeader.Controls.Add(this.btnPrev);
        this.pnlHeader.Controls.Add(this.btnNext);

        // Month/Year label
        this.lblMonthYear.AutoSize = false;
        this.lblMonthYear.TextAlign = ContentAlignment.MiddleCenter;
        this.lblMonthYear.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        this.lblMonthYear.ForeColor = Color.White;
        this.lblMonthYear.Dock = DockStyle.Fill;
        this.lblMonthYear.BringToFront();

        // Previous button
        this.btnPrev.Text = "◀";
        this.btnPrev.FlatStyle = FlatStyle.Flat;
        this.btnPrev.ForeColor = Color.White;
        this.btnPrev.BackColor = Color.Transparent;
        this.btnPrev.Size = new Size(35, 35);
        this.btnPrev.Dock = DockStyle.Left;
        this.btnPrev.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        this.btnPrev.Cursor = Cursors.Hand;
        this.btnPrev.Click += new EventHandler(btnPrev_Click);

        // Next button
        this.btnNext.Text = "▶";
        this.btnNext.FlatStyle = FlatStyle.Flat;
        this.btnNext.ForeColor = Color.White;
        this.btnNext.BackColor = Color.Transparent;
        this.btnNext.Size = new Size(35, 35);
        this.btnNext.Dock = DockStyle.Right;
        this.btnNext.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        this.btnNext.Cursor = Cursors.Hand;
        this.btnNext.Click += new EventHandler(btnNext_Click);

        // Days panel
        this.pnlDays.Dock = DockStyle.Fill;
        this.pnlDays.BackColor = Color.White;
        this.pnlDays.Padding = new Padding(5);

        // Today button
        this.btnToday.Text = "امروز";
        this.btnToday.FlatStyle = FlatStyle.Flat;
        this.btnToday.ForeColor = Color.White;
        this.btnToday.BackColor = Color.FromArgb(46, 204, 113);
        this.btnToday.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        this.btnToday.Cursor = Cursors.Hand;
        this.btnToday.Size = new Size(230, 25);
        this.btnToday.Location = new Point(5, 215);
        this.btnToday.Click += new EventHandler(btnToday_Click);

        // Add controls to form
        this.Controls.Add(this.btnToday);
        this.Controls.Add(this.pnlDays);
        this.Controls.Add(this.pnlHeader);

        // Create day buttons once
        CreateDayButtons();
    }

    private void CreateDayButtons()
    {
        // Create day headers (static)
        string[] dayNames = { "ش", "ی", "د", "س", "چ", "پ", "ج" };
        for (int i = 0; i < 7; i++)
        {
            Label lblDay = new Label();
            lblDay.Text = dayNames[i];
            lblDay.TextAlign = ContentAlignment.MiddleCenter;
            lblDay.Font = new Font("Segoe UI", 7F, FontStyle.Bold);
            lblDay.ForeColor = Color.FromArgb(127, 140, 141);
            lblDay.Size = new Size(30, 20);
            lblDay.Location = new Point(5 + i * 32, 5);
            pnlDays.Controls.Add(lblDay);
        }

        // Create day buttons (once)
        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                Button btnDay = new Button();
                btnDay.Size = new Size(30, 30);
                btnDay.Location = new Point(5 + col * 32, 25 + row * 32);
                btnDay.FlatStyle = FlatStyle.Flat;
                btnDay.FlatAppearance.BorderSize = 0;
                btnDay.Font = new Font("Segoe UI", 8F);
                btnDay.Cursor = Cursors.Hand;
                btnDay.TabStop = false;
                btnDay.Visible = false; // Initially invisible

                // Add events
                btnDay.MouseEnter += (s, e) => {
                    if (btnDay.BackColor != Color.FromArgb(41, 128, 185) &&
                        btnDay.BackColor != Color.FromArgb(46, 204, 113))
                    {
                        btnDay.BackColor = Color.FromArgb(236, 240, 241);
                    }
                };

                btnDay.MouseLeave += (s, e) => {
                    if (btnDay.BackColor != Color.FromArgb(41, 128, 185) &&
                        btnDay.BackColor != Color.FromArgb(46, 204, 113))
                    {
                        btnDay.BackColor = Color.Transparent;
                    }
                };

                btnDay.Click += (s, e) => {
                    DateTime now = DateTime.Now;
                    TimeSpan timeSinceLastClick = now - lastClickTime;

                    if (timeSinceLastClick.TotalMilliseconds < 500 && lastClickedButton == btnDay)
                    {
                        int day = (int)btnDay.Tag;
                        SelectedDate = pc.ToDateTime(currentYear, currentMonth, day, 0, 0, 0, 0);
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        ClearPreviousSelection();
                        btnDay.BackColor = Color.FromArgb(41, 128, 185);
                        btnDay.ForeColor = Color.White;
                        btnDay.Font = new Font("Segoe UI", 8F, FontStyle.Bold);

                        int day = (int)btnDay.Tag;
                        SelectedDate = pc.ToDateTime(currentYear, currentMonth, day, 0, 0, 0, 0);
                    }

                    lastClickTime = now;
                    lastClickedButton = btnDay;
                };

                dayButtons[row, col] = btnDay;
                pnlDays.Controls.Add(btnDay);
            }
        }
    }

    private void ClearPreviousSelection()
    {
        foreach (Button btn in dayButtons)
        {
            if (btn != null && btn.BackColor == Color.FromArgb(41, 128, 185))
            {
                btn.BackColor = Color.Transparent;
                btn.ForeColor = Color.FromArgb(52, 73, 94);
                btn.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
            }
        }
    }

    private void CalendarForm_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == (char)Keys.Escape)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }

    private void SetupCalendar()
    {
        UpdateCalendarLabels();
        GenerateCalendar();
    }

    private void UpdateCalendarLabels()
    {
        lblMonthYear.Text = $"{GetPersianMonthName(currentMonth)} {currentYear}";
    }

    private string GetPersianMonthName(int month)
    {
        string[] months = { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
                           "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };
        return months[month - 1];
    }

    private void GenerateCalendar()
    {
        // Calculate first day of month
        DateTime firstDayOfMonth = pc.ToDateTime(currentYear, currentMonth, 1, 0, 0, 0, 0);
        int dayOfWeek = (int)pc.GetDayOfWeek(firstDayOfMonth);
        int persianDayOfWeek = (dayOfWeek + 1) % 7;

        int daysInMonth = pc.GetDaysInMonth(currentYear, currentMonth);
        int dayCounter = 1;

        // Update existing buttons instead of creating new ones
        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                Button btnDay = dayButtons[row, col];
                if (btnDay == null) continue;

                if (row == 0 && col < persianDayOfWeek)
                {
                    btnDay.Visible = false;
                    continue;
                }

                if (dayCounter > daysInMonth)
                {
                    btnDay.Visible = false;
                    continue;
                }

                // Button settings
                btnDay.Text = dayCounter.ToString();
                btnDay.Tag = dayCounter;
                btnDay.Visible = true;

                // Styling based on state
                if (dayCounter == pc.GetDayOfMonth(SelectedDate) &&
                    currentMonth == pc.GetMonth(SelectedDate) &&
                    currentYear == pc.GetYear(SelectedDate))
                {
                    btnDay.BackColor = Color.FromArgb(41, 128, 185);
                    btnDay.ForeColor = Color.White;
                    btnDay.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                }
                else if (dayCounter == pc.GetDayOfMonth(DateTime.Now) &&
                         currentMonth == pc.GetMonth(DateTime.Now) &&
                         currentYear == pc.GetYear(DateTime.Now))
                {
                    btnDay.BackColor = Color.FromArgb(46, 204, 113);
                    btnDay.ForeColor = Color.White;
                    btnDay.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
                }
                else
                {
                    btnDay.BackColor = Color.Transparent;
                    btnDay.ForeColor = Color.FromArgb(52, 73, 94);
                    btnDay.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
                }

                dayCounter++;
            }
        }
    }

    private void btnPrev_Click(object sender, EventArgs e)
    {
        currentMonth--;
        if (currentMonth < 1)
        {
            currentMonth = 12;
            currentYear--;
        }
        UpdateCalendarLabels();
        GenerateCalendar();
    }

    private void btnNext_Click(object sender, EventArgs e)
    {
        currentMonth++;
        if (currentMonth > 12)
        {
            currentMonth = 1;
            currentYear++;
        }
        UpdateCalendarLabels();
        GenerateCalendar();
    }

    private void btnToday_Click(object sender, EventArgs e)
    {
        SelectedDate = DateTime.Now;
        currentYear = pc.GetYear(SelectedDate);
        currentMonth = pc.GetMonth(SelectedDate);
        UpdateCalendarLabels();
        GenerateCalendar();
    }
}

public partial class PersianDateTimePicker : UserControl
{
    private TextBox txtDate;
    private Button btnCalendar;
    private DateTime _value = DateTime.Now;
    private bool initialized = false;

    [Browsable(true)]
    [Category("Behavior")]
    [Description("تاریخ انتخاب شده")]
    public DateTime Value
    {
        get => _value;
        set
        {
            _value = value;
            if (initialized) UpdateDisplay();
        }
    }

    public PersianDateTimePicker()
    {
        InitializeComponent();
        initialized = true;
        UpdateDisplay();
    }

    private void InitializeComponent()
    {
        this.txtDate = new TextBox();
        this.btnCalendar = new Button();

        // Date textbox settings
        this.txtDate.Location = new Point(1, 1);
        this.txtDate.Size = new Size(70, 18);
        this.txtDate.BorderStyle = BorderStyle.None;
        this.txtDate.Font = new Font("Segoe UI", 8F);
        this.txtDate.TextAlign = HorizontalAlignment.Center;
        this.txtDate.ReadOnly = true;
        this.txtDate.BackColor = Color.White;
        this.txtDate.Leave += new EventHandler(txtDate_Leave);

        // Calendar button settings
        this.btnCalendar.Location = new Point(71, 1);
        this.btnCalendar.Size = new Size(18, 18);
        this.btnCalendar.Text = "📅";
        this.btnCalendar.Font = new Font("Segoe UI Emoji", 8F);
        this.btnCalendar.FlatStyle = FlatStyle.Flat;
        this.btnCalendar.FlatAppearance.BorderSize = 0;
        this.btnCalendar.BackColor = Color.FromArgb(41, 128, 185);
        this.btnCalendar.ForeColor = Color.White;
        this.btnCalendar.Cursor = Cursors.Hand;
        this.btnCalendar.Click += new EventHandler(btnCalendar_Click);

        // PersianDateTimePicker settings
        this.AutoScaleDimensions = new SizeF(6F, 13F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.Controls.Add(this.btnCalendar);
        this.Controls.Add(this.txtDate);
        this.Name = "PersianDateTimePicker";
        this.Size = new Size(90, 20);
        this.BackColor = Color.White;
        this.Paint += new PaintEventHandler(PersianDateTimePicker_Paint);
    }

    private void PersianDateTimePicker_Paint(object sender, PaintEventArgs e)
    {
        // Draw border
        Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
        using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1))
        {
            e.Graphics.DrawRectangle(pen, rect);
        }

        // Draw separator
        using (Pen pen = new Pen(Color.FromArgb(230, 230, 230), 1))
        {
            e.Graphics.DrawLine(pen, 70, 0, 70, this.Height);
        }
    }

    private void UpdateDisplay()
    {
        if (txtDate != null)
        {
            txtDate.Text = PersianDateConverter.ToPersianDateString(_value);
        }
    }

    private void btnCalendar_Click(object sender, EventArgs e)
    {
        this.Invoke(new Action(() =>
        {
            using (CalendarForm calendarForm = new CalendarForm(_value))
            {
                Point location = this.PointToScreen(new Point(0, this.Height));

                Rectangle screenBounds = Screen.GetWorkingArea(this);
                if (location.X + calendarForm.Width > screenBounds.Right)
                {
                    location.X = screenBounds.Right - calendarForm.Width;
                }
                if (location.Y + calendarForm.Height > screenBounds.Bottom)
                {
                    location.Y = screenBounds.Bottom - calendarForm.Height;
                }

                calendarForm.Location = location;

                if (calendarForm.ShowDialog() == DialogResult.OK)
                {
                    _value = calendarForm.SelectedDate;
                    UpdateDisplay();
                }
            }
        }));
    }

    private void txtDate_Leave(object sender, EventArgs e)
    {
        // Try to parse as Persian date first
        var newDate = PersianDateConverter.ToGregorianDateTime(txtDate.Text);
        if (newDate.HasValue)
        {
            _value = newDate.Value;
            UpdateDisplay();
            return;
        }

        // If Persian parsing failed, try Gregorian
        var gregorianDate = PersianDateConverter.ParseGregorianDateTime(txtDate.Text);
        if (gregorianDate.HasValue)
        {
            _value = gregorianDate.Value;
            UpdateDisplay();
        }
        else
        {
            UpdateDisplay(); // Restore valid value
        }
    }

    #region New Methods for Date Access

    /// <summary>
    /// Gets the Persian date as a string (yyyy/MM/dd format)
    /// </summary>
    public string GetPersianDate()
    {
        return PersianDateConverter.ToPersianDateString(_value);
    }

    /// <summary>
    /// Gets the Gregorian date as a string (yyyy/MM/dd format)
    /// </summary>
    public string GetGregorianDate()
    {
        return PersianDateConverter.ToGregorianDateString(_value);
    }

    /// <summary>
    /// Sets the date using a Persian date string (yyyy/MM/dd format)
    /// </summary>
    public void SetPersianDate(string persianDate)
    {
        var newDate = PersianDateConverter.ToGregorianDateTime(persianDate);
        if (newDate.HasValue)
        {
            _value = newDate.Value;
            UpdateDisplay();
        }
    }

    /// <summary>
    /// Sets the date using a Gregorian date string (yyyy/MM/dd format)
    /// </summary>
    public void SetGregorianDate(string gregorianDate)
    {
        var newDate = PersianDateConverter.ParseGregorianDateTime(gregorianDate);
        if (newDate.HasValue)
        {
            _value = newDate.Value;
            UpdateDisplay();
        }
    }

    #endregion
}

//string persianDate = persianDateTimePicker1.GetPersianDate();
//// خروجی: "1402/05/15"
//string gregorianDate = persianDateTimePicker1.GetGregorianDate();
//// خروجی: "2023/08/06"
//persianDateTimePicker1.SetPersianDate("1402/05/15");
//persianDateTimePicker1.SetGregorianDate("2023/08/06");