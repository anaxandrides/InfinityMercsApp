using InfinityMercsApp.Presentation.Leagues;

namespace InfinityMercsApp.Views.Controls;

/// <summary>Draws an invite's QR code, black on white, scaled to fill the view.</summary>
public sealed class InviteQrDrawable(InviteQr qr) : IDrawable
{
    private readonly PathF _modules = PathBuilder.Build(qr.Path);

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var side = Math.Min(dirtyRect.Width, dirtyRect.Height);
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(0, 0, side, side);
        canvas.Scale(side / qr.Size, side / qr.Size);
        canvas.FillColor = Colors.Black;
        canvas.FillPath(_modules);
    }
}
