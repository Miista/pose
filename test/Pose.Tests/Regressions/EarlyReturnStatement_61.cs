using System;
using FluentAssertions;
using Xunit;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable InconsistentNaming

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

#nullable enable

namespace Pose.Tests.Regressions
{
    public class EarlyReturnStatement_61
    {
        [Fact(DisplayName = "Poser ignoring early return statement #61")]
        public void Does_not_ignore_early_return_statement()
        {
            // Arrange
            var secondStaticMethodCalled = false;
            var shim1 = Shim
                .Replace(() => LegacyClass.FirstStaticMethodThatThrowsException())
                .With(delegate { });

            var shim2 = Shim
                .Replace(() => ClassWithStaticMethod.SecondStaticMethodThatThrowsException())
                .With(delegate
                {
                    secondStaticMethodCalled = true;
                });
            
            // Act
            var outObj = default(ServiceOutput);
            PoseContext.Isolate(() =>
            {
                var inputObj = new ServiceInput
                {
                    Email = "user@example.com",
                    Id = 0
                };

                var service = new ServiceUnderTest();
                outObj = service.Create(inputObj);
            }, shim1, shim2);
            
            // Assert
            outObj.Should().NotBeNull();
            outObj._ExtendedDescription.Should().Be("Id must be set");
            outObj._OperationStatus.Should().Be(-2);
            secondStaticMethodCalled.Should().BeFalse();
        }

        /*
         * The classes below were provided in the issue https://github.com/Miista/pose/issues/61.
         * Specifically: https://github.com/mai-pai/poser-bug-reproduction
         */
        
        private class ServiceInput
        {
            public string? Email { get; set; }
            public int Id { get; set; }
        }

        private class ServiceOutput
        {
            public int _OperationStatus;
            public string _ExtendedDescription;

            public string Email { get; set; }
            public int Id { get; set; }

            public void SetStatus_Failure(Exception ex)
            {
                _OperationStatus = -1;
                _ExtendedDescription = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}";
            }

            public void SetStatus_Success()
            {
                _OperationStatus = 0;
                _ExtendedDescription = string.Empty;
            }

            public void SetStatus_Failure(string message)
            {
                _OperationStatus = -2;
                _ExtendedDescription = message;
            }
        }

        private class ClassWithStaticMethod
        {
            public static void SecondStaticMethodThatThrowsException()
            {
                throw new Exception("This exception should not have occurred.");
            }
        }

        private class ErrorHandler
        {
            public static void PublishError(object extendedDescription)
            {
            }
        }

        private class LegacyLogger
        {
            public static void WriteLogInfo(string extendedDescription)
            {
            }
        }

        private class ServiceUnderTest
        {
            public ServiceOutput Create(ServiceInput input)
            {
                var serviceOutput = new ServiceOutput();
                try
                {
                    LegacyClass.FirstStaticMethodThatThrowsException();

                    if (input.Id < 1)
                    {
                        serviceOutput.SetStatus_Failure("Id must be set");
                        return serviceOutput;
                    }

                    serviceOutput.Email = input.Email?.Trim() ?? string.Empty;

                    ClassWithStaticMethod.SecondStaticMethodThatThrowsException();
                }
                catch (Exception ex)
                {
                    serviceOutput.SetStatus_Failure(ex);
                }
                finally
                {
                    if (serviceOutput._OperationStatus != 0)
                    {
                        ErrorHandler.PublishError(serviceOutput._ExtendedDescription);
                    }

                    LegacyLogger.WriteLogInfo(serviceOutput._ExtendedDescription);
                }

                return serviceOutput;
            }
        }

        private class LegacyClass
        {
            public static void FirstStaticMethodThatThrowsException()
            {
                throw new Exception("Exception from Legacy Class");
            }
        }
    }
}