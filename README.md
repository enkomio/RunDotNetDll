# RunDotNetDll - It is simple utility to list all methods of a given .NET Assembly and to invoke them.

_RunDotNetDll_ allows to introspect a given .NET Assembly in order to list all the methods which are implemented in the Assembly and to invoke them. All this is done via pure Reflection.

I created this utility in order to easily analyze malicious .NET programs that load at runtime additional .NET Assembly. Once that you have extracted the Assembly you need a mean to run it but if it is a DLL is not so easy. 

With RunDotNetDll you can invoke a specific method of the given Assembly.

## Release Download
 - [Source code][1]
 - [Download binary][2]
 
## Usage
_RunDotNetDll_ has a syntax similar to RunDll32, it accepts a DLL and a method to invoke. If no method is provided, a list of all defined methods is displayed.

Find below an example of execution:

<img src="https://github.com/enkomio/RunDotNetDll/blob/master/media/test_run.gif"></img>

## Authors

* **Antonio Parata** - *Core Developer* - [s4tan](https://twitter.com/s4tan)

## License

RunDotNetDll is licensed under the [MIT license](LICENSE.TXT)

  [1]: https://github.com/enkomio/RunDotNetDll/tree/master/Src
  [2]: https://github.com/enkomio/RunDotNetDll/releases/latest