# VeryBasic
An experiment in natural language programming. VeryBasic is a programming language 
which strives to make its syntax entirely human-readable, without any prior knowledge
of programming languages; the syntax only requires knowledge of English to understand.

## Build instructions

1. Install the .NET SDK 9.0 if you don't already have it.
    1.  Go to the [downloads page](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
        for Microsoft .NET SDK 9.0.
    2.  Download and install the version compatible with your system.
2. Compile and run the application
    1. Inside the VeryBasic.Repl directory, run `dotnet run`. This will compile and run the 
       REPL interface.

## Tutorial

### Preface: Using the REPL

The REPL is a simple, interactive way to get started using VeryBasic. Code will
be executed as soon as you type in a line of it. To enter multiple lines, type a "\\"
(backslash) on the end of the line. After doing this two dots ("..") will appear before your cursor
No code has been executed yet. Keep putting backslashes to type more lines. Once you're done,
don't include one. This will now run your multi-line code.

### Hello, world!

Type the code below into the REPL and press enter.

```verybasic
Print "hello world!".
```

> hello world!

Statements are terminated with "." as sentence are in English, and 
capitalization (except in strings) is unimportant. No unnecessary 
symbols are used in this function call: just a simple command in
English.

You can call other functions too, like `take input`.

```verybasic
Take input.
```

The REPL will now print a "?" prompting the user for input.

### Control flow

Control flow is simple, essentially expressed in English as well.

```verybasic
If 1 = 1 then print "hi!". 
Otherwise print "bye!". 
Done.
```

> hi!

#### The result

The function `take input` cannot be used in expression; all function calls are statements. Instead
its return value will be stored inside the system variable `the result`. Note that if you try to
access it after statement that did not return anything, it will produce an error.

#### Loops

##### Repeat loop

The repeat loop repeats code a variable number of times.

```verybasic
Repeat 1 + 1 times
Print "hi".
Done.
```

> hi \
> hi

##### While loop

The while loop checks a condition every iteration, including the first. If it is false,
the loop terminates.

```verybasic
Take input.
While the result =/= "exit" take input. Done.
```

### Variables

Variables are created using an easy-to-understand syntax that expresses what it does
in plain English.

```verybasic
Create variable x, a number, from 40.
Change x to [x] + 2. 
If [x] = 42 then print "The meaning of life!". Otherwise print "Meaningless!". Done.
```

Note that variable references are enclosed in brackets ("[]").

## License

This project is licensed under the GNU GPLv2. See [LICENSE](LICENSE).