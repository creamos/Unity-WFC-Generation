using System;
using UnityEngine;

[Serializable]
public class CircularStack<T>
{
    [field: Min(1)] public int bufferSize { get; private set; }
    int position;
    int end;
    [SerializeField] T[] buffer;
    public T[] RawContent() => buffer;
    public int EndIndex => end;
    public int StartIndex => position;
    
    public bool Empty { get; private set; }

#if NAUGHTY_ATTR
    [NaughtyAttributes.ShowNativeProperty] 
#endif
    public int Count => 
        Empty 
            ? 0 
            : position > end 
                ? position - end 
                : position + bufferSize - end;

    public CircularStack (uint size)
    {
        bufferSize = (int) size;
        buffer = new T[size];
        position = 0;
        end = 0;
        Empty = true;
    }

    public int GetLastRef()
    {
        if (Empty) throw new IndexOutOfRangeException();
        return (position+bufferSize-1)%bufferSize;
    }

    public T Peek ()
    {
        if (Empty) throw new IndexOutOfRangeException();
        return buffer[(position+bufferSize-1)%bufferSize];
    }

    public void Push(T data)
    {
        bool writeOverLastEntry = !Empty && position == end;
        
        buffer[position] = data;
        position = (position + 1) % (int)bufferSize;
        Empty = false;
        
        if (writeOverLastEntry) end = position;
    }

    public T Pop()
    {
        if (Empty) throw new IndexOutOfRangeException();
        
        position = (position + bufferSize - 1) % bufferSize;
        Empty = position == end;

        return buffer[position];
    }
}
