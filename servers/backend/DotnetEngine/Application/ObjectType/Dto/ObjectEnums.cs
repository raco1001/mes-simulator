namespace DotnetEngine.Application.ObjectType.Dto;

public enum DataType
{
    Number,
    String,
    Boolean,
    DateTime,
    Array,
    Object
}

public enum SimulationBehavior
{
    Constant,
    Settable,
    Rate,
    Accumulator,
    Derived
}

public enum Mutability
{
    Immutable,
    Mutable
}

public enum Persistence
{
    Permanent,
    Durable,
    Transient
}

public enum Dynamism
{
    Static,
    Dynamic,
    Reactive
}

public enum Cardinality
{
    Singular,
    Enumerable,
    Streaming
}
