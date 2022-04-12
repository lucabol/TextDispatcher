using System.Collections.Generic;
using System.Linq;
using TextDispatcher;
using static System.Console;

namespace Forth;

[Dispatcher][Encoder]
partial class DirectForth
{
    Stack<int> stack = new ();

    void ParseInt(int v) => stack.Push(v);

    [Symbol("+")] void plus()   => stack.Push(stack.Pop() + stack.Pop());
    [Symbol("-")] void minus()  => stack.Push(stack.Pop() - stack.Pop());
    [Symbol("*")] void mult()   => stack.Push(stack.Pop() * stack.Pop());
    [Symbol("/")] void div()    => stack.Push(stack.Pop() / stack.Pop());
    [Symbol("%")] void mod()    => stack.Push(stack.Pop() % stack.Pop());
    [Symbol(".")] void print()  => Write($"{stack.Pop()} ");

    void drop()     => stack.Pop();
    void dup()      => stack.Push(stack.Peek());
    void deep()     => stack.Push(stack.Count());
    void swap() { var x = stack.Pop(); var y = stack.Pop(); stack.Push(y); stack.Push(x);}


    [NoDispatch]
    void Clear() => stack.Clear();

    [NoDispatch]
    void ExecLine(string line) => line.Split(null).ToList().ForEach(Dispatch);

    [NoDispatch]
    IEnumerable<DirectForthToken> EncodeLine(string line)
        => line.Split(null).Select(Encode);

    [NoDispatch]
    string DecodeTokens(IEnumerable<DirectForthToken> tokens)
        => System.String.Join(" ", tokens.Select(Decode));

    [NoDispatch]
    internal static void MainTest()
    {
        DirectForth f = new();

        void Report(string expected, string line) {
            Write($"\t{expected,5} == "); f.ExecLine(line); WriteLine();
        }
        WriteLine("\nFORTH CAN EXECUTE:");
        Report("100", "50 50 + dup .");
        Report("30", "drop 30 60 swap - .");
        Report("1", "2 3 % .");
        Report("0", "deep .");

        WriteLine("\nFORTH CAN ENCODE & DECODE:");
        f.Clear();
        f.ExecLine("50");
        var encoded = f.EncodeLine("dup . .");
        var decoded = f.DecodeTokens(encoded);
        Write("\t");
        f.ExecLine(decoded);
        WriteLine("== 50 50");
    }
}
