using System;
using TextDispatcher;

namespace Foo
{
    [Dispatcher]
    public partial class C
    {
        static void Main()
        {
            var c = new C();
            c.Dispatch("M");
            c.Dispatch("N");
            c.Dispatch("S");

            var d = new D();
            d.Dispatch("M");

            var dpmu = new DontPickMeUp();
            // Correctly doesn't have dipatch to this class as it doesn't have a the marker attribute
            
            try { new C().Dispatch("K1");} catch(Exception) { Console.WriteLine("OK not dispatching to multiple parameters methods.");}
            try { new C().Dispatch("K2");} catch(Exception) { Console.WriteLine("OK not dispatching to not void returning methods.");}
        }

        void M() => Console.WriteLine("OK calling an instance method.");
        void N() => Console.WriteLine("OK calling another instance method.");
        static void S() => Console.WriteLine("OK calling static methods.");

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
