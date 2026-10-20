using Baraja;
using Baraja.Commands;
using Baraja.Implementaciones;
using Baraja.Implementaciones.Spider;
using Baraja.Interfaces;

namespace WinFormsUI;

public partial class SpiderForm : Form
{
    private SpiderGameTable _gameTable;
    private GameManager _gameManager;
    private IDealer _dealer;

    // Puente entre la UI y el motor
    private readonly Dictionary<PictureBox, PlayableCard> _pbToCard = new();
    private readonly Dictionary<PictureBox, SpiderTableauColumn> _pbToColumn = new();

    private PictureBox _pbStock;
    private Label _lblStockCount;

    // Estado temporal del Mouse
    private List<PictureBox> _draggedPbs = new();
    private SpiderTableauColumn _sourceColumn;
    private Point _mouseOffset;
    private int StockStartX = 50;
    private int StockStartY = 30; // Más arriba que BoardStartY (200)

    private int BoardStartX;
    private int BoardStartY;
    private  int CardWidth;
    private int CardHeight;
    private int ColumnSpacing;
    private List<PictureBox> _hoveredPbs = new();
    
    // Controles persistentes de las fundaciones
    private readonly List<PictureBox> _pbFoundations = new();
    // Constantes de diseño para las Fundaciones (Alineadas con el StockPile)
    private int FoundationStartX; // Separado del mazo
    private int FoundationStartY;
    private int FoundationSpacing;   // Espacio entre cada fundación
                                                            
    // Bloquea la UI mientras hay cartas volando
    private bool _isAnimating = false;
    private GameDifficulty _currentDifficulty = GameDifficulty.Easy;
    private int _currentSeed;

    // Barra de estado para mostrar movimientos y tiempo
    private StatusStrip _statusStrip;
    private ToolStripStatusLabel _lblMoves;
    private ToolStripStatusLabel _lblTime;
    // Cronómetro
    private System.Windows.Forms.Timer _gameTimer;
    private int _elapsedSeconds;
    public SpiderForm()
    {
        InitializeComponent();

        // 1. FUNDAMENTAL: Permite que el formulario escuche el teclado a nivel global
        this.KeyPreview = true;
        this.KeyDown += SpiderForm_KeyDown;

        // 1. FUNDAMENTAL: Evita el parpadeo negro al redimensionar WinForms
        this.DoubleBuffered = true;

        // 2. Tamaño inicial sugerido de la ventana
        this.MinimumSize = new Size(800, 600);
        this.Size = new Size(1200, 800); // Esto calculará una escala ~1.2 de inicio

        // 3. Suscribir evento de cambio de tamaño
        this.SizeChanged += SpiderForm_SizeChanged;

        InitializeMenu();
        InitializeStatusBar();

        // Empezar por defecto en Fácil
        StartBrandNewGame(GameDifficulty.Easy);
    }
    private void Form1_Load(object sender, EventArgs e)
    {

    }
    private void RenderBoard()
    {
        // Limpiar controles anteriores
        foreach (var pb in _pbToCard.Keys)
        {
            this.Controls.Remove(pb);
            pb.Dispose();
        }
        _pbToCard.Clear();
        _pbToColumn.Clear();
        
        RenderStockPile();
        RenderFoundations();

        int startX = BoardStartX;
        int startY = BoardStartY;
        int cardWidth = CardWidth;
        int cardHeight = CardHeight;
        int colSpacing = ColumnSpacing;
        int yOffsetTapada = 10;
        int yOffsetDescubierta = 25;

        // Renderizar Columnas
        for (int i = 0; i < _gameTable.Columns.Count; i++)
        {
            var column = _gameTable.Columns[i];
            int currentY = startY;

            foreach (var card in column.Cards)
            {
                var pb = new PictureBox
                {
                    Width = cardWidth,
                    Height = cardHeight,
                    Left = startX + (i * colSpacing),
                    Top = currentY,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Image = GetCardImage(card), // Método que lee de Properties.Resources
                    Cursor = card.IsFaceUp ? Cursors.Hand : Cursors.Default
                };

                // Suscribir eventos de mouse
                pb.MouseDown += Card_MouseDown;
                pb.MouseMove += Card_MouseMove;
                pb.MouseUp += Card_MouseUp;

                pb.MouseEnter += Card_MouseEnter;
                pb.MouseLeave += Card_MouseLeave;
                // Registrar en diccionarios
                _pbToCard[pb] = card;
                _pbToColumn[pb] = column;

                this.Controls.Add(pb);
                pb.BringToFront(); // Crucial para que se apilen correctamente

                currentY += card.IsFaceUp ? yOffsetDescubierta : yOffsetTapada;
            }
        }
    }

    private Image GetCardImage(PlayableCard card)
    {
        // 1. Si está tapada, devolvemos el dorso (esto ya te funciona)
        if (!card.IsFaceUp) return Properties.Resources.CardBack;

        // 2. Buscamos la carta descubierta (Ej: "Spades_Ace", "Hearts_10")
        string resourceName = $"{card.Card.Suit}_{card.Card.Rank}";
        var image = (Image)Properties.Resources.ResourceManager.GetObject(resourceName);

        // 3. Si la imagen no existe en tus recursos, dibujamos una temporal
        if (image == null)
        {
            return CreateFallbackCardImage(resourceName);
        }

        return image;
    }

    // Método salvavidas: Dibuja una carta en blanco con el nombre escrito
    private Image CreateFallbackCardImage(string cardName)
    {
        var bmp = new Bitmap(71, 96); // Usa tus constantes CardWidth y CardHeight si las tienes
        using var g = Graphics.FromImage(bmp);

        // Fondo blanco y borde negro
        g.Clear(Color.White);
        g.DrawRectangle(Pens.Black, 0, 0, 70, 95);

        // Escribimos el nombre de la carta que falta
        var font = new Font("Segoe UI", 9, FontStyle.Bold);
        var rect = new RectangleF(5, 30, 60, 40);

        g.DrawString(cardName, font, Brushes.Black, rect);

        return bmp;
    }
    
    private void Card_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggedPbs.Count == 0) return;

        // Mover en bloque calculando el delta
        int deltaX = e.X - _mouseOffset.X;
        int deltaY = e.Y - _mouseOffset.Y;

        foreach (var pb in _draggedPbs)
        {
            pb.Left += deltaX;
            pb.Top += deltaY;
        }
    }

    private void Card_MouseUp(object sender, MouseEventArgs e)
    {
        if (_draggedPbs.Count == 0 || _isAnimating) return;

        var targetPb = _draggedPbs.First();
        var destinationColumn = GetColumnUnderMouse(targetPb);

        if (destinationColumn != null && destinationColumn != _sourceColumn)
        {
            var cardsToMove = _draggedPbs.Select(pb => _pbToCard[pb]).ToList();
            var moveCommand = new SpiderMoveCommand(cardsToMove, _sourceColumn, destinationColumn);
            var macro = new SpiderMacroCommand();

            // 1. Ejecutar el movimiento físico primero
            var result = moveCommand.Execute();
            if (result.IsSuccess)
            {
                macro.AddCommand(moveCommand);
                _draggedPbs.Clear();
                _sourceColumn = null;
                RenderBoard();

                // 2. Evaluar y animar (le pasamos el macro en curso)
                CheckAndAnimateCompletedSuits(macro, () =>
                {
                    // 3. Guardar el bloque entero en el historial como 1 solo paso
                    _gameManager.TryExecuteMove(macro);
                    UpdateStatusBar();
                    RenderBoard();
                    CheckForVictory();
                });

                return;
            }
        }

        _draggedPbs.Clear();
        _sourceColumn = null;
        RenderBoard();
    }

    private SpiderTableauColumn? GetColumnUnderMouse(PictureBox topDraggedCard)
    {
        // 1. Calculamos el centro exacto de la carta arrastrada
        int cardCenterX = topDraggedCard.Left + (CardWidth / 2);
        int cardCenterY = topDraggedCard.Top + (CardHeight / 2);

        // 2. Si el jugador suelta las cartas muy arriba del tablero (ej. sobre los botones), ignoramos
        if (cardCenterY < BoardStartY - (CardHeight / 2))
        {
            return null;
        }

        // 3. Iterar sobre las columnas para ver en cuál "encaja" el centro X
        for (int i = 0; i < _gameTable.Columns.Count; i++)
        {
            // Calculamos el borde izquierdo y derecho de la columna visual
            int columnLeftEdge = BoardStartX + (i * ColumnSpacing);
            int columnRightEdge = columnLeftEdge + CardWidth;

            // Tolerancia UX: Agregamos 15 píxeles de margen a los lados para 
            // que el jugador no tenga que ser microscópicamente preciso
            int dropMargin = 15;

            bool isInsideHorizontalBounds =
                cardCenterX >= (columnLeftEdge - dropMargin) &&
                cardCenterX <= (columnRightEdge + dropMargin);

            if (isInsideHorizontalBounds)
            {
                return _gameTable.Columns[i];
            }
        }

        // Si soltó las cartas en el espacio vacío entre dos columnas lejanas o fuera de la pantalla
        return null;
    }
    // Llama a este método al principio de tu RenderBoard() existente
    private void RenderStockPile()
    {
        // 1. Si no existen, los inicializamos (solo ocurre en el primer render)
        if (_pbStock == null)
        {
            _pbStock = new PictureBox
            {
                Width = CardWidth,
                Height = CardHeight,
                Left = StockStartX,
                Top = StockStartY,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Cursor = Cursors.Hand
            };
            _pbStock.Click += StockPile_Click;
            this.Controls.Add(_pbStock);

            _lblStockCount = new Label
            {
                Left = StockStartX + CardWidth + 10,
                Top = StockStartY + (CardHeight / 2) - 10,
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Black // Cambia a White si tu fondo de ventana es oscuro
            };
            this.Controls.Add(_lblStockCount);
        }

        // 2. Actualizamos la apariencia según las cartas restantes
        if (_gameTable.CardsLeftInStock > 0)
        {
            _pbStock.Image = Properties.Resources.CardBack;
            _pbStock.BorderStyle = BorderStyle.None;
            _lblStockCount.Text = $"{_gameTable.CardsLeftInStock} cartas";
            _lblStockCount.Visible = true;
        }
        else
        {
            // Mazo vacío: Recuadro transparente
            _pbStock.Image = null;
            _pbStock.BorderStyle = BorderStyle.FixedSingle;
            _lblStockCount.Visible = false;
        }

        // 3. Forzar que se vean por encima de cualquier otro control
        _pbStock.BringToFront();
        _lblStockCount.BringToFront();
    }
    private void StockPile_Click(object? sender, EventArgs e)
    {
        if (_isAnimating || _gameTable.IsGameWon) return;

        var dealCommand = new SpiderDealCommand(_gameTable, _dealer);
        var macro = new SpiderMacroCommand();
        
        var result = dealCommand.Execute();
        if (result.IsSuccess)
        {
            macro.AddCommand(dealCommand);

            var animations = new List<(PictureBox, Point, Point)>();
            var tempPbs = new List<PictureBox>();

            // Configuración de las 10 cartas que van a volar
            for (int i = 0; i < 10; i++)
            {
                var column = _gameTable.Columns[i];
                var newCard = column.Cards.Last(); // La carta que acaba de caer lógicamente

                // Calcular a dónde tiene que caer en la pantalla
                int targetY = BoardStartY;
                int yOffsetTapada = 10;
                int yOffsetDescubierta = 25;

                for (int c = 0; c < column.Cards.Count - 1; c++)
                {
                    targetY += column.Cards[c].IsFaceUp ? yOffsetDescubierta : yOffsetTapada;
                }
                int targetX = BoardStartX + (i * ColumnSpacing);

                // Crear la carta visual temporal en el mazo
                var tempPb = new PictureBox
                {
                    Width = CardWidth,
                    Height = CardHeight,
                    Location = new Point(StockStartX, StockStartY),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Image = GetCardImage(newCard)
                };

                this.Controls.Add(tempPb);
                tempPb.BringToFront();
                tempPbs.Add(tempPb);

                animations.Add((tempPb, tempPb.Location, new Point(targetX, targetY)));
            }

            // Iniciar vuelo
            AnimateCards(animations, () =>
            {
                // Al llegar, borramos las temporales
                foreach (var pb in tempPbs) { this.Controls.Remove(pb); pb.Dispose(); }

                // Renderizamos las reales (que ya están en _gameTable)
                RenderBoard();

                // Revisamos si esto provocó completar un palo y animamos
                CheckAndAnimateCompletedSuits(macro, () =>
                {
                    _gameManager.TryExecuteMove(macro);
                    UpdateStatusBar();
                    RenderBoard();
                    CheckForVictory();
                });
            });
        }
        else
        {
            MessageBox.Show(result.ErrorMessage, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void Card_MouseEnter(object? sender, EventArgs e)
    {
        if (_isAnimating || _gameTable.IsGameWon) return;

        var pb = sender as PictureBox;
        if (pb == null || _draggedPbs.Count > 0) return;

        var column = _pbToColumn[pb];

        // 1. Obtener los PictureBox SOLO de esta columna, ordenados de arriba hacia abajo
        var pbsInColumn = _pbToColumn.Where(kvp => kvp.Value == column)
                                     .Select(kvp => kvp.Key)
                                     .OrderBy(p => p.Top)
                                     .ToList();

        // 2. Encontrar la posición exacta de este control en la columna
        int index = pbsInColumn.IndexOf(pb);

        // 3. Obtener las cartas lógicas que corresponden a estas posiciones
        var cardsToTest = column.Cards.Skip(index).ToList();

        // 4. Preguntar al motor si este bloque específico es válido
        if (column.CanExtract(cardsToTest).IsSuccess)
        {
            // Solo iluminar los PictureBox físicos que están debajo del ratón
            _hoveredPbs = pbsInColumn.Skip(index).ToList();

            foreach (var hPb in _hoveredPbs)
            {
                hPb.Cursor = Cursors.Hand;
                hPb.BackColor = Color.DeepSkyBlue;
                hPb.Padding = new Padding(2);
            }
        }
        else
        {
            pb.Cursor = Cursors.Default;
        }
    }

    private void Card_MouseDown(object sender, MouseEventArgs e)
    {
        if (_isAnimating || _gameTable.IsGameWon) return;

        if (e.Button != MouseButtons.Left) return;

        var pb = (PictureBox)sender;
        _sourceColumn = _pbToColumn[pb];

        // 1. Misma lógica: aislar visualmente los controles de esta columna
        var pbsInColumn = _pbToColumn.Where(kvp => kvp.Value == _sourceColumn)
                                     .Select(kvp => kvp.Key)
                                     .OrderBy(p => p.Top)
                                     .ToList();

        int index = pbsInColumn.IndexOf(pb);
        var cardsToMove = _sourceColumn.Cards.Skip(index).ToList();

        var extractResult = _sourceColumn.CanExtract(cardsToMove);
        if (!extractResult.IsSuccess) return;

        _mouseOffset = e.Location;

        // 2. Asignar al arrastre únicamente los PictureBox correspondientes
        _draggedPbs = pbsInColumn.Skip(index).ToList();

        foreach (var draggedPb in _draggedPbs)
        {
            draggedPb.BringToFront();
        }
    }
    private void Card_MouseLeave(object? sender, EventArgs e)
    {
        // Apagar el efecto de todas las cartas que estaban iluminadas
        foreach (var hPb in _hoveredPbs)
        {
            hPb.Cursor = Cursors.Default;
            hPb.BackColor = Color.Transparent;
            hPb.Padding = new Padding(0);
        }

        _hoveredPbs.Clear();
    }
    private void SpiderForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_isAnimating || _gameTable.IsGameWon) return;// <--- BLOQUEO DE VICTORIA

        // Detectar la combinación exacta: Ctrl presionado y la tecla Z
        if (e.Control && e.KeyCode == Keys.Z)
        {
            // Evita el sonido de "beep" molesto de Windows al usar atajos no nativos
            e.SuppressKeyPress = true;

            // 1. El motor hace la magia: 
            // Devuelve las cartas al origen, saca palos de la fundación 
            // y "destapa" las cartas correspondientes.
            _gameManager.UndoLastMove();

            // 2. La UI simplemente se sincroniza con el estado actual del núcleo
            RenderBoard();
            UpdateStatusBar();
        }
    }
    private void RenderFoundations()
    {
        // 1. Inicializar los 8 controles solo la primera vez
        if (_pbFoundations.Count == 0)
        {
            for (int i = 0; i < 8; i++)
            {
                var pb = new PictureBox
                {
                    Width = CardWidth,
                    Height = CardHeight,
                    Left = FoundationStartX + (i * FoundationSpacing),
                    Top = FoundationStartY,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    BorderStyle = BorderStyle.FixedSingle, // Borde sutil para zonas vacías
                    BackColor = Color.Transparent
                };

                _pbFoundations.Add(pb);
                this.Controls.Add(pb);
            }
        }

        // 2. Actualizar la apariencia de cada fundación según el estado del núcleo
        for (int i = 0; i < _gameTable.Foundations.Count; i++)
        {
            var foundation = _gameTable.Foundations[i];
            var pb = _pbFoundations[i];

            if (foundation.Cards.Count > 0)
            {
                // Si hay un palo completado, mostramos la carta superior (el As)
                var topCard = foundation.Cards.Last();

                pb.Image = GetCardImage(topCard);
                pb.BorderStyle = BorderStyle.None; // Quitamos el borde porque hay una carta
            }
            else
            {
                // Si está vacía, nos aseguramos de limpiarla (útil para el Undo)
                pb.Image = null;
                pb.BorderStyle = BorderStyle.FixedSingle;
            }

            pb.BringToFront();
        }
    }
    private void CheckForVictory()
    {
        // Consultamos al motor si las 8 fundaciones están llenas
        if (_gameTable.IsGameWon)
        {
            _gameTimer.Stop(); // Detenemos el cronómetro al ganar
            // Mostramos el mensaje y capturamos la respuesta
            var result = MessageBox.Show(
                "¡Felicidades! Has completado todos los palos y ganado el juego de Spider Solitaire.\n\n¿Quieres jugar una nueva partida?",
                "¡Victoria!",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information
            );

            if (result == DialogResult.Yes)
            {
                // Reutilizamos el método que ya tienes para limpiar y crear una mesa nueva
                StartBrandNewGame(_currentDifficulty);
            }
        }
    }
    private void AnimateCards(List<(PictureBox Pb, Point Start, Point End)> animations, Action onComplete)
    {
        _isAnimating = true;

        int steps = 20; // Cantidad de fotogramas
        int currentStep = 0;
        int interval = 15; // Milisegundos por fotograma (aprox 60 FPS)

        var timer = new System.Windows.Forms.Timer { Interval = interval };
        timer.Tick += (s, e) =>
        {
            currentStep++;

            // Cálculo de progreso de 0.0 a 1.0
            double progress = (double)currentStep / steps;

            // Efecto "Ease-Out" (frena suavemente al llegar)
            double easeProgress = 1 - Math.Pow(1 - progress, 3);

            foreach (var anim in animations)
            {
                if (anim.Pb.IsDisposed) continue;

                int newX = (int)(anim.Start.X + (anim.End.X - anim.Start.X) * easeProgress);
                int newY = (int)(anim.Start.Y + (anim.End.Y - anim.Start.Y) * easeProgress);
                anim.Pb.Location = new Point(newX, newY);
            }

            if (currentStep >= steps)
            {
                timer.Stop();
                timer.Dispose();
                _isAnimating = false;
                onComplete?.Invoke(); // Ejecutar lógica al terminar
            }
        };
        timer.Start();
    }
    private void CheckAndAnimateCompletedSuits(SpiderMacroCommand macro, Action onAllComplete)
    {
        var columnsToAnimate = _gameTable.Columns.Where(c => c.HasCompletedSuitRun()).ToList();

        if (columnsToAnimate.Count == 0)
        {
            onAllComplete?.Invoke();
            return;
        }

        var animations = new List<(PictureBox, Point, Point)>();
        int emptyFoundationIndex = _gameTable.Foundations.ToList().FindIndex(f => f.Cards.Count == 0);

        foreach (var col in columnsToAnimate)
        {
            var pbsToAnimate = _pbToColumn.Where(kvp => kvp.Value == col)
                                          .OrderBy(kvp => kvp.Key.Top)
                                          .Select(kvp => kvp.Key)
                                          .TakeLast(13).ToList();

            int targetX = FoundationStartX + (emptyFoundationIndex * FoundationSpacing);
            int targetY = FoundationStartY;

            foreach (var pb in pbsToAnimate)
            {
                pb.BringToFront();
                animations.Add((pb, pb.Location, new Point(targetX, targetY)));
            }
            emptyFoundationIndex++;
        }

        AnimateCards(animations, () =>
        {
            // 1. Ejecutamos la lógica en el núcleo
            var completeCmd = new SpiderCompleteSuitCommand(_gameTable);
            completeCmd.Execute();

            // 2. Lo añadimos a la transacción
            macro.AddCommand(completeCmd);

            onAllComplete?.Invoke();
        });
    }
    private void CalculateDimensions()
    {
        // 1. Calculamos el factor de escala basándonos en el ancho de la ventana
        // Usamos un ancho base de 1000px como referencia para escala 1.0
        float scale = this.ClientSize.Width / 1000f;

        // Limitamos la escala para que no sea microscópica ni absurdamente gigante
        scale = Math.Max(0.6f, Math.Min(3.0f, scale));

        // 2. Aplicamos la escala a todas las medidas del tablero
        CardWidth = (int)(71 * scale);
        CardHeight = (int)(96 * scale);
        ColumnSpacing = (int)(90 * scale);

        StockStartX = (int)(50 * scale);
        StockStartY = (int)(50 * scale);

        BoardStartX = (int)(50 * scale);

        // Damos más espacio vertical si la carta es más grande
        BoardStartY = StockStartY + CardHeight + (int)(30 * scale);

        FoundationStartX = StockStartX + (int)(200 * scale);
        FoundationSpacing = CardWidth + (int)(10 * scale);
    }
    private void SpiderForm_SizeChanged(object? sender, EventArgs e)
    {
        // Cuando el usuario estira la ventana, recalculamos la matemática y redibujamos
        if (_gameTable != null)
        {
            CalculateDimensions();
            RenderBoard();
        }
    }
    private void InitializeMenu()
    {
        var menuStrip = new MenuStrip();
        var gameMenu = new ToolStripMenuItem("Juego");

        var restartMenuItem = new ToolStripMenuItem("Reiniciar Partida (Misma Baraja)");
        restartMenuItem.Click += (s, e) => {
            if (ConfirmRestart()) StartGameWithSeed(_currentDifficulty, _currentSeed);
        };

        var newGameMenu = new ToolStripMenuItem("Nuevo Juego...");

        var easyMenu = new ToolStripMenuItem("Fácil (1 Palo - Picas)");
        easyMenu.Click += (s, e) =>
        {
            if (ConfirmRestart()) StartBrandNewGame(GameDifficulty.Easy);
        };

        var mediumMenu = new ToolStripMenuItem("Medio (2 Palos - Picas y Corazones)");
        mediumMenu.Click += (s, e) =>
        {
            if (ConfirmRestart()) StartBrandNewGame(GameDifficulty.Medium);
        };

        var hardMenu = new ToolStripMenuItem("Difícil (4 Palos)");
        hardMenu.Click += (s, e) =>
        {
            if (ConfirmRestart()) StartBrandNewGame(GameDifficulty.Hard);
        };

        newGameMenu.DropDownItems.Add(easyMenu);
        newGameMenu.DropDownItems.Add(mediumMenu);
        newGameMenu.DropDownItems.Add(hardMenu);

        gameMenu.DropDownItems.Add(newGameMenu);
        gameMenu.DropDownItems.Add(new ToolStripSeparator());
        gameMenu.DropDownItems.Add(restartMenuItem);

        menuStrip.Items.Add(gameMenu);

        this.MainMenuStrip = menuStrip;
        this.Controls.Add(menuStrip);
    }
    private void StartBrandNewGame(GameDifficulty difficulty)
    {
        // Generar una nueva semilla aleatoria basada en el reloj del sistema
        int newSeed = Environment.TickCount;
        StartGameWithSeed(difficulty, newSeed);
    }

    private void StartGameWithSeed(GameDifficulty difficulty, int seed)
    {
        _currentDifficulty = difficulty;
        _currentSeed = seed;

        // 1. Seleccionar la fábrica correcta según la dificultad
        IDeck deck = difficulty switch
        {
            GameDifficulty.Easy => SpiderDeckFactory.CreateOneSuit(),
            GameDifficulty.Medium => SpiderDeckFactory.CreateTwoSuits(),
            GameDifficulty.Hard => SpiderDeckFactory.CreateFourSuits(),
            _ => SpiderDeckFactory.CreateOneSuit()
        };

        // 2. Usar el shuffler con la semilla guardada para asegurar el mismo orden
        var shuffler = new RandomShuffler(seed);
        _dealer = new StandardDealer(deck, shuffler);

        _gameTable = new SpiderGameTable(_dealer);
        _gameTable.DealInitialBoard();
        _gameManager = new GameManager();

        // 3. Forzar detención de animaciones previas si el usuario reinició rápido
        _isAnimating = false;

        CalculateDimensions();
        RenderBoard();

        _elapsedSeconds = 0;
        _lblTime.Text = "Tiempo: 00:00";
        UpdateStatusBar();
        _gameTimer.Start();
    }
    private bool ConfirmRestart()
    {
        // Si el juego aún no ha cargado o ya se ganó, no hay progreso que perder
        if (_gameTable == null || _gameTable.IsGameWon)
        {
            return true;
        }

        var result = MessageBox.Show(
            "¿Estás seguro que deseas reiniciar? Perderás tu progreso actual.",
            "Confirmar reinicio",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2 // El botón 'No' por defecto para evitar clics accidentales
        );

        return result == DialogResult.Yes;
    }
    private void InitializeStatusBar()
    {
        _statusStrip = new StatusStrip();
        _statusStrip.BackColor = Color.WhiteSmoke;

        _lblMoves = new ToolStripStatusLabel("Movimientos: 0");
        _lblMoves.Font = new Font("Segoe UI", 10, FontStyle.Bold);

        // Este control invisible actúa como un "resorte" para empujar el tiempo a la derecha
        var springLabel = new ToolStripStatusLabel { Spring = true };

        _lblTime = new ToolStripStatusLabel("Tiempo: 00:00");
        _lblTime.Font = new Font("Segoe UI", 10, FontStyle.Bold);

        _statusStrip.Items.Add(_lblMoves);
        _statusStrip.Items.Add(springLabel);
        _statusStrip.Items.Add(_lblTime);

        this.Controls.Add(_statusStrip);

        // Configurar el reloj (1000 ms = 1 segundo)
        _gameTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _gameTimer.Tick += GameTimer_Tick;
    }

    private void GameTimer_Tick(object? sender, EventArgs e)
    {
        if (_gameTable == null || _gameTable.IsGameWon) return;

        _elapsedSeconds++;

        // Dar formato MM:SS al tiempo
        TimeSpan time = TimeSpan.FromSeconds(_elapsedSeconds);
        _lblTime.Text = $"Tiempo: {time:mm\\:ss}";
    }

    // Método de ayuda para sincronizar el texto con el motor
    private void UpdateStatusBar()
    {
        int moves = _gameManager?.MoveCount ?? 0;
        _lblMoves.Text = $"Movimientos: {moves}";
    }
}
