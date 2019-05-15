# coffeescript-module

A base little class for your Coffeescript projects.

```
npm install coffeescript-module
```

## Examples

``` coffeescript
{Module} = require 'coffeescript-module'

class Foo extends Module
  log: -> console.log 'hi!'

class Bar extends Module
  @delegate 'log', Foo
  @aliasFunction 'b', 'a'
  @aliasProperty 'd', 'c'

  c: 'test'
  a: -> console.log 'a'

class Baz extends Module
  @includes Bar

bar = new Bar()
bar.log() # calls Foo::log()
bar.b()   # calls Bar::a()
bar.d     # gets Bar::c

baz = new Baz()
baz.b()   # calls Bar::a()
```

## Contributing

If you have a useful addition or a bug fix, send a pull request!

## TODO

* Write tests