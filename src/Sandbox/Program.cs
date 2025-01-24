// See https://aka.ms/new-console-template for more information

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Pose.Sandbox
{
    public class Program
    {
        static void Constrain<TT>(TT a) where TT : IA{
            Console.WriteLine(a.GetString());
        }
        
        static void ConstrainD<TT>(TT a) where TT : D{
            Console.WriteLine(a.GetString2());
        }

        static void Box<TT>(TT a) where TT : B{
            Console.WriteLine(a.GetInt());
        }
        
        static void BoxD<TT>(TT a) where TT : B{
            Console.WriteLine(a.GetString2());
        }

        interface IA {
            string GetString();
        }
        
        abstract class B : D {
            public int GetInt(){return 0;}
        }

        abstract class D
        {
            public string GetString2() => "Wuu?";
        }
        
        class A : B, IA {
            public string GetString() => "Hello, World";
        }

        struct C : IA
        {
            public string GetString() => "Wee";
        }

        public class OverridenOperatorClass
        {
            public static explicit operator bool(OverridenOperatorClass c) => false;

            public static implicit operator int(OverridenOperatorClass c) => int.MinValue;

            public static OverridenOperatorClass operator +(OverridenOperatorClass l, OverridenOperatorClass r) => default(OverridenOperatorClass);
        }

        public class T
        {
            public double value { get; set; }
            public bool WasCalled { get; set; }
        }

        static Action<double> GetSetterForX(Expression<Func<double>> expression)
        {
            var parameter = Expression.Parameter(typeof(double), "value");
            var body = Expression.Assign(expression.Body, parameter);
            var lambda = Expression.Lambda<Action<double>>(body, parameter);
            return lambda.Compile();
        }
        
        static BinaryExpression CreateSetValue<TType>(Expression<Func<TType>> expression, TType assignValue)
        {
            LambdaExpression lambdaExpression = expression;
            var lambdaExpressionBody = lambdaExpression.Body as MemberExpression ?? throw new Exception($"Cannot get member expression from {expression}");
            var property = lambdaExpressionBody.Member as PropertyInfo ?? throw new Exception($"Cannot get property info from {expression}");
            var propertyExpression = Expression.Property(lambdaExpressionBody.Expression, property);

            return Expression.Assign(
                propertyExpression,
                Expression.Constant(assignValue)
            );
        }
        
        static void SetValue<TType>(Expression<Func<TType>> expression, TType assignValue)
        {
            var lambda = Expression.Lambda(
                CreateSetValue(expression, assignValue)
            );

            lambda.Compile().DynamicInvoke();
        }

        static Expression<Func<TReturn>> SetAndReturn<TType, TReturn>(Expression<Func<TType>> expression, TType assignValue, TReturn returnValue)
        {
            var lambda = Expression.Lambda<Func<TReturn>>(
                Expression.Block(
                    CreateSetValue(expression, assignValue),
                    Expression.Constant(returnValue)
                )
            );

            return lambda;
        }
        
        public static void Main(string[] args)
        {
            int tt = 0;
            
            Action<TType> SetX<TType>(Expression<Func<TType>> expression)
            {
                var parameter = Expression.Parameter(typeof(TType), "value");
                var body = Expression.Assign(expression.Body, parameter);
                var lambda = Expression.Lambda<Action<TType>>(body, parameter);
                return lambda.Compile();
            }
            
            
            var foo = new T();
            // var action = SetX(() => foo.value);
            // var x1 = SetX(() => tt);
            // SetValue(() => foo.WasCalled, true);
            // var invocationExpression = Expression.Lambda<Func<string>>(
            //     Expression.Block(
            //         CreateSetValue(() => foo.WasCalled, true),
            //         Expression.Constant("Hello, World")
            //     )
            // );
            var andReturn = SetAndReturn(() => foo.WasCalled, true, "Hello, World!");
            var dynamicInvoke = andReturn.Compile().Invoke();
            var setterForX = GetSetterForX(() => foo.value);

            // var hoisted = Expression.Field(typeof(T).GetProperty("I"), "Value");
            //
            // var lambdaExpression = Expression.Lambda(
            //     Expression.Assign(
            //         hoisted,
            //         Expression.Constant(3)
            //     )
            // );
            // var dynamicInvoke = lambdaExpression.Compile().DynamicInvoke();
            // Console.WriteLine(dynamicInvoke);
#if NET8_0
            Console.WriteLine(".NET 8");
            
            PoseContext.Isolate(
                () =>
                {
                    Console.WriteLine("Hello, World!");
                }
                , Shim.Replace(() => Console.WriteLine(Is.A<string>())).With(() => Console.WriteLine("Yo"))
            );
#elif NET48
            Console.WriteLine("4.8");
            var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
            PoseContext.Isolate(
                () =>
                {
                    Console.WriteLine(DateTime.Now);
                }, dateTimeShim);
#elif NETCOREAPP2_0
            Console.WriteLine("2.0");

            var with = Shim.Replace(() => Is.A<B>().GetInt()).With(delegate(B x) { return 5;});
            var with1 = Shim.Replace(() => Is.A<D>().GetString2()).With(delegate(D x) { return "HeyD";});
            var with2 = Shim.Replace(() => Is.A<A>().GetString()).With(delegate(A x) { return "Hey";});
            var shim = Shim.Replace(() => Is.A<C>().GetString()).With(delegate(ref C @this) { return "Hey2"; });

            // var with2 = Shim.Replace(() => Is.A<IA>().GetString()).With(delegate(IA x) { return "Hey";});
            PoseContext.Isolate(
                () =>
                {
                    var a = new A();
                    // Box(a);
                    Console.WriteLine(a.GetString());
                    Constrain(a);
                    ConstrainD(a);
                    BoxD(a);

                    var c = new C();
                    Console.WriteLine(c.GetString());
                    Constrain(c);
                }, with, with1, shim, with2);
            
            // var sut1 = new OverridenOperatorClass();
            // int s = sut1;
            // Shim.Replace(() => Is.A<OverridenOperatorClass>() + Is.A<OverridenOperatorClass>())
            //     .With(delegate(OverridenOperatorClass l, int r) { return default(OverridenOperatorClass); });
            // var operatorShim = Shim.Replace(() => (bool) sut1)
            //     .With(delegate (OverridenOperatorClass c) { return true; });
            // var dateTimeAddShim = Shim.Replace(() => Is.A<DateTime>() + Is.A<TimeSpan>())
            //     .With(delegate(DateTime dt, TimeSpan ts) { return new DateTime(2004, 01, 01); });
            // var dateTimeSubtractShim = Shim.Replace(() => Is.A<DateTime>() - Is.A<TimeSpan>())
            //     .With(delegate(DateTime dt, TimeSpan ts) { return new DateTime(1990, 01, 01); });
            //
            // PoseContext.Isolate(
            //     () =>
            //     {
            //         var dateTime = DateTime.Now;
            //         Console.WriteLine($"Date: {dateTime}");
            //         var ts = TimeSpan.FromSeconds(1);
            //         Console.WriteLine($"Time: {ts}");
            //
            //         var time = dateTime + ts;
            //         Console.WriteLine($"Result1: {time}");
            //
            //         var time2 = dateTime - ts;
            //         Console.WriteLine($"Result2: {time2}");
            //     }, dateTimeAddShim, dateTimeSubtractShim
            // );
#elif NET6_0
            Console.WriteLine("6.0");
            var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
            PoseContext.Isolate(
                () =>
                {
                    Console.WriteLine(DateTime.Now);
                }, dateTimeShim);
#elif NET7_0
            Console.WriteLine("7.0");

            var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
            PoseContext.Isolate(
                () =>
                {
                    Console.WriteLine(DateTime.Now);
                }, dateTimeShim);
#elif NETCOREAPP3_0
            Console.WriteLine("3.0");
            var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
            PoseContext.Isolate(
                () =>
                {
                    Console.WriteLine(DateTime.Now);
                }, dateTimeShim);
#else
            Console.WriteLine("Other");
            var dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 1, 1));
            PoseContext.Isolate(
                () =>
                {
                    Console.WriteLine(DateTime.Now);
                }, dateTimeShim);
#endif

            // var dateTimeShim = Shim.Replace(() => T.I).With(() => "L");
            // var dateTimeShim1 = Shim.Replace(() => T.Get()).With(() => "Word");
            // var inst = new Inst();
            // var f = new Func<Inst, string>(i => "Word");
            // var dateTimeShim2 = Shim.Replace(() => inst.S).With(f);
            // var dateTimeShim3 = Shim.Replace(() => inst.Get()).With(f);
            // var dateTimeShim4 = Shim.Replace(() => Is.A<Inst>().S).With(f);
            // var dateTimeShim5 = Shim.Replace(() => Is.A<Inst>().Get()).With(f);
            // var dateTimeShim6 = Shim.Replace(() => Is.A<Inst>().Get()).With(delegate(Inst @this) { return "Word"; });
            //
            // PoseContext.Isolate(
            //     () =>
            //     {
            //         // Console.Write(T.I);
            //         // Console.WriteLine(T.Get());
            //         try
            //         {
            //             Console.WriteLine(inst.S);
            //         }
            //         catch (Exception e) { }
            //         finally { }
            //
            //         // Console.WriteLine(T.I);
            //     }, dateTimeShim, dateTimeShim4);
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