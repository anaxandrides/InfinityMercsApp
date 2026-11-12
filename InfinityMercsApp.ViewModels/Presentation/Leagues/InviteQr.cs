using System.Globalization;
using System.Text;
using QRCoder;

namespace InfinityMercsApp.Presentation.Leagues;

/// <summary>
/// A QR code as one SVG path of unit squares, with its quiet zone, for the site to draw as SVG and the app to fill as a
/// path, at any size.
/// </summary>
/// <param name="Size">How many modules wide and high it is, the quiet zone included.</param>
/// <param name="Path">SVG path data: one "Mx yh1v1h-1z" square per dark module.</param>
public sealed record InviteQr(int Size, string Path)
{
    public static InviteQr For(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        var rows = data.ModuleMatrix;
        var path = new StringBuilder();
        for (var y = 0; y < rows.Count; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                if (rows[y][x])
                {
                    path.Append(CultureInfo.InvariantCulture, $"M{x} {y}h1v1h-1z");
                }
            }
        }

        return new InviteQr(rows.Count, path.ToString());
    }
}
