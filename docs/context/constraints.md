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
| Formatação `double`/`float` → `string` | Decompor em partes inteiras antes de formatar — ver nota abaixo |

**Padrão para novos gaps:** ao ver `does not contain a definition for X` num tipo padrão do .NET, suspeitar deste padrão primeiro. Verificar na fonte do MOSA (`Source/Mosa.Korlib` vs `Source/Mosa.TinyCoreLib`) antes de propor fix. Documentar workaround com comentário inline, no estilo já usado em `Drill.cs`.

**Causa raiz identificada** (rastreada via inspeção direta da fonte MOSA, motivada por uma discussão de Discord de 10/2021 em que `TaylanInan` sinalizava que `Mosa.Korlib.Array.cs`'s `Copy` precisava de cópia de memória uint-a-uint em vez de byte-a-byte — essa otimização específica **foi** implementada depois, em `Source/Mosa.Runtime/Internal.cs`'s `MemoryCopy`/`MemoryCopy1`/`MemoryCopy4`, e **não** é a causa deste bug): a implementação real de `System.Array::Copy` é um **Plug** (mecanismo `Mosa.Runtime.Plug`, mesmo padrão do `ConsolePlug`) em `Source/Mosa.Plug.Korlib/System/ArrayPlug.cs`, linhas 13-52. Esse arquivo é explicitamente marcado como incompleto pelos próprios autores do MOSA: `// TODO: Fix!!!` logo acima do método `Copy`, e dentro dele, o cálculo correto de tamanho por tipo está comentado com `// Broken! (Size property loads at an invalid address most likely)`, substituído por um fallback `var size = IntPtr.Size;` que assume que todo elemento do array tem tamanho de ponteiro (4 bytes em x86). Esse fallback é coincidentemente correto para arrays de tipo referência como `string[]` (o caso do Drill em `Dispatch()`), então não é a causa direta do travamento neste caso específico — mas confirma que o Plug inteiro é reconhecidamente incompleto pelo próprio upstream. A causa mais provável do travamento está em como a aritmética de ponteiro (`arrayPtr + IntPtr.Size * 2 + index * size`) combina com `Mosa.Runtime.Internal.MemoryCopy`, causando acesso de memória inválido — plausível dado que não há validação de bounds/alinhamento visível após as checagens de `length`. Ainda não isolado até a linha exata — melhor pista disponível, não causa raiz 100% confirmada.

**⚠️ Alerta preventivo — outros métodos do mesmo arquivo:** `ArrayPlug.cs` também contém `Clear`, `IndexOf` e `GetLowerBound`, todos com o mesmo padrão de implementação stub/incompleta (`GetLowerBound` sempre retorna `0` com `// TODO`; `IndexOf` sempre retorna `-1` com `// TODO`; `Clear` reusa a mesma aritmética de ponteiro do `Copy`, logo herda a mesma suspeita de bug). Nenhum desses foi exercitado/confirmado com problema real ainda — mas se algum dia `Array.Clear()`, `Array.IndexOf()` ou `array.GetLowerBound()` forem usados no Mandrillus e algo travar ou se comportar de forma errada sem mensagem de erro clara, suspeitar deste mesmo arquivo antes de qualquer outra hipótese.

**Potencial de contribuição upstream:** este achado é um candidato forte para uma Issue/PR real no `mosa/MOSA-Project` — já existe reprodução concreta em bare-metal, arquivo/linha exatos (`Source/Mosa.Plug.Korlib/System/ArrayPlug.cs:13-52`), e reconhecimento do próprio time via comentários `TODO`/`Broken` de que o Plug precisa de correção. Mais promissor que um gap de API simplesmente ausente, já que já há consciência prévia do problema por parte do upstream.

## Gap #4 — Formatação `double`/`float` → `string` inexistente

**Categoria: mesma do `Array.Copy`** (compila limpo, trava silenciosamente em runtime) — descoberto ao vivo no QEMU testando o `UptimeCommand`: `Console.WriteLine("Uptime (seconds): " + SystemTimer.UptimeSeconds)` (onde `UptimeSeconds` é `double`) parou de imprimir depois das duas linhas anteriores (que usam `ulong`/`uint`, sem problema) — sem exceção, sem crash, o prompt simplesmente retornou.

**Causa raiz confirmada por busca exaustiva na fonte:** `Mosa.Korlib` **não tem nenhuma formatação de `double`/`float` pra `string`, em lugar nenhum**. `Source/Mosa.Korlib/System/Numbers.cs` só cobre inteiros até 32 bits (`UInt8`/`Int8`/`Int16`/`UInt16`/`Int32`/`UInt32ToString` — nem `Int64`/`UInt64` estão lá). `Double.cs` não tem `ToString()` próprio.

**Cadeia do travamento:** `"texto" + double` é resolvido via `string.Concat(object, object)`, que chama `.ToString()` em cada argumento. Sem `ToString()` próprio, `Double` cai em `ValueType.ToString()` → `GetType().ToString()` — ou seja, **boxing do `double` + reflection via `GetType()`**, ambos pontos frágeis conhecidos em bare-metal. Esse é o ponto mais provável do travamento (não isolado até a instrução exata).

**Contraste confirmado:** `Int32`/`UInt32` **têm** `ToString()` próprio (chamam `Numbers.Int32ToString`/`UInt32ToString`) — por isso concatenação com inteiros sempre funcionou (ex.: `HistoryCommand.cs`). O problema é específico de `double`/`float`.

**Workaround:** nunca concatenar um `double`/`float` bruto numa string. Decompor em partes inteiras primeiro — mesmo padrão já usado pro `ConvertU64ToR8` no `SystemTimer.cs`. Exemplo (`UptimeCommand.cs`):

```csharp
var wholeSeconds = (uint)(SystemTimer.Ticks / SystemTimer.FrequencyHz);
var remainderTicks = (uint)(SystemTimer.Ticks % SystemTimer.FrequencyHz);

// remainderTicks é uma contagem de ticks (0..FrequencyHz-1), não uma fração
// decimal de segundo — imprimir direto após o ponto (ex.: "7.36") é
// matematicamente incorreto (achado do review automático do Copilot no PR
// da Issue #9). Escalar pra centésimos via aritmética inteira pura, nunca
// convertendo pra double.
var hundredths = (remainderTicks * 100) / SystemTimer.FrequencyHz;
var hundredthsStr = hundredths < 10 ? "0" + hundredths : hundredths.ToString();

Console.WriteLine("Uptime (seconds): " + wholeSeconds + "." + hundredthsStr);
```

**Potencial de contribuição upstream:** provavelmente o gap mais fundamental dos quatro documentados — formatação correta de ponto flutuante (arredondamento tipo Grisu/Dragon4) é genuinamente complexa, e não parece que ninguém implementou isso no `Mosa.Korlib` ainda. Candidato a uma quarta issue upstream, não levantado ainda.

## Gap #5 — `volatile` indisponível (`System.Runtime.CompilerServices.IsVolatile` ausente)

**Categoria: nova, distinta das anteriores** — não é API ausente (como `Dictionary`/`string.Join`), nem falha silenciosa em runtime (como `Array.Copy`/double-to-string), é um **tipo do runtime ausente que o próprio compilador C# depende para emitir código**.

**Sintoma:** declarar qualquer campo `volatile` em `Mandrillus.Kernel` falha na compilação com:

```code
CS0518: Predefined type 'System.Runtime.CompilerServices.IsVolatile' is not defined or imported
```

**Causa raiz:** o compilador C# injeta um `modreq` (`IsVolatile`) em todo campo declarado `volatile`, como parte do contrato binário do CLI (ECMA-335) que garante ao runtime que a leitura/escrita não pode ser otimizada/cacheada. `Mosa.Korlib` não define esse tipo — descoberto tentando implementar um seqlock em `SystemTimer.cs` (Issue #9, proteção de leitura do contador `Ticks` de 64 bits contra leitura "rasgada" em alvo 32-bit). Ou seja: **a palavra-chave `volatile` simplesmente não é utilizável neste alvo**, do mesmo jeito que `Dictionary<TKey,TValue>` não é.

**Implicação além do erro de compilação:** mesmo que esse tipo existisse, um seqlock sem `volatile` não teria garantia real de correção — o compilador ficaria livre para manter um campo "não-volátil" cacheado em registrador dentro de um loop, quebrando silenciosamente a premissa do algoritmo. Bom que travou em vez de compilar "errado".

**Workaround adotado (`SystemTimer.cs`, Issue #9):** em vez de proteger a leitura via seqlock, usar `HAL.DisableAllInterrupts()`/`HAL.EnableAllInterrupts()` (`Mosa.DeviceSystem.HardwareAbstraction.HAL` — público, agnóstico de plataforma, mesmo padrão de resolução por plug do `HAL.Yield()`, resolvendo em cadeia até `Native.Cli()`/`Native.Sti()` em x86 via `Mosa.Kernel.BareMetal.Platform.Interrupt`) ao redor da leitura de `Ticks`. Só o **leitor** precisa da seção crítica — o **escritor** (`IncrementTicks()`, chamado de dentro do handler de IRQ) já roda com interrupções mascaradas pelo próprio mecanismo de gate do x86, então não se envolve `EnableAllInterrupts()` ali dentro (reabilitar interrupções no meio do atendimento da própria IRQ, antes do `IRET`, não é algo pra fazer sem necessidade real).

**Potencial de contribuição upstream:** candidato razoável, mas de escopo maior que os outros — implicaria adicionar suporte a `modreq`/`IsVolatile` no toolchain do MOSA (compilador + Korlib), não só preencher uma lacuna de API. Não levantado ainda.

## Testes automatizados

Mandrillus não tem (e não terá) suíte de testes própria — usa o tooling de teste do MOSA como está. Motivo: `Mosa.Korlib` não roda sob xUnit/.NET host normal, e o próprio MOSA não usa xUnit convencional para código bare-metal. Prática de xUnit fica reservada para outro projeto com runtime .NET completo. Decisão fechada — ver [status.md](status.md#prática-de-git-disciplinado).

## Fronteira platform-agnostic vs. x86

- `Boot.cs` (**`Mandrillus.Kernel.x86`**) — entry point fino, específico de plataforma
- `Program.cs` (**`Mandrillus.Kernel`**) — `SetBootOptions()` e `EntryPoint()`; lógica de aplicação (incl. `Drill.Start()`) vive aqui
- HAL (`Mosa.DeviceSystem`) e `Kernel.Keyboard` (`Mosa.Kernel.BareMetal`, sem sufixo `.x86`) são platform-agnostic — só o runtime de `HAL.Yield()` varia por plataforma
- Verificar sempre o sufixo do assembly antes de decidir em qual projeto um arquivo novo entra

## ⚠️ Colisão de nomes: `Kernel` (namespace do Mandrillus) vs. `Kernel` (classe do MOSA)

**Causa:** o namespace raiz do Mandrillus é `Mandrillus.Kernel` (com `Hardware`, `Shell`, etc. como sub-namespaces). O MOSA tem uma classe estática literalmente chamada `Kernel`, em `Mosa.Kernel.BareMetal.Kernel` (expõe `ServiceManager`, `Keyboard`). Isso é uma colisão de nomes pura da linguagem C#, não um bug do MOSA/Korlib.

**Sintoma:** dentro de qualquer arquivo cujo namespace esteja sob a árvore `Mandrillus.Kernel.*` (ex.: `Mandrillus.Kernel.Hardware`), escrever `Kernel.Algo` sem qualificação completa — mesmo com `using Mosa.Kernel.BareMetal;` no topo — resolve para o **namespace** `Mandrillus.Kernel` (porque a busca de nome do C# prioriza namespaces/tipos da própria árvore de namespace *antes* das diretivas `using`), não para a classe do MOSA. Gera erro `CS0234` do tipo `The type or namespace name 'X' does not exist in the namespace 'Mandrillus.Kernel'` — confirmado empiricamente com um repro mínimo reproduzindo a mensagem exata.

**Regra prática:** sempre usar o caminho **totalmente qualificado** `Mosa.Kernel.BareMetal.Kernel.X` (nunca `Kernel.X` sozinho) em qualquer arquivo dentro de `Mandrillus.Kernel.*`. `Drill.cs` já seguia essa prática por acaso (`Mosa.Kernel.BareMetal.Kernel.Keyboard.GetKeyPressed()`); `HardwareSetup.cs` foi o primeiro caso real onde a forma abreviada quebrou. Vale revisar qualquer código novo que referencie `Kernel.ServiceManager`/`Kernel.Keyboard` do MOSA.

## Hardware do PIT (fatos, não decisão de projeto)

- Chip 8253/8254 tem **um único canal físico 0** — qualquer timer do Mandrillus e o `Scheduler.ClockInterrupt` do MOSA compartilham essa frequência. Restrição de hardware, não de arquitetura 32/64-bit nem de BIOS.
- Portas canônicas: `0x40` (dado canal 0), `0x43` (comando). Sequência: modo → LSB → MSB.
- MOSA não traz driver de PIT pronto. RTC (`0x70`/`0x71`) só dá hora/calendário, não gera tick periódico. `HAL.Sleep()` é `// TODO` vazio no framework.

## ✅ Issue #9 — RESOLVIDA: `TargetFrequencyHz = 250` (decisão final)

**Curva empírica completa de precisão de captura de ticks**, medida via testes cronometrados reais (QEMU+WHPX, aceleração de hardware):

| Frequência-alvo | % de ticks capturados | Desvio |
| --- | --- | --- |
| 100 Hz | ~97,3% | ~2,7% |
| **250 Hz (escolhida)** | **~97,2%** | **~2,8%** |
| 500 Hz | ~89,7% | ~10,3% |
| 1000 Hz (original) | ~62-64% | ~36-38% |

**A curva não é linear** — precisão fica estável até `250 Hz`, degrada moderadamente em `500 Hz`, e despenca em `1000 Hz`. Esse formato (plano → inclinação → queda acentuada) é consistente com um **overhead fixo por IRQ** (rodar `PitTimer.OnInterrupt()` + `Scheduler.ClockInterrupt()` + entrada/saída de ISR/EOI a cada disparo) que é irrelevante em frequências baixas, mas consome fatia crescente do orçamento de tempo por tick conforme a frequência sobe. **Mecanismo exato ainda não isolado** — só a natureza dependente-de-frequência foi comprovada empiricamente, com 4 pontos de dados.

**Decisão:** `250 Hz` — mesma precisão de `100 Hz` (~97%), mas granularidade 2,5x mais fina (4ms vs 10ms), e confortavelmente na parte estável/segura da curva. Divisor: `1193182 / 250 ≈ 4773` (bem dentro do limite de 16 bits).

**Nota pós-review:** a implementação inicial truncava essa divisão; corrigido posteriormente (review automático do Copilot no PR) para arredondar pro divisor mais próximo em vez de truncar, e `SystemTimer.FrequencyHz` passou a armazenar a frequência *real* resultante do divisor escolhido, não apenas o alvo nominal de `250` — diferença de ~0,015% neste caso específico, desprezível frente ao erro de medição de ~3% já documentado na tabela acima, mas a forma correta de calcular.

**Como o teste foi feito:** `TargetFrequencyHz` alterado manualmente em `PitTimer.cs` pra cada valor testado, seguido de teste cronometrado (`uptime` → cronômetro real → `uptime` de novo, com janelas de 30-60s) rodando via `qemu-system-x86_64 -accel whpx -cpu qemu32,...` (ver nota sobre WHPX no `tooling.md`).

**Itens em aberto, não bloqueiam o fechamento da Issue #9:**

- Mecanismo exato da perda de interrupções em frequências altas — não investigado a fundo; candidato a issue upstream se algum dia for relevante.
- Regressão do teclado no Hyper-V (não relacionada ao PIT — confirmado testando sem `HardwareSetup.RegisterPitTimer()` ativo, problema persiste) — rastreada separadamente, prioridade baixa.

**Reverificação pré-implementação da Issue #9 (feita contra o `master` atual do MOSA, commit de maio/2026 — mais recente que a versão `2.6.1.1669` pinada):**

- ✅ `Source/Mosa.Kernel.BareMetal.x86/IDT.cs`: o `case Scheduler.IRQ.Clock:` continua idêntico — `Interrupt?.Invoke(...)` seguido de `Scheduler.ClockInterrupt(...)`, sem mudanças. A Opção B (driver separado em paralelo, sem tocar no MOSA) continua tecnicamente válida.
- ✅ `Source/Mosa.DeviceSystem/Services/DeviceService.cs`: `IRQDispatch` continua `List<Device>[MaxInterrupts]`; `AddInterruptHandler` continua fazendo `.Add()` (não sobrescreve) — múltiplos devices no mesmo IRQ, incluindo IRQ0, seguem suportados.
- ⚠️ **Correção sobre um detalhe prático (não sobre a decisão em si):** `ISADeviceDriverRegistryEntry.AutoLoad` **não é lido em nenhum lugar do código atual** (`grep` confirmou zero ocorrências de `.AutoLoad` fora da própria declaração da propriedade). O fluxo real de start automático (`ISADeviceService.cs` → `DeviceService.Initialize(...)`) passa `autoStart: true` **hardcoded**, ignorando esse campo. Não é necessário configurar `AutoLoad` ao registrar o driver do timer — pode ser omitido sem efeito prático.

**Registro do driver — sem hook oficial no `Setup.cs` do MOSA:** confirmado por inspeção completa (código-fonte, exemplos `CoolWorld`/`TestWorld`/`Starter`, todos os 4 drivers ISA existentes) que **não há precedente nem extensão oficial** para adicionar um driver customizado à lista fixa de `Mosa.DeviceDriver.Setup.GetDeviceDriverRegistryEntries()` — ela não é `partial`, não tem callback, e nenhum projeto de exemplo do MOSA jamais precisou estendê-la. Solução adotada: chamar diretamente `DeviceService.Initialize(DeviceDriverRegistryEntry, ...)` — o mesmo método público que `ISADeviceService` usa internamente — a partir de `Program.cs`, depois que `Kernel.ServiceManager` já existe (ver `Hardware/HardwareSetup.cs`). Reproduz o pipeline completo (`Setup → Initialize → Probe → Start → AddInterruptHandler`) sem tocar em código do MOSA nem duplicar lógica.

**Validação de design contra fonte externa (OSDev Wiki, não MOSA/Cosmos):** o artigo [Programmable Interval Timer](https://wiki.osdev.org/Programmable_Interval_Timer) confirma várias decisões já tomadas de forma independente:

- Canal 0 é o único canal do PIT conectado a uma IRQ — valida a escolha de `IRQ0`.
- Mode 2 (rate generator) é escolha legítima e documentada para ganhar precisão de frequência, ainda que Mode 3 seja mais comum em BIOS/SOs — bate com a decisão já tomada em `PitTimer.cs`.
- `1000 Hz` (divisor `~1193`) — a frequência originalmente cogitada — **coincidia com o padrão do kernel Linux moderno**, mas foi descartada na prática após a curva empírica revelar perda significativa de ticks nessa faixa; `250 Hz` foi a escolha final (ver seção acima).
- O estado padrão sem programação (~18.2 Hz, ~54.9ms/tick) confirma exatamente a limitação do Cosmos que já documentamos evitar.
- Divisores excessivamente baixos podem travar o sistema inteiro.

**⚠️ Ressalva nova, trazida por essa fonte (não estava documentada antes):** a página do PIT no OSDev Wiki está marcada como **"curiosidade histórica, não recomendada para novos designs"** — em hardware real moderno, o PIT foi suplantado por HPET, ACPI Timer e APIC Timer (este último ciente de múltiplos processadores). A fonte cita o PIT como **confirmadamente quebrado em CPUs Arrow Lake** e **ausente no Surface Pro 4**. Isso não bloqueia nada hoje (QEMU emula o PIT fielmente, e Hyper-V Gen 1 também já está validado), mas é um risco real a monitorar **se o Mandrillus algum dia mirar hardware físico moderno** (não só VM/emulador) — um driver de APIC Timer ou HPET pode vir a ser necessário como alternativa/fallback futuro. Também vale registrar: precisão típica do PIT é de apenas ±1.73 segundos/dia — limitação real, mas irrelevante para o propósito de exibição de uptime.

**Confirmado: nenhum desses timers "mais modernos" é uma alternativa viável hoje no MOSA** (investigado como resposta direta à ressalva acima, considerando também o plano futuro de x64 do Mandrillus):

| Timer | Estado no MOSA |
| --- | --- |
| HPET | Inexistente — zero referências em todo o código-fonte |
| ACPI Timer (PM Timer) | `FADT.cs` tem os offsets corretos do campo, mas nada nunca lê esse valor |
| APIC Timer | `LocalAPIC.cs` só habilita o registro base e envia EOI — nenhum código toca nos registros de timer do APIC (`0x320`/`0x380`/`0x390`) |

**Causa raiz comum a ACPI Timer e APIC Timer:** `Source/Mosa.DeviceDriver/ACPI/ACPIDriver.cs`'s `Initialize()` tem a busca do RSDP (ponteiro raiz de todas as tabelas ACPI) comentada e substituída por `Pointer.Zero`, com `// TODO: Find the multiboot service` acima. Como `if (rsdp.IsNull) return;` vem logo em seguida, **`ACPIDriver.Initialize()` sempre retorna sem fazer nada** — nenhuma tabela ACPI é de fato descoberta em runtime. Confirmado também que `LocalAPIC.SendEndOfInterrupt()` tem fallback explícito pro PIC 8259 legado quando o APIC não está inicializado — ou seja, **o MOSA roda inteiramente sobre PIC legado hoje**, não sobre APIC, independente do alvo ser x86 ou x64.

**Esclarecimento arquitetural (relevante para o plano de x64 do Mandrillus):** PIC vs. APIC **não é uma distinção de 32 vs. 64-bit** — kernels x64 rodam normalmente sobre PIC legado em estágios iniciais. APIC se torna necessário especificamente para **SMP/múltiplos núcleos**, não pela largura de registrador. Como o scheduler do Mandrillus é cooperativo e single-core, sem SMP no roadmap, o PIT continua sendo a escolha proporcional e correta pra Issue #9, independente do alvo x64 futuro.

**Conclusão:** manter o PIT não é só "o caminho de menor resistência" — é **a única opção viável hoje no MOSA**, já que as alternativas dependem de uma lacuna própria do MOSA (descoberta de RSDP/ACPI) não resolvida, fora do escopo do que o Mandrillus poderia razoavelmente implementar sem antes corrigir infraestrutura upstream. Fica registrado como item de roadmap de longo prazo, não ação imediata: **se o Mandrillus algum dia mirar SMP ou hardware real moderno**, a lacuna de RSDP/ACPI precisaria ser corrigida primeiro — candidato plausível a contribuição upstream, no mesmo espírito das issues #1295/#1296 já abertas — antes mesmo de um driver de APIC Timer ser possível.

Decisão de design para Issue #9 (não é restrição, é escolha já fechada): ver [status.md](status.md#issue-9-pit).

**Status final:** Issue #9 fechada. PR revisado (incluindo review automático do Copilot, que identificou o bug de formatação do `uptime` e a questão de atomicidade de `Ticks` — ver [Gap #5](#gap-5-volatile-indisponível-systemruntimecompilerservicesisvolatile-ausente) acima para a resolução), corrigido, testado no QEMU e mesclado em `master`.

## ⚠️ Bug do compilador MOSA: `ulong`/`long` → `double` em x86

**Categoria diferente dos gaps anteriores** — não é biblioteca (`Korlib`) nem colisão de nome (C#), é uma lacuna real de *lowering* no próprio compilador `Mosa.Compiler.x86`.

**Sintoma:** compilar `(double)Ticks / FrequencyHz` (onde `Ticks` é `ulong`) falha com `Missing Code Transformation: IR.ConvertU64ToR8`. Aconteceu em `SystemTimer.cs` (`UptimeSeconds`, `ElapsedSeconds`), Issue #9.

**Causa raiz confirmada por inspeção direta da fonte:**

- `IR.ConvertU64ToR8` (`ulong → double`) **não tem nenhum transform real em nenhuma plataforma** do MOSA (x86, x64, ARM32) — só existe um constant-folding (`Source/Mosa.Compiler.Framework/Transforms/Optimizations/Auto/ConstantFolding/ConvertU64ToR8.cs`), que só ajuda valores literais conhecidos em tempo de compilação, não valores de runtime como `Ticks`.
- **Achado mais sério**: mesmo `ConvertI64ToR8` (`long → double`, com sinal), que **tem** transform em x86 (`Source/Mosa.Compiler.x86/Transforms/BaseIR/ConvertI64ToR8.cs`), está **incompleto** — divide o valor em metades de 32 bits e **descarta a metade alta**, convertendo só a parte baixa via `Cvtsi2sd32`. Ou seja, `(double)(long)Ticks` compilaria, mas retornaria **resultado silenciosamente errado** acima de `~2^31` (~24,9 dias de uptime a 1000 Hz) — sem erro, sem crash, só valor errado.
- Confirmado que o x64 equivalente é correto (`Cvtsi2sd64`, registrador completo de 64 bits) — o bug é específico do x86 32-bit.
- Por contraste, `ConvertU32ToR8` (`uint → double`) **está correto** em x86 — usa o valor de 32 bits inteiro, nada é descartado.

**Workaround aplicado em `SystemTimer.cs`:** dividir em aritmética inteira (`ulong`/`uint`) primeiro — `wholeSeconds = Ticks / FrequencyHz`, `remainderTicks = Ticks % FrequencyHz` — e só converter pra `double` os valores resultantes, já pequenos o suficiente pra serem seguros. `wholeSeconds` só arrisca o mesmo limite depois de ~68 anos de uptime contínuo (irrelevante); `remainderTicks` é sempre `< FrequencyHz`, dentro da faixa segura de `ConvertU32ToR8`.

**Contexto histórico (achado numa conversa do Discord de abril/2022, canal `#compiler`):** `charsleysa` e `tgiphil` resolveram exatamente esse tipo de problema, mas na **direção oposta** (`double → int64`) — descartaram FPU legado (`FLD`/`FISTTP`) por decisão explícita do `tgiphil` ("vamos evitar o FPU legado"), confirmaram que `CVTTSD2SI` não suporta destino de 64 bits em modo 32-bit, e resolveram via **decomposição manual de bits IEEE 754** em software puro — sem depender de FPU nem SSE. Essa solução está implementada hoje em `Mosa.Runtime.Math.Conversion.R8ToI8`/`R8ToU8`/etc. Crucialmente, `charsleysa` escreveu em 24/04/2022: *"I completed the software based float/double to UI64 conversion (though I'll probably also add the reverse conversion too)"* — reconhecendo explicitamente que a direção inversa (a que o Mandrillus precisa) também seria necessária. Confirmado por busca exaustiva que **essa conversão reversa nunca foi adicionada** ao `Conversion.cs` atual — a promessa aparentemente nunca foi cumprida (não foi possível confirmar via busca se chegou a existir um PR abandonado).

**Potencial de contribuição upstream — o mais forte dos três achados até agora:** já existe reprodução real (erro de build do Mandrillus), causa raiz com arquivo/linha exatos nos dois casos (ausência total e truncamento silencioso), **e** evidência histórica direta de que a equipe já conhece a técnica de solução (decomposição de bits IEEE 754, só que na direção inversa — reconstruir os bits de um `double` a partir de um inteiro, em vez de extrair um inteiro dos bits de um `double`).

## Achados com potencial de contribuição upstream (registro consolidado)

Cinco achados técnicos desta investigação têm reprodução concreta o suficiente para virar Issue/PR no `mosa/MOSA-Project`, caso Leandro decida contribuir de volta:

1. **`ArrayPlug.cs` — `Array.Copy` trava silenciosamente** (ver detalhes na seção [Korlib](#korlib) acima). Arquivo/linha exatos, reconhecimento do próprio time via `TODO`/`Broken`, reprodução em bare-metal via QEMU.
2. **Regressão de empacotamento do `Mosa.Tools.Package`** (ver [Versionamento de dependências](tooling.md#versionamento) no tooling.md). Stack trace exato, causa raiz identificada (`Mosa.Compiler.Platforms` referenciado via `ProjectReference` não é copiado corretamente para o pacote NuGet publicado), confirmado persistente por vários meses e múltiplas versões (`1694` até pelo menos `1724`).
3. **`ConvertU64ToR8`/`ConvertI64ToR8` ausente/truncado em x86** (ver seção acima). O mais forte dos cinco — vem com causa raiz precisa **e** evidência histórica de que a equipe já sabe como resolver (mesma técnica usada com sucesso na direção oposta, `R8ToI8`).
4. **Formatação `double`/`float` → `string` ausente em `Mosa.Korlib`** (ver [Gap #4](#gap-4---formatação-doublefloat--string-inexistente) acima). Causa raiz confirmada (nenhum `ToString()` próprio em `Double`/`Single`), mas escopo de implementação genuinamente complexo (arredondamento correto de ponto flutuante).
5. **`System.Runtime.CompilerServices.IsVolatile` ausente, tornando `volatile` inutilizável** (ver [Gap #5](#gap-5-volatile-indisponível-systemruntimecompilerservicesisvolatile-ausente) acima). Escopo maior que os demais — envolveria suporte a `modreq` no toolchain, não só uma lacuna de API isolada.

Todos os cinco são candidatos mais fortes que um simples "não funciona" — já chegam com causa raiz identificada e reprodução documentada.
