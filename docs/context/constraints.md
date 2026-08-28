# Limitações técnicas e restrições de plataforma

[← voltar ao CLAUDE.md](../../CLAUDE.md)

## Scheduler

O scheduler do BareMetal é **cooperativo, não preemptivo**. Chamadas bloqueantes no loop principal congelam o sistema inteiro.

- ❌ `Console.ReadLine()` — bloqueante por definição. Funciona no demo CoolWorld, mas isso **não** é confirmação de segurança: CoolWorld roda como loop single-thread sem scheduler ativo.
- ✅ `Mosa.Kernel.BareMetal.Kernel.Keyboard.GetKeyPressed()` — retorna `Key?` nullable (null quando não há scancode pendente ou é modificador). Polling não-bloqueante é o padrão correto.
- ✅ `HAL.Yield()` → resolve para `Native.Hlt()` em x86 (seguro com scheduler ativo ou inativo); busy-wait no-op em outras plataformas.

## Korlib

`Mosa.Korlib` é o corlib real usado no alvo BareMetal (via `Mosa.Runtime`). Menor que o BCL padrão **e** menor que `Mosa.TinyCoreLib` (outro corlib do MOSA, não usado no Mandrillus).

Gaps confirmados:

| API ausente/quebrada | Workaround |
|---|---|
| `Dictionary<TKey,TValue>` | Duas `List<T>` paralelas (Korlib tem `List<T>`, `Queue<T>`, `Stack<T>`, `LinkedList<T>`, interface `IDictionary` sem implementação) |
| `string.Join` | Loop manual com `string.Concat`/`+=` (Korlib tem `Concat` e `Format`, não `Join`) |
| `Array.Copy(Array, int, Array, int, int)` | Loop manual copiando elemento a elemento — ver nota abaixo |

**Padrão para novos gaps:** ao ver `does not contain a definition for X` num tipo padrão do .NET, suspeitar deste padrão primeiro. Verificar na fonte do MOSA (`Source/Mosa.Korlib` vs `Source/Mosa.TinyCoreLib`) antes de propor fix. Documentar workaround com comentário inline, no estilo já usado em `Drill.cs`.

**⚠️ `Array.Copy` é um gap de categoria diferente dos anteriores:** `Dictionary` e `string.Join` falham em tempo de **compilação** (member ausente, erro claro). `Array.Copy` **existe e compila normalmente**, mas trava silenciosamente em runtime — sem exceção, sem crash, a execução simplesmente para — quando chamado com `length > 0`. Confirmado via debug ao vivo no QEMU (Issue #8, pós-fechamento): `Array.Copy(tokens, 1, args, 0, args.Length)` travava com `args.Length >= 1`; com `length == 0` completava normalmente. Por não gerar erro de compilação, esse tipo de gap só aparece ao exercitar o código de fato no QEMU — tratar qualquer travamento sem mensagem de erro como suspeito desta categoria, além da hipótese já documentada de chamada bloqueante no scheduler cooperativo.

## Testes automatizados

Mandrillus não tem (e não terá) suíte de testes própria — usa o tooling de teste do MOSA como está. Motivo: `Mosa.Korlib` não roda sob xUnit/.NET host normal, e o próprio MOSA não usa xUnit convencional para código bare-metal. Prática de xUnit fica reservada para outro projeto com runtime .NET completo. Decisão fechada — ver [status.md](status.md#prática-de-git-disciplinado).

## Fronteira platform-agnostic vs. x86

- `Boot.cs` (**`Mandrillus.Kernel.x86`**) — entry point fino, específico de plataforma
- `Program.cs` (**`Mandrillus.Kernel`**) — `SetBootOptions()` e `EntryPoint()`; lógica de aplicação (incl. `Drill.Start()`) vive aqui
- HAL (`Mosa.DeviceSystem`) e `Kernel.Keyboard` (`Mosa.Kernel.BareMetal`, sem sufixo `.x86`) são platform-agnostic — só o runtime de `HAL.Yield()` varia por plataforma
- Verificar sempre o sufixo do assembly antes de decidir em qual projeto um arquivo novo entra

## Hardware do PIT (fatos, não decisão de projeto)

- Chip 8253/8254 tem **um único canal físico 0** — qualquer timer do Mandrillus e o `Scheduler.ClockInterrupt` do MOSA compartilham essa frequência. Restrição de hardware, não de arquitetura 32/64-bit nem de BIOS.
- Portas canônicas: `0x40` (dado canal 0), `0x43` (comando). Sequência: modo → LSB → MSB.
- MOSA não traz driver de PIT pronto. RTC (`0x70`/`0x71`) só dá hora/calendário, não gera tick periódico. `HAL.Sleep()` é `// TODO` vazio no framework.

Decisão de design para Issue #9 (não é restrição, é escolha já fechada): ver [status.md](status.md#issue-9-pit).
