using System;
using System.Collections.Generic;
using System.Reflection;

namespace Pose.IL.DebugHelpers
{
    internal class DynamicMethodBody : MethodBody
    {
        private readonly byte[] _ilBytes;

        public DynamicMethodBody(byte[] ilBytes, IList<LocalVariableInfo> locals)
        {
            _ilBytes = ilBytes;
            LocalVariables = locals;
        }

        public override int LocalSignatureMetadataToken => throw new NotImplementedException();

        public override IList<LocalVariableInfo> LocalVariables { get; }

        public override int MaxStackSize => throw new NotImplementedException();

        public override bool InitLocals => throw new NotImplementedException();

        public override byte[] GetILAsByteArray() => _ilBytes;

        public override IList<ExceptionHandlingClause> ExceptionHandlingClauses => throw new NotImplementedException();
    }
}