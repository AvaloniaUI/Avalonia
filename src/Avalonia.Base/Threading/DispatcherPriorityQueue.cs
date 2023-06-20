using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Threading;

namespace Avalonia.Threading;


internal class DispatcherPriorityQueue
{
    // Priority chains...
    private readonly SortedList<int, PriorityChain> _priorityChains; // NOTE: should be Priority
    private readonly Stack<PriorityChain> _cacheReusableChains;

    // Sequential chain...
    private DispatcherOperation? _head;
    private DispatcherOperation? _tail;
    
    public DispatcherPriorityQueue()
    {
        // Build the collection of priority chains.
        _priorityChains = new SortedList<int, PriorityChain>(); // NOTE: should be Priority
        _cacheReusableChains = new Stack<PriorityChain>(10);

        _head = _tail = null;
    }

    // NOTE: not used
    // public int Count {get{return _count;}}

    public DispatcherPriority MaxPriority // NOTE: should be Priority
    {
        get
        {
            int count = _priorityChains.Count;

            if (count > 0)
            {
                return _priorityChains.Keys[count - 1];
            }
            else
            {
                return DispatcherPriority.Invalid; // NOTE: should be Priority.Invalid;
            }
        }
    }

    public DispatcherOperation Enqueue(DispatcherPriority priority, DispatcherOperation item) // NOTE: should be Priority
    {
        // Find the existing chain for this priority, or create a new one
        // if one does not exist.
        PriorityChain chain = GetChain(priority);

        // Step 1: Append this to the end of the "sequential" linked list.
        InsertItemInSequentialChain(item, _tail);

        // Step 2: Append the item into the priority chain.
        InsertItemInPriorityChain(item, chain, chain.Tail);

        return item;
    }

    public DispatcherOperation Dequeue()
    {
        // Get the max-priority chain.
        int count = _priorityChains.Count;
        if (count > 0)
        {
            PriorityChain chain = _priorityChains.Values[count - 1];
            Debug.Assert(chain != null, "PriorityQueue.Dequeue: a chain should exist.");

            DispatcherOperation? item = chain.Head;
            Debug.Assert(item != null, "PriorityQueue.Dequeue: a priority item should exist.");

            RemoveItem(item);

            return item;
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    public DispatcherOperation? Peek()
    {
        // Get the max-priority chain.
        int count = _priorityChains.Count;
        if (count > 0)
        {
            PriorityChain chain = _priorityChains.Values[count - 1];
            Debug.Assert(chain != null, "PriorityQueue.Peek: a chain should exist.");

            DispatcherOperation? item = chain.Head;
            Debug.Assert(item != null, "PriorityQueue.Peek: a priority item should exist.");

            return item;
        }

        return null;
    }
    
    public void RemoveItem(DispatcherOperation item)
    {
        Debug.Assert(item != null, "PriorityQueue.RemoveItem: invalid item.");
        Debug.Assert(item.Chain != null, "PriorityQueue.RemoveItem: a chain should exist.");
        
        // Step 1: Remove the item from its priority chain.
        RemoveItemFromPriorityChain(item);

        // Step 2: Remove the item from the sequential chain.
        RemoveItemFromSequentialChain(item);
    }

    public void ChangeItemPriority(DispatcherOperation item, DispatcherPriority priority) // NOTE: should be Priority
    {
        // Remove the item from its current priority and insert it into
        // the new priority chain.  Note that this does not change the
        // sequential ordering.

        // Step 1: Remove the item from the priority chain.
        RemoveItemFromPriorityChain(item);

        // Step 2: Insert the item into the new priority chain.
        // Find the existing chain for this priority, or create a new one
        // if one does not exist.
        PriorityChain chain = GetChain(priority);
        InsertItemInPriorityChain(item, chain);
    }

    private PriorityChain GetChain(DispatcherPriority priority) // NOTE: should be Priority
    {
        PriorityChain? chain = null;

        int count = _priorityChains.Count;
        if (count > 0)
        {
            if (priority == _priorityChains.Keys[0])
            {
                chain = _priorityChains.Values[0];
            }
            else if (priority == _priorityChains.Keys[count - 1])
            {
                chain = _priorityChains.Values[count - 1];
            }
            else if ((priority > _priorityChains.Keys[0]) &&
                     (priority < _priorityChains.Keys[count - 1]))
            {
                _priorityChains.TryGetValue(priority, out chain);
            }
        }

        if (chain == null)
        {
            if (_cacheReusableChains.Count > 0)
            {
                chain = _cacheReusableChains.Pop();
                chain.Priority = priority;
            }
            else
            {
                chain = new PriorityChain(priority);
            }

            _priorityChains.Add(priority, chain);
        }

        return chain;
    }

    private void InsertItemInPriorityChain(DispatcherOperation item, PriorityChain chain)
    {
        // Scan along the sequential chain, in the previous direction,
        // looking for an item that is already in the new chain.  We will
        // insert ourselves after the item we found.  We can short-circuit
        // this search if the new chain is empty.
        if (chain.Head == null)
        {
            Debug.Assert(chain.Tail == null,
                "PriorityQueue.InsertItemInPriorityChain: both the head and the tail should be null.");
            InsertItemInPriorityChain(item, chain, null);
        }
        else
        {
            Debug.Assert(chain.Tail != null,
                "PriorityQueue.InsertItemInPriorityChain: both the head and the tail should not be null.");

            DispatcherOperation? after;

            // Search backwards along the sequential chain looking for an
            // item already in this list.
            for (after = item.SequentialPrev; after != null; after = after.SequentialPrev)
            {
                if (after.Chain == chain)
                {
                    break;
                }
            }

            InsertItemInPriorityChain(item, chain, after);
        }
    }

    internal void InsertItemInPriorityChain(DispatcherOperation item, PriorityChain chain, DispatcherOperation? after)
    {
        Debug.Assert(chain != null, "PriorityQueue.InsertItemInPriorityChain: a chain must be provided.");
        Debug.Assert(item.Chain == null && item.PriorityPrev == null && item.PriorityNext == null,
            "PriorityQueue.InsertItemInPriorityChain: item must not already be in a priority chain.");

        item.Chain = chain;

        if (after == null)
        {
            // Note: passing null for after means insert at the head.

            if (chain.Head != null)
            {
                Debug.Assert(chain.Tail != null,
                    "PriorityQueue.InsertItemInPriorityChain: both the head and the tail should not be null.");

                chain.Head.PriorityPrev = item;
                item.PriorityNext = chain.Head;
                chain.Head = item;
            }
            else
            {
                Debug.Assert(chain.Tail == null,
                    "PriorityQueue.InsertItemInPriorityChain: both the head and the tail should be null.");

                chain.Head = chain.Tail = item;
            }
        }
        else
        {
            item.PriorityPrev = after;

            if (after.PriorityNext != null)
            {
                item.PriorityNext = after.PriorityNext;
                after.PriorityNext.PriorityPrev = item;
                after.PriorityNext = item;
            }
            else
            {
                Debug.Assert(item.Chain.Tail == after,
                    "PriorityQueue.InsertItemInPriorityChain: the chain's tail should be the item we are inserting after.");
                after.PriorityNext = item;
                chain.Tail = item;
            }
        }

        chain.Count++;
    }

    private void RemoveItemFromPriorityChain(DispatcherOperation item)
    {
        Debug.Assert(item != null, "PriorityQueue.RemoveItemFromPriorityChain: invalid item.");
        Debug.Assert(item.Chain != null, "PriorityQueue.RemoveItemFromPriorityChain: a chain should exist.");

        // Step 1: Fix up the previous link
        if (item.PriorityPrev != null)
        {
            Debug.Assert(item.Chain.Head != item,
                "PriorityQueue.RemoveItemFromPriorityChain: the head should not point to this item.");

            item.PriorityPrev.PriorityNext = item.PriorityNext;
        }
        else
        {
            Debug.Assert(item.Chain.Head == item,
                "PriorityQueue.RemoveItemFromPriorityChain: the head should point to this item.");

            item.Chain.Head = item.PriorityNext;
        }

        // Step 2: Fix up the next link
        if (item.PriorityNext != null)
        {
            Debug.Assert(item.Chain.Tail != item,
                "PriorityQueue.RemoveItemFromPriorityChain: the tail should not point to this item.");

            item.PriorityNext.PriorityPrev = item.PriorityPrev;
        }
        else
        {
            Debug.Assert(item.Chain.Tail == item,
                "PriorityQueue.RemoveItemFromPriorityChain: the tail should point to this item.");

            item.Chain.Tail = item.PriorityPrev;
        }

        // Step 3: cleanup
        item.PriorityPrev = item.PriorityNext = null;
        item.Chain.Count--;
        if (item.Chain.Count == 0)
        {
            if (item.Chain.Priority == _priorityChains.Keys[_priorityChains.Count - 1])
            {
                _priorityChains.RemoveAt(_priorityChains.Count - 1);
            }
            else
            {
                _priorityChains.Remove(item.Chain.Priority);
            }

            if (_cacheReusableChains.Count < 10)
            {
                item.Chain.Priority = DispatcherPriority.Invalid;
                _cacheReusableChains.Push(item.Chain);
            }
        }

        item.Chain = null;
    }

    internal void InsertItemInSequentialChain(DispatcherOperation item, DispatcherOperation? after)
    {
        Debug.Assert(item.SequentialPrev == null && item.SequentialNext == null,
            "PriorityQueue.InsertItemInSequentialChain: item must not already be in the sequential chain.");

        if (after == null)
        {
            // Note: passing null for after means insert at the head.

            if (_head != null)
            {
                Debug.Assert(_tail != null,
                    "PriorityQueue.InsertItemInSequentialChain: both the head and the tail should not be null.");

                _head.SequentialPrev = item;
                item.SequentialNext = _head;
                _head = item;
            }
            else
            {
                Debug.Assert(_tail == null,
                    "PriorityQueue.InsertItemInSequentialChain: both the head and the tail should be null.");

                _head = _tail = item;
            }
        }
        else
        {
            item.SequentialPrev = after;

            if (after.SequentialNext != null)
            {
                item.SequentialNext = after.SequentialNext;
                after.SequentialNext.SequentialPrev = item;
                after.SequentialNext = item;
            }
            else
            {
                Debug.Assert(_tail == after,
                    "PriorityQueue.InsertItemInSequentialChain: the tail should be the item we are inserting after.");
                after.SequentialNext = item;
                _tail = item;
            }
        }
    }

    private void RemoveItemFromSequentialChain(DispatcherOperation item)
    {
        Debug.Assert(item != null, "PriorityQueue.RemoveItemFromSequentialChain: invalid item.");

        // Step 1: Fix up the previous link
        if (item.SequentialPrev != null)
        {
            Debug.Assert(_head != item,
                "PriorityQueue.RemoveItemFromSequentialChain: the head should not point to this item.");

            item.SequentialPrev.SequentialNext = item.SequentialNext;
        }
        else
        {
            Debug.Assert(_head == item,
                "PriorityQueue.RemoveItemFromSequentialChain: the head should point to this item.");

            _head = item.SequentialNext;
        }

        // Step 2: Fix up the next link
        if (item.SequentialNext != null)
        {
            Debug.Assert(_tail != item,
                "PriorityQueue.RemoveItemFromSequentialChain: the tail should not point to this item.");

            item.SequentialNext.SequentialPrev = item.SequentialPrev;
        }
        else
        {
            Debug.Assert(_tail == item,
                "PriorityQueue.RemoveItemFromSequentialChain: the tail should point to this item.");

            _tail = item.SequentialPrev;
        }

        // Step 3: cleanup
        item.SequentialPrev = item.SequentialNext = null;
    }
}


internal class PriorityChain
{
    public PriorityChain(DispatcherPriority priority) // NOTE: should be Priority
    {
        Priority = priority;
    }

    public DispatcherPriority Priority { get; set; } // NOTE: should be Priority

    public int Count { get; set; }

    public DispatcherOperation? Head { get; set; }

    public DispatcherOperation? Tail { get; set; }
}