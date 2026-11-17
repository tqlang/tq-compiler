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
  Operadores binários e unários (`IrBinary`, `IrUnary`), índice (`IrIndex`), `*T` (`IrReferenceTypeModifier`),
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
  Tipos de retorno ainda não conhecidos (`!T`, `?T`, genéricos) não são checados.
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
