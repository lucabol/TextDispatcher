using System;
using TextDispatcher;

namespace Foo
{
    [Dispatcher]
    public partial class C
    {
        [NoDispatch]
        static void Main()
        {
            var c = new C();
            c.Dispatch("M");
            c.Dispatch("N");
            c.Dispatch("S");
            c.Dispatch("64");
            c.Dispatch("'Not recognizable string'");

            var d = new D();
            d.Dispatch("M");

            var dpmu = new DontPickMeUp();
            // Correctly doesn't have dispatch to this class as it doesn't have a the marker attribute
            
            try { new D().Dispatch("K1");} catch(Exception) { Console.WriteLine("OK not dispatching to multiple parameters methods.");}
            try { new D().Dispatch("K2");} catch(Exception) { Console.WriteLine("OK not dispatching to not void returning methods.");}
        }

        void M() => Console.WriteLine("OK calling an instance method.");
        void N() => Console.WriteLine("OK calling another instance method.");
        static void S() => Console.WriteLine("OK calling static methods.");

        void ParseInt(int value) => Console.WriteLine($"OK parsing the int got {value}");
        void ParseString(string value) => Console.WriteLine($"OK parsing the string got {value}");

        void K1(string c) => Console.WriteLine("Cannot dispatch on this method as it is not empty");
        string K2(string c) => "Cannot dispatch on this because it returns a string instead of void";
    }

    [Dispatcher]
    public partial class D {
        
        void M() => Console.WriteLine("OK having multiple dispatchers.");
    }

    public partial class DontPickMeUp {
        
        void M() => Console.WriteLine("D:Test1");
    }
}
