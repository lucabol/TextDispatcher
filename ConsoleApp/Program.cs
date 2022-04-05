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

            var d = new D();
            d.Dispatch("M");

            var dpmu = new DontPickMeUp();
            // Correctly doesn't have dipatch
            
        }

        void M() => Console.WriteLine("C:Test1");
        void N() => Console.WriteLine("C:Test2");

        // Don't dispatch on this method
        void K(string c) => Console.WriteLine("Cannot dispatch on this method as it is not empty");
    }

    [Dispatcher]
    public partial class D {
        
        void M() => Console.WriteLine("D:Test1");
    }

    public partial class DontPickMeUp {
        
        void M() => Console.WriteLine("D:Test1");
    }
}
