//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
// Copyright (c) 2016 Igor Chevdar
//
// Licensed under the MIT/X11 license.
//

namespace GrEmit.MethodBodyParsing
{
    public class InstructionCollection : Collection<Instruction>
    {
        internal InstructionCollection()
        {
        }

        internal InstructionCollection(int capacity)
            : base(capacity)
        {
        }

        public Instruction GetInstruction(int offset)
        {
            var size = this.size;
            var items = this.items;
            if(offset < 0 || offset > items[size - 1].Offset)
                return null;

            int min = 0;
            int max = size - 1;
            while(min <= max)
            {
                int mid = min + ((max - min) / 2);
                var instruction = items[mid];
                var instruction_offset = instruction.Offset;

                if(offset == instruction_offset)
                    return instruction;

                if(offset < instruction_offset)
                    max = mid - 1;
                else
                    min = mid + 1;
            }

            return null;
        }

        protected override void OnAdd(Instruction item, int index)
        {
            if(index == 0)
                return;

            var previous = items[index - 1];
            previous.Next = item;
            item.Previous = previous;
        }

        protected override void OnInsert(Instruction item, int index)
        {
            if(size == 0)
                return;

            var current = items[index];
            if(current == null)
            {
                var last = items[index - 1];
                last.Next = item;
                item.Previous = last;
                return;
            }

            var previous = current.Previous;
            if(previous != null)
            {
                previous.Next = item;
                item.Previous = previous;
            }

            current.Previous = item;
            item.Next = current;
        }

        protected override void OnSet(Instruction item, int index)
        {
            var current = items[index];

            item.Previous = current.Previous;
            item.Next = current.Next;

            current.Previous = null;
            current.Next = null;
        }

        protected override void OnRemove(Instruction item, int index)
        {
            var previous = item.Previous;
            if(previous != null)
                previous.Next = item.Next;

            var next = item.Next;
            if(next != null)
                next.Previous = item.Previous;

            item.Previous = null;
            item.Next = null;
        }
    }
}