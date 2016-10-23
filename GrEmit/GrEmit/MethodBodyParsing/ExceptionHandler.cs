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

using System;
using System.Text;

namespace GrEmit.MethodBodyParsing
{
    public enum ExceptionHandlerType
    {
        Catch = 0,
        Filter = 1,
        Finally = 2,
        Fault = 4,
    }

    public sealed class ExceptionHandler
    {
        public ExceptionHandler(ExceptionHandlerType handlerType)
        {
            this.HandlerType = handlerType;
        }

        public Instruction TryStart { get; set; }

        public Instruction TryEnd { get; set; }

        public Instruction FilterStart { get; set; }

        public Instruction HandlerStart { get; set; }

        public Instruction HandlerEnd { get; set; }

        public object CatchType { get; set; }

        public ExceptionHandlerType HandlerType { get; set; }

        public override string ToString()
        {
            var result = new StringBuilder();

            result.AppendLine(HandlerType.ToString());
            result.AppendLine(String.Format("TryStart: {0}, TryEnd: {1}", TryStart, TryEnd));
            result.AppendLine(String.Format("HandlerStart: {0}, HandlerEnd: {1}", HandlerStart, HandlerEnd));

            return result.ToString();
        }
    }
}