using System.Drawing.Drawing2D;
using WeatherSystem.Domain.Enums;
using WeatherSystem.Services.DTOs;

namespace WeatherSystem.Desktop;

/// <summary>
/// Вікно ЛР7: клієнт для REST API, розгорнутого в хмарі Render (база PostgreSQL).
/// Показує статистику, картки пристроїв, пошук, додавання, увімкнення/вимкнення і видалення.
/// </summary>
public class MainForm : Form
{
    private static readonly Color Navy = Color.FromArgb(18, 58, 99);
    private static readonly Color Blue = Color.FromArgb(31, 95, 153);
    private static readonly Color Teal = Color.FromArgb(42, 138, 122);
    private static readonly Color Page = Color.FromArgb(243, 246, 250);
    private static readonly Color Line = Color.FromArgb(221, 229, 238);
    private static readonly Color Muted = Color.FromArgb(93, 109, 128);
    private static readonly Color OnGreen = Color.FromArgb(46, 139, 87);

    private readonly DevicesApiClient _api;
    private readonly string _apiUrl;
    private List<DeviceDto> _devices = new();

    private readonly Label lblStatus = new();
    private readonly Label sTotal = new(), sOn = new(), sTemp = new(), sHum = new();
    private readonly TextBox txtSearch = new();
    private readonly FlowLayoutPanel pnlCards = new();
    private readonly Label lblInfo = new();
    private readonly TextBox txtName = new();
    private readonly ComboBox cmbType = new();
    private readonly TextBox txtTemp = new();
    private readonly TextBox txtHum = new();
    private readonly Label lblError = new();

    public MainForm(DevicesApiClient api, string apiUrl)
    {
        _api = api;
        _apiUrl = apiUrl;
        BuildLayout();
        Shown += async (_, _) => await LoadDevicesAsync();
    }

    private void BuildLayout()
    {
        Text = "Метеосистема — ЛР7 (хмара Render + PostgreSQL)";
        Font = new Font("Segoe UI", 10F);
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1180, 740);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Page;

        // ---- Шапка з градієнтом
        var header = new Panel { Location = new Point(0, 0), Size = new Size(1180, 200) };
        header.Paint += (_, e) =>
        {
            using var brush = new LinearGradientBrush(header.ClientRectangle, Navy, Teal, 20F);
            e.Graphics.FillRectangle(brush, header.ClientRectangle);
        };
        var lblEyebrow = HeaderLabel("СИСТЕМА МОНІТОРИНГУ ПОГОДИ · ЛР 7", 9F, FontStyle.Regular, new Point(24, 16));
        var lblTitle = HeaderLabel("Пристрої метеосистеми", 20F, FontStyle.Bold, new Point(22, 36));
        var lblSub = HeaderLabel($"REST API у хмарі: {_apiUrl}", 9.5F, FontStyle.Regular, new Point(24, 78));

        lblStatus.Location = new Point(880, 20);
        lblStatus.Size = new Size(276, 32);
        lblStatus.TextAlign = ContentAlignment.MiddleCenter;
        lblStatus.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblStatus.ForeColor = Color.White;
        lblStatus.BackColor = Color.FromArgb(120, 255, 255, 255);
        lblStatus.Text = "Перевірка API…";

        header.Controls.AddRange(new Control[] { lblEyebrow, lblTitle, lblSub, lblStatus });
        string[] captions = { "усього пристроїв", "увімкнено", "середня температура", "середня вологість" };
        Label[] values = { sTotal, sOn, sTemp, sHum };
        for (int i = 0; i < 4; i++)
        {
            header.Controls.Add(StatBox(values[i], captions[i], new Point(24 + i * 284, 112)));
        }

        // ---- Ліва панель: пошук і картки
        var left = CardPanel(new Point(24, 216), new Size(800, 500));
        txtSearch.Location = new Point(16, 16);
        txtSearch.Size = new Size(470, 30);
        txtSearch.PlaceholderText = "Пошук за назвою, наприклад «датчик»";
        txtSearch.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await LoadDevicesAsync(); }
        };
        var btnSearch = FlatButton("Знайти", Blue, Color.White, new Point(496, 14), new Size(130, 34));
        btnSearch.Click += async (_, _) => await LoadDevicesAsync();
        var btnRefresh = FlatButton("Оновити", Color.White, Blue, new Point(636, 14), new Size(146, 34));
        btnRefresh.Click += async (_, _) => { txtSearch.Text = ""; await LoadDevicesAsync(); };

        lblInfo.Location = new Point(16, 56);
        lblInfo.Size = new Size(770, 22);
        lblInfo.ForeColor = Muted;
        lblInfo.Font = new Font("Segoe UI", 9F);

        pnlCards.Location = new Point(12, 82);
        pnlCards.Size = new Size(778, 406);
        pnlCards.AutoScroll = true;
        pnlCards.BackColor = Color.White;
        left.Controls.AddRange(new Control[] { txtSearch, btnSearch, btnRefresh, lblInfo, pnlCards });

        // ---- Права панель: новий пристрій
        var right = CardPanel(new Point(840, 216), new Size(316, 500));
        var lblAdd = new Label { Text = "Новий пристрій", Font = new Font("Segoe UI", 13F, FontStyle.Bold), Location = new Point(16, 14), AutoSize = true };
        var lblName = FieldLabel("Назва", new Point(16, 54));
        txtName.Location = new Point(16, 76);
        txtName.Size = new Size(282, 30);
        var lblType = FieldLabel("Тип", new Point(16, 116));
        cmbType.Location = new Point(16, 138);
        cmbType.Size = new Size(282, 30);
        cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbType.Items.AddRange(new object[] { "Датчик (Sensor)", "Виконавчий пристрій (Actuator)" });
        cmbType.SelectedIndex = 0;
        var lblT = FieldLabel("Температура, °C", new Point(16, 178));
        txtTemp.Location = new Point(16, 200);
        txtTemp.Size = new Size(134, 30);
        var lblH = FieldLabel("Вологість, %", new Point(164, 178));
        txtHum.Location = new Point(164, 200);
        txtHum.Size = new Size(134, 30);
        var btnAdd = FlatButton("Додати пристрій", Teal, Color.White, new Point(16, 248), new Size(282, 40));
        btnAdd.Click += async (_, _) => await AddDeviceAsync();
        lblError.Location = new Point(16, 296);
        lblError.Size = new Size(282, 90);
        lblError.ForeColor = Color.Firebrick;
        lblError.Font = new Font("Segoe UI", 9F);

        var lblHint = new Label
        {
            Text = "Дані зберігаються в базі PostgreSQL на Render.\nБезкоштовний сервер «засинає» без запитів, тому перше завантаження може тривати до хвилини.",
            Location = new Point(16, 396),
            Size = new Size(282, 92),
            ForeColor = Muted,
            Font = new Font("Segoe UI", 8.5F)
        };
        right.Controls.AddRange(new Control[] { lblAdd, lblName, txtName, lblType, cmbType, lblT, txtTemp, lblH, txtHum, btnAdd, lblError, lblHint });

        Controls.AddRange(new Control[] { header, left, right });
    }

    // ---------- Дані

    // GET api/devices (+ ?name=...), потім GET health/ready
    private async Task LoadDevicesAsync()
    {
        lblInfo.ForeColor = Muted;
        lblInfo.Text = "Завантаження… (сервер Render може прокидатися до хвилини)";
        UseWaitCursor = true;
        try
        {
            _devices = await _api.GetDevicesAsync(txtSearch.Text);
            lblInfo.Text = $"Отримано з API: {_devices.Count} пристр.   GET /api/devices{(string.IsNullOrWhiteSpace(txtSearch.Text) ? "" : "?name=" + txtSearch.Text.Trim())}";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _devices = new();
            lblInfo.ForeColor = Color.Firebrick;
            lblInfo.Text = "Сервер недоступний. Перевір інтернет або спробуй «Оновити» ще раз.";
        }
        finally
        {
            UseWaitCursor = false;
        }
        DrawCards();
        DrawStats();
        await CheckHealthAsync();
    }

    private async Task CheckHealthAsync()
    {
        try
        {
            var status = await _api.GetHealthAsync();
            bool ok = status == "Healthy";
            lblStatus.Text = ok ? "●  API і база даних: Healthy" : $"●  API: {status}";
            lblStatus.BackColor = ok ? OnGreen : Color.Firebrick;
        }
        catch (Exception)
        {
            lblStatus.Text = "●  API недоступний";
            lblStatus.BackColor = Color.Firebrick;
        }
    }

    private void DrawStats()
    {
        decimal? Avg(Func<DeviceDto, decimal?> pick)
        {
            var values = _devices.Select(pick).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            return values.Count == 0 ? null : values.Average();
        }
        sTotal.Text = _devices.Count.ToString();
        sOn.Text = _devices.Count(d => d.IsEnabled).ToString();
        sTemp.Text = Format(Avg(d => d.Temperature), " °C");
        sHum.Text = Format(Avg(d => d.Humidity), " %");
    }

    private void DrawCards()
    {
        pnlCards.SuspendLayout();
        pnlCards.Controls.Clear();
        if (_devices.Count == 0)
        {
            pnlCards.Controls.Add(new Label { Text = "Пристроїв немає", ForeColor = Muted, AutoSize = true, Margin = new Padding(8) });
        }
        foreach (var device in _devices)
        {
            pnlCards.Controls.Add(CreateCard(device));
        }
        pnlCards.ResumeLayout();
    }

    private Panel CreateCard(DeviceDto device)
    {
        bool on = device.IsEnabled;
        var card = new Panel
        {
            Size = new Size(368, 176),
            Margin = new Padding(6),
            BackColor = on ? Color.FromArgb(242, 250, 245) : Color.FromArgb(251, 252, 254)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(on ? Color.FromArgb(169, 216, 189) : Line, 1.5F);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            using var bar = new SolidBrush(on ? OnGreen : Color.FromArgb(170, 180, 190));
            e.Graphics.FillRectangle(bar, 0, 0, 5, card.Height);
        };

        var name = new Label { Text = device.Name, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Location = new Point(16, 10), Size = new Size(340, 24), AutoEllipsis = true };
        var type = Pill(device.Type == "Actuator" ? "Виконавчий пристрій" : "Датчик", Color.FromArgb(230, 238, 247), Blue, new Point(16, 38));
        var state = Pill(on ? "Увімкнений" : "Вимкнений", on ? Color.FromArgb(215, 240, 225) : Color.FromArgb(236, 239, 242), on ? OnGreen : Muted, new Point(type.Right + 6, 38));
        var readings = new Label
        {
            Text = $"Температура: {Format(device.Temperature, " °C")}     Вологість: {Format(device.Humidity, " %")}",
            Location = new Point(16, 70),
            Size = new Size(340, 22)
        };
        var updated = new Label
        {
            Text = $"Оновлено: {device.LastUpdated.ToLocalTime():dd.MM.yyyy HH:mm}",
            Location = new Point(16, 94),
            Size = new Size(340, 20),
            ForeColor = Muted,
            Font = new Font("Segoe UI", 8.5F)
        };
        var btnToggle = FlatButton(on ? "Вимкнути" : "Увімкнути", Color.White, Blue, new Point(16, 126), new Size(130, 34));
        btnToggle.Click += async (_, _) => await ToggleAsync(device);
        var btnDelete = FlatButton("Видалити", Color.White, Color.Firebrick, new Point(154, 126), new Size(120, 34));
        btnDelete.Click += async (_, _) => await DeleteAsync(device);

        card.Controls.AddRange(new Control[] { name, type, state, readings, updated, btnToggle, btnDelete });
        return card;
    }

    // PATCH api/devices/{id}/status
    private async Task ToggleAsync(DeviceDto device)
    {
        await Try(() => _api.SetStatusAsync(device.Id, !device.IsEnabled));
        await LoadDevicesAsync();
    }

    // DELETE api/devices/{id}
    private async Task DeleteAsync(DeviceDto device)
    {
        var answer = MessageBox.Show($"Видалити «{device.Name}»?", "Видалення", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;
        await Try(() => _api.DeleteAsync(device.Id));
        await LoadDevicesAsync();
    }

    // POST api/devices — відповідь 400 показується під формою
    private async Task AddDeviceAsync()
    {
        lblError.Text = "";
        if (!TryReadNumber(txtTemp.Text, out var temperature) || !TryReadNumber(txtHum.Text, out var humidity))
        {
            lblError.Text = "Температура і вологість мають бути числами (або порожні).";
            return;
        }
        var type = cmbType.SelectedIndex == 1 ? DeviceType.Actuator : DeviceType.Sensor;
        try
        {
            var (_, error) = await _api.CreateAsync(txtName.Text, type, temperature, humidity);
            if (error != null)
            {
                lblError.Text = error;
                return;
            }
            txtName.Text = txtTemp.Text = txtHum.Text = "";
            txtSearch.Text = "";
            await LoadDevicesAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            lblError.Text = "Сервер недоступний.";
        }
    }

    private static async Task Try(Func<Task<bool>> call)
    {
        try { await call(); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { }
    }

    // ---------- Допоміжні елементи вигляду

    private static Label HeaderLabel(string text, float size, FontStyle style, Point location) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", size, style),
        ForeColor = Color.White,
        BackColor = Color.Transparent,
        Location = location,
        AutoSize = true
    };

    private static Panel StatBox(Label value, string caption, Point location)
    {
        var box = new Panel { Location = location, Size = new Size(268, 70), BackColor = Color.FromArgb(55, 255, 255, 255) };
        value.Text = "–";
        value.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
        value.ForeColor = Color.White;
        value.BackColor = Color.Transparent;
        value.Location = new Point(12, 6);
        value.AutoSize = true;
        var text = new Label { Text = caption, ForeColor = Color.White, BackColor = Color.Transparent, Font = new Font("Segoe UI", 9F), Location = new Point(14, 44), AutoSize = true };
        box.Controls.AddRange(new Control[] { value, text });
        return box;
    }

    private static Panel CardPanel(Point location, Size size)
    {
        var panel = new Panel { Location = location, Size = size, BackColor = Color.White };
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(Line);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };
        return panel;
    }

    private static Label FieldLabel(string text, Point location) =>
        new() { Text = text, Location = location, AutoSize = true, ForeColor = Muted, Font = new Font("Segoe UI", 9F) };

    private static Label Pill(string text, Color back, Color fore, Point location)
    {
        var font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        var textSize = TextRenderer.MeasureText(text, font);
        return new Label
        {
            Text = text,
            BackColor = back,
            ForeColor = fore,
            Font = font,
            Location = location,
            Size = new Size(textSize.Width + 12, textSize.Height + 6),
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    private static Button FlatButton(string text, Color back, Color fore, Point location, Size size)
    {
        var button = new Button
        {
            Text = text,
            Location = location,
            Size = size,
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = fore,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = back == Color.White ? fore : back;
        return button;
    }

    private static bool TryReadNumber(string text, out decimal? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        bool ok = decimal.TryParse(text.Trim().Replace(',', '.'),
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var number);
        if (ok) value = number;
        return ok;
    }

    private static string Format(decimal? value, string unit) =>
        value.HasValue ? $"{value.Value:0.#}{unit}" : "—";
}
