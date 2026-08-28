*Leia em outros idiomas: [English](ROADMAP.md)*

# Roadmap do Mandrillus

Este documento acompanha a trajetória de desenvolvimento do **Mandrillus OS** e do ecossistema mais amplo **Mandrillus Systems** — um pequeno conjunto de aplicações construído em torno de um sistema operacional nativo em C#, desenvolvido com o [MOSA Project](https://github.com/mosa/MOSA-Project).

Legenda de status: ✅ Concluído · 🚧 Em andamento · ⏳ Planejado · 💭 Conceito

---

## Fase 0 — Fundação

- ✅ Estrutura da solution: `Mandrillus.Kernel` (agnóstico de plataforma) + `Mandrillus.Kernel.x86` (alvo x86)
- ✅ Scaffolding via template MOSA (`mosakrnl`)
- ✅ Pacotes MOSA fixados na versão `2.6.1.1669` (workaround para regressão de empacotamento em 2.6.1.1694+)
- ✅ Boot validado em QEMU (via Mosa.Tool.Launcher)
- ✅ Boot validado em Hyper-V (Generation 1, Secure Boot desabilitado, conversão manual `.img` → `.vhd`)
- ✅ Licenciamento: MIT (Mandrillus Systems) + `THIRD-PARTY-LICENSES.md` (New BSD do MOSA)
- ✅ README bilíngue (`README.md` / `README.pt-BR.md`)

## Fase 1 — Núcleo do Kernel (x86, 32 bits)

Rastreado como Issues no GitHub, sob o milestone **"Phase 1 — Kernel Core"**.

> **Nota:** uma investigação confirmou (via [documentação do MOSA](https://www.mosa-project.org/a-dive-into-baremetal.html), o log de boot real e inspeção direta de `Source/Mosa.DeviceDriver/ISA/` no repositório do MOSA) que a maior parte — mas não toda — da infraestrutura de baixo nível da Fase 1 já é fornecida pelo kernel MOSA BareMetal sobre o qual o `Mandrillus.Kernel.x86` é construído. Os itens abaixo marcados como *(herdado)* não são trabalho autoral.

- ✅ Configuração da GDT (Global Descriptor Table) *(herdado do MOSA BareMetal)* — #1
- ✅ IDT (Interrupt Descriptor Table) + tratamento básico de interrupções *(herdado do MOSA BareMetal)* — #2
- ✅ Gerenciador de memória física (alocador de páginas) *(herdado do MOSA BareMetal)* — #3
- ✅ Memória virtual / paginação *(herdado do MOSA BareMetal)* — #4
- ✅ Driver de teclado *(herdado do MOSA BareMetal — `StandardKeyboard`)* — #6
- ✅ Driver de vídeo em modo texto *(herdado do MOSA BareMetal — API `Console`)* — #7
- ⏳ Shell interativo mínimo (trabalho autoral — integra teclado + saída de console em um loop de comandos) — #8

> **Nota:** o item de timer PIT (originalmente #5 aqui) foi movido para a Fase 2 — veja abaixo. A investigação confirmou que o shell não tem nenhuma dependência de trabalho de timer; o timer é, na verdade, um pré-requisito de escalonamento preemptivo. Raciocínio completo na nota sob a Fase 2.
>
> **Atenção para a Issue #8:** discussões públicas no [Discord do MOSA](https://discord.gg/tRNMn3npsv) (canal `#general`) descrevem o scheduler do BareMetal como cooperativo, não preemptivo — foi relatado que uma chamada bloqueante de leitura de teclado travou o sistema inteiro porque nada forçava uma troca de contexto enquanto ela bloqueava. O loop principal do shell deveria seguir o mesmo padrão ao qual a comunidade chegou: verificar se há uma tecla sem bloquear indefinidamente, em vez de chamar uma leitura bloqueante diretamente. Veja a nota da Fase 2 abaixo para o contexto relacionado de timer/scheduler.

## Fase 2 — Serviços de Sistema

> **Nota sobre o timer PIT:** uma investigação mais profunda (duas passagens de pesquisa assistida por IA, independentes, sobre o código-fonte atual do MOSA na branch `master`) confirmou que não existe driver de PIT/8253/8254 em nenhum lugar do MOSA — mas também revelou que o IRQ0 *não* está ocioso: o `Scheduler` do `Mosa.Kernel.BareMetal` já o consome (`Scheduler.ClockInterrupt`) na frequência padrão que o BIOS/QEMU deixou o PIT rodando (~18.2Hz, não configurada). O `HAL.Sleep()` existe como superfície de API, mas seu corpo é um `// TODO` vazio. Ou seja, isso não é "adicionar um timer que não existe" — é "assumir controle de um timer que já está silenciosamente guiando o scheduler". Por isso pertence aqui, como pré-requisito da Fase 2 para escalonamento preemptivo de verdade, em vez da Fase 1 (que não tem nenhuma dependência disso — veja a nota da Fase 1 acima).
>
> Isso é refletido de forma independente em discussões públicas no [Discord do MOSA](https://discord.gg/tRNMn3npsv) (canal `#operating-system`): a comunidade já observou que a frequência do tick de clock do Scheduler não é documentada nem configurada deliberadamente, batendo com o que a investigação de código encontrou. Separadamente (canal `#general`), a comunidade discutiu que o scheduler é cooperativo, não preemptivo, e que multithreading existe como recurso do framework, mas não é exercitado pelos kernels de demonstração.
>
> **Decisão de design (Issue #9):** a inspeção direta de `Source/Mosa.Kernel.BareMetal.x86/IDT.cs` confirmou que o driver de timer pode ser registrado como um dispositivo separado (`IRQ = 0` via `DeviceService`/`ISADeviceDriverRegistryEntry`), rodando junto com — não em vez de — `Scheduler.ClockInterrupt`, sem modificar o próprio MOSA. Isso desacopla o *código* do timer do Scheduler, embora ambos ainda compartilhem a frequência única do canal 0 do PIT (uma restrição física do chip 8253/8254, sem relação com a arquitetura da CPU ou com o BIOS). Detalhamento completo, incluindo um risco residual documentado ligado a versões futuras do MOSA, na Issue #9.
>
> **Referência de design (não de código):** uma investigação paralela do `Cosmos.HAL/PIT.cs` do Cosmos OS (também duas passagens de pesquisa assistida por IA, independentes, cruzadas com a implementação canônica de PIT do kernel xv6) confirmou que o Cosmos usa as mesmas portas de I/O padrão (0x40/0x43) e a mesma sequência de escrita modo→LSB→MSB, e oferece uma camada de timer por software baseada em callback (`PITTimer`/`OnTrigger`) que vale usar como inspiração para o mesmo tipo de design desacoplado confirmado viável acima — com uma limitação conhecida (granularidade fixa de rearme de ~54.9ms, não reprogramada para o próximo prazo) que vale evitar repetir. Isso é apenas inspiração, não código reaproveitável: Cosmos e MOSA usam abstrações de baixo nível incompatíveis (`Cosmos.Core.IOPort` vs. `IOPortReadWrite` do MOSA), então os dois frameworks não são interoperáveis nessa camada. Detalhamento técnico completo na Issue #9.

- 💭 Gerenciamento de processos/tarefas
- 💭 Escalonador básico (cooperativo → preemptivo)
- ⏳ Timer PIT: programar uma frequência conhecida e assumir controle do tick de clock do scheduler (IRQ0/vetor 0x20 compartilhado) — #9
- 💭 Comunicação entre processos (IPC)
- 💭 Suporte a sistema de arquivos (começar somente leitura, FS simples)
- 💭 Camada de abstração de dispositivos

## Fase 3 — Portabilidade para 64 bits

- 💭 Avaliação do alvo x64 assim que o kernel x86 tiver escalonador + gerenciador de memória + drivers
- 💭 Transição para long mode
- 💭 Paginação de 4 níveis
- 💭 Revalidação da cadeia de boot (configuração do Launcher, tratamento de formato de imagem) para x64

## Fase 4 — Ecossistema Mandrillus Systems (Aplicações)

Trabalho posterior aos marcos do kernel — requer um modelo de processos e sistema de arquivos funcionais.

- 💭 Editor de texto simples
- 💭 Editor de código
- 💭 Aplicação simples de desenho
- 💭 Pacote office nativo e leve

---

## Notas

- Ferramentas: `Mosa.Tool.Launcher` (GUI) e `Mosa.Tool.Launcher.Console` (CLI, flags de traço único, ex: `-platform x86`, `-emulator qemu`, `-destination <path>`).
- Plataforma alvo para as Fases 0–2: **x86 (32 bits)** — escolhida pela maturidade do toolchain, paginação mais simples e configuração de modo protegido melhor documentada dentro do MOSA.
- Tagline: *"Mandrillus Systems — evolution, one bit at a time."*
- **Nota de arquitetura:** o `Mandrillus.Kernel.x86` é construído sobre o **kernel MOSA BareMetal** (`Mosa.Kernel.BareMetal.x86.dll` / `Mosa.Kernel.BareMetal.dll`, referenciado via os pacotes `Mosa.Platform.x86` e `Mosa.Platform`). O BareMetal cuida de GDT, IDT, gerenciamento de memória física/virtual, primitivas de escalonamento, HAL e registro de drivers de dispositivo (incluindo teclado e saída de vídeo/console) automaticamente durante o boot, antes de qualquer código específico do Mandrillus ser executado. O trabalho autoral do Mandrillus começa na camada de aplicação — o shell interativo em diante. Veja o `README.md` para uma divisão clara entre componentes herdados e autorais.
- **Resumo da investigação de PIT/timer (Fase 2, Issue #9):**

  | Fonte | O que confirmou |
  |---|---|
  | Código-fonte do MOSA (`Source/Mosa.DeviceDriver/ISA/`, análise estática) | Nenhum driver de PIT/8253/8254 em nenhum lugar do MOSA |
  | Código-fonte do MOSA (`Mosa.Kernel.BareMetal`, `Scheduler.cs`, `IDT.cs`) | IRQ0/vetor `0x20` já consumido pelo `Scheduler.ClockInterrupt`, numa frequência padrão não configurada do BIOS/QEMU |
  | Código-fonte do MOSA (`Mosa.Kernel.BareMetal.x86/IDT.cs`, inspeção direta do `ProcessInterrupt`) | Confirmou que um driver de dispositivo separado pode se registrar em `IRQ = 0` via `DeviceService`, rodando junto com `Scheduler.ClockInterrupt` sem modificar o MOSA — resolveu uma contradição anterior entre duas investigações |
  | Comunidade do MOSA, [Discord](https://discord.gg/tRNMn3npsv), discussão pública | Frequência da interrupção não é documentada; scheduler é cooperativo, não preemptivo; chamadas bloqueantes podem travar o sistema inteiro |
  | Código-fonte do Cosmos OS (`Cosmos.HAL/PIT.cs`), cruzado com xv6 | Confirma uso de portas/sequência canônicas; padrão de callback `PITTimer`/`OnTrigger` é uma referência de design útil, com uma limitação conhecida de granularidade a evitar repetir |

  Detalhamento completo e citações na Issue #9.
