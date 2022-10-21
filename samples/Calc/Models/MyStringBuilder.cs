using System;
using System.Text;

namespace Calc.Models;

public sealed class MyStringBuilder
{
    private readonly StringBuilder _stringBuilder = new();

    public int Length => _stringBuilder.Length;
    
    public char this[int index] => _stringBuilder[index];

    public char this[Index index] => _stringBuilder[index];

    public string this[Range range] {
        get
        {
            var chain = new MyStringBuilder();
            
            for (var i = range.Start.Value; i < range.End.Value; i++)
            {
                chain.Append(this[i]);
            }

            return chain.ToString();
        }
    }

    public MyStringBuilder(){}
    
    public MyStringBuilder(string s)
    {
        _stringBuilder.Append(s);
    }

    public override string ToString()
    {
        return _stringBuilder.ToString();
    }

    public MyStringBuilder Append<T>(T stuff)
    {
        _stringBuilder.Append(stuff);

        return this;
    }

    public MyStringBuilder Clear()
    {
        _stringBuilder.Clear();

        return this;
    }

    public int Count(char character)
    {
        char[] chars = { character };
        return Count(chars);
    }
    
    public int Count(char[] chars)
    {
        var count = 0;
        var i = 0;

        while (i < Length)
        {
            foreach (var character in chars)
            {
                if (this[i].Equals(character))
                {
                    count++;
                    break;
                }
            }
            i++;
        }

        return count;
    }

    public int IndexOf(char character, int startIndex = 0)
    {
        char[] chars = { character };
        return IndexOfAny(chars, startIndex);
    }

    public int IndexOfAny(char[] chars, int startIndex = 0)
    {
        var i = startIndex;
        
        while (i >= 0 && i < Length)
        {
            foreach (var character in chars)
            {
                if (this[i].Equals(character))
                    return i;
            }
            i++;
        }

        return -1;
    }

    public MyStringBuilder Insert<T>(int index, T stuff)
    {
        _stringBuilder.Insert(index, stuff);

        return this;
    }
    
    public int LastIndexOf(char character)
    {
        return LastIndexOf(character, Length - 1);
    }
    
    public int LastIndexOf(char character, int startIndex)
    {
        char[] chars = { character };
        return LastIndexOfAny(chars, startIndex);
    }

    public int LastIndexOfAny(char[] chars)
    {
        return LastIndexOfAny(chars, Length - 1);
    }
    
    public int LastIndexOfAny(char[] chars, int startIndex)
    {
        var i = startIndex;
        while (i >= 0 && i < Length)
        {
            foreach (var character in chars)
            {
                if (this[i].Equals(character))
                    return i;
            }
            i--;
        }

        return -1;
    }
    
    public MyStringBuilder Remove(int index, int length = 1)
    {
        _stringBuilder.Remove(index, length);

        return this;
    }

    public MyStringBuilder Replace<T>(int startIndex, int endIndex, T replacement)
    {
        Remove(startIndex, endIndex - startIndex)
            .Insert(startIndex, replacement);
        
        return this;
    }
}