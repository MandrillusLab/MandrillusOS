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

**Causa raiz identificada** (rastreada via inspeção direta da fonte MOSA, motivada por uma discussão de Discord de 10/2021 em que `TaylanInan` sinalizava que `Mosa.Korlib.Array.cs`'s `Copy` precisava de cópia de memória uint-a-uint em vez de byte-a-byte — essa otimização específica **foi** implementada depois, em `Source/Mosa.Runtime/Internal.cs`'s `MemoryCopy`/`MemoryCopy1`/`MemoryCopy4`, e **não** é a causa deste bug): a implementação real de `System.Array::Copy` é um **Plug** (mecanismo `Mosa.Runtime.Plug`, mesmo padrão do `ConsolePlug`) em `Source/Mosa.Plug.Korlib/System/ArrayPlug.cs`, linhas 13-52. Esse arquivo é explicitamente marcado como incompleto pelos próprios autores do MOSA: `// TODO: Fix!!!` logo acima do método `Copy`, e dentro dele, o cálculo correto de tamanho por tipo está comentado com `// Broken! (Size property loads at an invalid address most likely)`, substituído por um fallback `var size = IntPtr.Size;` que assume que todo elemento do array tem tamanho de ponteiro (4 bytes em x86). Esse fallback é coincidentemente correto para arrays de tipo referência como `string[]` (o caso do Drill em `Dispatch()`), então não é a causa direta do travamento neste caso específico — mas confirma que o Plug inteiro é reconhecidamente incompleto pelo próprio upstream. A causa mais provável do travamento está em como a aritmética de ponteiro (`arrayPtr + IntPtr.Size * 2 + index * size`) combina com `Mosa.Runtime.Internal.MemoryCopy`, causando acesso de memória inválido — plausível dado que não há validação de bounds/alinhamento visível após as checagens de `length`. Ainda não isolado até a linha exata — melhor pista disponível, não causa raiz 100% confirmada.

**⚠️ Alerta preventivo — outros métodos do mesmo arquivo:** `ArrayPlug.cs` também contém `Clear`, `IndexOf` e `GetLowerBound`, todos com o mesmo padrão de implementação stub/incompleta (`GetLowerBound` sempre retorna `0` com `// TODO`; `IndexOf` sempre retorna `-1` com `// TODO`; `Clear` reusa a mesma aritmética de ponteiro do `Copy`, logo herda a mesma suspeita de bug). Nenhum desses foi exercitado/confirmado com problema real ainda — mas se algum dia `Array.Clear()`, `Array.IndexOf()` ou `array.GetLowerBound()` forem usados no Mandrillus e algo travar ou se comportar de forma errada sem mensagem de erro clara, suspeitar deste mesmo arquivo antes de qualquer outra hipótese.

**Reportado upstream:** [Issue #1296](https://github.com/mosa/MOSA-Project/issues/1296) no `mosa/MOSA-Project` — ver [seção de contribuições upstream](#contribuicoes-upstream) abaixo para o registro consolidado.

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

**Reverificação pré-implementação da Issue #9 (feita contra o `master` atual do MOSA, commit de maio/2026 — mais recente que a versão `2.6.1.1669` pinada):**

- ✅ `Source/Mosa.Kernel.BareMetal.x86/IDT.cs`: o `case Scheduler.IRQ.Clock:` continua idêntico — `Interrupt?.Invoke(...)` seguido de `Scheduler.ClockInterrupt(...)`, sem mudanças. A Opção B (driver separado em paralelo, sem tocar no MOSA) continua tecnicamente válida.
- ✅ `Source/Mosa.DeviceSystem/Services/DeviceService.cs`: `IRQDispatch` continua `List<Device>[MaxInterrupts]`; `AddInterruptHandler` continua fazendo `.Add()` (não sobrescreve) — múltiplos devices no mesmo IRQ, incluindo IRQ0, seguem suportados.
- ⚠️ **Correção sobre um detalhe prático (não sobre a decisão em si):** `ISADeviceDriverRegistryEntry.AutoLoad` **não é lido em nenhum lugar do código atual** (`grep` confirmou zero ocorrências de `.AutoLoad` fora da própria declaração da propriedade). O fluxo real de start automático (`ISADeviceService.cs` → `DeviceService.Initialize(...)`) passa `autoStart: true` **hardcoded**, ignorando esse campo. Não é necessário configurar `AutoLoad` ao registrar o driver do timer — pode ser omitido sem efeito prático.

Decisão de design para Issue #9 (não é restrição, é escolha já fechada): ver [status.md](status.md#issue-9-pit).

## Contribuições upstream ao MOSA Project {#contribuicoes-upstream}

Dois achados técnicos desta investigação tinham reprodução concreta o suficiente para virar Issue no `mosa/MOSA-Project` — **ambos já foram abertos por Leandro em 28/08/2026**, suas primeiras contribuições upstream ao projeto (não só consumo). Confirmados publicados (visíveis no canal `#github-activity` do Discord do MOSA); nenhuma issue duplicada/existente foi encontrada antes da publicação.

1. **[Issue #1296](https://github.com/mosa/MOSA-Project/issues/1296) — `ArrayPlug.cs`: `Array.Copy` trava silenciosamente** (ver detalhes na seção [Korlib](#korlib) acima). Arquivo/linha exatos, reconhecimento do próprio time via `TODO`/`Broken`, reprodução em bare-metal via QEMU.
2. **[Issue #1295](https://github.com/mosa/MOSA-Project/issues/1295) — Regressão de empacotamento do `Mosa.Tools.Package`** (ver [Versionamento de dependências](tooling.md#versionamento) no tooling.md). Stack trace exato, causa raiz identificada (`Mosa.Compiler.Platforms` referenciado via `ProjectReference` não é copiado corretamente para o pacote NuGet publicado), confirmado persistente por vários meses e múltiplas versões (`1694` até pelo menos `1724`).

Ambas chegaram com causa raiz identificada e reprodução documentada, não como um simples "não funciona". Para checar o status atual de qualquer uma, buscar diretamente no GitHub em vez de assumir.
