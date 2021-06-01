# MiniScript-cs

This is an alternative version of the excellent [MiniScript scripting language](http://miniscript.org) by Joe Strout written in C#.

The main idea was to use the features of modern versions of C# and to add new features that make it easier to embed functions and support them.
Here is a list of the main features:
- automatic embedding of methods (`FunctionInjector.cs`)
- cataloging the methods you want to embed in MiniScript (built-in "category" function, using `CategoryAttribute` in C#)
- output signatures of built-in functions and their descriptions (built-in "help" function, using `DescriptionAttribute` in C#)
- using the C# 8.0 syntax with some restrictions (everything that works in `Unity 2020`, at the moment this is a restriction on the language version)

Despite the fact that I wanted to make the code as compatible as possible with the original version of [MiniScript-cs](https://github.com/JoeStrout/miniscript/tree/master/MiniScript-cs) there are differences:
- the `toString()` method now returns the function name, for example, `print @rnd` will return `rnd(seed)` not  `FUNCTION(seed)` 

There may be other differences, but I will try to keep them to a minimum.
You can always see the differences by comparing the files for testing `TestSuiteData.txt` in repositories.

Tested in Unity `2020.3.3f1, 2021.1.4f1` and `NetFramework v4.7.1, netcoreapp2.2, net5.0`.

Supported language version: `C# 8`.


Example of using it in Unity:
```
using System.ComponentModel;
using Miniscript.interpreter;
using Miniscript.intrinsic;
using UnityEngine;

public class Demo : MonoBehaviour {

    private const string CODE = "print \"Help 'foo':\n\" + help(\"foo\");" +
                                "foo 10, 10;" +
                                "print \"Help 'bar':\n\" + help(\"bar\");" +
                                "bar;" +
                                "print \"All categories of functions:\" + category(all);" +
                                "print \"All functions:\" + help(all)";
    
    private void Start() {
        // I embed public methods from this class into MiniScript and specify log output
        // to see information about all injected functions
        FunctionInjector.AddFunctions(this, Debug.Log);
        
        var interpreter = new Interpreter(CODE, Debug.Log, Debug.Log);
        interpreter.Compile();
        interpreter.RunUntilDone(60, false);
    }
    
    [Description(
        "\n   Description of function 'foo'."
    )]
    [Category("foo_bar")]
    public void Foo(int x, int y) {
        Debug.Log($"Here the function 'foo' performs some actions with parameters x: {x}, y: {y}");
    }
    
    public void Bar() {
        Debug.Log($"Here the function 'bar' performs some actions");
    }
    
    private void Baz() {
        // Private functions will not be injected!
    }

}
```


