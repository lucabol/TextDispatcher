using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TextDispatcher;
using static System.Console;

namespace Forth;

[Dispatcher]
partial class DirectForth
{
    Stack<int> stack = new ();

    void ParseInt(int v) => stack.Push(v);
    void ParseString(string s) {
        if(s[0] == '"' && s[s.Length - 1] == '"') 
          Write(s.Substring(1, s.Length - 1));
        else
          throw new Exception("Not a match.");
    }

    [Symbol("+")] void Plus()   => stack.Push(stack.Pop() + stack.Pop());
    [Symbol("-")] void Minus()  => stack.Push(stack.Pop() - stack.Pop());
    [Symbol("*")] void Mult()   => stack.Push(stack.Pop() * stack.Pop());
    [Symbol("/")] void Div()    => stack.Push(stack.Pop() / stack.Pop());
    [Symbol("%")] void Mod()    => stack.Push(stack.Pop() % stack.Pop());
    [Symbol(".")] void Print()  => Write($"{stack.Pop()} ");

    void drop()     => stack.Pop();
    void dup()      => stack.Push(stack.Peek());
    void deep()     => stack.Push(stack.Count());
    void swap() { var x = stack.Pop(); var y = stack.Pop(); stack.Push(y); stack.Push(x);}

    [NoDispatch]
    void ExecLine(string line) => SplitBetter(line).ToList().ForEach(Dispatch);

    [NoDispatch]
    internal static void MainTest()
    {
        DirectForth f = new();
        f.ExecLine(@"50 50 + dup . ""= 100"" drop 30 60 swap - . ""= 30"" 2 3 % . ""= 1"" deep . ""= 0""'");
    }
    static Regex regex = new Regex("(?<match>[^\\s\"]+)|(?<match>\"[^\"]*\")");

    [NoDispatch]
    static IEnumerable<string> SplitBetter(string line)
        => regex.Matches(line).Cast<Match>().Select(m => m.Value);
}
