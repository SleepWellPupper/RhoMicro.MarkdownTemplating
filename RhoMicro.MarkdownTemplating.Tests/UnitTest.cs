// SPDX-License-Identifier: MPL-2.0

namespace RhoMicro.MarkdownTemplating.Tests;

public class Foo
{
    public void MethodToInclude()
    {
        Console.WriteLine("Hello, World!");
    }
}

public class UnitTest
{
    [Fact]
    public void Foo()
    {
        var referencedFile =
            """
            public class Foo
            {
                public void MethodToInclude()
                {
                    Console.WriteLine("Hello, World!");
                }
            }
            """;
        var sourcePath = "docs/readme.md";
        var source =
            """
            # Sample Code
            
            [The method to include.](mdtl://referencedFile.cs@[namespace:][0][type:Foo][0][member:MethodToInclude][0])
            """;
        
        // mdt --input docs/readme.md --output docs/readme.md
        
        // output path: filename.md
        // look for regex \[[^\]]\]\(mdtl://(path:[^\)]*)\) 
        // path: filepath '@' selector
        // filepath: [^@]*
        // selector: '[' [parameter ':'] argument ']'
        // argument: number_argument | text_argument
        // number_argument: 0-9 *[0-9]
        // text_argument: ^']'
        /*
           ; Query language
           ; Example:
           ; referencedFile.cs@[namespace:][0][type:Foo][0][member:MethodToInclude][0]
           
           query           = file-path "@" selector
           
           ; File path:
           ; - must not be empty
           ; - may contain any character except "@"
           file-path       = 1*file-char
           file-char       = %x00-3F / %x41-10FFFF
                             ; any Unicode scalar value except "@"
           
           ; Selector:
           ; - one or more composable selector segments
           selector        = 1*segment
           
           segment         = "[" [ parameter ":" ] argument "]"
           
           parameter       = 1*param-char
           param-char      = unescaped-char
           
           ; Arguments:
           ; - purely numeric => number-argument
           ; - otherwise => text-argument
           argument        = number-argument / text-argument
           
           number-argument = 1*DIGIT
           
           text-argument   = 1*( escaped-char / text-char )
           
           ; Escaping:
           ; "\", "[" and "]" must be escaped inside arguments
           escaped-char    = "\" ( "\" / "[" / "]" / ":" )
           
           ; Any character except unescaped "\", "[" or "]"
           text-char       = %x00-5A / %x5E-10FFFF
                             ; excludes "\" (%x5C), "[" (%x5B), "]" (%x5D)
           
           unescaped-char  = %x00-39 / %x3B-5A / %x5E-10FFFF
                             ; excludes ":" (%x3A), "\" (%x5C),
                             ; "[" (%x5B), "]" (%x5D)
         */
        
        var expected =
            """
            ```cs
            public void MethodToInclude()
            {
                Console.WriteLine("Hello, World!");
            }
            ```
            """;
    }
}
