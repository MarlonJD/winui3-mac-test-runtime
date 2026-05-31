using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml.Input;

public sealed class KeyboardAccelerator
{
    public VirtualKey Key { get; set; }

    public VirtualKeyModifiers Modifiers { get; set; }

    public event TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs>? Invoked;

    public void Invoke()
    {
        var args = new KeyboardAcceleratorInvokedEventArgs();
        Invoked?.Invoke(this, args);
    }
}

public sealed class KeyboardAcceleratorInvokedEventArgs : EventArgs
{
    public bool Handled { get; set; }
}
