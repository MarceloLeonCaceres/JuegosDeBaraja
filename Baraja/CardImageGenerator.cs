using System.Drawing;
using System.Drawing.Drawing2D;

namespace Baraja;

public static partial class CardImageGenerator
{
    public static void GenerateDeck(string outputFolder = "CartasGeneradas")
    {
        Directory.CreateDirectory(outputFolder);

        // Tamaño clásico (puedes multiplicarlo x2 para HD)
        int width = 142;
        int height = 192;

        string[] suits = { "Hearts", "Spades", "Clubs", "Diamonds" };
        string[] ranks = { "Ace", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Jack", "Queen", "King" };
        string[] symbols = { "♥", "♠", "♣", "♦" };
        string[] rankChars = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

        for (int s = 0; s < 4; s++)
        {
            for (int r = 0; r < 13; r++)
            {
                using var bmp = new Bitmap(width, height);
                using var g = Graphics.FromImage(bmp);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Fondo de la carta
                g.Clear(Color.White);
                g.DrawRectangle(Pens.Black, 0, 0, width - 1, height - 1);

                // Rojo para corazones/brillos, Negro para tréboles/picas
                Brush brush = (s == 0 || s == 3) ? Brushes.Red : Brushes.Black;

                using var fontSmall = new Font("Segoe UI", 10, FontStyle.Bold);
                using var fontBig = new Font("Segoe UI", 26, FontStyle.Regular);

                // Símbolo y número en la esquina
                g.DrawString(rankChars[r], fontSmall, brush, 2, 2);
                g.DrawString(symbols[s], fontSmall, brush, 2, 16);

                // Símbolo gigante en el centro
                var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(symbols[s], fontBig, brush, new RectangleF(0, 0, width, height + 10), format);

                // Guardar con el nombre exacto del Enum
                bmp.Save(Path.Combine(outputFolder, $"{suits[s]}_{ranks[r]}.png"));
            }
        }

        // Generar el Dorso (CardBack)
        using var backBmp = new Bitmap(width, height);
        using var bg = Graphics.FromImage(backBmp);
        bg.Clear(Color.RoyalBlue);
        bg.DrawRectangle(Pens.Black, 0, 0, width - 1, height - 1);

        // Patrón de rombos para el dorso
        using var hatch = new HatchBrush(HatchStyle.DiagonalCross, Color.LightBlue, Color.RoyalBlue);
        bg.FillRectangle(hatch, 3, 3, width - 7, height - 7);

        backBmp.Save(Path.Combine(outputFolder, "CardBack.png"));
    }
}