using System;
using FluentAssertions;
using Pose.IL;
using Xunit;

namespace Pose.Tests
{
    public class MethodDisassemblerTests
    {
        [Fact]
        public void Can_get_IL_instructions()
        {
            // Arrange
            var methodDisassembler = new MethodDisassembler(typeof(Console).GetMethod(nameof(Console.Clear)));

            // Act
            var instructions = methodDisassembler.GetILInstructions();

            // Assert
            instructions.Should().NotBeNullOrEmpty();
        }
    }
}