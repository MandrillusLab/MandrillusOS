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

| API ausente | Workaround |
|---|---|
| `Dictionary<TKey,TValue>` | Duas `List<T>` paralelas (Korlib tem `List<T>`, `Queue<T>`, `Stack<T>`, `LinkedList<T>`, interface `IDictionary` sem implementação) |
| `string.Join` | Loop manual com `string.Concat`/`+=` (Korlib tem `Concat` e `Format`, não `Join`) |

**Padrão para novos gaps:** ao ver `does not contain a definition for X` num tipo padrão do .NET, suspeitar deste padrão primeiro. Verificar na fonte do MOSA (`Source/Mosa.Korlib` vs `Source/Mosa.TinyCoreLib`) antes de propor fix. Documentar workaround com comentário inline, no estilo já usado em `Drill.cs`.

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
