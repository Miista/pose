using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Pose.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static System.Console;

namespace Pose.Tests
{
    [TestClass]
    public class ShimHelperTests
    {
        [TestMethod]
        public void TestGetMethodFromExpressionThrowNotImplemented()
        {
            Expression<Func<bool>> expr = () => true;
            Expression<Func<DateTime>> expr1 = () => DateTime.MaxValue;

            Assert.ThrowsException<NotImplementedException>(
                () => ShimHelper.GetMethodFromExpression(expr.Body, false, out Object instance));

            Assert.ThrowsException<NotImplementedException>(
                () => ShimHelper.GetMethodFromExpression(expr1.Body, false, out Object instance));
        }

        [TestMethod]
        public void TestGetMethodFromExpressionValid()
        {
            Expression<Func<DateTime>> expr = () => DateTime.Now;
            Expression<Func<string>> expr1 = () => ReadLine();

            Assert.AreEqual<MethodBase>(typeof(DateTime).GetMethod("get_Now"),
                ShimHelper.GetMethodFromExpression(expr.Body, false, out Object instance));

            Assert.AreEqual<MethodBase>(typeof(Console).GetMethod("ReadLine"),
                ShimHelper.GetMethodFromExpression(expr1.Body, false, out instance));
        }

        public static async Task<int> GetAsync() => await Task.FromResult(1);
        
        [TestMethod]
        public void TestGetAsyncMethodFromExpressionValid()
        {
            Expression<Func<DateTime>> expr = () => DateTime.Now;
            Expression<Func<Task<int>>> expr1 = () => GetAsync();

            Assert.AreEqual<MethodBase>(typeof(DateTime).GetMethod("get_Now"),
                ShimHelper.GetMethodFromExpression(expr.Body, false, out Object instance));

            Assert.AreEqual<MethodBase>(typeof(ShimHelperTests).GetMethod(nameof(GetAsync)),
                ShimHelper.GetMethodFromExpression(expr1.Body, false, out instance));
        }

        [TestMethod]
        public void TestGetObjectInstanceFromExpressionValueType()
        {
            DateTime dateTime = new DateTime();
            Expression<Func<DateTime>> expression = () => dateTime.AddDays(2);

            Assert.ThrowsException<NotSupportedException>(
                () => ShimHelper.GetObjectInstanceOrType((expression.Body as MethodCallExpression).Object));
        }

        [TestMethod]
        public void TestGetObjectInstanceFromExpression()
        {
            ShimHelperTests shimHelperTests = new ShimHelperTests();
            Expression<Action> expression = () => shimHelperTests.TestGetObjectInstanceFromExpression();
            var instance = ShimHelper.GetObjectInstanceOrType((expression.Body as MethodCallExpression).Object);

            Assert.IsNotNull(instance);
            Assert.AreEqual<Type>(typeof(ShimHelperTests), instance.GetType());
            Assert.AreSame(shimHelperTests, instance);
            Assert.AreNotSame(new ShimHelperTests(), instance);
        }
    }
}
