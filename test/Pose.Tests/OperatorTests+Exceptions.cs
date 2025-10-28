using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using Xunit;

// ReSharper disable UnusedParameter.Local
// ReSharper disable ConvertToLambdaExpression

namespace Pose.Tests
{
    public partial class OperatorTests
    {
        public class OperatorExceptionsData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                var arithmeticOperators = ArithmeticOperators
                    .Concat(BitwiseAndShit)
                    .Concat(Equality)
                    .Concat(Boolean)
                    .Concat(Conversion);

                return arithmeticOperators.GetEnumerator();
            }

            private static IEnumerable<object[]> ArithmeticOperators
            {
                get
                {
                    // Addition
                    yield return TestCase(
                        () => Shim.Replace(() => Is.A<int>() + Is.A<int>())
                            .With(delegate(int l, int r) { return int.MinValue; })
                    );

                    // Subtraction
                    yield return TestCase(
                        () => Shim.Replace(() => Is.A<int>() - Is.A<int>())
                            .With(delegate(int l, int r) { return int.MinValue; })
                    );

                    // Multiplication
                    yield return TestCase(
                        () => Shim.Replace(() => Is.A<int>() * Is.A<int>())
                            .With(delegate(int l, int r) { return int.MinValue; })
                    );

                    // Division
                    yield return TestCase(
                        () => Shim.Replace(() => Is.A<int>() / Is.A<int>())
                            .With(delegate(int l, int r) { return int.MinValue; })
                    );

                    // Modulus
                    yield return TestCase(
                        () => Shim.Replace(() => ~Is.A<int>())
                            .With(delegate(int l) { return int.MinValue; })
                    );

                    // Unary plus
                    yield return TestCase(
                        () => Shim.Replace(() => +Is.A<int>())
                            .With(delegate(int l) { return int.MinValue; })
                    );

                    // Unary minus
                    yield return TestCase(
                        () => Shim.Replace(() => -Is.A<int>())
                            .With(delegate(int l) { return int.MinValue; })
                    );
                }
            }

            private static IEnumerable<object[]> BitwiseAndShit
            {
                get
                {
                    // Left shift
                    yield return TestCase(
                        () => Shim.Replace(() => Is.A<int>() << Is.A<int>())
                            .With(delegate(int l, int r) { return int.MinValue; })
                    );

                    // Right shift
                    yield return TestCase(
                        () => Shim.Replace(() => Is.A<int>() >> Is.A<int>())
                            .With(delegate(int l, int r) { return int.MinValue; })
                    );
                }
            }

            [SuppressMessage("ReSharper", "EqualExpressionComparison")]
            private static IEnumerable<object[]> Equality
            {
                get
                {
                    // Equal
                    yield return TestCase(
                        () => Shim.Replace(() => Is.A<int>() == Is.A<int>())
                            .With(delegate(int l, int r) { return int.MinValue; })
                    );

                    // Not equal
                    yield return TestCase(
                        () => Shim.Replace(() => Is.A<int>() != Is.A<int>())
                            .With(delegate(int l, int r) { return int.MinValue; })
                    );

                    // Less than
                    yield return TestCase(
                        () => Shim.Replace(() => Is.A<int>() < Is.A<int>())
                            .With(delegate(int l, int r) { return int.MinValue; })
                    );

                    // Greater than
                    yield return TestCase(
                        () => Shim.Replace(() => Is.A<int>() > Is.A<int>())
                            .With(delegate(int l, int r) { return int.MinValue; })
                    );

                    // Less than or equal to
                    yield return TestCase(
                        () => Shim.Replace(() => Is.A<int>() <= Is.A<int>())
                            .With(delegate(int l, int r) { return int.MinValue; })
                    );

                    // Greater than or equal to
                    yield return TestCase(
                        () => Shim.Replace(() => Is.A<int>() >= Is.A<int>())
                            .With(delegate(int l, int r) { return int.MinValue; })
                    );
                }
            }

            private static IEnumerable<object[]> Conversion
            {
                get
                {
                    yield return TestCase(
                        () => Shim.Replace(() => (long) Is.A<int>())
                            .With(delegate(int l) { return default(long); })
                    );
                }
            }
            
            private static IEnumerable<object[]> Boolean
            {
                get
                {
                    // Logical negation
                    yield return TestCase(
                        () => Shim.Replace(() => !Is.A<bool>())
                            .With(delegate(bool b) { return false; })
                    );

                    // Logical AND
                    yield return TestCase(
                        () => Shim.Replace(() => Is.A<bool>() & Is.A<bool>())
                            .With(delegate(bool l, bool r) { return default(bool); })
                    );

                    // Exclusive OR
                    yield return TestCase(
                        () => Shim.Replace(() => Is.A<bool>() ^ Is.A<bool>())
                            .With(delegate(bool l, bool r) { return default(bool); })
                    );
                    
                    // Logical OR
                    yield return TestCase(
                        () => Shim.Replace(() => Is.A<bool>() | Is.A<bool>())
                            .With(delegate(bool l, bool r) { return default(bool); })
                    );
                }
            }
            private static object[] TestCase(Func<Shim> func)
            {
                return new object[] { func };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [ClassData(typeof(OperatorExceptionsData))]
        public void Throws_exception_if_the_operator_cannot_be_shimmed(Func<Shim> shimFactory)
        {
            shimFactory.Should().Throw<Exception>(because: "the operator cannot be shimmed");
        }
    }
}