using System;

namespace Eiredynamic.Pharos;

public class ProbeEventArgs<T> : EventArgs
{
    public T Event { get; }

    public ProbeEventArgs(T eventInfo)
    {
        Event = eventInfo;
    }
}