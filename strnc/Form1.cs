using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace strnc
{
    public partial class Form1 : Form
    {
        private const int TileSize = 50; // Tahta üzerindeki her karenin boyutu (piksel cinsinden)

        private readonly Chessboard chessboard; // Satranç tahtasını temsil eden nesne

        public Form1()
        {
            InitializeComponent();
            chessboard = new Chessboard(); // Satranç tahtasını oluştur
        }

        private void DrawChessboard(List<ChessPiece> chessPieces)
        {
            // Satranç tahtasını çizmek için bir bitmap oluştur
            Bitmap boardBitmap = new Bitmap(TileSize * 8, TileSize * 8);
            Graphics g = Graphics.FromImage(boardBitmap);

            // Tahtayı arka plan rengi ile doldur
            g.FillRectangle(Brushes.BurlyWood, 0, 0, boardBitmap.Width, boardBitmap.Height);

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    // Karelerin rengini belirle
                    Color tileColor = (row + col) % 2 == 0 ? Color.Cyan : Color.DarkCyan;
                    Brush tileBrush = new SolidBrush(tileColor);

                    // Kareyi çiz
                    g.FillRectangle(tileBrush, col * TileSize, row * TileSize, TileSize, TileSize);

                    // Karede taş varsa
                    ChessPiece piece = chessPieces.FirstOrDefault(p => p.Row == row && p.Col == col);
                    if (piece != null)
                    {
                        // Taşın tehdit edilip edilmediğini kontrol et
                        bool isThreatened = chessboard.IsPieceThreatened(piece, chessPieces);
                        Color pieceTileColor = isThreatened ? Color.Red : tileColor;

                        // Taşın rengini belirle ve kareyi çiz
                        g.FillRectangle(new SolidBrush(pieceTileColor), col * TileSize, row * TileSize, TileSize, TileSize);

                        // Taşın tipini ve koordinatlarını yazdır
                        Brush pieceBrush = piece.Color == ChessPieceColor.Beyaz ? Brushes.White : Brushes.Black;
                        Font boldFont = new Font("Arial", 11, FontStyle.Bold);
                        g.DrawString(piece.Type.ToString(), boldFont, pieceBrush, col * TileSize, row * TileSize);
                        string coordinates = $"{chessboard.GetColumnLetter(col)}{8 - row}";
                        g.DrawString(coordinates, new Font("Arial", 11), Brushes.Black, col * TileSize, row * TileSize + 30);
                    }
                }
            }

            // PictureBox'a tahtayı yerleştir
            pictureBox1.Image = boardBitmap;
        }

        private void ShowCalculationProcess(List<ChessPiece> chessPieces, ChessPieceColor color, double points)
        {
            // Hesaplama sürecini kullanıcıya gösteren bir iletişim penceresi oluştur
            string message = $"{(color == ChessPieceColor.Siyah ? "Siyah" : "Beyaz")} Taşların Hesaplanma Süreci:\n";

            foreach (ChessPiece piece in chessPieces)
            {
                if (piece.Color == color)
                {
                    // Taşın tehdit edilip edilmediğini kontrol et ve puan hesapla
                    double threatenedMultiplier = chessboard.IsPieceThreatened(piece, chessPieces) ? 0.5 : 1.0;
                    double piecePoints = piece.GetPoints() * threatenedMultiplier;
                    string coordinates = $"{chessboard.GetColumnLetter(piece.Col)}{8 - piece.Row}";

                    // Hesaplama sonuçlarını iletişim penceresine ekle
                    message += $"{piece.Color} {piece.Type} ({coordinates}): {piece.GetPoints()} x {threatenedMultiplier} = {piecePoints}\n";
                }
            }

            // Toplam puanı iletişim penceresine ekle.
            message += $"\nToplam Puan: {points}";

            MessageBox.Show(message, "Hesaplama Sonucu", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Dosya seçme düğmesine tıklandığında çalışacak metod
        private void SelectFile_Click(object sender, EventArgs e)
        {
            // Dosya seçme işlemi için bir OpenFileDialog oluştur
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Metin Dosyaları (*.txt)|*.txt";
            openFileDialog.FileName = "chessboard.txt"; // Varsayılan dosya adı

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Kullanıcı bir dosya seçtiğinde, seçilen dosyanın yolunu al
                string secilenDosyaYolu = openFileDialog.FileName;

                try
                {
                    // Dosyayı oku ve işle
                    List<string> satirlar = File.ReadAllLines(secilenDosyaYolu).ToList();

                    if (satirlar.Count != 8)
                    {
                        MessageBox.Show("Dosya, 8 satır içermelidir.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    List<ChessPiece> chessPieces = chessboard.ParseChessPieces(satirlar);

                    if (chessPieces.Count > 32 || chessPieces.Count <= 2)
                    {
                        MessageBox.Show("Dosya geçerli bir oyun durumu içermelidir (32 taş veya daha az taş).", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Satranç tahtasını çiz
                    DrawChessboard(chessPieces);

                    // Beyaz ve siyah taşların puanlarını hesapla ve göster
                    double whitePoints = chessboard.CalculatePoints(chessPieces, ChessPieceColor.Beyaz);
                    double blackPoints = chessboard.CalculatePoints(chessPieces, ChessPieceColor.Siyah);

                    ShowCalculationProcess(chessPieces, ChessPieceColor.Beyaz, whitePoints);
                    ShowCalculationProcess(chessPieces, ChessPieceColor.Siyah, blackPoints);
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("Dosya bulunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (IOException)
                {
                    MessageBox.Show("Dosya okunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Beklenmeyen bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    // Satranç taşlarının türlerini temsil eden enum
    public enum ChessPieceType
    {
        Piyon = 'p',
        At = 'a',
        Fil = 'f',
        Kale = 'k',
        Vezir = 'v',
        Şah = 's'
    }

    // Satranç taşlarının renklerini temsil eden enum
    public enum ChessPieceColor
    {
        Beyaz = 's',
        Siyah = 'b'
    }

    // Satranç taşlarını temsil eden sınıf
    public class ChessPiece
    {
        public ChessPieceType Type { get; } // Taşın türü
        public ChessPieceColor Color { get; } // Taşın rengi
        public double Points { get; } // Taşın puan değeri
        public int Row { get; } // Taşın satır konumu
        public int Col { get; } // Taşın sütun konumu

        public ChessPiece(ChessPieceType type, ChessPieceColor color, int row, int col)
        {
            Type = type;
            Color = color;
            Points = GetPiecePoints(type);
            Row = row;
            Col = col;
        }

        public double GetPoints()
        {
            return Points;
        }

        // Taşın türüne göre puanını döndüren metod
        public static double GetPiecePoints(ChessPieceType type)
        {
            switch (type)
            {
                case ChessPieceType.Piyon:
                    return 1.0;
                case ChessPieceType.At:
                    return 3.0;
                case ChessPieceType.Fil:
                    return 3.0;
                case ChessPieceType.Kale:
                    return 5.0;
                case ChessPieceType.Vezir:
                    return 9.0;
                case ChessPieceType.Şah:
                    return 100.0;
                default:
                    return 0.0;
            }
        }
    }

    // Satranç tahtasını temsil eden sınıf
    public class Chessboard
    {
        // Dosyadan okunan satırları satranç taşlarına dönüştüren metod
        public List<ChessPiece> ParseChessPieces(List<string> lines)
        {
            List<ChessPiece> chessPieces = new List<ChessPiece>();

            for (int row = 0; row < lines.Count; row++)
            {
                string[] pieces = lines[row].Split(' ');

                for (int col = 0; col < pieces.Length; col++)
                {
                    string piece = pieces[col];
                    if (piece != "--")
                    {
                        ChessPieceType type = (ChessPieceType)piece[0];
                        ChessPieceColor color = (ChessPieceColor)piece[1];
                        ChessPiece chessPiece = new ChessPiece(type, color, row, col);
                        chessPieces.Add(chessPiece);
                    }
                }
            }

            return chessPieces;
        }

        // Belirli bir renkteki taşların toplam puanını hesaplayan metod
        public double CalculatePoints(List<ChessPiece> chessPieces, ChessPieceColor color)
        {
            double points = 0;

            foreach (ChessPiece piece in chessPieces)
            {
                if (piece.Color == color)
                {
                    // Taşın tehdit edilip edilmediğini kontrol et
                    bool isThreatened = IsPieceThreatened(piece, chessPieces);
                    double threatenedMultiplier = isThreatened ? 0.5 : 1.0;
                    double piecePoints = ChessPiece.GetPiecePoints(piece.Type) * threatenedMultiplier;
                    points += piecePoints;
                }
            }

            return points;
        }

        // Bir taşın tehdit edilip edilmediğini kontrol eden metod
        public bool IsPieceThreatened(ChessPiece piece, List<ChessPiece> chessPieces)
        {
            foreach (ChessPiece opponent in chessPieces)
            {
                if (opponent.Color != piece.Color)
                {
                    if (IsPieceThreatenedByOpponent(opponent, piece, chessPieces))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Bir taşın belirli bir rakip tarafından tehdit edilip edilmediğini kontrol eden metod
        public bool IsPieceThreatenedByOpponent(ChessPiece piece, ChessPiece opponent, List<ChessPiece> chessPieces)
        {
            switch (piece.Type)
            {
                case ChessPieceType.Piyon:
                    return IsPawnThreatenedByOpponent(piece, opponent);
                case ChessPieceType.At:
                    return IsKnightThreatenedByOpponent(piece, opponent);
                case ChessPieceType.Fil:
                    return IsBishopThreatenedByOpponent(piece, opponent, chessPieces);
                case ChessPieceType.Kale:
                    return IsRookThreatenedByOpponent(piece, opponent, chessPieces);
                case ChessPieceType.Vezir:
                    return IsQueenThreatenedByOpponent(piece, opponent, chessPieces);
                case ChessPieceType.Şah:
                    return IsKingThreatenedByOpponent(piece, opponent);
                default:
                    return false;
            }
        }

        // Bir piyonun belirli bir rakip tarafından tehdit edilip edilmediğini kontrol eden metod
        public bool IsPawnThreatenedByOpponent(ChessPiece pawn, ChessPiece opponent)
        {
            int rowDiff = opponent.Row - pawn.Row;
            int colDiff = Math.Abs(opponent.Col - pawn.Col);

            if (pawn.Color == ChessPieceColor.Beyaz)
            {
                if (rowDiff == 1 && colDiff == 1 && opponent.Row > pawn.Row)
                    return true;
            }
            else if (pawn.Color == ChessPieceColor.Siyah)
            {
                if (rowDiff == -1 && colDiff == 1 && opponent.Row < pawn.Row)
                    return true;
            }

            return false;
        }

        // Bir atın belirli bir rakip tarafından tehdit edilip edilmediğini kontrol eden metod
        public bool IsKnightThreatenedByOpponent(ChessPiece knight, ChessPiece opponent)
        {
            int rowDiff = Math.Abs(knight.Row - opponent.Row);
            int colDiff = Math.Abs(knight.Col - opponent.Col);
            return (rowDiff == 2 && colDiff == 1) || (rowDiff == 1 && colDiff == 2);
        }

        // Bir filin belirli bir rakip tarafından tehdit edilip edilmediğini kontrol eden metod
        public bool IsBishopThreatenedByOpponent(ChessPiece bishop, ChessPiece opponent, List<ChessPiece> chessPieces)
        {
            int rowDiff = Math.Abs(bishop.Row - opponent.Row);
            int colDiff = Math.Abs(bishop.Col - opponent.Col);

            if (rowDiff == colDiff)
            {
                int rowStep = Math.Sign(opponent.Row - bishop.Row);
                int colStep = Math.Sign(opponent.Col - bishop.Col);

                int currentRow = bishop.Row + rowStep;
                int currentCol = bishop.Col + colStep;

                while (currentRow != opponent.Row || currentCol != opponent.Col)
                {
                    if (chessPieces.Any(p => p.Row == currentRow && p.Col == currentCol))
                    {
                        return false;
                    }

                    currentRow += rowStep;
                    currentCol += colStep;
                }

                return true;
            }

            return false;
        }

        // Bir kalin belirli bir rakip tarafından tehdit edilip edilmediğini kontrol eden metod
        public bool IsRookThreatenedByOpponent(ChessPiece rook, ChessPiece opponent, List<ChessPiece> chessPieces)
        {
            if (rook.Row == opponent.Row || rook.Col == opponent.Col)
            {
                int minRow = Math.Min(rook.Row, opponent.Row);
                int maxRow = Math.Max(rook.Row, opponent.Row);
                int minCol = Math.Min(rook.Col, opponent.Col);
                int maxCol = Math.Max(rook.Col, opponent.Col);

                bool blockingPieceFound = false;

                if (rook.Row == opponent.Row)
                {
                    for (int col = minCol + 1; col < maxCol; col++)
                    {
                        ChessPiece blockingPiece = chessPieces.FirstOrDefault(p => p.Row == rook.Row && p.Col == col);
                        if (blockingPiece != null)
                        {
                            blockingPieceFound = true;
                            break;
                        }
                    }
                }
                else
                {
                    for (int row = minRow + 1; row < maxRow; row++)
                    {
                        ChessPiece blockingPiece = chessPieces.FirstOrDefault(p => p.Row == row && p.Col == rook.Col);
                        if (blockingPiece != null)
                        {
                            blockingPieceFound = true;
                            break;
                        }
                    }
                }

                return !blockingPieceFound;
            }

            return false;
        }

        // Bir vezirin belirli bir rakip tarafından tehdit edilip edilmediğini kontrol eden metod
        public bool IsQueenThreatenedByOpponent(ChessPiece queen, ChessPiece opponent, List<ChessPiece> chessPieces)
        {
            int rowDiff = Math.Abs(queen.Row - opponent.Row);
            int colDiff = Math.Abs(queen.Col - opponent.Col);

            if (rowDiff == colDiff || queen.Row == opponent.Row || queen.Col == opponent.Col)
            {
                int rowStep = Math.Sign(opponent.Row - queen.Row);
                int colStep = Math.Sign(opponent.Col - queen.Col);

                int currentRow = queen.Row + rowStep;
                int currentCol = queen.Col + colStep;

                while (currentRow != opponent.Row || currentCol != opponent.Col)
                {
                    if (chessPieces.Any(p => p.Row == currentRow && p.Col == currentCol))
                    {
                        return false;
                    }

                    currentRow += rowStep;
                    currentCol += colStep;
                }

                return true;
            }

            return false;
        }

        // Bir şahın belirli bir rakip tarafından tehdit edilip edilmediğini kontrol eden metod
        public bool IsKingThreatenedByOpponent(ChessPiece king, ChessPiece opponent)
        {
            int rowDiff = Math.Abs(king.Row - opponent.Row);
            int colDiff = Math.Abs(king.Col - opponent.Col);
            return rowDiff <= 1 && colDiff <= 1;
        }

        // Sütun numarasını harf olarak döndüren metod
        public char GetColumnLetter(int col)
        {
            return (char)('A' + col);
        }
    }
}
