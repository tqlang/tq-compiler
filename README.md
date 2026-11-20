# tq-compiler

Compilador da linguagem **Tq**, escrito em C# (.NET 10). O alvo atual é gerar código .NET.

O projeto é um único `Tqc.csproj` (assembly `tqc`). Os namespaces continuam `Tqc.*`.

## Estrutura

```
Tqc.csproj            projeto único (Mono.Cecil, Tomlyn)
Src/                  código C# do compilador, uma pasta por etapa
  Core/               SourceSpan, SourceLocation
  Diagnostics/        DiagnosticBag, Guard, erros de compilação
  Syntax/             Lexer, Parser e a AST (Nodes/)
  IR/                 representação intermediária: membros do programa, nós, referências
  Semantics/          AST -> IR (TreeLower) e resolução de nomes (ReferenceBinder)
  Build/              modelo do build.toml (Model/) e leitor TOML (Toml/)
  Dotnet/             lê assemblies .NET com Cecil e expõe como módulo Tq
  Cli/                Program (comando `tqc`), BuildPlanner, árvore de arquivos-fonte
Tq/                   código escrito em Tq
  build.toml          manifesto: módulos e dependências
  Bootstrap/          compilador reescrito em Tq (início)
  Lib/Std             stdlib mínima, a que o compilador atual consegue usar
  Lib/Std-complete    stdlib completa (em grande parte ainda comentada)
  Tests/              testes escritos em Tq
  ilverify.sh         valida o IL gerado
```

## Fluxo de um build

`tqc build_toml [build.toml]`, em `Cli/Program.cs`:

1. `Build/Toml` lê o manifesto e `Cli/BuildPlanner` ordena os módulos (dependências primeiro).
2. Módulo `dotnet`: `Dotnet/` abre os assemblies e monta a árvore de namespaces e tipos.
3. Módulo Tq: cada subpasta vira um namespace, o `Syntax/Parser` lê cada `.tq`, o
   `Semantics/TreeLower` converte para a IR e o `Semantics/ReferenceBinder` resolve imports e nomes.

Os caminhos do `build.toml` são relativos ao diretório atual, então execute de dentro de `Tq/`:

```
cd Tq
dotnet run --project ../Tqc.csproj -- build_toml
```

## O módulo Std do compilador

Os tipos primitivos não são intrínsecos: o compilador cria o módulo `Std` (`IR/ProgramMembers/Primitive/CompilerStd.cs`),
que existe mesmo sem uma biblioteca Std no `build.toml`.

- `Std.Types`: `Bool`, `Char`, `String`, `Void`, `Opaque`, `NoReturn`, `Label`, `Type`, `AnyType` e os genéricos
  `Integer(bool signed, int bitSize)`, `NaturalInteger(bool signed)`, `NativeInteger(bool signed)`, `Float(int bitSize)`.
- `Std.Memory`: vazio por enquanto.
- As keywords (`i32`, `u8`, `int`, `iptr`, `f64`, `bool`, `string`...) são aliases desses membros, como `int` em C#, e valem em
  qualquer arquivo. Escrever `Std.Types.Integer` diretamente exige o import.
- Se o projeto tiver um módulo chamado `Std`, ele é fundido com o do compilador: namespaces iguais se unem e um nome
  declarado dos dois lados é erro (`Semantics/StdMerger.cs`).
- O analisador só guarda as assinaturas genéricas (`struct X(...)`). Cada aplicação, como `Integer(true, 32)` ou a keyword
  `i32`, vira uma entrada da `GenericInstanceTable` (um `GenericInstance`, que é um `IStruct`) e é referenciada como
  qualquer struct. Aplicações com a mesma definição e os mesmos argumentos literais compartilham a entrada. Os argumentos
  ficam como escritos; a avaliação e a criação do tipo concreto vêm depois.

## Overloads

Depois do binding, `Semantics/TypeChecking` tipa as expressões (`TqModule.Types`) e troca cada chamada a um grupo de
funções por uma chamada a uma única função (`FunctionTypeReference`), convertendo os argumentos com `IrConvert`.

- Correspondência exata vence. Sem ela, só alargamento sem perda é implícito (`i16`→`i32`, `u8`→`i16`, `f32`→`f64`);
  qualquer outra conversão exige `as`.
- Um literal inteiro prefere `int`, depois `uint`. Se só sobram tipos `iN`/`uN`/`iptr`/`uptr`, a chamada é ambígua e o
  literal precisa de tipo explícito (`5 as i32`).
- Se faltar informação de tipo (de um argumento ou parâmetro), a chamada fica sem resolver, sem erro.
- Sem overload aplicável, ou com mais de um empatado, é erro de compilação.
- Funções com `@implicitConvert` (um parâmetro, um retorno) registram uma conversão implícita; o argumento passa por
  uma chamada a elas, com custo maior que o alargamento. Só valem para código que alcança a função.
- `@onset` torna um tipo observável: se o tipo de `a` tem uma função `@onset` (um parâmetro) que aceita `v`, então
  `a = v` vira `a.f(v)` (`player.health = 80` é `player.health.change(80)`). Vale para campos, acessos e variáveis já
  declaradas; a declaração (`let`/inicializador de campo) inicializa e não chama. Sem overload `@onset` aplicável (por
  exemplo, atribuir um `Health` a um `Health`) a atribuição é a comum, e dentro da própria função `@onset` também, para
  que ela consiga guardar o valor. Com vários, vale a escolha de overload das chamadas. `@onset` herda do tipo base.

## Atributos e acesso

O lowering guarda os atributos de cada campo, função e struct (`Attributes`). Os atributos da linguagem estão em
`Semantics/Attributes/BuiltinAttributes.cs`, que valida o alvo, os argumentos e as combinações contraditórias
(`@public @private`, `@inline @noInline`...). Um atributo desconhecido (como `@readonly`) é guardado sem erro, já que
atributos definidos pelo usuário ainda não existem. A referência escreve `@allowAcessTo`/`@denyAcessTo`; a grafia
correta também é aceita.

Acesso (`Semantics/Access/`), verificado ao resolver nomes e `a.b`, e ao escolher overloads:

- `@public`: qualquer código. `@internal`: código do mesmo módulo.
- Sem atributo, o membro é `@private`: só o código do mesmo namespace, em qualquer arquivo dele. Namespaces aninhados
  não alcançam.
- `@allowAcessTo(A.B)` recebe o próprio membro (namespace ou struct, ou um array deles: `@allowAcessTo([A, B.C])`) e
  deixa entrar o código dentro dele, inclusive o dos namespaces aninhados; `@denyAcessTo` o barra, mesmo que o resto
  permita. O binder resolve esses argumentos numa primeira passada (atributos) antes de resolver os corpos, e
  `MemberAccess.Allowed/Denied` guarda os membros, não nomes.
- Membros de struct e typedef pertencem ao namespace do tipo.
- Membros .NET e os criados pelo compilador (`Std.Types`) são abertos.

## Arrays, structs e typedefs

- `[a, b, c]` vira `IrArray`; o tipo é um slice do tipo comum dos elementos (literais inteiros dão `[]int`).
- Struct: campos, funções (overloads em grupo), `constructor`, `destructor` e `extends` (`TqStruct`, `TqConstructor`,
  `TqDestructor`). Dentro dele valem os parâmetros genéricos, os membros herdados, os próprios e `this`.
  `extends` aceita qualquer struct, escrito em Tq ou vindo do .NET (os membros herdados de classes .NET também são
  alcançados); um struct genérico precisa dos argumentos (`extends Base(i32)`). Herdar de si mesmo, direta ou
  indiretamente, é erro.
- Valor e referência: tudo em Tq é por valor, também com herança. `@class` faz um struct de Tq ser sempre por
  referência; vale automaticamente para classes .NET e para quem herda de um struct `@class` ou de uma classe .NET
  (`Semantics/TypeChecking/StructSemantics.cs`). O tipo de um struct por referência é `ReferenceTypeRef(T)`, em
  declarações, parâmetros e no resultado de `new`. `@valueOnly` e `@referenceOnly` são aceitos mas ignorados.
- `new T(args)`: o type checker escolhe o construtor pelas mesmas regras dos overloads (conversões, literais,
  ambiguidade) e o guarda em `IrNew.Constructor`, com os argumentos convertidos. Construtores de Tq são `@private` por
  padrão, como qualquer membro. Struct de Tq sem construtor é criado sem argumentos. Construtores não são herdados.
  Instâncias genéricas ficam sem escolha, para o backend; um struct .NET sem construtor declarado é criado sem argumentos.
- Typedef: `typedef Nome { }` ou `typedef(T) Nome { }`, com `case 1, 2`, `case nome` e `case nome = valor`, além de
  campos e funções (`TqTypedef`, `TqNamedCase`). `Nome.caso` é uma referência direta ao caso.
- Ainda não suportado: intervalos em `case` (`1..5`), propriedades, `@protected`.

## Statements, operadores e membros por valor

- Lowering: `if`/`elif`/`else` viram uma cadeia de `IrIf` (o `else` é o próximo `IrIf`); escopos aninhados viram `IrBody`.
  Operadores binários e unários (`IrBinary`, `IrUnary`), índice (`IrIndex`), `*T` (`Ref(T)`),
  literais decimais e `true`/`false`. `a += b` vira `a = a + b` (o alvo é avaliado duas vezes) e `-1` é um literal só.
- Operadores: os dois lados precisam ter um tipo em comum (o mesmo; o numérico ao lado de um literal; o maior, se o outro
  alarga sem perda); senão é erro e pede `as`. Comparações dão `bool`. `if` exige `bool`. `!` exige `bool`.
- `valor.nome`: o membro é procurado no tipo do valor, incluindo o que o tipo estende; o acesso é verificado e o
  `IrAccess` passa a apontar para o membro. Em tipos que listam seus membros, um nome inexistente é erro.
- `valor.f(args)`: os overloads do grupo (só métodos de instância, sem `@static`) são resolvidos como numa chamada comum,
  e o receptor continua no `IrAccess`. Uma função única também é checada e tem os argumentos convertidos.

## Operadores, índices e decimais

- Strings só operam com strings: `string + string`, comparações e igualdade entre strings; qualquer mistura
  (`string + char`, `string + int`) é erro. Booleanos e outros tipos só têm igualdade; aritmética pede números.
- Um literal ao lado de um número com tamanho precisa caber nele (`x + 300` com `x: i8` é erro). Um literal inteiro
  não vira float implicitamente (escreva `2.0`).
- Literais decimais têm o tipo `ComptimeDecimalTypeRef`: viram `f64` quando nada pede outro float (`let a = 1.5`); em
  chamadas, `f64` tem prioridade e os outros floats empatam (pede tipo explícito, como nos inteiros).
- `@overrideOperator(Std.Math.Operator.Add)`: `Std.Math.Operator` é criado pelo compilador; os casos têm os nomes de
  `BinaryOperator`/`UnaryOperator` (`Add`, `Sub`, `Equal`, `Less`, `Not`, `Negate`, `Increment`...). Uma função fora de
  struct (ou `@static`) recebe todos os operandos; uma função de instância recebe o operando da esquerda como `this`.
  O operador embutido vale primeiro; a sobrecarga é usada quando ele não existe para os tipos. O resultado é uma
  chamada (`IrCall`) à função escolhida.
- `a[i]`: slices e strings aceitam qualquer inteiro (string dá `char`); outros tipos usam funções `@indexerGet`
  (leitura) e `@indexerSet` (`a[i] = v`, os índices primeiro e o valor por último), que também são resolvidas como
  overloads e viram chamadas. Índices de tipos .NET ainda não são lidos.

## Valores contra tipos declarados

- Atribuição, `let T x = v`, inicializador de campo, valor padrão de parâmetro, valor de `case` (contra o tipo do
  `typedef(T)`) e `return` seguem a regra dos argumentos: só conversão sem perda é implícita (inclusive literal que
  cabe); o resto é erro e pede `as`. Quando há conversão, o valor vira `IrConvert` (ou uma chamada `@implicitConvert`).
- `return`: função sem tipo de retorno (e construtor/destrutor) não devolve valor; com tipo, `return` sem valor é erro.
  `!T` aceita um `T` (e `return` sem valor em `!void`). Tipos de retorno ainda não conhecidos (genéricos) não são checados.
- Uma referência a uma struct `@class` (ou classe .NET) vai onde se espera uma referência a algo que ela estende;
  quanto mais degraus, maior o custo no overload. Struct por valor nunca é "cortada" para a base.

## Herança, interfaces e `@virtual`/`@override`

`Semantics/Inheritance/InheritanceChecker.cs`, depois que `extends` está resolvido:

- `@final`: não pode ser estendida. `@final`, `@abstract` e `@interface` se contradizem.
- `@interface`: sem construtor nem destrutor; só estende interfaces. Uma struct implementa uma interface estendendo-a
  (só há um `extends` por struct); os campos vêm pela extensão e as funções sem corpo precisam ser implementadas.
- `@abstract` e `@interface` não podem ser criadas com `new`. Função sem corpo nelas é abstrata.
- `@virtual`/`@override`: só em função de struct e nunca com `@static`. `@override` troca uma função de mesmo nome e
  mesmos parâmetros/retorno que seja `@virtual`, `@override` ou abstrata, e precisa de corpo. Uma função com a
  assinatura de uma sobrescrevível, sem `@override`, é erro. Em base .NET a assinatura também é comparada, e só métodos virtuais, abstratos ou de interface podem ser sobrescritos.
- Uma struct que não é `@abstract`/`@interface` precisa implementar toda função sem corpo que herda, inclusive os
  métodos abstratos de classes e interfaces .NET.
- Um só mecanismo para Tq e .NET: `IObjectType` (`IsAbstract`, `IsInterface`, `IsFinal`, `IsReference`; structs, interfaces e typedefs) e `IMethod`
  (`IsStatic`, `IsAbstract`, `CanBeOverridden`). Em Tq vêm dos atributos (`@abstract`, `@interface`, `@final`, `@class`,
  `@virtual`, `@override`); em .NET, dos metadados. O verificador de herança, o `new` em tipo abstrato, a conversão
  para a base e o "por referência" só usam essas interfaces.
- Assinaturas: `FunctionSignature.Of(função)` serve para Tq e .NET e dá os tipos dos parâmetros e do retorno; um tipo
  desconhecido combina com qualquer outro. `IsSameAs` (parâmetros e retorno) e `HasSameParametersAs` substituem as
  comparações à mão; o overload e o tipo de retorno de uma chamada também passam por ela.

## Laços: `while` e `break`

Três formas, conforme a spec; as partes são separadas por `:`:

```tq
while cond statement
while cond : passo statement
while let i = 0 : i < 5 : i++ statement
```

- A declaração exige `let` (`while i = 0 : ...` é erro). A variável existe só dentro do laço: a condição, o passo e o corpo a enxergam.
- A condição é testada antes de cada iteração e precisa ser `bool`. O passo roda depois de cada iteração.
- Um `{` depois da última parte é sempre o corpo, como em `if`; uma coleção literal na condição precisa de parênteses.
- `break` sai do laço mais próximo e só vale dentro de um laço.
- IR: `IrWhile(Declaration?, Condition, Step?, Body)` e `IrBreak`. `for` e `continue` não existem ainda (a spec marca `for` como não implementado).

## Argumentos de structs genéricas

`Integer(true, 8 * 4)` é avaliado ao ser ligado: literais, `-`, `!` e os operadores aritméticos e de comparação sobre constantes
(`ComptimeEvaluator`) viram uma constante, então `Integer(true, 8 * 4)` e `Integer(true, 32)` são a mesma entrada da
`GenericInstanceTable`.

- O número de argumentos tem de bater com o de parâmetros.
- Uma constante tem de caber no parâmetro: `bool` pede `true`/`false`; um inteiro pede um número inteiro que caiba no tipo do parâmetro; `string` pede texto.
- Divisão por zero e estouro em tempo de compilação são erros.
- Um argumento que não é constante (um tipo, um nome) é mantido como foi escrito.
- Ainda não: referências a `const` (o parser não guarda se o campo é `const`), chamadas de função em tempo de compilação (`@comptime`) e
  limites específicos de cada struct (por exemplo, quais valores de `bitSize` são válidos).

## Propriedades

```tq
struct Pessoa {
    let int idade { get; set }          # automática: a propriedade tem armazenamento próprio
    let nome { get = lerNome }          # o tipo vem da função (get: () -> T, set: (T) -> void)
    let int dobro {
        get { return this.idade * 2 }
    }
    let int nivel {
        get                              # vazio: lê o armazenamento
        set { field = Math.clamp(value, 0, 10) }   # `field` é o armazenamento
    }
    let int limite {
        get = lerLimite
        set { this.guardado = value }   # `value` é o valor atribuído
    }
}
```

- Os acessores ficam em linhas separadas ou separados por `;`. Cada um é vazio (`get`), definido por uma função
  (`get = f`) ou escrito ali (`get { ... }`).
- `get` tem a assinatura `() -> T` e `set` a assinatura `(T) -> void`. Uma função indicada (`get = f`) precisa ser de
  instância e ter essa assinatura; se houver sobrecargas, vale a única que serve.
- Sem tipo declarado, ele vem do `get` ou do `set` indicado por função. Um acessor vazio ou escrito no lugar exige o tipo.
- Ler `a.p` chama o `get` em `a`; `a.p = v` converte `v` ao tipo da propriedade e chama o `set`. O nome sozinho (`p`)
  dentro da struct usa `this`. `a.p += 1` vira `a.p = a.p + 1`.
- Um dos dois acessores, ou os dois, podem ser vazios (como no C# moderno), e o outro pode ser escrito. Dentro de um
  acessor escrito ali, `field` é o armazenamento da propriedade, do tipo dela; `field` só existe nos acessores.
- A IR **não declara** esse armazenamento nem gera corpo para o acessor vazio: `TqProperty.HasBackingField` diz que o
  backend precisa criar o campo (true se um acessor usa `field` ou se há acessor vazio numa struct que pode ser criada), e
  o acessor vazio sem corpo (`Body == null`) é implementado por ele.
- Uma propriedade vazia numa struct `@abstract` ou `@interface` é abstrata: quem estende a struct declara uma
  propriedade de mesmo nome com o acessor (não precisa de `@override`).
- Ainda não: propriedades estáticas, `@virtual`/`@override` em propriedades, `++`/`--` sobre propriedades.

### Indexadores de tipos .NET

As propriedades indexadas de um tipo .NET (`this[int i]`, `Item`) servem a `a[i]` e `a[i] = v`, com escolha de
sobrecarga pelos argumentos, como as funções marcadas com `@indexerGet`/`@indexerSet`. Elas não são alcançadas pelo nome.

## `const`

`const` é um modificador do tipo, não uma palavra à parte da IR: `const T x` é `let Const(T) x`, e `const x = v` é
`let Const(AnyType) x` (o tipo vem do valor). `Const(T)` é uma struct genérica primitiva (`Std.Types.Const`), criada pelo
compilador como `Integer` e `Float`.

- Vale para campos e locais (`const i32 myInt = 100`, `const myByte = 255`). Propriedades não podem ser `const`.
- Os valores de uma constante são os de `T`: `const x = 1` é usada como um `int`, e o valor é convertido para `T` na declaração.
- Depois da declaração, `x = ...`, `x += ...`, `x++` e `x--` são erros ("is a constant").
- Um campo `const` sem tipo só tem o tipo conhecido depois de a declaração ser verificada; usado antes disso (por
  exemplo, numa função declarada acima) seu tipo fica desconhecido e nada é checado.

## `Ref(T)`, `Failable(T)` e `Nullable(T)`

Como `Const(T)`, são structs genéricas primitivas (`Std.Types.Ref`, `Failable` e `Nullable`, parâmetro `type: Type`) e
cada aplicação é uma `GenericInstance` da tabela compartilhada; não há nó nem tipo próprio na IR.

- `*T` é `Ref(T)`. Uma struct por referência (`@class`, classe .NET, ou que estende uma) tem como tipo de valor `Ref(S)`,
  então `*Foo` e `Foo` são o mesmo tipo quando `Foo` é uma classe. Membros de `Ref(T)` são os de `T`; o upcast entre
  referências e a mensagem de erro (`ref T`) usam `Std.Types.IsRef`.
- `!T` e `?T` valem como expressão: o parser e o lowering os tratam como os operadores unários `!` (não lógico) e `?`
  (`UnaryOperator.Nullable`). O ReferenceBinder, depois de resolver o operando, decide: se ele é um tipo, a expressão vira
  uma referência a `Failable(T)` / `Nullable(T)`; senão continua sendo o operador. `?x` com `x` valor é erro de operador.
- Numa função `!T`, `return` é checado contra `T` (e `return` sem valor vale em `!void`); chamar a função dá `Failable(T)`.
- Ainda não existem: propagação, `.ignored()` e as regras de uso de `?T`.

## Dump da IR (`--dump-ir`)

`tqc build_toml [build.toml] --dump-ir[=pasta]` escreve a IR de cada módulo Tq em `pasta/<Módulo>.tqir` (padrão `./tqir`),
depois da checagem de tipos e mesmo quando há erros. É escrita como a própria linguagem (`SemanticAnalyser.DumpIr` / `IrDumper`):
o que foi escrito em Tq sai como Tq, e os comentários dizem o que o compilador sabe.

```
namespace Std.Console {
    public struct Health : Base {    // class · ns=Game · /caminho/health.tq(3,1)
        public value: i32;    // ns=Game · /caminho/health.tq(4,5)
        public func change(new: i32) -> void {    // ns=Game · /caminho/health.tq(6,5)
            this.value = (new + 1) /* binary Add: i32 */ /* assign: i32 */;    // @7:9
            Console.Write(s /* param: string */) /* call -> Write(string): void */;    // @8:9
        }
    }
}
```

- **Toda** expressão leva `/* o que é: tipo */`. O tipo é o do type checker, escrito como nas mensagens de erro (`i32`, `ref Foo`,
  `!i32`, `?T`, `[]string`); `?` se não tem tipo.
- O "o que é" de um nome é a ligação do binder: `local`, `param`, `field`, `property`, `this of Tipo`, `backing field of prop`,
  `func Nome(tipos)`, `group of overloads`, `namespace`, `type`, `struct type`, `typedef`, `protocol`, `slice type`, `UNSOLVED`.
  Outros nós dizem `binary Add`, `unary Neg`, `convert`, `assign`, `index`, `array`, `int constant`, `decimal constant`,
  `bool constant`, `string constant`, `string concat`, `local declaration`.
- Uma chamada diz a sobrecarga escolhida (`call -> Write(string)`); se ainda é um grupo: `call, UNRESOLVED: Write is still a
  group of overloads`. Um `new` diz `new -> construtor` ou `new, UNRESOLVED constructor`.
- Um nome que o binder não conseguiu ligar sai como `?nome`.
- Flags nos comentários dos membros: propriedades mostram `getter/setter`, `accessors resolved`, `uses field`; funções mostram
  `owner=`, `overridable`; structs mostram `class`, `abstract`, `final`, etc.
- Cada instrução termina em `// @linha:coluna`; membros trazem acesso (`public`, `private`...), o namespace (`ns=`), `allow=`/`deny=`
  e a posição `arquivo(linha,coluna)` no comentário.
- Também saem imports e declarações por arquivo, atributos (`@onset`, e `UNKNOWN attribute` para os que não existem), `extends`,
  construtores, `typedef`s (tipo base, casos), propriedades (`get`/`set`, o que o checker resolveu, `backing field`), e operadores
  na escrita da linguagem (`+%`, `**`, `as`).

## `<inteiro>.min` e `<inteiro>.max`

`u8.max`, `i32.min`, `i128.max`... são os limites dos tipos inteiros (`int`/`uint` têm 32 bits). São constantes **com o tipo** (`u8.max` é
um `u8`, não um literal sem tamanho), então `Test.assert(u8.max, 255)` escolhe a sobrecarga de `u8` e `u8.max + 1` estoura em `u8`. Em
`iptr`/`uptr` os limites dependem da plataforma, e pedi-los é erro. Duas constantes literais continuam dobrando em uma só
(`2 ** 8 - 1`); uma constante com tipo, não.

## `null`

`null` é um literal de tipo `null`, que só se converte para um `?T` (parâmetro, atribuição, retorno, campo: `let ?i32 x = null`).
Para `T` de referência (`?string`) sai `ldnull`; para `T` de valor, `?T` é `System.Nullable<T>` e `null` é o valor vazio
(`initobj`). `let x = null` é erro (não há tipo a inferir), e `null` onde não cabe diz que só um tipo `?T` pode ser `null`.
Ainda não existem: `x == null`, `T` → `?T` implícito, `??` e `.unwrap()`.

## Operadores de inteiros em .NET

| Operador | Significado | Como sai em .NET |
|---|---|---|
| `+ - *` | checados (lançam `OverflowException`) | `add.ovf`, `sub.ovf`, `mul.ovf` |
| `+% -% *%` | dão a volta (wrap) | `add`, `sub`, `mul` + corte para o tamanho |
| `+\| -\| *\|` | saturados nos limites do tipo | helper `Tq.Ops.AddOnBounds_Byte(a, b)` etc. |
| `/` `%` | divisão truncada, resto | `div`, `rem` |
| `/_` `/^` | divisão para baixo / para cima | helper `Tq.Ops.DivFloor_Int32(a, b)` |
| `**` | potência, checada (floats: `Math.Pow`) | helper `Tq.Ops.Pow_Byte(a, b)` |
| `**%` `**\|` | potência com wrap / saturada | helpers `PowWrap_*` e `PowOnBounds_*` |

Os helpers são métodos estáticos de `Tq.Ops`, no `Tq.Runtime.dll` (o mesmo assembly do `Tq.Failable`, que só é gerado e escrito quando
algum módulo precisa dele): uma cópia para o build inteiro, criada pelo primeiro módulo que usa o operador com aquele tipo, e chamada com
`a`, `b`, `call`. Até 32 bits a conta é feita em 64 (onde não estoura) e cortada/estreitada; `i64` satura com
`Int128`, `u64` com comparações e `UInt128`. A potência é por quadrados sucessivos (sem alocação e exata: `Math.Pow` é `double`, só
tem 53 bits). `i128`/`u128` têm literais e comparações, mas ainda não esses operadores nem `+ - * / %`. Floats têm `**`, `/_` e `/^`
(`Math.Pow`, `Math.Floor`, `Math.Ceiling`); saturar ou dar a volta num float é erro.

**Constantes:** os inteiros de compile time são `System.Numerics.BigInteger` (antes `decimal`, limite de ~7,9e28). Duas constantes
numéricas viram uma constante só no type checker (`2 ** 128 - 1`, `-(2 ** 127)`), sem tamanho, então não estouram: só precisam
caber no tipo que as recebe. `+% +| ...` entre constantes é a operação comum (não há tamanho para dar a volta).

Os módulos que não são Tq também saem, na mesma pasta (`SemanticAnalyser.DumpMembers` / `ModuleDumper`), com os
membros como o type checker os enxerga, para comparar com uma chamada que "could not be resolved":

- `Std.compiler.tqir`: o que o compilador cria para o `Std` (os structs primitivos com seus parâmetros e o
  `Std.Math.Operator` com os casos).
- `<Módulo>.dotnet.tqir`: cada módulo `dotnet`, com namespaces, `class`/`struct`/`enum`/`interface` (e `extends`),
  métodos (`func nome(p: tipo) -> retorno <static virtual abstract>`, construtores, propriedades, campos e constantes).
  Cada tipo e método traz o nome .NET em comentário (`// [System.Runtime]System.Console::WriteLine`). Um tipo que o
  tradutor ainda não suporta aparece como `UNSUPPORTED(nome)`, e é ele que deixa uma sobrecarga sem resolver.
  Só os membros próprios de cada tipo são listados, não os herdados.

## Backend .NET (`Tqc.Backend.Cecil`)

Com `target = "dotnet"` em `[project]` do `build.toml`, depois de checar todos os módulos sem erros o compilador emite um
assembly por módulo Tq (`<Módulo>.dll`) em `out-dir` (padrão `.tq-out`), com Mono.Cecil. O módulo raiz (`root`) vira um
programa: `main()` ou `main([]string args)` é chamado por `EntryPoint.Main(string[])` (classe própria no topo), e ele ganha um
`<Módulo>.runtimeconfig.json` para rodar com `dotnet <Módulo>.dll`. Os módulos saem em ordem de dependência, e um
assembly referencia os definidos antes dele.

**Herança de struct por valor** (`struct FileReader extends Reader`, nenhuma das duas `@class`): .NET não tem herança de
value type, então ela é montada com o que existe. A struct que estende guarda a base num campo `__base`; por valor, o código
percorre o caminho (`d.__base.x`). Cada struct da hierarquia (as que estendem ou são estendidas por outra do mesmo módulo)
ganha uma **interface aninhada `Ref`**, que é o tipo de `*S`. A interface de `D` estende a de `B`, então `*D` serve onde se
espera `*B`. A interface tem `ref S get_Value()` (os campos são lidos e escritos por ela, sem cópia) e um método por função
da struct; a derivada implementa o que herda repassando para o `__base`, ou chamando o `@override`, então a chamada por
referência chega na função mais derivada. Limites: a base precisa ser do mesmo módulo; uma referência é a struct *boxada*
(passar um valor onde se espera `*S` copia); structs `@abstract` por valor e funções genéricas não entram na interface.

**Membros herdados de tipos .NET:** classes herdam de `System.Object` (`ToString`, `Equals`, `GetHashCode`) e de toda a
cadeia, inclusive de bases fora dos assemblies do módulo (seus membros também são lidos); structs e enums enxergam os de
`Object`, interfaces os das interfaces que estendem e os de `Object`. As sobrecargas da classe se juntam às herdadas
(a da base com os mesmos parâmetros some), construtores não são herdados, e um valor que chama algo de `Object` é boxado.

**Funções genéricas** (`func copy(type T, []T a, []T b)`): os parâmetros do tipo `type` vêm primeiro na chamada
(`copy(i8, a, b)`), valem como `T` no resto da assinatura (o type checker troca `T` pelo tipo dado em cada chamada, também
no tipo de retorno) e viram **parâmetros genéricos do método .NET**; o `type` nunca vai para o runtime, e as chamadas
instanciam o método com os tipos dados. `anytype` vira `Object`. Dentro do corpo, `T == T` compara como objeto e `x as string`
usa `Convert.ToString`.

**Os corpos são traduzidos para IL** (`BodyEmitter`): `return`, `if/elif/else`, `while` (com declaração e passo),
`break`, variáveis locais, atribuições (a variáveis, parâmetros, campos, propriedades .NET e elementos de slice),
chamadas (Tq e .NET, com valores padrão de parâmetros), `new`, conversões numéricas, strings (`+`, comparações,
interpolação), `[a, b]`, `++`/`--`, `!`, `-`. Literais assumem o tipo esperado; `a + b` é checado (lança no overflow) e
`+%`/`-%`/`*%` dão a volta. `return v` em função `!T` embrulha em `Tq.Failable<T>` sem erro. O que ainda não existe
(`+|` e afins `OnBounds`, `DivFloor`/`DivCeil`, `?` nullable, propagação de falha, `lazy`, índice múltiplo) é
**reportado como erro do backend**, nunca ignorado em silêncio. Propriedades automáticas e construtores padrão são reais;
uma função sem corpo (`@abstract`) continua lançando `NotImplementedException`. `System.Runtime` (de onde vêm `Object`, `Int32` etc.) é implícito: é
usado com ou sem um módulo `dotnet` que o liste.

- Um namespace que tem membros vira uma classe estática: suas funções e campos são membros estáticos dela, seus
  structs e typedefs são tipos aninhados, e os namespaces filhos são classes estáticas aninhadas (`Std/Console/Foo`).
  Um namespace sem membros próprios no topo é só um namespace .NET no nome do que contém.
- `struct` é um value type, ou uma classe com `@class` (ou que estende uma classe, Tq ou .NET); `@abstract`, `@final` e `@interface` viram os flags .NET.
  `extends` de uma struct-interface é `implements`, o resto é a classe base. `typedef` é um enum (os casos nomeados são
  as constantes; casos numéricos sem nome não aparecem; o valor de um caso é um número ou uma constante .NET, como `CsSeekOrigin.Begin`). Structs genéricas ainda não.
- Acesso: `@public` é público; o resto (namespace/módulo) é `internal`, o máximo que o .NET diz.
- Funções: `@static`, `@virtual` e `@override` viram os flags; `lazy` ainda não.
  Classes sem construtor ganham o construtor sem argumentos; o `destructor` é o `Finalize` (vazio por enquanto).
- Propriedades são propriedades .NET; o campo de apoio (`<nome>k__BackingField`) é criado pelo backend quando
  `HasBackingField`. `get = f` precisa nomear uma função do mesmo tipo.
- Tipos: `i8`..`i128`/`u8`..`u128`, `int`/`uint` = 32 bits, `iptr`/`uptr` = `IntPtr`/`UIntPtr`, `f16/f32/f64`
  (`Half/Single/Double`), `bool`, `char`, `string`, `[]T` = `T[]`, `const T` = `T` (uma constante com valor literal é
  um campo `literal`). `*T` de uma classe é a própria classe e de um valor é `T&` (só em parâmetros e retornos).
  `?T` é `Nullable<T>` para valores e `T` para referências. `!T` é `Tq.Failable<T>` e `!void` é `Tq.Failable`, structs
  de um assembly `Tq.Runtime.dll` (campos `Value` e `Error`; `Error` é `object` até existir `Std.Types.Error`).
- Ainda sem: valor inicial de campos (precisa de construtor), `@packed`/`@union`/layout, `f80`/`f128`/`i256`,
  funções ou campos dentro de `typedef`, `for`/`continue`.

**Tipos genéricos do .NET (etapa 1a):** o nome de um tipo genérico não leva a aridade, então `Span<T>` é `Span` e
`from Dotnet.System import { Span }` funciona. Se um nome tem várias aridades (`Action`, `Action<T>`...), a de menos
parâmetros fica com o nome e as outras viram `` Nome`N `` (N = número de parâmetros). Usar o tipo ainda dá erro claro
no backend ("The generic type 'Span' of .NET..."): instanciar com `Span(i8)` é a etapa 1b.

**Métodos .NET que o Tq não consegue chamar:** um método (ou construtor) com um parâmetro de tipo sem equivalente em Tq
(`char[]`, `Span<T>`, genéricos...) não é candidato a sobrecarga. Antes o parâmetro desconhecido aceitava qualquer
argumento e a chamada ficava sem decidir em silêncio (`Console.Write(string)` competia com `Write(char[])`).

**`len` de slices:** `slice.len` é um `int` (o mesmo tipo de `let i = 0`, então `i < slice.len` não pede `as`); não dá para
atribuir. No .NET é `ldlen` + `conv.i4`.

**Métodos genéricos e arrays do .NET (etapa 1b, parte 1):** os parâmetros genéricos de um método .NET são os primeiros
argumentos da chamada, como numa função genérica de Tq: `GC.AllocateArray(T, length as int, false)` chama
`GC.AllocateArray<T>(int, bool)` (`DotnetMethod.TypeParameters`; no dump aparece `func AllocateArray(type T, ...)`). O checker troca
o `T` pelo tipo dado no retorno e nos parâmetros, e o backend monta um `GenericInstanceMethod`. Não há inferência: o tipo
é sempre escrito. Um array `T[]` é um slice `[]T` (só de uma dimensão), então os métodos com `char[]`, `byte[]` etc. voltam a ser
chamáveis; `char` do .NET sai como `System.Char`. Tipos genéricos (`Span<T>`, `List<T>`) ainda não: parte 2.

**Tipos genéricos do .NET (etapa 1b, parte 2):** um tipo genérico (`List<T>`, `Span<T>`, `Dictionary<K, V>`) é um construtor de
tipos e se usa como uma struct genérica de Tq: `List(i8)`, `Dictionary(string, i32)`. Só classes e structs (interfaces e
delegates genéricos ainda não). Os argumentos são tipos explícitos, sem inferência, e `new List(i8)()` usa os construtores de `List`.
- Os `T` do tipo são parâmetros do tipo `type` (`DotnetClass.Parameters`, no dump `class List(T)`). Os membros do tipo usam esses `T`,
  e o checker os troca pelos argumentos do valor em que o membro é usado: o campo, a propriedade, o indexador e o retorno de
  `list.add(x)` de um `List(i8)` saem em termos de `i8`, e `x` é verificado como `i8`.
- Um tipo genérico de .NET que aparece na assinatura de um membro (`IEnumerable<T>` não, mas `List<T>` e `Span<byte>` sim) vira essa aplicação,
  então esses membros passam a poder ser chamados.
- No backend vira um `GenericInstanceType`, e os métodos, campos, propriedades e construtores são referenciados através dele.
  Chamar métodos estáticos através de um tipo genérico (`List(i8).Foo()`) ainda não.

**Potência `**`:** `a ** b` tem precedência acima de `*` e associa à direita (`2 ** 3 ** 2` é `2 ** 9`); o operando é um prefixo, então
`-2 ** 2` é `(-2) ** 2` (use `-(2 ** 2)`). O lexer não distingue `**` de `**T` (referência a referência), então são duas estrelas coladas e
quem decide é o parser: depois de um operando é potência. `a * *b` (com espaço) continua sendo `a * (*b)`. Por enquanto só em tempo de compilação
(os dois lados constantes): o expoente inteiro não pode ser negativo e o resultado precisa caber no `decimal` dos números de compilação
(~7,9e28, ou seja, `2 ** 96` passa e `2 ** 97` dá erro), o que ainda não cobre os limites de `i128`/`u128`.
