using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

public static class PersianDateConverter
{
    private static readonly PersianCalendar pc = new PersianCalendar();
    private static readonly object lockObject = new object();
    private static readonly Dictionary<string, DateTime?> persianToGregorianCache = new Dictionary<string, DateTime?>();

    public static readonly string[] PersianMonths = {
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    };

    public static string ToPersianDateString(DateTime date, bool includeMonthName = false)
    {
        lock (lockObject)
        {
            if (includeMonthName)
            {
                return $"{pc.GetDayOfMonth(date)} {PersianMonths[pc.GetMonth(date) - 1]} {pc.GetYear(date)}";
            }

            return $"{pc.GetYear(date)}/{pc.GetMonth(date):D2}/{pc.GetDayOfMonth(date):D2}";
        }
    }

    public static string ToGregorianDateString(DateTime date)
    {
        return date.ToString("yyyy/MM/dd");
    }

    public static DateTime? ToGregorianDateTime(string persianDate)
    {
        if (string.IsNullOrWhiteSpace(persianDate))
            return null;

        if (persianToGregorianCache.TryGetValue(persianDate, out DateTime? cachedResult))
        {
            return cachedResult;
        }

        string[] parts = persianDate.Split(new[] { '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            return null;

        if (!int.TryParse(parts[0], out int year) ||
            !int.TryParse(parts[1], out int month) ||
            !int.TryParse(parts[2], out int day))
        {
            return null;
        }

        if (year < 1000 || year > 1500 || month < 1 || month > 12 || day < 1 || day > 31)
            return null;

        try
        {
            lock (lockObject)
            {
                DateTime result = pc.ToDateTime(year, month, day, 0, 0, 0, 0);
                persianToGregorianCache.Add(persianDate, result);
                return result;
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            persianToGregorianCache.Add(persianDate, null);
            return null;
        }
    }

    public static DateTime? ParseGregorianDateTime(string gregorianDate)
    {
        if (string.IsNullOrWhiteSpace(gregorianDate))
            return null;

        string[] formats = {
            "yyyy/MM/dd", "yyyy-MM-dd", "yyyy/MM/dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss", "MM/dd/yyyy", "MM-dd-yyyy",
            "dd/MM/yyyy", "dd-MM-yyyy"
        };

        if (DateTime.TryParseExact(gregorianDate, formats,
                                 CultureInfo.InvariantCulture,
                                 DateTimeStyles.None, out DateTime result))
        {
            return result;
        }

        return null;
    }
}

public class DateSelectedEventArgs : EventArgs
{
    public DateTime SelectedDate { get; }

    public DateSelectedEventArgs(DateTime selectedDate)
    {
        SelectedDate = selectedDate;
    }
}

public class DateValidationErrorEventArgs : EventArgs
{
    public string InvalidDate { get; }
    public string ErrorMessage { get; }

    public DateValidationErrorEventArgs(string invalidDate, string errorMessage)
    {
        InvalidDate = invalidDate;
        ErrorMessage = errorMessage;
    }
}

public class MonthInfo
{
    public int StartDayOfWeek { get; set; }
    public int DaysInMonth { get; set; }

    public MonthInfo(int startDayOfWeek, int daysInMonth)
    {
        StartDayOfWeek = startDayOfWeek;
        DaysInMonth = daysInMonth;
    }
}

public class CalendarPanel : Panel
{
    private const int DaysInWeek = 7;
    private const int MaxWeeks = 6;
    private const int DayWidth = 32;
    private const int DayHeight = 25;
    private const int DaySpacing = 2;
    private const int HeaderHeight = 25;
    private static readonly Font DayFont = new Font("Segoe UI", 8F, FontStyle.Regular);
    private static readonly Font BoldDayFont = new Font("Segoe UI", 8F, FontStyle.Bold);
    private static readonly Font HeaderFont = new Font("Segoe UI", 7F, FontStyle.Bold);
    private static readonly StringFormat CenterFormat = new StringFormat
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };

    private int _year;
    private int _month;
    private int _selectedDay;
    private int _todayDay;
    private int _hoveredDay = -1;
    private MonthInfo _monthInfo;

    public event EventHandler<DaySelectedEventArgs> DaySelected;

    public int Year
    {
        get => _year;
        set
        {
            if (_year != value)
            {
                _year = value;
                UpdateMonthInfo();
                Invalidate();
            }
        }
    }

    public int Month
    {
        get => _month;
        set
        {
            if (_month != value)
            {
                _month = value;
                UpdateMonthInfo();
                Invalidate();
            }
        }
    }

    public int SelectedDay
    {
        get => _selectedDay;
        set
        {
            if (_selectedDay != value)
            {
                _selectedDay = value;
                Invalidate();
            }
        }
    }

    public int TodayDay
    {
        get => _todayDay;
        set
        {
            if (_todayDay != value)
            {
                _todayDay = value;
                Invalidate();
            }
        }
    }

    public CalendarPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

        // تنظیمات اولیه
        var now = DateTime.Now;
        var pc = new PersianCalendar();
        _year = pc.GetYear(now);
        _month = pc.GetMonth(now);
        _selectedDay = pc.GetDayOfMonth(now);
        _todayDay = pc.GetDayOfMonth(now);
        UpdateMonthInfo();
    }

    private void UpdateMonthInfo()
    {
        var pc = new PersianCalendar();
        DateTime firstDayOfMonth = pc.ToDateTime(_year, _month, 1, 0, 0, 0, 0);
        int dayOfWeek = (int)pc.GetDayOfWeek(firstDayOfMonth);
        int persianDayOfWeek = (dayOfWeek + 1) % 7;
        int daysInMonth = pc.GetDaysInMonth(_year, _month);
        _monthInfo = new MonthInfo(persianDayOfWeek, daysInMonth);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        // رسم هدر روزها
        DrawDayHeaders(g);
        // رسم روزهای ماه
        DrawMonthDays(g);
    }

    private void DrawDayHeaders(Graphics g)
    {
        string[] dayNames = { "ش", "ی", "د", "س", "چ", "پ", "ج" };
        using (var headerBrush = new SolidBrush(Color.FromArgb(127, 140, 141)))
        {
            for (int i = 0; i < DaysInWeek; i++)
            {
                var x = i * (DayWidth + DaySpacing);
                var rect = new Rectangle(x, 0, DayWidth, HeaderHeight);

                g.DrawString(dayNames[i], HeaderFont, headerBrush, rect, CenterFormat);
            }
        }
    }

    private void DrawMonthDays(Graphics g)
    {
        int dayCounter = 1;

        using (var regularBrush = new SolidBrush(Color.FromArgb(52, 73, 94)))
        using (var selectedBrush = new SolidBrush(Color.White))
        using (var todayBrush = new SolidBrush(Color.White))
        using (var hoverBrush = new SolidBrush(Color.FromArgb(52, 73, 94)))
        using (var selectedPen = new Pen(Color.FromArgb(41, 128, 185)))
        using (var todayPen = new Pen(Color.FromArgb(46, 204, 113)))
        using (var hoverPen = new Pen(Color.FromArgb(236, 240, 241)))
        {
            for (int week = 0; week < MaxWeeks; week++)
            {
                for (int dayOfWeek = 0; dayOfWeek < DaysInWeek; dayOfWeek++)
                {
                    // محاسبه موقعیت روز
                    int x = dayOfWeek * (DayWidth + DaySpacing);
                    int y = HeaderHeight + week * (DayHeight + DaySpacing);
                    var rect = new Rectangle(x, y, DayWidth, DayHeight);

                    // بررسی آیا این روز باید نمایش داده شود
                    if (week == 0 && dayOfWeek < _monthInfo.StartDayOfWeek)
                    {
                        continue; // روزهای خالی قبل از شروع ماه
                    }

                    if (dayCounter > _monthInfo.DaysInMonth)
                    {
                        break; // تمام روزهای ماه نمایش داده شده‌اند
                    }
                    // تعیین وضعیت روز
                    bool isSelected = (dayCounter == _selectedDay);
                    bool isToday = (dayCounter == _todayDay);
                    bool isHovered = (dayCounter == _hoveredDay);
                    // رسم پس‌زمینه
                    if (isSelected)
                    {
                        using (var bgBrush = new SolidBrush(Color.FromArgb(41, 128, 185)))
                            g.FillRectangle(bgBrush, rect);
                        g.DrawRectangle(selectedPen, rect);
                    }
                    else if (isToday)
                    {
                        using (var bgBrush = new SolidBrush(Color.FromArgb(46, 204, 113)))
                            g.FillRectangle(bgBrush, rect);
                        g.DrawRectangle(todayPen, rect);
                    }
                    else if (isHovered)
                    {
                        using (var bgBrush = new SolidBrush(Color.FromArgb(236, 240, 241)))
                            g.FillRectangle(bgBrush, rect);
                        g.DrawRectangle(hoverPen, rect);
                    }
                    // رسم متن روز
                    var font = isSelected ? BoldDayFont : DayFont;
                    var brush = isSelected || isToday ? selectedBrush : regularBrush;
                    g.DrawString(dayCounter.ToString(), font, brush, rect, CenterFormat);
                    dayCounter++;
                }

                if (dayCounter > _monthInfo.DaysInMonth)
                {
                    break;
                }
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        // محاسبه روز زیر ماوس
        int day = GetDayFromPoint(e.Location);

        if (day != _hoveredDay)
        {
            _hoveredDay = day;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);

        if (_hoveredDay != -1)
        {
            _hoveredDay = -1;
            Invalidate();
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        int day = GetDayFromPoint(e.Location);
        if (day > 0 && day <= _monthInfo.DaysInMonth)
        {
            // فقط انتخاب روز بدون بستن پنجره
            _selectedDay = day;
            Invalidate();
        }
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);

        int day = GetDayFromPoint(e.Location);
        if (day > 0 && day <= _monthInfo.DaysInMonth)
        {
            // دابل کلیک - انتخاب تاریخ و بستن پنجره
            _selectedDay = day;
            DaySelected?.Invoke(this, new DaySelectedEventArgs(_year, _month, day));
        }
    }

    private int GetDayFromPoint(Point point)
    {
        // بررسی آیا نقطه در محدوده روزها قرار دارد
        if (point.Y < HeaderHeight)
            return -1;
        // محاسبه هفته و روز هفته
        int week = (point.Y - HeaderHeight) / (DayHeight + DaySpacing);
        int dayOfWeek = point.X / (DayWidth + DaySpacing);
        if (week < 0 || week >= MaxWeeks || dayOfWeek < 0 || dayOfWeek >= DaysInWeek)
            return -1;
        // محاسبه شماره روز
        int day = (week * DaysInWeek) + dayOfWeek - _monthInfo.StartDayOfWeek + 1;
        // بررسی آیا روز در محدوده معتبر است
        if (day < 1 || day > _monthInfo.DaysInMonth)
            return -1;
        return day;
    }

    public void UpdateCalendar(int year, int month, int selectedDay, int todayDay)
    {
        _year = year;
        _month = month;
        _selectedDay = selectedDay;
        _todayDay = todayDay;
        UpdateMonthInfo();
        Invalidate();
    }
}

public class DaySelectedEventArgs : EventArgs
{
    public int Year { get; }
    public int Month { get; }
    public int Day { get; }

    public DaySelectedEventArgs(int year, int month, int day)
    {
        Year = year;
        Month = month;
        Day = day;
    }
}

public class CalendarForm : Form
{
    private const int CalendarWidth = 240;
    private const int CalendarHeight = 250;
    private static readonly Color HeaderBackColor = Color.FromArgb(41, 128, 185);
    private static readonly Font HeaderFont = new Font("Segoe UI", 11F, FontStyle.Bold);
    private Panel pnlHeader;
    private Label lblMonthYear;
    private Button btnPrev;
    private Button btnNext;
    private CalendarPanel calendarPanel;
    private Button btnToday;
    private PersianCalendar pc = new PersianCalendar();
    public DateTime SelectedDate { get; private set; }
    public int CurrentYear { get; private set; }
    public int CurrentMonth { get; private set; }

    public event EventHandler<DateSelectedEventArgs> DateSelected;

    public CalendarForm(DateTime initialDate)
    {
        SelectedDate = initialDate;
        CurrentYear = pc.GetYear(initialDate);
        CurrentMonth = pc.GetMonth(initialDate);
        InitializeComponents();
        SetupCalendar();
    }

    private void InitializeComponents()
    {
        this.pnlHeader = new Panel();
        this.lblMonthYear = new Label();
        this.btnPrev = new Button();
        this.btnNext = new Button();
        this.calendarPanel = new CalendarPanel();
        this.btnToday = new Button();
        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.Manual;
        this.Size = new Size(CalendarWidth, CalendarHeight);
        this.BackColor = Color.White;
        this.Padding = new Padding(1);
        this.DoubleBuffered = true;
        this.KeyPreview = true;
        this.KeyPress += CalendarForm_KeyPress;
        this.pnlHeader.BackColor = HeaderBackColor;
        this.pnlHeader.Dock = DockStyle.Top;
        this.pnlHeader.Height = 35;
        this.pnlHeader.Controls.Add(this.lblMonthYear);
        this.pnlHeader.Controls.Add(this.btnPrev);
        this.pnlHeader.Controls.Add(this.btnNext);
        this.lblMonthYear.AutoSize = false;
        this.lblMonthYear.TextAlign = ContentAlignment.MiddleCenter;
        this.lblMonthYear.Font = HeaderFont;
        this.lblMonthYear.ForeColor = Color.White;
        this.lblMonthYear.Dock = DockStyle.Fill;
        this.lblMonthYear.BringToFront();
        this.btnPrev.Text = "◀";
        this.btnPrev.FlatStyle = FlatStyle.Flat;
        this.btnPrev.ForeColor = Color.White;
        this.btnPrev.BackColor = Color.Transparent;
        this.btnPrev.Size = new Size(35, 35);
        this.btnPrev.Dock = DockStyle.Left;
        this.btnPrev.Font = HeaderFont;
        this.btnPrev.Cursor = Cursors.Hand;
        this.btnPrev.Click += btnPrev_Click;
        this.btnNext.Text = "▶";
        this.btnNext.FlatStyle = FlatStyle.Flat;
        this.btnNext.ForeColor = Color.White;
        this.btnNext.BackColor = Color.Transparent;
        this.btnNext.Size = new Size(35, 35);
        this.btnNext.Dock = DockStyle.Right;
        this.btnNext.Font = HeaderFont;
        this.btnNext.Cursor = Cursors.Hand;
        this.btnNext.Click += btnNext_Click;
        // تنظیمات پنل تقویم
        this.calendarPanel.Location = new Point(5, 40);
        this.calendarPanel.Size = new Size(230, 180);
        this.calendarPanel.BackColor = Color.White;
        this.calendarPanel.DaySelected += CalendarPanel_DaySelected;
        // تنظیمات دکمه امروز
        this.btnToday.Text = "امروز";
        this.btnToday.FlatStyle = FlatStyle.Flat;
        this.btnToday.ForeColor = Color.White;
        this.btnToday.BackColor = Color.FromArgb(46, 204, 113);
        this.btnToday.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        this.btnToday.Cursor = Cursors.Hand;
        this.btnToday.Size = new Size(230, 25);
        this.btnToday.Location = new Point(5, 225);
        this.btnToday.Click += btnToday_Click;
        this.Controls.Add(this.btnToday);
        this.Controls.Add(this.calendarPanel);
        this.Controls.Add(this.pnlHeader);
    }

    private void CalendarPanel_DaySelected(object sender, DaySelectedEventArgs e)
    {
        SelectedDate = pc.ToDateTime(e.Year, e.Month, e.Day, 0, 0, 0, 0);
        DateSelected?.Invoke(this, new DateSelectedEventArgs(SelectedDate));
        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private void CalendarForm_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == (char)Keys.Escape)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }

    public void UpdateCalendar(DateTime date)
    {
        SelectedDate = date;
        CurrentYear = pc.GetYear(date);
        CurrentMonth = pc.GetMonth(date);
        SetupCalendar();
    }

    private void SetupCalendar()
    {
        UpdateCalendarLabels();
        UpdateCalendarPanel();
    }

    private void UpdateCalendarLabels()
    {
        lblMonthYear.Text = $"{PersianDateConverter.PersianMonths[CurrentMonth - 1]} {CurrentYear}";
    }

    private void UpdateCalendarPanel()
    {
        DateTime today = DateTime.Now;
        int todayYear = pc.GetYear(today);
        int todayMonth = pc.GetMonth(today);
        int todayDay = pc.GetDayOfMonth(today);
        int selectedYear = pc.GetYear(SelectedDate);
        int selectedMonth = pc.GetMonth(SelectedDate);
        int selectedDay = pc.GetDayOfMonth(SelectedDate);
        calendarPanel.UpdateCalendar(
            CurrentYear,
            CurrentMonth,
            selectedDay,
            (CurrentYear == todayYear && CurrentMonth == todayMonth) ? todayDay : -1
        );
    }

    private void btnPrev_Click(object sender, EventArgs e)
    {
        CurrentMonth--;
        if (CurrentMonth < 1)
        {
            CurrentMonth = 12;
            CurrentYear--;
        }
        UpdateCalendarLabels();
        UpdateCalendarPanel();
    }

    private void btnNext_Click(object sender, EventArgs e)
    {
        CurrentMonth++;
        if (CurrentMonth > 12)
        {
            CurrentMonth = 1;
            CurrentYear++;
        }
        UpdateCalendarLabels();
        UpdateCalendarPanel();
    }

    private void btnToday_Click(object sender, EventArgs e)
    {
        UpdateCalendar(DateTime.Now);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            btnPrev.Click -= btnPrev_Click;
            btnNext.Click -= btnNext_Click;
            btnToday.Click -= btnToday_Click;
            this.KeyPress -= CalendarForm_KeyPress;
            calendarPanel.DaySelected -= CalendarPanel_DaySelected;
        }

        base.Dispose(disposing);
    }
}

public partial class PersianDateTimePicker : UserControl
{
    private TextBox txtDate;
    private Button btnCalendar;
    private DateTime _value = DateTime.Now;
    private bool initialized = false;
    private CalendarForm calendarForm;
    private PersianCalendar pc = new PersianCalendar();
    // کش کردن تاریخ‌های رایج برای افزایش سرعت
    private static readonly Dictionary<DateTime, string> persianDateCache = new Dictionary<DateTime, string>();

    [Browsable(true)]
    [Category("Behavior")]
    [Description("تاریخ انتخاب شده")]
    public DateTime Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                if (initialized)
                {
                    UpdateDisplay();
                    OnDateChanged(EventArgs.Empty);
                }
            }
        }
    }

    [Browsable(true)]
    [Category("Appearance")]
    [Description("رنگ دکمه تقویم")]
    public Color CalendarButtonColor { get; set; } = Color.FromArgb(41, 128, 185);

    [Browsable(true)]
    [Category("Behavior")]
    [Description("نمایش پیام خطای اعتبارسنجی")]
    public bool ShowValidationError { get; set; } = true;

    public event EventHandler DateChanged;
    public event EventHandler<DateValidationErrorEventArgs> DateValidationError;

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
        this.txtDate.Location = new Point(1, 1);
        this.txtDate.Size = new Size(70, 18);
        this.txtDate.BorderStyle = BorderStyle.None;
        this.txtDate.Font = new Font("Segoe UI", 8F);
        this.txtDate.TextAlign = HorizontalAlignment.Center;
        this.txtDate.ReadOnly = true;
        this.txtDate.BackColor = Color.White;
        this.txtDate.Leave += txtDate_Leave;



        this.btnCalendar.Location = new Point(71, 1);
        this.btnCalendar.Size = new Size(18, 18);
        this.btnCalendar.Text = "📅";
        this.btnCalendar.Font = new Font("Segoe UI Emoji", 8F);
        this.btnCalendar.FlatStyle = FlatStyle.Flat;
        this.btnCalendar.FlatAppearance.BorderSize = 0;
        this.btnCalendar.BackColor = CalendarButtonColor;
        this.btnCalendar.ForeColor = Color.White;
        this.btnCalendar.Cursor = Cursors.Hand;
        this.btnCalendar.Click += btnCalendar_Click;

        this.AutoScaleDimensions = new SizeF(6F, 13F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.Controls.Add(this.btnCalendar);
        this.Controls.Add(this.txtDate);
        this.Name = "PersianDateTimePicker";
        this.Size = new Size(90, 20);
        this.BackColor = Color.White;
        this.Paint += PersianDateTimePicker_Paint;
        // فعال‌سازی DoubleBuffering برای کاهش flicker
        this.DoubleBuffered = true;
    }

    private void PersianDateTimePicker_Paint(object sender, PaintEventArgs e)
    {
        Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
        using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1))
        {
            e.Graphics.DrawRectangle(pen, rect);
        }
        using (Pen pen = new Pen(Color.FromArgb(230, 230, 230), 1))
        {
            e.Graphics.DrawLine(pen, 70, 0, 70, this.Height);
        }
    }

    private void UpdateDisplay()
    {
        if (txtDate != null)
        {
            // استفاده از کش برای تاریخ‌های رایج
            if (persianDateCache.TryGetValue(_value, out string cachedPersianDate))
            {
                txtDate.Text = cachedPersianDate;
            }
            else
            {
                string persianDate = PersianDateConverter.ToPersianDateString(_value);
                txtDate.Text = persianDate;
                persianDateCache.Add(_value, persianDate);
            }
        }
    }

    private void btnCalendar_Click(object sender, EventArgs e)
    {
        if (calendarForm == null || calendarForm.IsDisposed)
        {
            calendarForm = new CalendarForm(_value);
            calendarForm.DateSelected += (s, args) =>
            {
                _value = args.SelectedDate;
                UpdateDisplay();
                OnDateChanged(EventArgs.Empty);
            };
        }
        else
        {
            // استفاده از متد عمومی برای به‌روزرسانی تقویم
            calendarForm.UpdateCalendar(_value);
        }

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

        // نمایش به صورت مودال برای جلوگیری از تعامل با فرم اصلی
        calendarForm.ShowDialog(this);
    }

    private void txtDate_Leave(object sender, EventArgs e)
    {
        var newDate = PersianDateConverter.ToGregorianDateTime(txtDate.Text);
        if (newDate.HasValue)
        {
            Value = newDate.Value;
            return;
        }

        var gregorianDate = PersianDateConverter.ParseGregorianDateTime(txtDate.Text);
        if (gregorianDate.HasValue)
        {
            Value = gregorianDate.Value;
        }
        else
        {
            var args = new DateValidationErrorEventArgs(txtDate.Text, "تاریخ وارد شده معتبر نیست");
            OnDateValidationError(args);

            if (ShowValidationError)
            {
                MessageBox.Show(args.ErrorMessage, "خطا در تاریخ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateDisplay();
        }
    }

    protected virtual void OnDateChanged(EventArgs e)
    {
        DateChanged?.Invoke(this, e);
    }

    protected virtual void OnDateValidationError(DateValidationErrorEventArgs e)
    {
        DateValidationError?.Invoke(this, e);
    }

    public string GetPersianDate()
    {
        return PersianDateConverter.ToPersianDateString(_value);
    }

    public string GetGregorianDate()
    {
        return PersianDateConverter.ToGregorianDateString(_value);
    }

    public bool SetPersianDate(string persianDate)
    {
        var newDate = PersianDateConverter.ToGregorianDateTime(persianDate);
        if (newDate.HasValue)
        {
            Value = newDate.Value;
            return true;
        }
        return false;
    }

    public bool SetGregorianDate(string gregorianDate)
    {
        var newDate = PersianDateConverter.ParseGregorianDateTime(gregorianDate);
        if (newDate.HasValue)
        {
            Value = newDate.Value;
            return true;
        }
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (calendarForm != null && !calendarForm.IsDisposed)
            {
                calendarForm.Dispose();
            }
        }
        base.Dispose(disposing);
    }
}

//string persianDate = persianDateTimePicker1.GetPersianDate();
//// خروجی: "1402/05/15"
//string gregorianDate = persianDateTimePicker1.GetGregorianDate();
//// خروجی: "2023/08/06"
//persianDateTimePicker1.SetPersianDate("1402/05/15");
//persianDateTimePicker1.SetGregorianDate("2023/08/06");
