using System.Text.Json;

namespace GreenEconomyApp;

public partial class FormImpostazioni : Form
{
    private Label lblTitolo = null!;
    private Label lblLingua = null!;
    private Label lblUnita = null!;
    private Label lblApiKey = null!;
    private Label lblApiKeyIQ = null!;
    private Label lblCitta = null!;
    private ComboBox cmbLingua = null!;
    private ComboBox cmbUnita = null!;
    private TextBox txtApiKey = null!;
    private TextBox txtApiKeyIQ = null!;
    private TextBox txtCitta = null!;
    private Button btnSalva = null!;
    private Button btnIndietro = null!;

    private bool _aggiornandoLingua = false;

    private void InitializeComponent()
    {
        Text = "Impostazioni";
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(13, 13, 13);

        // TITOLO
        lblTitolo = new Label
        {
            Font = new Font("Segoe UI", 32, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            Height = 100,
            TextAlign = ContentAlignment.BottomCenter
        };
        Controls.Add(lblTitolo);

        // LINEA DECORATIVA
        Panel lineaDec = new Panel
        {
            BackColor = Color.FromArgb(0, 210, 180),
            Height = 1,
            Dock = DockStyle.Top
        };
        Controls.Add(lineaDec);

        // PANNELLO CENTRALE
        Panel pannello = new Panel
        {
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill
        };
        Controls.Add(pannello);

        this.Load += (s, e) =>
        {
            int cx = ClientSize.Width / 2 - 200;
            int cy = 160;
            int gap = 90;

            Impostazioni imp = CaricaImpostazioni();

            // --- LINGUA ---
            lblLingua = new Label { AutoSize = true, Location = new Point(cx, cy) };
            StilizzaLabel(lblLingua);
            pannello.Controls.Add(lblLingua);

            cmbLingua = new ComboBox();
            StilizzaCombo(cmbLingua, cx, cy + 35);
            AggiornaVociLingua();
            cmbLingua.SelectedIndexChanged += async (s, e) =>
            {
                if (_aggiornandoLingua) return;
                string codice = cmbLingua.SelectedIndex switch
                {
                    1 => "en", 2 => "es", 3 => "zh", 4 => "hi", _ => "it"
                };
                await MostraCaricamentoECambia(codice);
            };
            pannello.Controls.Add(cmbLingua);

            // --- UNITÀ DI MISURA ---
            lblUnita = new Label { AutoSize = true, Location = new Point(cx, cy + gap) };
            StilizzaLabel(lblUnita);
            pannello.Controls.Add(lblUnita);

            cmbUnita = new ComboBox();
            cmbUnita.Items.AddRange(new string[] { "°C - Celsius", "°F - Fahrenheit" });
            cmbUnita.SelectedItem = imp.UnitaMisura;
            if (cmbUnita.SelectedIndex == -1) cmbUnita.SelectedIndex = 0;
            StilizzaCombo(cmbUnita, cx, cy + gap + 35);
            pannello.Controls.Add(cmbUnita);

            // --- API KEY ---
            lblApiKey = new Label { AutoSize = true, Location = new Point(cx, cy + gap * 2) };
            StilizzaLabel(lblApiKey);
            pannello.Controls.Add(lblApiKey);

            txtApiKey = new TextBox { Text = imp.ApiKey, PlaceholderText = "Es. 82f6..." };
            StilizzaTextBox(txtApiKey, cx, cy + gap * 2 + 35);
            pannello.Controls.Add(txtApiKey);

            // --- API KEY IQAir ---
            lblApiKeyIQ = new Label { AutoSize = true, Location = new Point(cx, cy + (int)(gap * 2.8)) };
            StilizzaLabel(lblApiKeyIQ);
            pannello.Controls.Add(lblApiKeyIQ);

            txtApiKeyIQ = new TextBox { Text = imp.ApiKeyIQAir, PlaceholderText = "Es. 47a1..." };
            StilizzaTextBox(txtApiKeyIQ, cx, cy + (int)(gap * 2.8) + 35);
            pannello.Controls.Add(txtApiKeyIQ);

            // --- CITTÀ DI DEFAULT ---
            lblCitta = new Label { AutoSize = true, Location = new Point(cx, cy + (int)(gap * 3.8)) };
            StilizzaLabel(lblCitta);
            pannello.Controls.Add(lblCitta);

            txtCitta = new TextBox { Text = imp.CittaDefault, PlaceholderText = "Es. Roma, Milano, New York..." };
            StilizzaTextBox(txtCitta, cx, cy + (int)(gap * 3.8) + 35);
            pannello.Controls.Add(txtCitta);

            // --- BOTTONI ---
            btnSalva = new Button { Size = new Size(180, 48), Location = new Point(cx, cy + (int)(gap * 4.8) + 20) };
            StilizzaBottone(btnSalva);
            btnSalva.Click += (s, e) =>
            {
                SalvaImpostazioni(new Impostazioni
                {
                    Lingua = Lang.DisplayFromCodice(Lang.LinguaCorrente),
                    UnitaMisura = cmbUnita.SelectedItem?.ToString() ?? "°C - Celsius",
                    ApiKey = txtApiKey.Text,
                    ApiKeyIQAir = txtApiKeyIQ.Text,
                    CittaDefault = txtCitta.Text
                });
                MessageBox.Show(Lang.Get("salvato"));
            };
            pannello.Controls.Add(btnSalva);

            btnIndietro = new Button { Size = new Size(180, 48), Location = new Point(cx + 220, cy + (int)(gap * 4.8) + 20) };
            StilizzaBottone(btnIndietro);
            btnIndietro.Click += (s, e) => this.Close();
            pannello.Controls.Add(btnIndietro);

            AggiornaTesti();
        };

        Lang.OnLanguageChanged += AggiornaTesti;
        this.FormClosed += (s, e) => Lang.OnLanguageChanged -= AggiornaTesti;
    }

    private void AggiornaTesti()
    {
        if (InvokeRequired) { Invoke(AggiornaTesti); return; }
        lblTitolo.Text   = Lang.Get("impostazioni_titolo");
        lblLingua.Text   = Lang.Get("lingua");
        lblUnita.Text    = Lang.Get("unita_misura");
        lblApiKey.Text   = Lang.Get("api_key_owm");
        lblApiKeyIQ.Text = Lang.Get("api_key_iq");
        lblCitta.Text    = Lang.Get("citta_default");
        btnSalva.Text    = Lang.Get("salva");
        btnIndietro.Text = Lang.Get("indietro");
        if (cmbLingua != null) AggiornaVociLingua();
    }

    private void AggiornaVociLingua()
    {
        _aggiornandoLingua = true;

        var voci = Lang.LinguaCorrente switch
        {
            "en" => new[] { "Italian", "English", "Spanish", "Chinese", "Hindi" },
            "es" => new[] { "Italiano", "Inglés", "Español", "Chino", "Hindi" },
            "zh" => new[] { "意大利语", "英语", "西班牙语", "中文", "印地语" },
            "hi" => new[] { "इतालवी", "अंग्रेज़ी", "स्पेनिश", "चीनी", "हिंदी" },
            _    => new[] { "Italiano", "English", "Español", "中文", "Hindi" }
        };

        int indiceCorrente = Lang.LinguaCorrente switch
        {
            "en" => 1, "es" => 2, "zh" => 3, "hi" => 4, _ => 0
        };

        cmbLingua.Items.Clear();
        cmbLingua.Items.AddRange(voci);
        cmbLingua.SelectedIndex = indiceCorrente;

        _aggiornandoLingua = false;
    }

    private async Task MostraCaricamentoECambia(string codice)
    {
        string testoCaricamento = codice switch
        {
            "en" => "Loading...", "es" => "Cargando...",
            "zh" => "加载中...", "hi" => "लोड हो रहा है...",
            _ => "Caricamento..."
        };

        Form frmLoad = new Form
        {
            Size = new Size(300, 120),
            StartPosition = FormStartPosition.Manual,
            Location = new Point(
                this.Location.X + (this.Width - 300) / 2,
                this.Location.Y + (this.Height - 120) / 2),
            FormBorderStyle = FormBorderStyle.None,
            BackColor = Color.FromArgb(20, 20, 20)
        };

        Label lbl = new Label
        {
            Text = testoCaricamento,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 210, 180),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        frmLoad.Controls.Add(lbl);
        frmLoad.Show(this);

        await Task.Delay(600);
        await Lang.CambiaLingua(codice);

        frmLoad.Close();
    }

    /// <summary>Carica le impostazioni da disco. Restituisce valori di default se il file non esiste.</summary>
    public static Impostazioni CaricaImpostazioni()
    {
        try
        {
            if (!File.Exists(AppConstants.SettingsFile))
                return new Impostazioni();
            string json = File.ReadAllText(AppConstants.SettingsFile);
            return JsonSerializer.Deserialize<Impostazioni>(json) ?? new Impostazioni();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore caricamento impostazioni: {ex.Message}\nVerranno usati i valori predefiniti.");
            return new Impostazioni();
        }
    }

    /// <summary>Salva le impostazioni su disco in formato JSON.</summary>
    public static void SalvaImpostazioni(Impostazioni imp)
    {
        try
        {
            Directory.CreateDirectory(AppConstants.SettingsFolder);
            string json = JsonSerializer.Serialize(imp, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(AppConstants.SettingsFile, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore salvataggio impostazioni: {ex.Message}");
        }
    }

    private void StilizzaLabel(Label l)
    {
        l.Font = new Font("Segoe UI", 11, FontStyle.Regular);
        l.ForeColor = Color.FromArgb(0, 210, 180);
        l.BackColor = Color.Transparent;
    }

    private void StilizzaCombo(ComboBox c, int x, int y)
    {
        c.Size = new Size(400, 35);
        c.Location = new Point(x, y);
        c.BackColor = Color.FromArgb(30, 30, 30);
        c.ForeColor = Color.White;
        c.Font = new Font("Segoe UI", 11);
        c.FlatStyle = FlatStyle.Flat;
        c.DropDownStyle = ComboBoxStyle.DropDownList;
    }

    private void StilizzaTextBox(TextBox t, int x, int y)
    {
        t.Size = new Size(400, 35);
        t.Location = new Point(x, y);
        t.BackColor = Color.FromArgb(30, 30, 30);
        t.ForeColor = Color.White;
        t.Font = new Font("Segoe UI", 11);
        t.BorderStyle = BorderStyle.FixedSingle;
    }

    private void StilizzaBottone(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderColor = Color.FromArgb(0, 210, 180);
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 0, 210, 180);
        btn.BackColor = Color.FromArgb(20, 0, 210, 180);
        btn.ForeColor = Color.White;
        btn.Font = new Font("Segoe UI", 12);
        btn.Cursor = Cursors.Hand;
    }

    public FormImpostazioni()
    {
        InitializeComponent();
    }
}