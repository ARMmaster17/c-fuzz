# c-fuzz

## What is it?
Test HTTP endpoints using a variety of edge-cases.

## How do I use it?
Compile the code or download the executable from the 'releases' tab to a location in your `$PATH`. Then run `c-fuzz http://example.org/test?id=#`, replacing the URL with your own. Substitute the value of any parameters with `#` and c-fuzz will automatically generate permutations of your URL.

You can also execute `c-fuzz -h` to see all available options.