using System.Collections.Generic;
using System.Linq;
using TextDispatcher;
using static System.Console;

namespace Forth;

[Dispatcher]
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
    void ExecLine(string line) => line.Split(null).ToList().ForEach(Dispatch);

    [NoDispatch]
    internal static void MainTest()
    {
        DirectForth f = new();

        void Assert(string expected, string line) {
            Write($"{expected,5} == "); f.ExecLine(line); WriteLine();
        }
        Assert("100", "50 50 + dup .");
        Assert("30", "drop 30 60 swap - .");
        Assert("1", "2 3 % .");
        Assert("0", "deep .");
    }
}
