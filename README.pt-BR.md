**[English](./README.md)** · **Português (Brasil)**

---

# Mandrillus OS

> Um sistema operacional bare-metal escrito inteiramente em C#, construído sobre o [MOSA Project](https://github.com/mosa/MOSA-Project) — o núcleo fundacional do ecossistema **Mandrillus Systems**.

---

## Por que um sistema operacional em C#?

A resposta curta é: porque a maioria dos desenvolvedores .NET nunca precisa pensar abaixo da camada do runtime. O CLR abstrai gerenciamento de memória, agendamento de threads e acesso a hardware — e essa abstração é exatamente o que torna C# produtivo para aplicações de negócio. Mas também é o que torna raro encontrar desenvolvedores .NET que entendam *o que existe por baixo* dessa abstração.

O Mandrillus OS nasce dessa lacuna. Não é um sistema operacional destinado a uso em produção — é um exercício deliberado de remover as camadas de conforto do .NET (garbage collector gerenciado por runtime, BCL completa, sistema de arquivos, drivers) e reconstruir, peça por peça, o que é estritamente necessário para um kernel booter, gerenciar memória e executar código.

Projetos como o [Cosmos OS](https://github.com/CosmosOS/Cosmos) e o [MOSA Project](https://github.com/mosa/MOSA-Project) provam que isso é possível: ambos compilam IL (Intermediate Language) diretamente para código de máquina nativo via AOT (Ahead-of-Time), eliminando a dependência de um runtime tradicional em tempo de execução. O Mandrillus OS usa o MOSA como base de compilação e parte daí para construir uma identidade própria — tanto em termos de kernel quanto, eventualmente, no ecossistema de aplicações que roda sobre ele.

## O que este projeto demonstra

Este não é um projeto de "CRUD com banco de dados". Ele existe para evidenciar competências que raramente aparecem em portfólios .NET convencionais:

- **Compreensão de baixo nível**: gerenciamento manual de memória, boot bare-metal, ausência de sistema operacional hospedeiro
- **Compilação AOT e IL**: entendimento prático de como C# é traduzido para código de máquina, não apenas JIT em tempo de execução
- **Arquitetura sem rede de segurança**: sem GC gerenciado por runtime completo, sem exceções tratadas por um framework maduro, sem BCL inteira disponível — decisões de design precisam ser feitas explicitamente
- **Toolchain não convencional**: MOSA, emulação via QEMU, geração de imagens de disco bootáveis
- **Maturidade arquitetural**: saber com precisão quais partes de um sistema são infraestrutura herdada versus trabalho de engenharia autoral, e ser capaz de explicar essa fronteira com clareza

## Referências e inspiração

| Projeto | O que o Mandrillus aproveita dele |
| --- | --- |
| [MOSA Project](https://github.com/mosa/MOSA-Project) | Toolchain de compilação AOT (IL → código nativo), templates de kernel, base do projeto atual |
| [Cosmos OS](https://github.com/CosmosOS/Cosmos) | Referência conceitual de arquitetura — prova de que um SO gerenciado em C# é viável em produção educacional |

O Mandrillus OS não é um fork de nenhum dos dois — é construído sobre o MOSA como toolchain de compilação, mas a arquitetura do kernel, as decisões de design e o roadmap de aplicações são desenvolvidos de forma independente.

### Uma nota sobre pesquisa e atribuição

Construir o Mandrillus envolve ler regularmente o código-fonte, a documentação e as discussões da comunidade do MOSA (e, quando relevante, do Cosmos) para entender o que já é fornecido versus o que precisa ser construído — veja "Arquitetura: o que é autoral vs. herdado" abaixo. Quando uma decisão de design é informada por uma fonte específica (código-fonte, uma implementação de referência, discussão pública da comunidade), ela é citada diretamente no [ROADMAP.pt-BR.md](ROADMAP.pt-BR.md), em vez de apresentada como se tivesse sido alcançada isoladamente.

## Status atual

🚧 **Fase 1 concluída** — kernel gerado a partir do template `mosakrnl`, com boot validado em **dois hypervisors diferentes**: QEMU e Hyper-V (Generation 1). O kernel MOSA BareMetal por baixo cuida de gerenciamento de memória, interrupções e detecção de dispositivos no boot (veja abaixo); o shell interativo autoral do Mandrillus, o **Drill**, está implementado e fechado (Issue #8). **A Fase 2 já está em andamento** — o timer PIT (Issue #9) está fechado, calibrado em 250Hz e exposto via `SystemTimer` e o comando `uptime` do shell. Veja o [ROADMAP.pt-BR.md](ROADMAP.pt-BR.md) para o acompanhamento completo por fase.

Validar o boot em mais de um hypervisor não é redundância — é evidência de robustez. QEMU (sem KVM) é majoritariamente um emulador por software, mais tolerante a peculiaridades de bootloader. O Hyper-V é um hypervisor nativo (tipo 1), com acesso direto às extensões de virtualização da CPU, e historicamente mais rígido sobre o que aceita bootar. O Mandrillus OS já roda em ambos, lado a lado com sistemas operacionais de produção no mesmo host Hyper-V.

## Arquitetura: o que é autoral vs. herdado

O Mandrillus OS é construído sobre o **kernel BareMetal** do MOSA, não implementado em assembly puro. Essa é uma escolha deliberada — o MOSA já resolve com solidez as questões de baixo nível do x86 (GDT, IDT, gerenciamento de memória, detecção de dispositivos), permitindo que o Mandrillus foque na camada de aplicação em vez de re-derivar décadas de trabalho de base em desenvolvimento de SO.

Isso foi confirmado cruzando a [documentação do MOSA](https://www.mosa-project.org/a-dive-into-baremetal.html) com a árvore de dependências real do `Mandrillus.Kernel.x86` (que referencia `Mosa.Kernel.BareMetal.x86.dll` e `Mosa.Kernel.BareMetal.dll`) e o próprio log de boot do kernel:

| Componente | Status |
| --- | --- |
| GDT (Global Descriptor Table) | Fornecido pelo MOSA BareMetal |
| IDT (Interrupt Descriptor Table) | Fornecido pelo MOSA BareMetal |
| Gerenciamento de memória física e virtual | Fornecido pelo MOSA BareMetal |
| Entrada de teclado | Fornecido pelo MOSA BareMetal (dispositivo `StandardKeyboard`) |
| Console / saída de texto | Fornecido pelo MOSA BareMetal (API `Console`) |
| Shell interativo | **Trabalho autoral do Mandrillus** |
| Futuro: modelo de processos, sistema de arquivos, aplicações | **Trabalho autoral do Mandrillus** |

Na prática, isso significa que o trabalho de engenharia autoral do Mandrillus começa pelo shell interativo, em vez da camada de boot/memória/entrada. O acompanhamento completo de cada item, incluindo notas de verificação, está no [ROADMAP.pt-BR.md](ROADMAP.pt-BR.md).

> **Sobre o timer PIT:** a investigação confirmou que o MOSA BareMetal não fornece nenhum driver de PIT/timer, mas seu `Scheduler` já depende silenciosamente do IRQ0 numa frequência não configurada, herdada do BIOS. Assumir controle disso é trabalho autoral real — rastreado como pré-requisito da **Fase 2** (para escalonamento preemptivo), não da Fase 1, já que o shell não depende disso. Junte-se ao canal do MOSA pelo [Discord](https://discord.gg/tRNMn3npsv) para mais informações, e cruze com a implementação de PIT do Cosmos OS como referência de design (não fonte de código — veja abaixo). Veja o [ROADMAP.pt-BR.md](ROADMAP.pt-BR.md) para o detalhamento completo.

### Referências de design (crédito onde é devido)

Algumas decisões de design do Mandrillus são informadas pela leitura do código-fonte de outros projetos, mesmo quando nenhum código é reaproveitado. Documentado aqui por transparência:

| Decisão | Informada por | Por que não foi simplesmente copiado |
|---|---|---|
| Design do timer PIT (Fase 2, Issue #9) | `Cosmos.HAL/PIT.cs` do [Cosmos OS](https://github.com/CosmosOS/Cosmos) — especificamente seu padrão de timer por software baseado em callback `PITTimer`/`OnTrigger` | Cosmos e MOSA compilam sobre abstrações de baixo nível incompatíveis (`Cosmos.Core.IOPort` vs. `IOPortReadWrite` do MOSA) — os dois frameworks não são interoperáveis nessa camada. O uso de portas de I/O e a sequência de temporização do Cosmos também foram cruzados com a implementação canônica de PIT do kernel [xv6](https://github.com/mit-pdos/xv6-public), para confirmar que segue prática padrão, não uma peculiaridade específica do Cosmos. |

## Estrutura do projeto

```code
Mandrillus/                              # Solution
├── Mandrillus.Kernel/                   # Projeto de kernel agnóstico de plataforma
│   ├── Mandrillus.Kernel.csproj
│   └── Program.cs                       # classe Program: SetBootOptions(), EntryPoint()
├── Mandrillus.Kernel.x86/               # Projeto executável, alvo x86
│   ├── Mandrillus.Kernel.x86.csproj
│   ├── Boot.cs                          # classe Boot: Main() — entry point real, específico de x86
│   └── bin/
│       └── Tools/                       # Mosa.Tool.Launcher / Launcher.Console
└── README.md
```

O template MOSA (`mosakrnl`) gera essa separação em dois projetos: o kernel agnóstico (`Mandrillus.Kernel`), contendo `Program.cs` com a configuração de boot e o `EntryPoint()` lógico, e um projeto por arquitetura-alvo (`Mandrillus.Kernel.x86`), que referencia o primeiro, adiciona os pacotes específicos da plataforma (`Mosa.Platform.x86`) e contém `Boot.cs` — o entry point executável real (`Main()`) invocado pelo bootloader, que por sua vez chama o `EntryPoint()` do kernel agnóstico.

Conforme o kernel evolui, esta seção será expandida para refletir a superfície de API interna que futuras aplicações do ecossistema vão consumir — construída sobre a camada BareMetal herdada descrita acima.

## O ecossistema Mandrillus Systems

O Mandrillus OS é o núcleo de um ecossistema maior de aplicações nativas, todas planejadas para rodar sobre este kernel:

- **Editor de texto simples** — edição de arquivos de texto puro
- **Editor de código** — syntax highlighting básico e edição para desenvolvimento dentro do próprio SO
- **Aplicação de desenho** — manipulação gráfica simples, validando a camada de vídeo do kernel
- **Pacote office nativo** — conjunto mínimo de ferramentas de produtividade (texto, planilha simples)

Cada uma dessas aplicações será tratada como um projeto separado dentro do repositório/organização Mandrillus, com sua própria documentação — mas todas dependem diretamente da estabilidade e da superfície de API exposta pelo kernel documentado aqui.

## Como rodar

### Pré-requisitos

- .NET 10 SDK
- Visual Studio 2022+ (ou 2026) recomendado para desenvolvimento no Windows; Linux: qualquer editor + `dotnet` CLI
- [QEMU](https://www.qemu.org/) instalado (ou use os binários empacotados em `Tools/QEMU`, se presentes)
- Opcional, para testar em Hyper-V: Windows com Hyper-V habilitado

### ⚠️ Nota sobre versão dos pacotes MOSA

Os pacotes NuGet do MOSA (`Mosa.Platform`, `Mosa.Platform.x86`, `Mosa.DeviceSystem`, `Mosa.Tools.Package`) estão **travados na versão `2.6.1.1669`** neste projeto, em vez de usar `Version="*"`. Isso não é acidental: builds mais recentes (`2.6.1.1694` em diante, incluindo a última publicada até o momento, `2.6.1.1724`) têm uma regressão de empacotamento que quebra a resolução do assembly `Mosa.Compiler.Platforms`, impedindo tanto o `Mosa.Tool.Launcher` (GUI) quanto o `Mosa.Tool.Launcher.Console` de funcionar (`System.IO.FileNotFoundException`).

Esse problema foi isolado via bisecção manual entre as builds publicadas no NuGet e reportado upstream como [Issue #1295 do MOSA-Project](https://github.com/mosa/MOSA-Project/issues/1295). Caso o pacote seja corrigido em uma versão futura, esta nota deve ser revisada e o pin removido.

### Setup

```powershell
dotnet new install Mosa.Templates
dotnet new mosakrnl -o Mandrillus
cd Mandrillus
dotnet build
```

Depois de gerar o projeto, ajuste os `PackageReference` nos `.csproj` para a versão travada (ver nota acima), rode `dotnet restore` e `dotnet build` novamente.

### Executar no QEMU

Dentro da pasta `bin` do projeto x86, rode o Launcher (GUI ou Console) apontando para a DLL compilada:

```powershell
cd Mandrillus.Kernel.x86\bin
Tools\Mosa.Tool.Launcher.exe Mandrillus.Kernel.x86.dll
# ou, sem interface gráfica:
Tools\Mosa.Tool.Launcher.Console.exe Mandrillus.Kernel.x86.dll
```

O Launcher compila a DLL para código nativo x86, gera a imagem de disco bootável e invoca o QEMU automaticamente com os argumentos corretos.

### Executar no Hyper-V (Generation 1)

O Mandrillus OS também foi validado rodando em Hyper-V, ao lado de outros sistemas operacionais no mesmo host. Passos:

1. No Launcher, mude **Image Format** de `IMG (.img)` para `Microsoft (.vhd)` antes de compilar.
   > ⚠️ Na versão atual do MOSA (`2.6.1.1669`), essa opção não gera de fato um `.vhd` — o pipeline continua produzindo apenas `.bin`/`.img`, independente da seleção na UI. É necessário converter manualmente (próximo passo).
2. Converta a imagem `.img` gerada para `.vhd` usando `qemu-img` (já disponível junto do QEMU):

   ```powershell
   qemu-img convert -f raw -O vpc Mandrillus.Kernel.x86.img Mandrillus.Kernel.x86.vhd
   ```

   Alternativamente, ferramentas como o [StarWind V2V Converter](https://www.starwindsoftware.com/starwind-v2v-converter) também fazem essa conversão.
3. No **Hyper-V Manager**, crie uma nova VM como **Generation 1**, com Secure Boot desabilitado, e anexe o `.vhd` gerado como disco de boot.
4. Inicie a VM — o kernel deve bootar e exibir a saída de debug/console, da mesma forma que no QEMU.

## Contribuindo / Fluxo de Git

Este projeto segue um **fluxo trunk-based simplificado** (GitHub Flow), não o Git Flow completo — adequado para um projeto mantido solo e ainda em estágio inicial, com ritmo acelerado de mudanças.

- **`master`** está sempre estável e bootável. Todo commit aqui deve, no mínimo, compilar e bootar sem travar no QEMU.
- **`feature/<número-da-issue>-<nome-curto>`** — uma branch por funcionalidade/issue, a partir de `master` (ex: `feature/8-interactive-shell`). O número da issue no nome da branch permite que o GitHub vincule automaticamente o PR à issue correspondente.
- **`fix/<nome-curto>`** — para correções de bugs que não estão ligadas a uma funcionalidade planejada.

Fluxo: abrir a issue → criar branch a partir de `master` → commits incrementais → abrir um Pull Request de volta para `master` (mesmo trabalhando sozinho, PRs documentam *por que* uma decisão foi tomada e servem como um gate natural de CI assim que o xUnit estiver em uso) → **squash merge** → deletar a branch.

O squash merge mantém o histórico de `master` legível como uma linha do tempo limpa, um commit por funcionalidade, independentemente de quantos commits intermediários aconteceram na branch.

## Licença

O código deste repositório é licenciado sob a [MIT License](./LICENSE).

O Mandrillus OS depende do [MOSA Project](https://github.com/mosa/MOSA-Project) (New BSD License) como toolchain de compilação. Os termos dessa licença, incluindo o aviso de copyright exigido para redistribuição, estão reproduzidos em [THIRD-PARTY-LICENSES.md](./THIRD-PARTY-LICENSES.md).

---

*Mandrillus Systems — evolução, um bit de cada vez.*
