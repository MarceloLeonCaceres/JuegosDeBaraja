using System.Drawing;
using System.Drawing.Drawing2D;

namespace Baraja;

public static partial class CardImageGenerator
{
    public static class DeckGenerator
    {
        private const int Width = 142;
        private const int Height = 192;

        // Área donde se colocan los palos (centros de los palos)
        private const float PipLeft = 44f;
        private const float PipRight = 98f;
        private const float PipTop = 52f;
        private const float PipBottom = 152f;

        private static readonly string[] Suits = { "Hearts", "Spades", "Clubs", "Diamonds" };
        private static readonly string[] Ranks = { "Ace", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Jack", "Queen", "King" };
        private static readonly string[] Symbols = { "♥", "♠", "♣", "♦" };
        private static readonly string[] RankChars = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

        // Posiciones normalizadas (x: 0=izq, 0.5=centro, 1=der / y: 0=arriba, 1=abajo)
        private static readonly Dictionary<int, (float X, float Y)[]> PipLayouts = BuildPipLayouts();

        private static Dictionary<int, (float X, float Y)[]> BuildPipLayouts()
        {
            var sixCore = new (float X, float Y)[] { (0, 0), (1, 0), (0, .5f), (1, .5f), (0, 1), (1, 1) };
            var nineCore = new (float X, float Y)[]
            {
            (0, 0), (1, 0), (0, 1f / 3), (1, 1f / 3),
            (0, 2f / 3), (1, 2f / 3), (0, 1), (1, 1)
            };

            return new Dictionary<int, (float X, float Y)[]>
            {
                [2] = new (float, float)[] { (.5f, 0), (.5f, 1) },
                [3] = new (float, float)[] { (.5f, 0), (.5f, .5f), (.5f, 1) },
                [4] = new (float, float)[] { (0, 0), (1, 0), (0, 1), (1, 1) },
                [5] = new (float, float)[] { (0, 0), (1, 0), (.5f, .5f), (0, 1), (1, 1) },
                [6] = sixCore,
                [7] = sixCore.Append((.5f, .25f)).ToArray(),
                [8] = sixCore.Append((.5f, .25f)).Append((.5f, .75f)).ToArray(),
                [9] = nineCore.Append((.5f, .5f)).ToArray(),
                [10] = nineCore.Append((.5f, 1f / 6)).Append((.5f, 5f / 6)).ToArray(),
            };
        }

        public static void GenerateDeck(string outputFolder = "CartasGeneradas")
        {
            Directory.CreateDirectory(outputFolder);

            for (int s = 0; s < Suits.Length; s++)
            {
                for (int r = 0; r < Ranks.Length; r++)
                {
                    using var bmp = DrawCard(s, r);
                    bmp.Save(Path.Combine(outputFolder, $"{Suits[s]}_{Ranks[r]}.png"));
                }
            }

            using var back = DrawCardBack();
            back.Save(Path.Combine(outputFolder, "CardBack.png"));
        }

        private static Bitmap DrawCard(int suitIndex, int rankIndex)
        {
            var bmp = new Bitmap(Width, Height);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            g.Clear(Color.White);
            g.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);

            // Rojo para corazones/diamantes, negro para tréboles/picas
            Brush brush = (suitIndex == 0 || suitIndex == 3) ? Brushes.Red : Brushes.Black;
            string symbol = Symbols[suitIndex];
            string rankChar = RankChars[rankIndex];

            DrawCornerIndex(g, rankChar, symbol, brush);

            int number = rankIndex + 1; // Ace=1 ... Ten=10, Jack=11, Queen=12, King=13
            if (PipLayouts.TryGetValue(number, out var layout))
                DrawPips(g, symbol, brush, layout);
            else
                DrawCenterLetter(g, rankChar, brush);

            return bmp;
        }

        private static void DrawCornerIndex(Graphics g, string rankChar, string symbol, Brush brush)
        {
            using var fontRank = new Font("Segoe UI", 18, FontStyle.Bold);
            using var fontSuit = new Font("Segoe UI Symbol", 18, FontStyle.Regular);

            const float x = 4f;
            const float y = 3f;
            const float gap = 1f;

            // GenericTypographic elimina el padding interno para medir el ancho real del texto
            var format = StringFormat.GenericTypographic;
            float rankWidth = g.MeasureString(rankChar, fontRank, PointF.Empty, format).Width;

            g.DrawString(rankChar, fontRank, brush, x, y, format);
            g.DrawString(symbol, fontSuit, brush, x + rankWidth + gap, y, format);
        }

        private static void DrawPips(Graphics g, string symbol, Brush brush, (float X, float Y)[] layout)
        {
            using var fontPip = new Font("Segoe UI Symbol", 20, FontStyle.Regular);

            foreach (var (nx, ny) in layout)
            {
                float x = PipLeft + nx * (PipRight - PipLeft);
                float y = PipTop + ny * (PipBottom - PipTop);
                DrawCentered(g, symbol, fontPip, brush, x, y);
            }
        }

        private static void DrawCenterLetter(Graphics g, string letter, Brush brush)
        {
            using var fontBig = new Font("Segoe UI", 64, FontStyle.Bold);
            DrawCentered(g, letter, fontBig, brush, Width / 2f, Height / 2f);
        }

        private static void DrawCentered(Graphics g, string text, Font font, Brush brush, float cx, float cy)
        {
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(text, font, brush, cx, cy, format);
        }

        private static Bitmap DrawCardBack()
        {
            var bmp = new Bitmap(Width, Height);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.RoyalBlue);
            g.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);

            using var hatch = new HatchBrush(HatchStyle.DiagonalCross, Color.LightBlue, Color.RoyalBlue);
            g.FillRectangle(hatch, 3, 3, Width - 7, Height - 7);

            return bmp;
        }
    }
}