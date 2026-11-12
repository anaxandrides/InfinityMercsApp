using InfinityMercsApp.Presentation.Leagues;

namespace InfinityMercsApp.Views.Controls;

/// <summary>A league invite's QR code, which a phone's camera opens as the invite link.</summary>
public sealed class InviteQrView : GraphicsView
{
    public static readonly BindableProperty QrProperty = BindableProperty.Create(
        nameof(Qr), typeof(InviteQr), typeof(InviteQrView), propertyChanged: OnQrChanged);

    public InviteQr? Qr
    {
        get => (InviteQr?)GetValue(QrProperty);
        set => SetValue(QrProperty, value);
    }

    private static void OnQrChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var view = (InviteQrView)bindable;
        view.Drawable = newValue is InviteQr qr ? new InviteQrDrawable(qr) : null;
        view.Invalidate();
    }
}
