# The OverScript Programming Language

This is the main source code repository for [OverScript]. 
OverScript is a simple and powerful C-like statically-typed language written in C# and is great for both embedding in .NET programs and building standalone applications. The project was developed from scratch without looking back at traditional approaches to creating languages. The unique approach allows the language to go beyond the standard features and have great potential for improvement.

[OverScript]: https://overscript.org/

Simple code example:
```
Point[] arr = new Point[]{new Point(25, 77), new Point(122, 219)}; //creating an array of two instances
int n; // 0 by default
foreach(Point p in arr){ // iterating over all elements of an array
    n++;
    WriteLine($"{n}) {p.X}; {p.Y}"); // outputting values using string interpolation
}
//1) 25; 77
//2) 122; 219
ReadKey();

class Point{
    public int X, Y;
    New(int x, int y){ // constructor
        X=x;
        Y=y;
    }
}
```


