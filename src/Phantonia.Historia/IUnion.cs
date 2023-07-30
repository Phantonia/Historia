using System;

namespace Phantonia.Historia;

public interface IUnion<out T0, out T1>
{
    int Discriminator { get; }

    T0 Value0 { get; }

    T1 Value1 { get; }

    object? AsObject();

    void Run(Action<T0> action0, Action<T1> action1);

    T Evaluate<T>(Func<T0, T> action0, Func<T1, T> action1);
}

public interface IUnion<out T0, out T1, out T2>
{
    int Discriminator { get; }

    T0 Value0 { get; }

    T1 Value1 { get; }

    T2 Value2 { get; }

    object? AsObject();

    void Run(Action<T0> action0, Action<T1> action1, Action<T2> action2);

    T Evaluate<T>(Func<T0, T> action0, Func<T1, T> action1, Func<T2, T> action2);
}

public interface IUnion<out T0, out T1, out T2, out T3>
{
    int Discriminator { get; }

    T0 Value0 { get; }

    T1 Value1 { get; }

    T2 Value2 { get; }

    T3 Value3 { get; }

    object? AsObject();

    void Run(Action<T0> action0, Action<T1> action1, Action<T2> action2, Action<T3> action3);

    T Evaluate<T>(Func<T0, T> action0, Func<T1, T> action1, Func<T2, T> action2, Func<T3, T> action3);
}

public interface IUnion<out T0, out T1, out T2, out T3, out T4>
{
    int Discriminator { get; }

    T0 Value0 { get; }

    T1 Value1 { get; }

    T2 Value2 { get; }

    T3 Value3 { get; }

    T4 Value4 { get; }

    object? AsObject();

    void Run(Action<T0> action0, Action<T1> action1, Action<T2> action2, Action<T3> action3, Action<T4> action4);

    T Evaluate<T>(Func<T0, T> action0, Func<T1, T> action1, Func<T2, T> action2, Func<T3, T> action3, Func<T4, T> action4);
}

public interface IUnion<out T0, out T1, out T2, out T3, out T4, out T5>
{
    int Discriminator { get; }

    T0 Value0 { get; }

    T1 Value1 { get; }

    T2 Value2 { get; }

    T3 Value3 { get; }

    T4 Value4 { get; }

    T5 Value5 { get; }

    object? AsObject();

    void Run(Action<T0> action0, Action<T1> action1, Action<T2> action2, Action<T3> action3, Action<T4> action4, Action<T5> action5);

    T Evaluate<T>(Func<T0, T> action0, Func<T1, T> action1, Func<T2, T> action2, Func<T3, T> action3, Func<T4, T> action4, Func<T5, T> action5);
}

public interface IUnion<out T0, out T1, out T2, out T3, out T4, out T5, out T6>
{
    int Discriminator { get; }

    T0 Value0 { get; }

    T1 Value1 { get; }

    T2 Value2 { get; }

    T3 Value3 { get; }

    T4 Value4 { get; }

    T5 Value5 { get; }

    T6 Value6 { get; }

    object? AsObject();

    void Run(Action<T0> action0, Action<T1> action1, Action<T2> action2, Action<T3> action3, Action<T4> action4, Action<T5> action5, Action<T6> action6);

    T Evaluate<T>(Func<T0, T> action0, Func<T1, T> action1, Func<T2, T> action2, Func<T3, T> action3, Func<T4, T> action4, Func<T5, T> action5, Func<T6, T> action6);
}

public interface IUnion<out T0, out T1, out T2, out T3, out T4, out T5, out T6, out T7>
{
    int Discriminator { get; }

    T0 Value0 { get; }

    T1 Value1 { get; }

    T2 Value2 { get; }

    T3 Value3 { get; }

    T4 Value4 { get; }

    T5 Value5 { get; }

    T6 Value6 { get; }

    T7 Value7 { get; }

    object? AsObject();

    void Run(Action<T0> action0, Action<T1> action1, Action<T2> action2, Action<T3> action3, Action<T4> action4, Action<T5> action5, Action<T6> action6, Action<T7> action7);

    T Evaluate<T>(Func<T0, T> action0, Func<T1, T> action1, Func<T2, T> action2, Func<T3, T> action3, Func<T4, T> action4, Func<T5, T> action5, Func<T6, T> action6, Func<T7, T> action7);
}

public interface IUnion<out T0, out T1, out T2, out T3, out T4, out T5, out T6, out T7, out T8>
{
    int Discriminator { get; }

    T0 Value0 { get; }

    T1 Value1 { get; }

    T2 Value2 { get; }

    T3 Value3 { get; }

    T4 Value4 { get; }

    T5 Value5 { get; }

    T6 Value6 { get; }

    T7 Value7 { get; }

    T8 Value8 { get; }

    object? AsObject();

    void Run(Action<T0> action0, Action<T1> action1, Action<T2> action2, Action<T3> action3, Action<T4> action4, Action<T5> action5, Action<T6> action6, Action<T7> action7, Action<T8> action8);

    T Evaluate<T>(Func<T0, T> action0, Func<T1, T> action1, Func<T2, T> action2, Func<T3, T> action3, Func<T4, T> action4, Func<T5, T> action5, Func<T6, T> action6, Func<T7, T> action7, Func<T8, T> action8);
}

public interface IUnion<out T0, out T1, out T2, out T3, out T4, out T5, out T6, out T7, out T8, out T9>
{
    int Discriminator { get; }

    T0 Value0 { get; }

    T1 Value1 { get; }

    T2 Value2 { get; }

    T3 Value3 { get; }

    T4 Value4 { get; }

    T5 Value5 { get; }

    T6 Value6 { get; }

    T7 Value7 { get; }

    T8 Value8 { get; }

    T9 Value9 { get; }

    object? AsObject();

    void Run(Action<T0> action0, Action<T1> action1, Action<T2> action2, Action<T3> action3, Action<T4> action4, Action<T5> action5, Action<T6> action6, Action<T7> action7, Action<T8> action8, Action<T9> action9);

    T Evaluate<T>(Func<T0, T> action0, Func<T1, T> action1, Func<T2, T> action2, Func<T3, T> action3, Func<T4, T> action4, Func<T5, T> action5, Func<T6, T> action6, Func<T7, T> action7, Func<T8, T> action8, Func<T9, T> action9);
}
