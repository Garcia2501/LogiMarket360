using System.Globalization;

namespace LogiMarket.WinForms;

internal static class Tema
{
    public static readonly Color Fondo = Color.FromArgb(241, 245, 249);
    public static readonly Color Papel = Color.White;
    public static readonly Color Texto = Color.FromArgb(20, 38, 59);
    public static readonly Color Secundario = Color.FromArgb(65, 84, 103);
    public static readonly Color Borde = Color.FromArgb(209, 222, 233);
    public static readonly Color Acento = Color.FromArgb(0, 102, 107);
    public static readonly Color Suave = Color.FromArgb(222, 243, 242);
    public static readonly Color Riesgo = Color.FromArgb(154, 43, 44);
    public static readonly Color Barra = Color.FromArgb(232, 240, 246);

    public static FlowLayoutPanel Acciones(params Control[] controles)
    {
        var fila = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8),
            WrapContents = true, FlowDirection = FlowDirection.LeftToRight };
        fila.Controls.AddRange(controles);
        return fila;
    }

    public static Label TextoPlano(string texto = "") => new()
    {
        AutoSize = true, Text = texto, ForeColor = Secundario, Margin = new Padding(0, 4, 0, 4)
    };

    public static TextBox TextoCampo(string nombre, int ancho = 195) => new() { Width = ancho, AccessibleName = nombre };

    public static NumericUpDown Numero(string nombre, decimal valor, decimal minimo, decimal maximo, int decimales = 0) => new()
    {
        Width = 103, Minimum = minimo, Maximum = maximo, Value = valor, DecimalPlaces = decimales,
        AccessibleName = nombre, ThousandsSeparator = true
    };

    public static ComboBox Lista<T>(string nombre, int ancho = 156) where T : struct, Enum => new()
    {
        Width = ancho, AccessibleName = nombre, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = Enum.GetValues<T>()
    };

    public static Control Campo(string texto, Control entrada)
    {
        var panel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoSize = true, Margin = new Padding(0, 0, 12, 4) };
        var etiqueta = TextoPlano(texto);
        etiqueta.Margin = new Padding(0, 1, 0, 2);
        entrada.Margin = new Padding(0, 0, 12, 0);
        panel.Controls.Add(etiqueta);
        panel.Controls.Add(entrada);
        return panel;
    }

    public static Button Boton(string texto, bool principal = false)
    {
        var fondo = principal ? Acento : Papel;
        var boton = new Button
        {
            AutoSize = true, MinimumSize = new Size(86, 33), Text = texto, FlatStyle = FlatStyle.Flat,
            BackColor = fondo, ForeColor = principal ? Color.White : Texto,
            Margin = new Padding(0, 0, 8, 5), Padding = new Padding(8, 2, 8, 2), Cursor = Cursors.Hand
        };
        boton.FlatAppearance.BorderColor = principal ? Acento : Borde;
        boton.FlatAppearance.MouseOverBackColor = principal ? Color.FromArgb(0, 82, 86) : Suave;
        boton.EnabledChanged += (_, _) =>
        {
            boton.BackColor = boton.Enabled ? fondo : Barra;
            boton.FlatAppearance.BorderColor = boton.Enabled && principal ? Acento : Borde;
        };
        return boton;
    }

    public static TableLayoutPanel Pilas() => new()
    {
        Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(0), Margin = new Padding(0),
        ColumnStyles = { new ColumnStyle(SizeType.Percent, 100) }
    };

    public static void Fila(TableLayoutPanel tabla, Control control, float porcentaje = 0)
    {
        var fila = tabla.RowCount++;
        tabla.RowStyles.Add(porcentaje == 0 ? new RowStyle(SizeType.AutoSize) : new RowStyle(SizeType.Percent, porcentaje));
        control.Dock = DockStyle.Fill;
        tabla.Controls.Add(control, 0, fila);
    }

    public static string Usd(decimal valor) => "USD " + valor.ToString("N2", CultureInfo.GetCultureInfo("en-US"));
    public static string Fecha(DateTime utc) => utc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}

internal sealed class Pantalla : IDisposable
{
    private readonly TableLayoutPanel _contenido = Tema.Pilas();

    public Pantalla(string titulo, string detalle)
    {
        _contenido.Padding = new Padding(25, 20, 25, 14);
        _contenido.Dock = DockStyle.Top;
        _contenido.AutoSize = true;
        _contenido.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Cuerpo.AutoSize = true;
        Cuerpo.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Tema.Fila(_contenido, new Label { Text = titulo, AutoSize = true, Font = new Font("Segoe UI", 19, FontStyle.Bold),
            ForeColor = Tema.Texto, Margin = new Padding(0, 0, 0, 3), AccessibleName = titulo });
        Tema.Fila(_contenido, Tema.TextoPlano(detalle));
        Estado.Margin = new Padding(0, 3, 0, 8);
        Tema.Fila(_contenido, Estado);
        Tema.Fila(_contenido, Cuerpo, 100);
        Contenedor.Controls.Add(_contenido);
        Contenedor.ClientSizeChanged += (_, _) => _contenido.MinimumSize = new Size(0, Contenedor.ClientSize.Height);
    }

    public Panel Contenedor { get; } = new() { Dock = DockStyle.Fill, AutoScroll = true };
    public TableLayoutPanel Cuerpo { get; } = Tema.Pilas();
    public Label Estado { get; } = Tema.TextoPlano();
    public Func<IServiceProvider, Task>? Cargar { get; set; }

    public void Avisar(string mensaje, bool error = false)
    {
        Estado.ForeColor = error ? Tema.Riesgo : Tema.Acento;
        Estado.Text = mensaje;
    }

    public void Dispose() => Contenedor.Dispose();
}

internal sealed class Tarjeta
{
    private readonly Label _valor = new() { AutoSize = true, ForeColor = Tema.Texto,
        Font = new Font("Segoe UI", 19, FontStyle.Bold), Margin = new Padding(0, 2, 0, 0) };

    public Tarjeta(string titulo)
    {
        Contenedor.Controls.Add(Tema.TextoPlano(titulo));
        _valor.AccessibleName = titulo;
        Contenedor.Controls.Add(_valor);
    }

    public FlowLayoutPanel Contenedor { get; } = new()
    {
        Width = 164, Height = 92, BackColor = Tema.Papel, FlowDirection = FlowDirection.TopDown,
        WrapContents = false, Padding = new Padding(12, 7, 8, 7), Margin = new Padding(0, 0, 9, 9)
    };

    public void Mostrar(object valor) => _valor.Text = Convert.ToString(valor, CultureInfo.CurrentCulture) ?? "";
}

internal sealed class Cuadricula<T>(string nombre, Func<T, object> identidad) where T : class
{
    private object? _anterior;
    private bool _cargando;
    public DataGridView Control { get; } = new()
    {
        AccessibleName = nombre, ReadOnly = true, AutoGenerateColumns = false, AllowUserToAddRows = false,
        AllowUserToDeleteRows = false, AllowUserToResizeRows = false, MultiSelect = false, RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = Tema.Papel, BorderStyle = BorderStyle.FixedSingle, GridColor = Tema.Borde,
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, EnableHeadersVisualStyles = false,
        ColumnHeadersHeight = 35, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Tema.Barra, ForeColor = Tema.Texto, SelectionBackColor = Tema.Suave, SelectionForeColor = Tema.Texto },
        DefaultCellStyle = new DataGridViewCellStyle { ForeColor = Tema.Texto, BackColor = Tema.Papel,
            SelectionBackColor = Tema.Acento, SelectionForeColor = Color.White },
        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(247, 250, 252) },
        RowTemplate = { Height = 29 }, Margin = new Padding(0, 0, 0, 9), MinimumSize = new Size(0, 97)
    };

    public T? Seleccion => Control.SelectedRows.Count == 0 ? null : Control.SelectedRows[0].DataBoundItem as T;

    public Cuadricula<T> Col(string etiqueta, string propiedad, int peso = 100, string formato = "", int minimo = 5)
    {
        var anchoEncabezado = TextRenderer.MeasureText(etiqueta, Control.Font).Width + 16;
        Control.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = etiqueta, DataPropertyName = propiedad, FillWeight = peso,
            MinimumWidth = Math.Max(minimo, anchoEncabezado),
            DefaultCellStyle = new DataGridViewCellStyle { Format = formato, FormatProvider = CultureInfo.GetCultureInfo("en-US") },
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        return this;
    }

    public void AlSeleccionar(Action<T?> accion) => Control.SelectionChanged += (_, _) =>
    {
        if (!_cargando)
            accion(Seleccion);
    };

    public void Cargar(IEnumerable<T> filas, object? preferida = null)
    {
        var actual = Seleccion;
        var clave = preferida ?? (actual is null ? _anterior : identidad(actual));
        _cargando = true;
        try
        {
            Control.DataSource = filas.ToArray();
            if (clave is null)
                return;
            _anterior = clave;
            foreach (DataGridViewRow fila in Control.Rows)
            {
                if (fila.DataBoundItem is T item && Equals(identidad(item), clave))
                {
                    Control.CurrentCell = fila.Cells[0];
                    fila.Selected = true;
                    return;
                }
            }
            Control.ClearSelection();
        }
        finally { _cargando = false; }
    }
}
