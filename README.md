# MiniScript-cs

This is an alternative version of the excellent MiniScript language by Joe Strout [MiniScript scripting language](http://miniscript.org) written in C#.

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

Tested in Unity `2020.3.3f1` and `NetFramework v4.7.1`

Example of using it in Unity:
`
using System.ComponentModel;
using Miniscript.interpreter;
using Miniscript.intrinsic;
using UnityEngine;

public class Demo : MonoBehaviour {

    private const string CODE = "drawPixel 10, 10" + "\n" +
                                "a = help(\"getPixel\")" + "\n" +
                                "print a" + "\n" +
                                "b = help(\"drawPixel\")" + "\n" +
                                "print b" + "\n" +
                                "c = category(all)" + "\n" +
                                "print c" + "\n" +
                                "d = help(all)" + "\n" +
                                "print d";
    
    private void Start() {
        var interpreter = new Interpreter(CODE, Debug.Log, Debug.Log );
        // I embed public methods from this class into MiniScript
        FunctionInjector.AddFunctions(this, Debug.Log);
        
        interpreter.Compile();
        interpreter.RunUntilDone(60, false);
    }
    
    [Description(
        "\n   Draw pixel on screen." +
        "\n"
    )]
    [Category("graphics")]
    public void DrawPixel(int x, int y) {
        Debug.Log($"DrawPixel at x: {x}, y: {y}");
    }
    
    public void GetPixel(int x, int y) {
        Debug.Log($"GetPixel at x: {x}, y: {y}");
    }

}
`


