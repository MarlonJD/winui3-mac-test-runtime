namespace Windows.Foundation
{
    public delegate void TypedEventHandler<TSender, TResult>(TSender sender, TResult args);
}

namespace Windows.System
{
    public enum VirtualKey
    {
        Number1,
        Number2,
        Number3,
        Number4,
        Number5,
        Number6,
        Number7,
        R,
        N,
        M
    }

    [Flags]
    public enum VirtualKeyModifiers
    {
        None = 0,
        Control = 1,
        Shift = 2
    }
}
