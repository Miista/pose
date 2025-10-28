using System;
using FluentAssertions;
using Xunit;
using DateTime = System.DateTime;

namespace Pose.Tests.Regressions
{
    // ReSharper disable once InconsistentNaming
    public class EnumIsDefined_26
    {
        private enum TestEnum { A }
        
        [Fact(DisplayName = "Enum.IsDefined cannot be called from within PoseContext.Isolate #26")]
        public void Can_call_EnumIsDefined_from_Isolate()
        {
            // Arrange
            var shim = Shim
                .Replace(() => new DateTime(2024, 2, 2))
                .With((int year, int month, int day) => new DateTime(2004, 1, 1));
            var isDefined = false;
            
            // Act
            PoseContext.Isolate(
                () =>
                {
                    isDefined = Enum.IsDefined(typeof(TestEnum), nameof(TestEnum.A));
                }, shim);
            
            // Assert
            isDefined.Should().BeTrue(because: "Enum.IsDefined can be called from Isolate");
        }
    }
}