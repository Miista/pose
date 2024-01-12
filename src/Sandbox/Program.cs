// See https://aka.ms/new-console-template for more information

using System;
using Pose;

namespace Pose.Sandbox
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var dateTimeShim = Shim.Replace(() => T.I).With(() => "L");
            var dateTimeShim1 = Shim.Replace(() => T.Get()).With(() => "Word");
            var inst = new Inst();
            var f = new Func<Inst, string>(i => "Word");
            var dateTimeShim2 = Shim.Replace(() => inst.S).With(f);
            var dateTimeShim3 = Shim.Replace(() => inst.Get()).With(f);
            var dateTimeShim4 = Shim.Replace(() => Is.A<Inst>().S).With(f);
            var dateTimeShim5 = Shim.Replace(() => Is.A<Inst>().Get()).With(f);
            var dateTimeShim6 = Shim.Replace(() => Is.A<Inst>().Get()).With(delegate(Inst @this) { return "Word"; });
            
            PoseContext.Isolate(
                () =>
                {
                    // Console.Write(T.I);
                    // Console.WriteLine(T.Get());
                    Console.WriteLine(inst.S);
                    // Console.WriteLine(T.I);
                }, dateTimeShim, dateTimeShim4);
        }
    }

    public class Inst
    {
        public string S { get; set; } = "_";

        public string Get()
        {
            return "h";
        }
    }
    
    public static class T
    {
        public static string I
        {
            get { return "H"; }
        }

        public static string Get() => "Hello";
    }
}