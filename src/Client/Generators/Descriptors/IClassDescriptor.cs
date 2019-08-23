using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public interface IClassDescriptor
        : ICodeDescriptor
    {
        INamedType Type { get; }

        IReadOnlyList<IFieldDescriptor> Fields { get; }

        IReadOnlyList<IInterfaceDescriptor> Implements { get; }
    }
}
