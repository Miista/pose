[![Build status](https://dev.azure.com/palmund/Pose/_apis/build/status/Pose-CI?branchName=master)](https://dev.azure.com/palmund/Pose/_build/latest?definitionId=12)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![NuGet version](https://badge.fury.io/nu/Poser.svg)](https://www.nuget.org/packages/Poser)
# Poser

Poser allows you to replace any .NET method (including static and non-virtual) with a delegate. It is similar to [Microsoft Fakes](https://msdn.microsoft.com/en-us/library/hh549175.aspx) but unlike it Poser is implemented _entirely_ in managed code (Reflection Emit API). Everything occurs at runtime and in-memory, no unmanaged Profiling APIs and no file system pollution with re-written assemblies.

Poser is cross platform and runs anywhere .NET is supported. It targets .NET Standard 2.0 so it can be used across .NET platforms including .NET Framework, .NET Core, Mono and Xamarin. See version compatibility table [here](https://docs.microsoft.com/en-us/dotnet/standard/net-standard).

## Installation

Available on [NuGet](https://www.nuget.org/packages/Poser/)

Visual Studio:

```powershell
PM> Install-Package Poser
```

.NET Core CLI:

```bash
dotnet add package Poser
```

## Usage

Poser gives you the ability to create shims by way of the `Shim` class. Shims are basically objects that let you specify the method you want to replace as well as the replacement delegate. Delegate signatures (arguments and return type) must match that of the methods they replace. The `Is` class is used to create instances of a type and all code you want to apply your shims to is isolated using the `PoseContext` class.


### Shim static method

```csharp
using Pose;

Shim consoleShim = Shim.Replace(() => Console.WriteLine(Is.A<string>())).With(
    delegate (string s) { Console.WriteLine("Hijacked: {0}", s); });
```

### Shim static property getter

```csharp
using Pose;

Shim dateTimeShim = Shim.Replace(() => DateTime.Now).With(() => new DateTime(2004, 4, 4));
```

### Shim static property setter

```csharp
using Pose;

Shim setterShim = Shim.Replace(() => Console.Title, true).With((string title) => { Console.Title = "My Title"; });
```

### Shim instance property getter

```csharp
using Pose;

class MyClass
{
    public int MyProperty { get; set; }
    public void DoSomething() => Console.WriteLine("doing someting");
}

Shim classPropShim = Shim.Replace(() => Is.A<MyClass>().MyProperty).With((MyClass @this) => 100);
```

### Shim instance property setter

```csharp
using Pose;

Shim classPropShim = Shim.Replace(() => Is.A<MyClass>().MyProperty, true).With((MyClass @this, int prop) => { @this.MyProperty = prop * 10; });
```

### Shim constructor

```csharp
using Pose;

Shim ctorShim = Shim.Replace(() => new MyClass()).With(() => new MyClass() { MyProperty = 10 });
```

### Shim instance method of a Reference Type

```csharp
using Pose;

Shim classShim = Shim.Replace(() => Is.A<MyClass>().DoSomething()).With(
    delegate (MyClass @this) { Console.WriteLine("doing someting else"); });
```

_Note: The first argument to an instance method replacement delegate is always the instance of the class_

### Shim method of specific instance of a Reference Type

```csharp
using Pose;

MyClass myClass = new MyClass();
Shim myClassShim = Shim.Replace(() => myClass.DoSomething()).With(
    delegate (MyClass @this) { Console.WriteLine("doing someting else with myClass"); });
```

### Shim instance method of a Value Type

```csharp
using Pose;

Shim structShim = Shim.Replace(() => Is.A<MyStruct>().DoSomething()).With(
    delegate (ref MyStruct @this) { Console.WriteLine("doing someting else"); });
```

_Note: You cannot shim methods on specific instances of Value Types_

### Shim static async method
```csharp
using Pose;

Shim staticTaskShim = Shim.Replace(() => DoWorkAsync()).With(
    delegate
    {
        Console.Write("refusing to do work");
        return Task.CompletedTask;
    });
```
### Shim async instance method of a Reference Type
```csharp
using Pose;

Shim instanceTaskShim = Shim.Replace(() => Is.A<MyClass>().DoSomethingAsync()).With(
    delegate(MyClass @this)
    {
        Console.WriteLine("doing something else async");
        return Task.CompletedTask;
    });
```

### Shim method of specific instance of a Reference Type
_Not supported for now. When supported, however, it will look like the following._

```csharp
using Pose;

MyClass myClass = new MyClass();
Shim myClassTaskShim = Shim.Replace(() => myClass.DoSomethingAsync()).With(
    delegate(MyClass @this)
    {
        Console.WriteLine("doing something else with myClass async");
        return Task.CompletedTask;
    });
```

### Isolating your code

#### Non-async
```csharp
// This block executes immediately
PoseContext.Isolate(() =>
{
    // All code that executes within this block
    // is isolated and shimmed methods are replaced

    // Outputs "Hijacked: Hello World!"
    Console.WriteLine("Hello World!");

    // Outputs "4/4/04 12:00:00 AM"
    Console.WriteLine(DateTime.Now);

    // Outputs "doing someting else"
    new MyClass().DoSomething();

    // Outputs "doing someting else with myClass"
    myClass.DoSomething();

}, consoleShim, dateTimeShim, classPropShim, classShim, myClassShim, structShim);
```

#### Async
```csharp
// This block executes immediately
await PoseContext.Isolate(async () =>
{
    // All code that executes within this block
    // is isolated and shimmed methods are replaced

    // Outputs "refusing to do work"
    await DoWorkAsync();
    
    // Outputs "doing something else async"
    new MyClass().DoSomethingAsync();

    // Outputs "doing something else with myClass async"
    await myClass.DoSomethingAsync();
    
}, staticTaskShim, instanceTaskShim, myClassTaskShim);
```
## Caveats & Limitations

* **Breakpoints** - At this time any breakpoints set anywhere in the isolated code and its execution path will not be hit. However, breakpoints set within a shim replacement delegate are hit.
* **Exceptions** - At this time all unhandled exceptions thrown in isolated code and its execution path are always wrapped in `System.Reflection.TargetInvocationException`.

## Roadmap

* **Performance Improvements** - Poser can be used outside the context of unit tests. Better performance would make it suitable for use in production code, possibly to override legacy functionality.
* **Exceptions Stack Trace** - Currently when exceptions are thrown in your own code under isolation, the supplied exception stack trace is quite confusing. Providing an undiluted exception stack trace is needed.

## Issues & Contributions

If you find a bug or have a feature request, please report them at this repository's issues section. Contributions are highly welcome, however, except for very small changes kindly file an issue and let's have a discussion before you open a pull request.

## License

This project is licensed under the MIT license. See the [LICENSE](LICENSE) file for more info.
