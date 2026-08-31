# Material de consulta e referência - Mandrillus OS

Mapa de referência permanente do projeto. Inclui a tabela-resumo de linhagens de toolchain — a parte que mais importa na prática, porque é o que determina se um projeto específico vale a pena investigar a fundo quando houver uma dúvida concreta (ex.: "como o Abanu lida com scheduling" é mais transferível do que "como o AuraOS lida com scheduling", já que Abanu compartilha o compilador MOSA).

## 1. Referências primárias

Use estas como fonte primária de orientação, mantendo liberdade para incorporar materiais adicionais conforme necessário ao longo do desenvolvimento.

### **OSDev.org**

- Wiki — <https://wiki.osdev.org/>
- C# — <https://wiki.osdev.org/C_Sharp>
- C# Bare Bones — <https://wiki.osdev.org/C_Sharp_Bare_Bones>
- Creating an Operating System — <https://wiki.osdev.org/Creating_an_Operating_System>

### **MOSA Project**

- Documentação — <https://www.mosa-project.org/>
- Repositório — <https://github.com/mosa/MOSA-Project>

### **Cosmos OS**

- Documentação — <https://www.gocosmos.org/>
- Repositório — <https://github.com/CosmosOS/Cosmos>

### **Toolchains e compiladores históricos**

- ZeroSharp — <https://github.com/MichalStrehovsky/zerosharp>
- SharpOS — <https://github.com/sharpos/SharpOS>
- bflat — <https://github.com/bflattened/bflat/>

### **Microsoft Research — Singularity e Midori**

- Página do projeto Singularity — <https://www.microsoft.com/en-us/research/project/singularity/>
- Artigos de pesquisa do Singularity — <https://www.microsoft.com/en-us/research/project/singularity/publications/>
- Blog do Midori OS (Joe Duffy) — <https://joeduffyblog.com/2015/11/03/blogging-about-midori/> — relato em primeira pessoa sobre o Midori, sucessor interno do Singularity na Microsoft; não é open source, relato apenas em blog

### **Referências de shell/scripting**

- `dotnet-shell` — <https://dotnet-shell.github.io/> — shell compatível com scripts C# para .NET; relevante para o design do Drill como referência de sintaxe de shell "sabor C#" e convenções de REPL, independente das restrições bare-metal do Mandrillus

### **Recursos de aprendizado**

- Write Your Own Operating System — <https://wyoos.org/> — conceitos gerais de desenvolvimento de SO, desafios e armadilhas comuns, complementado pelo canal no YouTube — <https://www.youtube.com/@writeyourownoperatingsystem>

---

## 2. Projetos comparáveis de SO em C# — ranking

Critérios de ranqueamento: relevância arquitetural para o Mandrillus (família
de toolchain, AOT/bare-metal vs. baseado em framework), maturidade/atividade,
e valor como fonte de padrões de design reutilizáveis. Agrupados primeiro por
linhagem de toolchain, já que essa variável determina comparabilidade mais do
que popularidade bruta.

### Tier S — Referências arquiteturais diretas (mesma família AOT bare-metal)

| # | Projeto | Repo | Toolchain | Status | Por que importa |
| --- | --- | --- | --- | --- | --- |
| 1 | **Cosmos** | github.com/CosmosOS/Cosmos | IL2CPU (compilador AOT próprio) + X# | Ativo | Maior comunidade; historicamente o projeto de SO em C# mais influente |
| 2 | **MOSA Project** | github.com/mosa/MOSA-Project | Mosa-Compiler (AOT próprio) | Ativo | Toolchain do próprio Mandrillus |
| 3 | **Abanu** | github.com/abanu-org/abanu | **Mosa-Compiler** (mesmo do MOSA) | Semi-ativo | Parente arquitetural mais próximo — mesma base de compilador, já possui proteção de memória, task-switching, modo usuário, console-server, IPC básico |
| 4 | **SharpOS** | github.com/sharpos/SharpOS | Compilador AOT próprio | Descontinuado (recursos movidos ao MOSA) | Ancestral direto do MOSA; membro fundador da Managed Operating System Alliance |
| 5 | **tysos** | github.com/jncronin/tysos | tysila (compilador AOT próprio) | Inativo desde ~2011 | Microkernel preemptivo de 64 bits, espaço de endereçamento único para todos os processos; pioneiro clássico junto com SharpOS/Cosmos/MOSA |

### Tier A — Toolchains modernas alternativas (bflat / NativeAOT / CoreRT, sem framework)

| # | Projeto | Repo | Toolchain | Status | Por que importa |
| --- | --- | --- | --- | --- | --- |
| 6 | **ProtonOS** | github.com/ProtonOS/ProtonOS | bflat `--stdlib:zero` | Ativo, ambicioso | JIT Tier 0 customizado, GC compactador, tratamento completo de exceções, scheduling SMP + reconhecimento NUMA, UEFI x86-64 |
| 7 | **PatienceOS** | github.com/FrankRay78/PatienceOS | Compiladores IL/AOT da Microsoft + toolchain GNU, runtime zerosharp opcional | Ativo, projeto pessoal de 12 meses | Mais próximo em escala/espírito ao Mandrillus entre os do Tier A |
| 8 | **RoseOS** | github.com/Michael-K-GH/RoseOS | CoreRT (antecessor do NativeAOT) | WIP inicial | Padrão de loader UEFI + kernel separados, CoreRT puro |
| 9 | **ZeroSharp** | github.com/MichalStrehovsky/zerosharp | Demos NativeAOT / bflat | Amostras de referência, não um SO completo | Amostra `efi-no-runtime` é o exemplo mais claro de "C# com zero framework" disponível; criado pelo próprio autor do bflat/NativeAOT |
| 10 | **terminal-cs/Kernel** | github.com/terminal-cs/Kernel | NativeAOT (sem bflat) | Muito inicial, pré-boot | Mostra o caminho "NativeAOT puro, sem toolchain de terceiros" — útil como ponto de contraste às escolhas de design do MOSA |

### Tier B — Ecossistema Cosmos (maduros o suficiente para ensinar padrões)

| # | Projeto | Repo | Status | Por que importa |
| --- | --- | --- | --- | --- |
| 11 | **AuraOS** | github.com/aura-systems/Aura-Operating-System | Pausado (aguardando merge do nativeaot-patcher no Cosmos) | SO baseado em Cosmos mais avançado atualmente: ATA IDE/AHCI, FAT32/16/12 + VFS, scan PCI, PS/2, shutdown ACPI, interpretador de comandos — comparação mais próxima de para onde o Drill deve crescer |
| 12 | **FlingOS** | github.com/FlingOS/FlingOS | Inativo mas bem documentado | Compilador AOT próprio (nem Cosmos, nem MOSA); explicitamente educacional, abordagem em três partes (código + artigos + vídeo-tutoriais) |
| 13 | **AtomOS** | github.com/amaneureka/AtomOS | Inativo | Kernel monolítico multitarefa x86, toolchain própria, foco em drivers gerenciados de alto nível |
| 14 | **RedPandaOS** | github.com/giawa/RedPandaOS | Experimental/ativo | Totalmente do zero: loader de PE próprio, interpretador de IL próprio, conversor IL-para-assembly próprio — nenhum toolchain de terceiros |
| 15 | **SphereOS** | github.com/LumaTechnologies/SphereOS | Descontinuado | Preservado como está; útil só como referência histórica de padrões |
| 16 | **XenOS** | github.com/AAM1075/XenOS (espelho: MEMESCOEP/XenOS) | Alpha | Padrão simples de bootstrap Cosmos + VMware, I/O de arquivo FAT32 |

### Tier C — Nicho / drivers / curiosidades históricas

| # | Projeto | Repo | Relevância |
| --- | --- | --- | --- |
| 17 | **WDK.NET / KernelSharp / ZeroKernel** | github.com/ZeroLP/WDK.NET, VollRagm/KernelSharp, ZeroLP/ZeroKernel | *Drivers* de modo kernel do Windows em C# via NativeAOT — não é um SO completo, mas mostra técnicas de remoção de runtime em outro contexto de código privilegiado |
| 18 | **Singularity RDK** (Microsoft Research) | página do projeto + artigos (ver Seção 1) | Descontinuado (projeto de pesquisa, ~2003-2010) o projeto de pesquisa de SO gerenciado que mais influenciou toda essa categoria; Midori (sucessor) foi a inspiração citada na fundação do MOSA |
| 19 | **Forks universitários/de curso do Cosmos** | vários, buscar "university OS course C# Cosmos" | Vale uma busca dedicada futura se quiser comparar como programas acadêmicos estruturam projetos semelhantes |
| 20 | **Pequenos projetos do topic `cosmos-os`** | Aqua, Sail OS, Nexon Kernel, Prism OS, e dezenas de outros em github.com/topics/cosmos-os | Majoritariamente projetos pequenos de aprendizado individual; como *conjunto* são um bom termômetro de erros comuns de iniciante, não valem investigação individual profunda |

---

## 3. Resumo de linhagens de toolchain

Isso importa mais do que o ranking individual: os 20 projetos acima se
dividem em três famílias de toolchain, e a comparabilidade com o Mandrillus
depende de a qual família um dado projeto pertence.

| Linhagem | Compilador | Projetos | Comparabilidade com Mandrillus |
| --- | --- | --- | --- |
| **MOSA** | Mosa-Compiler (próprio) | MOSA, Abanu, SharpOS (ancestral) | 🟢 Máxima — mesmo compilador/filosofia |
| **Cosmos/IL2CPU** | IL2CPU + X# | Cosmos, AuraOS, SphereOS, XenOS, dezenas de forks | 🟡 Média — CIL→nativo mas runtime/abstrações diferentes |
| **NativeAOT/bflat/CoreRT** | ILC (Microsoft) ou bflat | ProtonOS, PatienceOS, RoseOS, ZeroSharp, terminal-cs/Kernel | 🟠 Baixa-média — filosofia "zero framework" mais radical, UEFI em vez de BIOS |

*Nota: FlingOS, AtomOS e RedPandaOS usam toolchains próprias independentes
(nem Cosmos, nem MOSA, nem bflat) e não se encaixam perfeitamente nesta
tabela — trate-os como pontos de dados isolados.*
