using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinForms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using LiteDB;

namespace GreenEconomyApp;

public partial class Form1 : Form
{
    private Button btnEntra        = null!;
    private Button btnImpostazioni = null!;
    private Button btnChiudi       = null!;
    private PictureBox backgroundGif = null!;

    private Panel pnlDashboard = null!;
    private Button btnTabMappa = null!;
    private Button btnTabAnalisi = null!;
    private Button btnTabDati = null!;
    private Button btnExportCsv = null!;
    private Button btnExportDb = null!;
    private Button btnPulisciDb = null!;

    private Panel pnlMappa = null!;
    private Panel pnlAnalisi = null!;
    private Panel pnlDati = null!;

    private WebView2 mapControl = null!;

    private CartesianChart chartCorrelazione = null!;
    private CartesianChart chartPollutanti = null!;
    private PieChart chartDistribuzione = null!;
    private Label lblStatistiche = null!;

    private DataGridView dgvDati = null!;

    private readonly Font _fontTitolo = new("Segoe UI", 58, FontStyle.Bold);
    private readonly Font _fontSotto = new("Segoe UI", 13, FontStyle.Italic);

    // Trasforma le sigle tecniche delle API (es. p2, o3) in nomi leggibili per l'utente
    private string FormatPollutantName(string code) => code.ToLower() switch
    {
        "p2" => "PM2.5",
        "p1" => "PM10",
        "o3" => "Ozono (O3)",
        "n2" => "Biossido Azoto (NO2)",
        "s2" => "Anidride Solforosa (SO2)",
        "co" => "Monossido Carbonio (CO)",
        _ => code.ToUpper()
    };

    public Form1()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Green Economy App";
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.Black;

        InitializeWelcomeScreen();
        InitializeDashboard();

        Lang.OnLanguageChanged += AggiornaTesti;
        this.FormClosed += (s, e) => Lang.OnLanguageChanged -= AggiornaTesti;
    }

    // ══════════════════════════════════════════════
    //  WELCOME SCREEN
    // ══════════════════════════════════════════════

    private void InitializeWelcomeScreen()
    {
        backgroundGif = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.StretchImage,
            Image = Image.FromFile(Path.Combine(AppConstants.AssetsPath, "WelcomeScreenGif.gif"))
        };

        backgroundGif.Paint += BackgroundGif_Paint;
        Controls.Add(backgroundGif);

        // Bottoni Welcome
        btnEntra        = CreateWelcomeButton();
        btnImpostazioni = CreateWelcomeButton();
        btnChiudi       = CreateWelcomeButton();

        Controls.Add(btnEntra);
        Controls.Add(btnImpostazioni);
        Controls.Add(btnChiudi);
        btnEntra.BringToFront();
        btnImpostazioni.BringToFront();
        btnChiudi.BringToFront();

        this.Load += (s, e) =>
        {
            int cx = ClientSize.Width / 2 - 130;
            int cy = (int)(ClientSize.Height * 0.58f);
            btnEntra.Location        = new Point(cx, cy);
            btnImpostazioni.Location = new Point(cx, cy + 65);
            btnChiudi.Location       = new Point(cx, cy + 130);

            AddHoverAnimation(btnEntra);
            AddHoverAnimation(btnImpostazioni);
            AddHoverAnimation(btnChiudi);

            AggiornaTesti();
        };

        btnChiudi.Click += (s, e) => Application.Exit();
        btnEntra.Click  += BtnEntra_Click;
        btnImpostazioni.Click += (s, e) => { new FormImpostazioni().Show(); };
    }

    private void BackgroundGif_Paint(object? sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        // Overlay scuro
        using var overlayBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
        g.FillRectangle(overlayBrush, backgroundGif.ClientRectangle);

        // Titolo
        string titolo = Lang.Get("titolo");
        SizeF sizeTitolo = g.MeasureString(titolo, _fontTitolo);
        float xTitolo = (backgroundGif.Width - sizeTitolo.Width) / 2;
        float yTitolo = backgroundGif.Height * 0.22f;

        using var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 210, 180));
        g.DrawString(titolo, _fontTitolo, shadowBrush, xTitolo + 3, yTitolo + 3);
        g.DrawString(titolo, _fontTitolo, Brushes.White, xTitolo, yTitolo);

        // Linea decorativa
        float lineY = yTitolo + sizeTitolo.Height + 2;
        float lineX = backgroundGif.Width * 0.35f;
        float lineW = backgroundGif.Width * 0.30f;
        using var linePen = new Pen(Color.FromArgb(200, 0, 210, 180), 1.5f);
        g.DrawLine(linePen, lineX, lineY, lineX + lineW, lineY);

        // Sottotitolo
        string sotto = Lang.Get("sottotitolo");
        SizeF sizeSotto = g.MeasureString(sotto, _fontSotto);
        float xSotto = (backgroundGif.Width - sizeSotto.Width) / 2;
        float ySotto = lineY + 12;
        using var sottoBrush = new SolidBrush(Color.FromArgb(200, 180, 220, 215));
        g.DrawString(sotto, _fontSotto, sottoBrush, xSotto, ySotto);
    }

    private Button CreateWelcomeButton()
    {
        var btn = new Button
        {
            Size = new Size(260, 52),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 13, FontStyle.Regular),
            Cursor = Cursors.Hand,
            BackColor = Color.FromArgb(20, 0, 210, 180)
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(0, 210, 180);
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 0, 210, 180);
        return btn;
    }

    // ══════════════════════════════════════════════
    //  DASHBOARD
    // ══════════════════════════════════════════════

    private void InitializeDashboard()
    {
        pnlDashboard = new Panel
        {
            Size = new Size(1000, 700),
            BackColor = Color.FromArgb(250, 15, 15, 15),
            Visible = false,
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(pnlDashboard);
        pnlDashboard.BringToFront();

        // === TABS ===
        btnTabMappa = CreateTabButton(Lang.Get("tab_mappa"), new Point(20, 20));
        btnTabAnalisi = CreateTabButton(Lang.Get("tab_analisi"), new Point(230, 20));
        btnTabDati = CreateTabButton(Lang.Get("tab_dati"), new Point(440, 20));

        btnTabMappa.Click += (s, e) => ShowTab("mappa");
        btnTabAnalisi.Click += (s, e) => { ShowTab("analisi"); LoadRegionalData(); };
        btnTabDati.Click += async (s, e) => { ShowTab("dati"); await RefreshDataGridAsync(); };

        pnlDashboard.Controls.Add(btnTabMappa);
        pnlDashboard.Controls.Add(btnTabAnalisi);
        pnlDashboard.Controls.Add(btnTabDati);

        // === BOTTONI AZIONE ===
        btnExportCsv = CreateActionButton("CSV", new Point(pnlDashboard.Width - 380, 20), Color.FromArgb(0, 210, 180));
        btnExportCsv.Click += (s, e) =>
        {
            using var sfd = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = $"dataset_greeneconomy_{DateTime.Now:yyyyMMdd}.csv" };
            if (sfd.ShowDialog() == DialogResult.OK)
                MessageBox.Show(DataExporter.ExportCsv(sfd.FileName));
        };
        pnlDashboard.Controls.Add(btnExportCsv);

        btnExportDb = CreateActionButton("DB", new Point(pnlDashboard.Width - 280, 20), Color.FromArgb(0, 210, 180));
        btnExportDb.Click += (s, e) =>
        {
            using var sfd = new SaveFileDialog { Filter = "Database files (*.db)|*.db", FileName = $"dataset_greeneconomy_{DateTime.Now:yyyyMMdd}.db" };
            if (sfd.ShowDialog() == DialogResult.OK)
                MessageBox.Show(DataExporter.ExportDatabase(sfd.FileName));
        };
        pnlDashboard.Controls.Add(btnExportDb);

        btnPulisciDb = CreateActionButton(Lang.Get("pulisci_db"), new Point(pnlDashboard.Width - 180, 20), Color.OrangeRed);
        btnPulisciDb.Click += (s, e) =>
        {
            if (MessageBox.Show(Lang.Get("conferma_pulizia"), Lang.Get("pulisci_db"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                using (var db = new LiteDatabase(AppConstants.DbPath))
                    db.DropCollection(AppConstants.DbCollectionName);
                mapControl.CoreWebView2?.ExecuteScriptAsync("window.clearMarkers();");
                LoadRegionalData();
            }
        };
        pnlDashboard.Controls.Add(btnPulisciDb);

        // Bottone chiudi
        Button btnBack = new Button
        {
            Text = "X", Size = new Size(40, 40),
            Location = new Point(pnlDashboard.Width - 50, 20),
            FlatStyle = FlatStyle.Flat, ForeColor = Color.Red
        };
        btnBack.Click += (s, e) => pnlDashboard.Visible = false;
        pnlDashboard.Controls.Add(btnBack);

        // === PANNELLO MAPPA ===
        InitializeMapPanel();

        // === PANNELLO ANALISI ===
        InitializeAnalysisPanel();

        // === PANNELLO TABELLA DATI ===
        InitializeDataPanel();

        pnlDashboard.Controls.Add(pnlMappa);
        pnlDashboard.Controls.Add(pnlAnalisi);
        pnlDashboard.Controls.Add(pnlDati);

        this.Resize += (s, e) =>
        {
            pnlDashboard.Location = new Point(
                (ClientSize.Width - pnlDashboard.Width) / 2,
                (ClientSize.Height - pnlDashboard.Height) / 2);
        };
    }

    private void InitializeMapPanel()
    {
        pnlMappa = new Panel { Size = new Size(960, 600), Location = new Point(20, 80) };
        mapControl = new WebView2 { Dock = DockStyle.Fill };
        pnlMappa.Controls.Add(mapControl);
        _ = InitWebView();
    }

    private void InitializeAnalysisPanel()
    {
        pnlAnalisi = new Panel { Size = new Size(960, 600), Location = new Point(20, 80), Visible = false };

        // 1. Grafico Correlazione Temp/AQI (Line)
        var lblCorr = new Label
        {
            Text = "Relazione Temperatura / AQI",
            ForeColor = Color.FromArgb(0, 210, 180),
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            AutoSize = true, Location = new Point(10, 0)
        };
        chartCorrelazione = new CartesianChart
        {
            Size = new Size(580, 220), Location = new Point(10, 30),
            BackColor = Color.FromArgb(30, 30, 30)
        };

        // 2. Distribuzione Qualità Aria (Pie)
        var lblDist = new Label
        {
            Text = "Stato Globale Aria",
            ForeColor = Color.FromArgb(0, 210, 180),
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            AutoSize = true, Location = new Point(610, 0)
        };
        chartDistribuzione = new PieChart
        {
            Size = new Size(340, 220), Location = new Point(610, 30),
            BackColor = Color.FromArgb(30, 30, 30)
        };

        // Riepilogo statistico (testuale)
        lblStatistiche = new Label
        {
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11),
            Location = new Point(10, 260),
            Size = new Size(940, 45),
            BackColor = Color.FromArgb(25, 25, 25),
            Padding = new Padding(10, 5, 10, 5),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // 3. Impatto Inquinanti (Bar)
        var lblPoll = new Label
        {
            Text = "Frequenza Inquinanti Rilevati",
            ForeColor = Color.FromArgb(0, 210, 180),
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            AutoSize = true, Location = new Point(10, 315)
        };
        chartPollutanti = new CartesianChart
        {
            Size = new Size(940, 240), Location = new Point(10, 345),
            BackColor = Color.FromArgb(30, 30, 30)
        };

        pnlAnalisi.Controls.Add(lblCorr);
        pnlAnalisi.Controls.Add(chartCorrelazione);
        pnlAnalisi.Controls.Add(lblDist);
        pnlAnalisi.Controls.Add(chartDistribuzione);
        pnlAnalisi.Controls.Add(lblStatistiche);
        pnlAnalisi.Controls.Add(lblPoll);
        pnlAnalisi.Controls.Add(chartPollutanti);
    }

    private void InitializeDataPanel()
    {
        pnlDati = new Panel { Size = new Size(960, 600), Location = new Point(20, 80), Visible = false };

        dgvDati = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.FromArgb(20, 20, 20),
            ForeColor = Color.White,
            GridColor = Color.FromArgb(50, 50, 50),
            BorderStyle = BorderStyle.None,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(0, 150, 130),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(25, 25, 25),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                SelectionBackColor = Color.FromArgb(0, 100, 90),
                SelectionForeColor = Color.White
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(35, 35, 35),
                ForeColor = Color.White
            },
            EnableHeadersVisualStyles = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        // Definisci colonne manualmente per controllo totale
        dgvDati.Columns.Add("colTimestamp", "Data/Ora");
        dgvDati.Columns.Add("colCity", "Città");
        dgvDati.Columns.Add("colRegion", "Regione");
        dgvDati.Columns.Add("colTemp", "Temp (°C)");
        dgvDati.Columns.Add("colAQI", "AQI");
        dgvDati.Columns.Add("colPollutant", "Inquinante");
        dgvDati.Columns.Add("colTrend", "Trend");

        pnlDati.Controls.Add(dgvDati);
    }

    // ══════════════════════════════════════════════
    //  WEBVIEW2 — Mappa
    // ══════════════════════════════════════════════

    private async Task InitWebView()
    {
        try
        {
            var userDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebView2Data");
            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await mapControl.EnsureCoreWebView2Async(env);

            mapControl.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            string mapPath = Path.Combine(AppConstants.AssetsPath, "map.html");
            if (File.Exists(mapPath))
                mapControl.CoreWebView2.Navigate(new Uri(mapPath).AbsoluteUri);
            else
                System.Diagnostics.Debug.WriteLine("[Form1] map.html non trovato: " + mapPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Form1] Errore InitWebView: {ex.Message}");
        }
    }

    private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var msg = e.TryGetWebMessageAsString();
            var data = System.Text.Json.JsonSerializer.Deserialize<MapMessage>(msg);
            if (data == null) return;

            if (data.type == "ready")
            {
                LoadHistoryMarkersFromDb();
                return;
            }

            if (data.type == "coords" && data.lat != 0)
            {
                var imp = FormImpostazioni.CaricaImpostazioni();
                var record = await WeatherAirService.Instance.GetCombinedDataByCoordsAsync(
                    data.lat, data.lng, imp.ApiKey, imp.ApiKeyIQAir);

                if (record != null)
                    AddMarkerToWebView(record, true);
                else
                    await mapControl.CoreWebView2.ExecuteScriptAsync("window.hideLoader();");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Form1] Errore WebMessage: {ex.Message}");
            await mapControl.CoreWebView2.ExecuteScriptAsync("window.hideLoader();");
        }
    }

    private void AddMarkerToWebView(GreenEconomyRecord record, bool doFly = true)
    {
        string color = record.AQI >= 100 ? "red" : record.AQI >= 50 ? "yellow" : "green";

        string popup = $"<b style=\"font-size:16px;\">{record.City}</b><br/><br/>" +
                       $"Temp: <b>{record.Temperature:F1}°C</b><br/>" +
                       $"AQI: <b>{record.AQI}</b> ({record.MainPollutant})<br/>" +
                       $"Trend: <i>{record.TrendInfo}</i>";
        popup = popup.Replace("'", "\\'");

        string latStr = record.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string lngStr = record.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);

        string script = $"window.addMarker({latStr}, {lngStr}, '{color}', '{popup}', {doFly.ToString().ToLower()});";
        mapControl.CoreWebView2.ExecuteScriptAsync(script);
    }

    private void LoadHistoryMarkersFromDb()
    {
        using var db = new LiteDatabase(AppConstants.DbPath);
        var history = db.GetCollection<GreenEconomyRecord>(AppConstants.DbCollectionName).FindAll().ToList();

        var recentHistory = history
            .GroupBy(x => x.City)
            .Select(x => x.OrderByDescending(t => t.Timestamp).First());

        foreach (var h in recentHistory)
        {
            if (h.Latitude != 0 && h.Longitude != 0)
                AddMarkerToWebView(h, false);
        }
    }

    // ══════════════════════════════════════════════
    //  NAVIGAZIONE TABS
    // ══════════════════════════════════════════════

    private void ShowTab(string tab)
    {
        pnlMappa.Visible = tab == "mappa";
        pnlAnalisi.Visible = tab == "analisi";
        pnlDati.Visible = tab == "dati";

        SetTabActive(btnTabMappa, tab == "mappa");
        SetTabActive(btnTabAnalisi, tab == "analisi");
        SetTabActive(btnTabDati, tab == "dati");
    }

    private void SetTabActive(Button btn, bool active)
    {
        btn.FlatAppearance.BorderColor = active ? Color.FromArgb(0, 210, 180) : Color.Gray;
        btn.ForeColor = active ? Color.FromArgb(0, 210, 180) : Color.White;
    }

    private void BtnEntra_Click(object? sender, EventArgs e)
    {
        pnlDashboard.Visible = true;
        ShowTab("mappa");
    }

    // ══════════════════════════════════════════════
    //  TABELLA DATI (Requisito: visualizzazione tabellare)
    // ══════════════════════════════════════════════

    private async Task RefreshDataGridAsync()
    {
        dgvDati.Rows.Clear();
        
        using var db = new LiteDatabase(AppConstants.DbPath);
        var records = db.GetCollection<GreenEconomyRecord>(AppConstants.DbCollectionName)
                        .FindAll()
                        .OrderByDescending(x => x.Timestamp)
                        .ToList();
        
        if (records.Count == 0)
        {
             dgvDati.Rows.Add(Lang.Get("stats_no_data") ?? "Nessun dato trovato nel database.", "", "", "", "", "", "");
             return;
        }

        foreach (var r in records)
        {
            dgvDati.Rows.Add(
                r.Timestamp.ToString("dd/MM/yyyy HH:mm"),
                r.City,
                r.Region,
                r.Temperature.ToString("F1"),
                r.AQI,
                FormatPollutantName(r.MainPollutant),
                r.TrendInfo
            );
        }
    }

    // ══════════════════════════════════════════════
    //  ANALISI STATISTICA (Requisiti: grafici, riepiloghi, correlazione)
    // ══════════════════════════════════════════════

    private void LoadRegionalData()
    {
        UpdateCorrelazioneChart();
        UpdateStatistiche();

        using var db = new LiteDatabase(AppConstants.DbPath);
        var records = db.GetCollection<GreenEconomyRecord>(AppConstants.DbCollectionName).FindAll().ToList();

        if (records.Count > 0)
        {
            // 1. AQI Distribution (Pie Chart)
            var distribution = new[]
            {
                new { Label = "Ottima (0-50)", Value = records.Count(x => x.AQI <= 50), Color = SkiaSharp.SKColors.LightGreen },
                new { Label = "Moderata (51-100)", Value = records.Count(x => x.AQI > 50 && x.AQI <= 100), Color = SkiaSharp.SKColors.Yellow },
                new { Label = "Scarsa (100+)", Value = records.Count(x => x.AQI > 100), Color = SkiaSharp.SKColors.OrangeRed }
            }.Where(x => x.Value > 0).ToList();

            chartDistribuzione.Series = distribution.Select(d => new PieSeries<double>
            {
                Values = new double[] { d.Value },
                Name = d.Label,
                Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(d.Color),
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsSize = 12,
                DataLabelsPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.Black),
                DataLabelsFormatter = point => $"{d.Label}: {point.Coordinate.PrimaryValue}"
            }).ToArray();

            // 2. Pollutant Frequency (Bar Chart)
            var pollutants = records
                .Where(x => !string.IsNullOrEmpty(x.MainPollutant) && x.MainPollutant != "N/A")
                .GroupBy(x => x.MainPollutant)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            chartPollutanti.Series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = pollutants.Select(p => (double)p.Count).ToArray(),
                    Name = "Frequenza",
                    Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.Aquamarine),
                    MaxBarWidth = 45,
                    DataLabelsPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.White),
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
                }
            };
            chartPollutanti.XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = pollutants.Select(p => FormatPollutantName(p.Name)).ToArray(),
                    LabelsPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.LightGray),
                    TextSize = 14
                }
            };
            chartPollutanti.YAxes = new Axis[]
            {
                new Axis
                {
                    LabelsPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.LightGray),
                    TextSize = 12,
                    MinLimit = 0
                }
            };
        }
    }

    private void UpdateCorrelazioneChart()
    {
        using var db = new LiteDatabase(AppConstants.DbPath);
        var history = db.GetCollection<GreenEconomyRecord>(AppConstants.DbCollectionName)
                        .FindAll()
                        .OrderBy(x => x.Temperature)
                        .ToList();

        if (history.Count == 0) return;

        chartCorrelazione.Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = history.Select(h => h.Temperature).ToArray(),
                Name = "Temp (°C)",
                Fill = null,
                Stroke = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.Yellow) { StrokeThickness = 3 },
                GeometryFill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.Yellow),
                GeometryStroke = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.Yellow),
                GeometrySize = 6
            },
            new LineSeries<int>
            {
                Values = history.Select(h => h.AQI).ToArray(),
                Name = "AQI",
                Fill = null,
                Stroke = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.Cyan) { StrokeThickness = 3 },
                GeometryFill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.Cyan),
                GeometryStroke = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.Cyan),
                GeometrySize = 6
            }
        };

        chartCorrelazione.XAxes = new Axis[]
        {
            new Axis
            {
                Labels = history.Select(h => $"{h.City}\n{h.Temperature:F1}°C").ToArray(),
                LabelsPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.LightGray),
                TextSize = 10, LabelsRotation = 15
            }
        };
        chartCorrelazione.YAxes = new Axis[]
        {
            new Axis
            {
                LabelsPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.LightGray),
                TextSize = 12
            }
        };
    }

    /// <summary>
    /// Calcola e mostra riepilogo statistico con coefficiente di correlazione di Pearson.
    /// Soddisfa il requisito: "individuare possibili relazioni tra temperatura e inquinamento".
    /// </summary>
    private void UpdateStatistiche()
    {
        using var db = new LiteDatabase(AppConstants.DbPath);
        var records = db.GetCollection<GreenEconomyRecord>(AppConstants.DbCollectionName)
                        .FindAll().ToList();

        if (records.Count < 2)
        {
            lblStatistiche.Text = Lang.Get("stats_no_data");
            return;
        }

        double avgTemp = records.Average(r => r.Temperature);
        double avgAqi = records.Average(r => r.AQI);
        double minTemp = records.Min(r => r.Temperature);
        double maxTemp = records.Max(r => r.Temperature);
        int minAqi = records.Min(r => r.AQI);
        int maxAqi = records.Max(r => r.AQI);

        // Coefficiente di correlazione di Pearson (r) tra Temperatura e AQI
        double r = CalcolaPearson(
            records.Select(x => x.Temperature).ToArray(),
            records.Select(x => (double)x.AQI).ToArray()
        );

        string interpretazione = Math.Abs(r) switch
        {
            < 0.2 => Lang.Get("corr_nessuna"),
            < 0.5 => Lang.Get("corr_debole"),
            < 0.7 => Lang.Get("corr_moderata"),
            _ => Lang.Get("corr_forte")
        };

        lblStatistiche.Text =
            $"📊 {Lang.Get("stats_campioni")}: {records.Count}  |  " +
            $"🌡 Temp: {avgTemp:F1}°C (min {minTemp:F1}, max {maxTemp:F1})  |  " +
            $"💨 AQI: {avgAqi:F0} (min {minAqi}, max {maxAqi})  |  " +
            $"📈 Pearson r = {r:F3} → {interpretazione}";
    }

    /// <summary>
    /// Calcola il coefficiente di correlazione di Pearson tra due serie di valori.
    /// r = 1 → correlazione positiva perfetta, r = -1 → negativa perfetta, r ≈ 0 → nessuna.
    /// </summary>
    private static double CalcolaPearson(double[] x, double[] y)
    {
        if (x.Length != y.Length || x.Length < 2) return 0;

        int n = x.Length;
        double avgX = x.Average();
        double avgY = y.Average();

        double sumXY = 0, sumX2 = 0, sumY2 = 0;
        for (int i = 0; i < n; i++)
        {
            double dx = x[i] - avgX;
            double dy = y[i] - avgY;
            sumXY += dx * dy;
            sumX2 += dx * dx;
            sumY2 += dy * dy;
        }

        double denominator = Math.Sqrt(sumX2 * sumY2);
        return denominator == 0 ? 0 : sumXY / denominator;
    }

    // ══════════════════════════════════════════════
    //  UI HELPERS
    // ══════════════════════════════════════════════

    private void AggiornaTesti()
    {
        if (InvokeRequired) { Invoke(AggiornaTesti); return; }

        btnEntra.Text        = Lang.Get("entra");
        btnImpostazioni.Text = Lang.Get("impostazioni");
        btnChiudi.Text       = Lang.Get("chiudi");

        if (btnTabMappa != null) btnTabMappa.Text = Lang.Get("tab_mappa");
        if (btnTabAnalisi != null) btnTabAnalisi.Text = Lang.Get("tab_analisi");
        if (btnTabDati != null) btnTabDati.Text = Lang.Get("tab_dati");
        if (btnPulisciDb != null) btnPulisciDb.Text = Lang.Get("pulisci_db");

        backgroundGif.Invalidate();
    }

    private void AddHoverAnimation(Button btn)
    {
        Font normalFont = new Font("Segoe UI", 13, FontStyle.Regular);
        Font hoverFont  = new Font("Segoe UI", 14, FontStyle.Bold);

        btn.MouseEnter += (s, e) =>
        {
            btn.Font = hoverFont;
            btn.FlatAppearance.BorderSize = 2;
            btn.ForeColor = Color.FromArgb(0, 210, 180);
            btn.BackColor = Color.FromArgb(50, 0, 210, 180);
        };

        btn.MouseLeave += (s, e) =>
        {
            btn.Font = normalFont;
            btn.FlatAppearance.BorderSize = 1;
            btn.ForeColor = Color.White;
            btn.BackColor = Color.FromArgb(20, 0, 210, 180);
        };
    }

    private Button CreateTabButton(string text, Point location)
    {
        var btn = new Button
        {
            Text = text, Size = new Size(200, 40), Location = location,
            FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Color.Gray;
        return btn;
    }

    private Button CreateActionButton(string text, Point location, Color borderColor)
    {
        var btn = new Button
        {
            Text = text, Size = new Size(90, 40), Location = location,
            FlatStyle = FlatStyle.Flat, ForeColor = Color.White
        };
        btn.FlatAppearance.BorderColor = borderColor;
        return btn;
    }
}